using UnityEngine;
using UnityEngine.Networking;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using Newtonsoft.Json;

public class IKController : MonoBehaviour
{
    [System.Serializable]
    public class IKRequest
    {
        public float[][] left_pose;    // 4x4 matrix
        public float[][] right_pose;   // 4x4 matrix
        public float[] motorstate;     // current joint angles
        public float[] motorV;         // current joint velocities
    }

    [System.Serializable]
    public class IKResponse
    {
        public bool success;
        public float[] q;      // calculated joint angles
        public float[] tau;    // calculated joint torques
    }

    public ArticulationBody[] joints; // robot joints
    private string serverUrl = "http://localhost:5000/ik";

    // left and right target poses
    public Transform leftTargetPose;
    public Transform rightTargetPose;

    public Transform baseTransform;

    // interpolation related parameters
    private float[] currentJointAngles;
    private float[] targetJointAngles;
    private float interpolationTime = 0.5f; // interpolation time (seconds)
    private float currentInterpolationTime = 0f;
    private bool isInterpolating = false;

    // keyboard control
    public KeyCode triggerKey = KeyCode.Space; // trigger IK key

    void Start()
    {
        // ensure all joints are assigned
        if (joints == null || joints.Length == 0)
        {
            Debug.LogError("Please set the robot joints!");
            enabled = false;
            return;
        }

        // initialize current joint angles
        currentJointAngles = joints.Select(j => j.jointPosition[0] * Mathf.Rad2Deg).ToArray();
        targetJointAngles = currentJointAngles.ToArray();
    }

    void Update()
    {
        // detect keyboard input
        if (Input.GetKeyDown(triggerKey))
        {
            Debug.Log("开始IK计算");
            // convert target position to base coordinate system
            float[][] left_target_matrix = ConvertTargetToBaseMatrix(leftTargetPose, baseTransform);
            float[][] right_target_matrix = ConvertTargetToBaseMatrix(rightTargetPose, baseTransform);

            // build request data
            var request = new IKRequest
            {
                left_pose = left_target_matrix,
                right_pose = right_target_matrix,
                motorstate = joints.Select(j => j.jointPosition[0]).ToArray(),
                motorV = joints.Select(j => j.jointVelocity[0]).ToArray()
            };
            StartCoroutine(SendIKRequest(request));
        }

        // execute interpolation
        if (isInterpolating)
        {
            UpdateJointInterpolation();
        }
    }


    // convert target position to base coordinate system
    Vector3 ConvertToBaseCoordinates(Vector3 targetPosition, Transform baseTransform)
    {
        Vector3 relativePosition = targetPosition - baseTransform.position;
        Vector3 result = Quaternion.Inverse(baseTransform.rotation) * relativePosition;
        //Debug.Log("relative position to the base: " + result);
        return result;
    }
    
    private void UpdateJointInterpolation()
    {
        if (currentInterpolationTime >= interpolationTime)
        {
            isInterpolating = false;
            return;
        }

        currentInterpolationTime += Time.deltaTime;
        float t = currentInterpolationTime / interpolationTime;
        t = Mathf.SmoothStep(0, 1, t); // use smooth interpolation

        // update each joint angle
        for (int i = 0; i < joints.Length; i++)
        {
            float interpolatedAngle = Mathf.LerpAngle(currentJointAngles[i], targetJointAngles[i], t);
            var drive = joints[i].xDrive;
            drive.target = interpolatedAngle;
            joints[i].xDrive = drive;
        }
    }

    IEnumerator SendIKRequest(IKRequest request)
    {
        string jsonData = JsonConvert.SerializeObject(request);

        using (UnityWebRequest www = new UnityWebRequest(serverUrl, "POST"))
        {
            byte[] jsonToSend = new System.Text.UTF8Encoding().GetBytes(jsonData);
            www.uploadHandler = new UploadHandlerRaw(jsonToSend);
            www.downloadHandler = new DownloadHandlerBuffer();
            www.SetRequestHeader("Content-Type", "application/json");

            yield return www.SendWebRequest();

            if (www.result == UnityWebRequest.Result.Success)
            {
                var response = JsonConvert.DeserializeObject<IKResponse>(www.downloadHandler.text);
                
                if (response.success)
                {
                    // save current joint angles as interpolation start point
                    currentJointAngles = joints.Select(j => j.jointPosition[0] * Mathf.Rad2Deg).ToArray();
                    
                    // set target joint angles
                    targetJointAngles = response.q.Select(angle => angle * Mathf.Rad2Deg).ToArray();
                    
                    // reset interpolation parameters
                    currentInterpolationTime = 0f;
                    isInterpolating = true;

                    Debug.Log("start interpolation to the new target position");
                }
                else
                {
                    Debug.LogWarning("IK solution failed");
                }
            }
            else
            {
                Debug.LogError($"request failed: {www.error}. URL: {serverUrl}");
            }
        }
    }

    private float[][] TransformToMatrix(Vector3 position, Quaternion rotation)
    {
        Matrix4x4 matrix = Matrix4x4.TRS(position, rotation, Vector3.one);
        return new float[][]
        {
            new float[] { matrix.m00, matrix.m01, matrix.m02, matrix.m03 },
            new float[] { matrix.m10, matrix.m11, matrix.m12, matrix.m13 },
            new float[] { matrix.m20, matrix.m21, matrix.m22, matrix.m23 },
            new float[] { matrix.m30, matrix.m31, matrix.m32, matrix.m33 }
        };
    }

    private float[][] ConvertTargetToBaseMatrix(Transform targetPose, Transform baseTransform)
    {
        // convert target position to base coordinate system
        Vector3 relativePosition = targetPose.position - baseTransform.position;
        Vector3 basePosition = Quaternion.Inverse(baseTransform.rotation) * relativePosition;

        // use the way in IKClient to build translation
        List<float> translation = new List<float>
        {
            basePosition.z,
            -basePosition.x,
            basePosition.y
        };

        // convert target rotation to base coordinate system
        Quaternion baseRotation = Quaternion.Inverse(baseTransform.rotation) * targetPose.rotation;

        // convert Quaternion to Matrix4x4
        Matrix4x4 rotationMatrix = Matrix4x4.Rotate(baseRotation);

        // generate 4x4 transformation matrix
        return new float[][]
        {
            new float[] { rotationMatrix.m00, rotationMatrix.m01, rotationMatrix.m02, translation[0] },
            new float[] { rotationMatrix.m10, rotationMatrix.m11, rotationMatrix.m12, translation[1] },
            new float[] { rotationMatrix.m20, rotationMatrix.m21, rotationMatrix.m22, translation[2] },
            new float[] { 0, 0, 0, 1 }
        };
    }

    // auxiliary method: normalize angle to [-180, 180] range
    private float NormalizeAngle(float angle)
    {
        while (angle > 180f) angle -= 360f;
        while (angle < -180f) angle += 360f;
        return angle;
    }

    public void ProcessTargetPosition(Vector3 newTargetPosition, bool isLeftArm)
    {
        // select target pose and base transform
        Transform targetPose = isLeftArm ? leftTargetPose : rightTargetPose;
        Transform baseTransform = this.baseTransform;

        // convert target position to base coordinate system
        Vector3 targetPositionRelative = ConvertToBaseCoordinates(newTargetPosition, baseTransform);

        // build target pose matrix
        float[][] targetMatrix = TransformToMatrix(targetPositionRelative, targetPose.rotation);

        // build request data
        var request = new IKRequest
        {
            left_pose = isLeftArm ? targetMatrix : null,
            right_pose = isLeftArm ? null : targetMatrix,
            motorstate = joints.Select(j => j.jointPosition[0]).ToArray(),
            motorV = joints.Select(j => j.jointVelocity[0]).ToArray()
        };

        // send IK request
        StartCoroutine(SendIKRequest(request));
    }
} 