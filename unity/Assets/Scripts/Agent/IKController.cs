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
        public float[][] left_pose;    // 4x4 矩阵
        public float[][] right_pose;   // 4x4 矩阵
        public float[] motorstate;     // 当前关节角度
        public float[] motorV;         // 当前关节角速度
    }

    [System.Serializable]
    public class IKResponse
    {
        public bool success;
        public float[] q;      // 计算得到的关节角度
        public float[] tau;    // 计算得到的关节力矩
    }

    public ArticulationBody[] joints; // 机器人的关节
    private string serverUrl = "http://localhost:5000/ik";

    // 左右手目标位姿
    public Transform leftTargetPose;
    public Transform rightTargetPose;

    // 插值相关参数
    private float[] currentJointAngles;
    private float[] targetJointAngles;
    private float interpolationTime = 0.5f; // 插值时间（秒）
    private float currentInterpolationTime = 0f;
    private bool isInterpolating = false;

    // 键盘控制
    public KeyCode triggerKey = KeyCode.Space; // 触发IK的按键

    void Start()
    {
        // 确保所有关节都已赋值
        if (joints == null || joints.Length == 0)
        {
            Debug.LogError("请设置机器人关节！");
            enabled = false;
            return;
        }

        // 初始化当前关节角度
        currentJointAngles = joints.Select(j => j.jointPosition[0] * Mathf.Rad2Deg).ToArray();
        targetJointAngles = currentJointAngles.ToArray();
    }

    void Update()
    {
        // 检测键盘输入
        if (Input.GetKeyDown(triggerKey))
        {
            StartCoroutine(SendIKRequest());
        }

        // 执行插值
        if (isInterpolating)
        {
            UpdateJointInterpolation();
        }
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
        t = Mathf.SmoothStep(0, 1, t); // 使用平滑插值

        // 更新每个关节的角度
        for (int i = 0; i < joints.Length; i++)
        {
            float interpolatedAngle = Mathf.LerpAngle(currentJointAngles[i], targetJointAngles[i], t);
            var drive = joints[i].xDrive;
            drive.target = interpolatedAngle;
            joints[i].xDrive = drive;
        }
    }

    IEnumerator SendIKRequest()
    {
        // 构建请求数据
        var request = new IKRequest
        {
            left_pose = TransformToMatrix(leftTargetPose),
            right_pose = TransformToMatrix(rightTargetPose),
            motorstate = joints.Select(j => j.jointPosition[0]).ToArray(),
            motorV = joints.Select(j => j.jointVelocity[0]).ToArray()
        };

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
                    // 保存当前关节角度作为插值起点
                    currentJointAngles = joints.Select(j => j.jointPosition[0] * Mathf.Rad2Deg).ToArray();
                    
                    // 设置目标关节角度
                    targetJointAngles = response.q.Select(angle => angle * Mathf.Rad2Deg).ToArray();
                    
                    // 重置插值参数
                    currentInterpolationTime = 0f;
                    isInterpolating = true;

                    Debug.Log("开始插值到新的目标位置");
                }
                else
                {
                    Debug.LogWarning("IK求解失败");
                }
            }
            else
            {
                Debug.LogError($"请求失败: {www.error}");
            }
        }
    }

    private float[][] TransformToMatrix(Transform transform)
    {
        Matrix4x4 matrix = Matrix4x4.TRS(transform.position, transform.rotation, Vector3.one);
        float[][] result = new float[4][];
        for (int i = 0; i < 4; i++)
        {
            result[i] = new float[4];
            for (int j = 0; j < 4; j++)
            {
                result[i][j] = matrix[i, j];
            }
        }
        return result;
    }

    // 辅助方法：标准化角度到 [-180, 180] 范围
    private float NormalizeAngle(float angle)
    {
        while (angle > 180f) angle -= 360f;
        while (angle < -180f) angle += 360f;
        return angle;
    }
} 