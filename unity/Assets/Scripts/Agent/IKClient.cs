using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Networking;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using System;

public class IKClient : MonoBehaviour
{
    private string url = "http://192.168.31.212:5000/ik";
    public AgentMovement agentMovement;
    private List<float> initial_q = new List<float> { 0f, 0f, 0f, 0f, 0f, 0f };
    public Transform ur5BaseLeft; // 左臂的基座
    private Vector3 offsetLeft = new Vector3(0.0f, 0.24f, 0.0f);  // 左臂的偏移量（夹爪中心与末端关节）
    public Transform ur5BaseRight; // 右臂的基座
<<<<<<< HEAD
    private Vector3 offsetRight = new Vector3(0.0f, 0.24f, 0.0f);  // 右臂的偏移量（夹爪中心与末端关节）

    public event Action<List<float>> OnTargetJointAnglesUpdated;
=======
    private Vector3 offsetRight = new Vector3(0.0f, 0.24f, 0.0f);  // 右臂的偏移量（夹爪中心与末端关节）

    public event Action<List<float>> OnTargetJointAnglesUpdated;
>>>>>>> 0c14a5c8d787bef23f3133ad2b2203f5035105bb

    // 接收和处理目标位置
    public void ProcessTargetPosition(Vector3 newTargetPosition, bool isLeftArm)
    {
        Vector3 offset = isLeftArm ? offsetLeft : offsetRight;
        Transform baseTransform = isLeftArm ? ur5BaseLeft : ur5BaseRight;

        // 转换目标位置到基座坐标系
        Vector3 targetPositionRelative = ConvertToBaseCoordinates(newTargetPosition + offset, baseTransform);

        // 构建 translation 数据
        List<float> translation = new List<float>
        {
            targetPositionRelative.z,
            -targetPositionRelative.x,
            targetPositionRelative.y
        };

        // 构建 JSON 数据字典
        var data = new Dictionary<string, object>
        {
            { "joint_id", 6 },
            { "rotation", isLeftArm ? new List<List<float>>
                {
                    new List<float> { 1f, 0f, 0f },
                    new List<float> { 0f, 1f, 0f },
                    new List<float> { 0f, 0f, 1f }
                } : new List<List<float>>
                {
                    new List<float> { 1f, 0f, 0f },
                    new List<float> { 0f, 1f, 0f },
                    new List<float> { 0f, 0f, -1f }
                }
            },
            { "translation", translation },
            { "initial_q", initial_q }
        };

            // 发送反向运动学请求
            StartCoroutine(SendIKRequest(data));
    }

    // 发送反向运动学请求的协程
    private IEnumerator SendIKRequest(Dictionary<string, object> data)
    {
        using (UnityWebRequest request = new UnityWebRequest(url, "POST"))
        {
            string jsonData = JsonConvert.SerializeObject(data);
            request.uploadHandler = new UploadHandlerRaw(System.Text.Encoding.UTF8.GetBytes(jsonData));
            request.downloadHandler = new DownloadHandlerBuffer();
            request.SetRequestHeader("Content-Type", "application/json");

            //Debug.Log("发送的 JSON 数据: " + jsonData);

            yield return request.SendWebRequest();

            if (request.result == UnityWebRequest.Result.Success)
            {
                //Debug.Log("请求成功，响应数据: " + request.downloadHandler.text);
                ProcessResponse(request.downloadHandler.text);
            }
            else
            {
                Debug.LogError("IK请求失败，响应代码：" + request.responseCode);
            }
        }
    }

    // 处理服务器响应
<<<<<<< HEAD
    private void ProcessResponse(string jsonResponse)
    {
        JObject result = JObject.Parse(jsonResponse);

        if (result["success"].Value<bool>())
        {
            List<float> jointAnglesDegrees = ConvertToDegrees(result["q"].ToObject<List<float>>());
            OnTargetJointAnglesUpdated?.Invoke(jointAnglesDegrees);
            Debug.Log("IK角度: " + string.Join(", ", jointAnglesDegrees));
        }
        else
        {
            Debug.Log("IK计算失败");
        }
=======
    private void ProcessResponse(string jsonResponse)
    {
        JObject result = JObject.Parse(jsonResponse);

        if (result["success"].Value<bool>())
        {
            List<float> jointAnglesDegrees = ConvertToDegrees(result["q"].ToObject<List<float>>());
            OnTargetJointAnglesUpdated?.Invoke(jointAnglesDegrees);
            Debug.Log("IK角度: " + string.Join(", ", jointAnglesDegrees));
        }
        else
        {
            Debug.Log("IK计算失败");
        }
>>>>>>> 0c14a5c8d787bef23f3133ad2b2203f5035105bb
    }

    // 将目标位置转换为基座坐标系
    Vector3 ConvertToBaseCoordinates(Vector3 targetPosition, Transform baseTransform)
    {
        Vector3 relativePosition = targetPosition - baseTransform.position;
        Vector3 result = Quaternion.Inverse(baseTransform.rotation) * relativePosition;
        //Debug.Log("相对于基座的目标位置: " + result);
        return result;
    }

    // 将弧度转换为归一化的角度
    private List<float> ConvertToDegrees(List<float> jointAnglesRadians)
    {
        List<float> jointAnglesDegrees = new List<float>();
        foreach (float radian in jointAnglesRadians)
        {
            float degree = radian * (180f / Mathf.PI);
            degree = agentMovement.NormalizeAngle(degree);
            jointAnglesDegrees.Add(degree);
        }
        //Debug.Log("关节角度（弧度转换为度）: " + string.Join(", ", jointAnglesDegrees));
        return jointAnglesDegrees;
    }
}