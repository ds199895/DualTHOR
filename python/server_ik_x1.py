import pinocchio as pin
from pinocchio.robot_wrapper import RobotWrapper
import numpy as np
import casadi
from pinocchio import casadi as cpin
from numpy.linalg import norm, solve
from copy import deepcopy
from flask import Flask, request, jsonify
import os

app = Flask(__name__)

class X1_IK_Solver:
    def __init__(self):
        np.set_printoptions(precision=5, suppress=True, linewidth=200)
        
        # 加载机器人模型
        self.robot = pin.RobotWrapper.BuildFromURDF('../assets/x1/urdf/x1.urdf', '../assets/x1/meshes/')
        print(self.robot.model)
        
        # 获取所有关节名称以供参考
        print("所有关节名称：")
        for i, name in enumerate(self.robot.model.names):
            print(f"{name}")

        self.mixed_jointsToLockIDs = [   
            "left_hip_pitch_joint",
            "left_hip_roll_joint",
            "left_hip_yaw_joint",
            "left_knee_pitch_joint",
            "left_ankle_pitch_joint",
            "left_ankle_roll_joint",

           
            "left_hand_joint",
            "hand_left_01_joint",
            "hand_left_02_joint",
            
          
            "right_hand_joint",
            "hand_right_01_joint",
            "hand_right_02_joint",

            "right_hip_pitch_joint",
            "right_hip_roll_joint",
            "right_hip_yaw_joint",
            "right_knee_pitch_joint",
            "right_ankle_pitch_joint",
            "right_ankle_roll_joint",
        ]

            # 构建简化机器人模型
        self.reduced_robot = self.robot.buildReducedRobot(
            list_of_joints_to_lock=self.mixed_jointsToLockIDs,
            reference_configuration=np.array([0.0] * self.robot.model.nq),
        )


        # 添加末端执行器帧
        self.reduced_robot.model.addFrame(
            pin.Frame('L_ee',
                     self.reduced_robot.model.getJointId('left_wrist_pitch_joint'),
                     pin.SE3(np.eye(3), np.array([0.2605 + 0.05,0,0]).T),
                     pin.FrameType.OP_FRAME)
        )

        self.reduced_robot.model.addFrame(
            pin.Frame('R_ee',
                     self.reduced_robot.model.getJointId('right_wrist_pitch_joint'),
                     pin.SE3(np.eye(3), np.array([0.2605 + 0.05,0,0]).T),
                     pin.FrameType.OP_FRAME)
        )


        # 初始化数据
        self.init_data = np.zeros(self.reduced_robot.model.nq)
        # 创建 Casadi 模型
        self.cmodel = cpin.Model(self.reduced_robot.model)
        self.cdata = self.cmodel.createData()
        
        # 创建符号变量
        self.cq = casadi.SX.sym("q", self.reduced_robot.model.nq, 1)
        self.cTf_l = casadi.SX.sym("tf_l", 4, 4)
        self.cTf_r = casadi.SX.sym("tf_r", 4, 4)
        cpin.framesForwardKinematics(self.cmodel, self.cdata, self.cq)

        # 获取左右手臂末端执行器帧ID
        self.L_hand_id = self.reduced_robot.model.getFrameId("L_ee")
        self.R_hand_id = self.reduced_robot.model.getFrameId("R_ee")
        
        print(f"左手臂末端ID: {self.L_hand_id}, 名称: {self.reduced_robot.model.frames[self.L_hand_id].name}")
        print(f"右手臂末端ID: {self.R_hand_id}, 名称: {self.reduced_robot.model.frames[self.R_hand_id].name}")
        
        # 定义误差函数
        self.error = casadi.Function(
            "error",
            [self.cq, self.cTf_l, self.cTf_r],
            [
                casadi.vertcat(
                    cpin.log6(
                        self.cdata.oMf[self.L_hand_id].inverse() * cpin.SE3(self.cTf_l)
                    ).vector[:3],
                    cpin.log6(
                        self.cdata.oMf[self.R_hand_id].inverse() * cpin.SE3(self.cTf_r)
                    ).vector[:3]
                )
            ],
        )
        
        # 创建优化问题
        self.opti = casadi.Opti()
        self.var_q = self.opti.variable(self.reduced_robot.model.nq)
        self.param_tf_l = self.opti.parameter(4, 4)
        self.param_tf_r = self.opti.parameter(4, 4)
        self.totalcost = casadi.sumsqr(self.error(self.var_q, self.param_tf_l, self.param_tf_r))
        self.regularization = casadi.sumsqr(self.var_q)
        
        # 设置优化约束和目标
        self.opti.subject_to(self.opti.bounded(
            self.reduced_robot.model.lowerPositionLimit,
            self.var_q,
            self.reduced_robot.model.upperPositionLimit)
        )
        self.opti.minimize(10 * self.totalcost + 0.001 * self.regularization)
        
        # 配置求解器选项
        opts = {
            'ipopt':{
                'print_level':0,
                'max_iter':50,
                'tol':1e-4
            },
            'print_time':False
        }
        self.opti.solver("ipopt", opts)
    
    def solve_ik(self, left_pose, right_pose, motorstate=None, motorV=None):
        """求解IK问题"""
        if motorstate is not None:
            self.init_data = motorstate
        self.opti.set_initial(self.var_q, self.init_data)
        
        self.opti.set_value(self.param_tf_l, left_pose)
        self.opti.set_value(self.param_tf_r, right_pose)
        
        try:
            sol = self.opti.solve_limited()
            sol_q = self.opti.value(self.var_q)
            self.init_data = sol_q
            
            if motorV is not None:
                v = motorV * 0.0
            else:
                v = (sol_q-self.init_data) * 0.0
                
            tau_ff = pin.rnea(self.reduced_robot.model, self.reduced_robot.data, 
                            sol_q, v, np.zeros(self.reduced_robot.model.nv))
            
            # 打印关节角度以便调试
            print("IK解算结果 - 关节角度:")
            for i, q in enumerate(sol_q):
                print(f"关节 {i}: {q}")
                
            return True, sol_q, tau_ff
            
        except Exception as e:
            print(f"IK求解失败: {e}")
            return False, None, None

# 创建IK求解器实例
ik_solver = X1_IK_Solver()

@app.route('/ik', methods=['POST'])
def ik_service():
    """IK服务的HTTP端点"""
    try:
        data = request.json
        print("data: ", data)
        left_pose = np.array(data['left_pose'])
        right_pose = np.array(data['right_pose'])
        motorstate = np.array(data.get('motorstate', None))
        motorV = np.array(data.get('motorV', None))

        print("left_pose: ", left_pose)
        print("right_pose: ", right_pose)
        print("motorstate: ", motorstate)
        success, q, tau = ik_solver.solve_ik(left_pose, right_pose, motorstate, motorV)

        response = {
            'success': success,
            'q': q.tolist() if q is not None else None,
            'tau': tau.tolist() if tau is not None else None
        }

        print("发送响应到客户端:")
        print(response)

        return jsonify(response)

    except Exception as e:
        print(f"处理请求时发生错误: {str(e)}")
        return jsonify({
            'success': False,
            'error': str(e)
        }), 400

def start_server_ik():
    """启动IK服务器"""
    print("启动IK服务器...")
    app.run(host='0.0.0.0', port=5000)

if __name__ == '__main__':
    start_server_ik()