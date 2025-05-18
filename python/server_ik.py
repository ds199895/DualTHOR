import pinocchio
from pinocchio.robot_wrapper import RobotWrapper
import numpy as np
from numpy.linalg import norm, solve
from copy import deepcopy
from flask import Flask, request, jsonify
import os

app = Flask(__name__)


# get current script directory
script_dir = os.path.dirname(os.path.abspath(__file__))
urdf_filename = os.path.join(script_dir, "../unity/Assets/UR5/ur_description/urdf/ur5_robot.urdf")
urdf_filename = os.path.normpath(urdf_filename)  # normalize path

print(f"URDF file path: {urdf_filename}")

# load URDF model
model = pinocchio.buildModelFromUrdf(urdf_filename)
print("model name: " + model.name)

# set and output UR5 initial offset q0
q0 = pinocchio.neutral(model)  # get model neutral pose (usually zero configuration)
print("UR5 initial offset q0:", q0.T)  # transpose for display
data = model.createData()  # create data structure for calculation

# initial relative position and orientation of each joint
print("Initial relative position and orientation of each joint:")

for joint_id in range(1, model.njoints):  # start from 1, because 0 is fixed base
    joint_name = model.names[joint_id]
    joint_relative_pose = model.jointPlacements[joint_id]  # initial pose of each joint relative to its parent
    relative_translation = joint_relative_pose.translation
    relative_rotation = joint_relative_pose.rotation

    print(f"{joint_name} relative position (translation): {relative_translation.T}")
    print(f"{joint_name} relative orientation (rotation):\n{relative_rotation}\n")

# calculate and output end-effector pose
pinocchio.forwardKinematics(model, data, q0)
end_effector_id = model.getJointId("wrist_3_joint")  # assume end-effector joint is "wrist_3_joint", please adjust according to actual situation
end_effector_pose = data.oMi[end_effector_id]
end_effector_translation = end_effector_pose.translation
end_effector_rotation = end_effector_pose.rotation

print("End-effector initial position (translation):", end_effector_translation.T)
print("End-effector initial orientation (rotation):\n", end_effector_rotation)

# create data for algorithm
data = model.createData()

def get_ik(model, data, JOINT_ID, oMdes, initial_q, eps=1e-4, IT_MAX=5000, DT=1e-1, damp=1e-12):
    q = deepcopy(initial_q).astype(np.float64)
    i = 0
    while True:
        pinocchio.forwardKinematics(model, data, q)
        iMd = data.oMi[JOINT_ID].actInv(oMdes)
        err = pinocchio.log(iMd).vector  # in joint frame
        if norm(err) < eps:
            success = True
            break
        if i >= IT_MAX:
            success = False
            break
        J = pinocchio.computeJointJacobian(model, data, q, JOINT_ID)  # in joint frame
        J = -np.dot(pinocchio.Jlog6(iMd.inverse()), J)
        v = -J.T.dot(solve(J.dot(J.T) + damp * np.eye(6), err))
        q = pinocchio.integrate(model, q, v * DT)
        # if not i % 10:
        #     print("%d: error = %s" % (i, err.T))
        i += 1

    # FK verification
    if success:
        fk_data = model.createData()
        pinocchio.forwardKinematics(model, fk_data, q)
        final_oMi = fk_data.oMi[JOINT_ID]
        
        # get FK pose result (translation and rotation parts)
        fk_translation = final_oMi.translation
        fk_rotation = final_oMi.rotation
        
        # calculate FK result error with target oMdes
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
        fk_err = None  # when IK fails, there is no FK error

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
    
    # print final data sent to client
    print("Sending response to client:")
    print(response)
    
    return jsonify(response)

def start_server_ik():
    """start IK server"""
    print("Starting IK server...")
    app.run(host='0.0.0.0', port=5000)

if __name__ == '__main__':
    start_server_ik()