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

    void Start()
    {
        // 确保所有关节都已赋值
        if (joints == null || joints.Length == 0)
        {
            Debug.LogError("请设置机器人关节！");
            enabled = false;
            return;
        }
    }

    void FixedUpdate()
    {
        StartCoroutine(SendIKRequest());
    }

    IEnumerator SendIKRequest()
    {
        // 构建请求数据
        var request = new IKRequest
        {
            // 转换Transform为4x4矩阵
            left_pose = TransformToMatrix(leftTargetPose),
            right_pose = TransformToMatrix(rightTargetPose),
            
            // 获取当前关节状态
            motorstate = joints.Select(j => j.jointPosition[0]).ToArray(),
            motorV = joints.Select(j => j.jointVelocity[0]).ToArray()
        };

        // 转换为JSON
        string jsonData = JsonConvert.SerializeObject(request);

        // 创建POST请求
        using (UnityWebRequest www = new UnityWebRequest(serverUrl, "POST"))
        {
            byte[] jsonToSend = new System.Text.UTF8Encoding().GetBytes(jsonData);
            www.uploadHandler = new UploadHandlerRaw(jsonToSend);
            www.downloadHandler = new DownloadHandlerBuffer();
            www.SetRequestHeader("Content-Type", "application/json");

            // 发送请求
            yield return www.SendWebRequest();

            if (www.result == UnityWebRequest.Result.Success)
            {
                // 解析响应
                var response = JsonConvert.DeserializeObject<IKResponse>(www.downloadHandler.text);
                
                if (response.success)
                {
                    // 更新关节位置
                    for (int i = 0; i < joints.Length; i++)
                    {
                        var drive = joints[i].xDrive;
                        drive.target = response.q[i] * Mathf.Rad2Deg; // 转换为角度
                        joints[i].xDrive = drive;
                    }
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
} 