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
    public Transform ur5BaseLeft; // ��۵Ļ���
    private Vector3 offsetLeft = new Vector3(0.0f, 0.24f, 0.0f);  // ��۵�ƫ��������צ������ĩ�˹ؽڣ�
    public Transform ur5BaseRight; // �ұ۵Ļ���
<<<<<<< HEAD
    private Vector3 offsetRight = new Vector3(0.0f, 0.24f, 0.0f);  // �ұ۵�ƫ��������צ������ĩ�˹ؽڣ�

    public event Action<List<float>> OnTargetJointAnglesUpdated;
=======
    private Vector3 offsetRight = new Vector3(0.0f, 0.24f, 0.0f);  // �ұ۵�ƫ��������צ������ĩ�˹ؽڣ�

    public event Action<List<float>> OnTargetJointAnglesUpdated;
>>>>>>> 0c14a5c8d787bef23f3133ad2b2203f5035105bb

    // ���պʹ���Ŀ��λ��
    public void ProcessTargetPosition(Vector3 newTargetPosition, bool isLeftArm)
    {
        Vector3 offset = isLeftArm ? offsetLeft : offsetRight;
        Transform baseTransform = isLeftArm ? ur5BaseLeft : ur5BaseRight;

        // ת��Ŀ��λ�õ���������ϵ
        Vector3 targetPositionRelative = ConvertToBaseCoordinates(newTargetPosition + offset, baseTransform);

        // ���� translation ����
        List<float> translation = new List<float>
        {
            targetPositionRelative.z,
            -targetPositionRelative.x,
            targetPositionRelative.y
        };

        // ���� JSON �����ֵ�
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

            // ���ͷ����˶�ѧ����
            StartCoroutine(SendIKRequest(data));
    }

    // ���ͷ����˶�ѧ�����Э��
    private IEnumerator SendIKRequest(Dictionary<string, object> data)
    {
        using (UnityWebRequest request = new UnityWebRequest(url, "POST"))
        {
            string jsonData = JsonConvert.SerializeObject(data);
            request.uploadHandler = new UploadHandlerRaw(System.Text.Encoding.UTF8.GetBytes(jsonData));
            request.downloadHandler = new DownloadHandlerBuffer();
            request.SetRequestHeader("Content-Type", "application/json");

            //Debug.Log("���͵� JSON ����: " + jsonData);

            yield return request.SendWebRequest();

            if (request.result == UnityWebRequest.Result.Success)
            {
                //Debug.Log("����ɹ�����Ӧ����: " + request.downloadHandler.text);
                ProcessResponse(request.downloadHandler.text);
            }
            else
            {
                Debug.LogError("IK����ʧ�ܣ���Ӧ���룺" + request.responseCode);
            }
        }
    }

    // �����������Ӧ
<<<<<<< HEAD
    private void ProcessResponse(string jsonResponse)
    {
        JObject result = JObject.Parse(jsonResponse);

        if (result["success"].Value<bool>())
        {
            List<float> jointAnglesDegrees = ConvertToDegrees(result["q"].ToObject<List<float>>());
            OnTargetJointAnglesUpdated?.Invoke(jointAnglesDegrees);
            Debug.Log("IK�Ƕ�: " + string.Join(", ", jointAnglesDegrees));
        }
        else
        {
            Debug.Log("IK����ʧ��");
        }
=======
    private void ProcessResponse(string jsonResponse)
    {
        JObject result = JObject.Parse(jsonResponse);

        if (result["success"].Value<bool>())
        {
            List<float> jointAnglesDegrees = ConvertToDegrees(result["q"].ToObject<List<float>>());
            OnTargetJointAnglesUpdated?.Invoke(jointAnglesDegrees);
            Debug.Log("IK�Ƕ�: " + string.Join(", ", jointAnglesDegrees));
        }
        else
        {
            Debug.Log("IK����ʧ��");
        }
>>>>>>> 0c14a5c8d787bef23f3133ad2b2203f5035105bb
    }

    // ��Ŀ��λ��ת��Ϊ��������ϵ
    Vector3 ConvertToBaseCoordinates(Vector3 targetPosition, Transform baseTransform)
    {
        Vector3 relativePosition = targetPosition - baseTransform.position;
        Vector3 result = Quaternion.Inverse(baseTransform.rotation) * relativePosition;
        //Debug.Log("����ڻ�����Ŀ��λ��: " + result);
        return result;
    }

    // ������ת��Ϊ��һ���ĽǶ�
    private List<float> ConvertToDegrees(List<float> jointAnglesRadians)
    {
        List<float> jointAnglesDegrees = new List<float>();
        foreach (float radian in jointAnglesRadians)
        {
            float degree = radian * (180f / Mathf.PI);
            degree = agentMovement.NormalizeAngle(degree);
            jointAnglesDegrees.Add(degree);
        }
        //Debug.Log("�ؽڽǶȣ�����ת��Ϊ�ȣ�: " + string.Join(", ", jointAnglesDegrees));
        return jointAnglesDegrees;
    }
}