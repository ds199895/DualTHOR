import pinocchio
from pinocchio.robot_wrapper import RobotWrapper
import numpy as np
from numpy.linalg import norm, solve
from copy import deepcopy
from flask import Flask, request, jsonify
import os

app = Flask(__name__)


# 获取当前脚本目录
script_dir = os.path.dirname(os.path.abspath(__file__))
urdf_filename = os.path.join(script_dir, "../unity/Assets/UR5/ur_description/urdf/ur5_robot.urdf")
urdf_filename = os.path.normpath(urdf_filename)  # 标准化路径

print(f"URDF file path: {urdf_filename}")

# 加载URDF模型
model = pinocchio.buildModelFromUrdf(urdf_filename)
print("model name: " + model.name)

# 设置并输出 UR5 的初始偏移量 q0
q0 = pinocchio.neutral(model)  # 获取模型的中性姿态（通常为零配置）
print("UR5 initial offset q0:", q0.T)  # 转置以方便显示
data = model.createData()  # 创建用于计算的数据结构

# 每个关节的相对坐标（相对于父关节的初始位姿）
print("Initial relative position and orientation of each joint:")

for joint_id in range(1, model.njoints):  # 从1开始，因为0号是固定基座
    joint_name = model.names[joint_id]
    joint_relative_pose = model.jointPlacements[joint_id]  # 每个关节相对父关节的初始位姿
    relative_translation = joint_relative_pose.translation
    relative_rotation = joint_relative_pose.rotation

    print(f"{joint_name} relative position (translation): {relative_translation.T}")
    print(f"{joint_name} relative orientation (rotation):\n{relative_rotation}\n")

# 计算并输出末端的位姿
pinocchio.forwardKinematics(model, data, q0)
end_effector_id = model.getJointId("wrist_3_joint")  # 假设末端关节为“wrist_3_joint”，请根据实际情况调整
end_effector_pose = data.oMi[end_effector_id]
end_effector_translation = end_effector_pose.translation
end_effector_rotation = end_effector_pose.rotation

print("End-effector initial position (translation):", end_effector_translation.T)
print("End-effector initial orientation (rotation):\n", end_effector_rotation)

# 创建算法所需的数据
data = model.createData()

def get_ik(model, data, JOINT_ID, oMdes, initial_q, eps=1e-4, IT_MAX=5000, DT=1e-1, damp=1e-12):
    q = deepcopy(initial_q).astype(np.float64)
    i = 0
    while True:
        pinocchio.forwardKinematics(model, data, q)
        iMd = data.oMi[JOINT_ID].actInv(oMdes)
        err = pinocchio.log(iMd).vector  # 在关节框架中
        if norm(err) < eps:
            success = True
            break
        if i >= IT_MAX:
            success = False
            break
        J = pinocchio.computeJointJacobian(model, data, q, JOINT_ID)  # 在关节框架中
        J = -np.dot(pinocchio.Jlog6(iMd.inverse()), J)
        v = -J.T.dot(solve(J.dot(J.T) + damp * np.eye(6), err))
        q = pinocchio.integrate(model, q, v * DT)
        # if not i % 10:
        #     print("%d: error = %s" % (i, err.T))
        i += 1

    # FK 验证
    if success:
        fk_data = model.createData()
        pinocchio.forwardKinematics(model, fk_data, q)
        final_oMi = fk_data.oMi[JOINT_ID]
        
        # 获取 FK 的位姿结果（平移和旋转部分）
        fk_translation = final_oMi.translation
        fk_rotation = final_oMi.rotation
        
        # 计算 FK 结果与目标 oMdes 的误差
        fk_err = pinocchio.log(final_oMi.actInv(oMdes)).vector
        print("FK verification result:")
        print("FK translation:", fk_translation.T)
        print("FK rotation:\n", fk_rotation)
        # print("FK verification error:", fk_err.T)

        if norm(fk_err) < eps:
            print("FK verification successful: Solution is correct.")
        else:
            print("FK verification failed: Solution is not accurate enough.")
    else:
        fk_err = None  # IK 失败时，没有 FK 误差

    return success, q, err


@app.route('/ik', methods=['POST'])
def ik_service():
    data = request.json
    JOINT_ID = data['joint_id']
    oMdes = pinocchio.SE3(np.array(data['rotation']), np.array(data['translation']))
    initial_q = np.array(data['initial_q'])
    
    success, q, err = get_ik(model, model.createData(), JOINT_ID, oMdes, initial_q)
    
    response = {
        'success': success,
        'q': q.tolist(),
        'err': err.tolist()
    }
    
    # 打印最终发送给客户端的数据
    print("Sending response to client:")
    print(response)
    
    return jsonify(response)

def start_server_ik():
    """启动 IK 服务"""
    print("Starting IK server...")
    app.run(host='0.0.0.0', port=5000)

if __name__ == '__main__':
    start_server_ik()
