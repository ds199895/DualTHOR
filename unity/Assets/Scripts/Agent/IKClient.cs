using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Networking;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using System;

public class IKClient : MonoBehaviour
{
    private string url = "http://127.0.0.1:5000/ik";
    public AgentMovement agentMovement;
    private List<float> initial_q = new List<float> { 0f, 0f, 0f, 0f, 0f, 0f };
    public Transform ur5BaseLeft; // left arm base
    private Vector3 offsetLeft = new Vector3(0.0f, 0.24f, 0.0f);  // left arm offset (gripper center to end joint)
    public Transform ur5BaseRight; // right arm base
    private Vector3 offsetRight = new Vector3(0.0f, 0.24f, 0.0f);  // right arm offset (gripper center to end joint)

    public event Action<List<float>> OnTargetJointAnglesUpdated;


    // left and right target poses
    public Transform leftTargetPose;
    public Transform rightTargetPose;

    // keyboard control
    public KeyCode triggerKey = KeyCode.Space; // trigger IK key
     
    void Update()
    {
        // detect keyboard input
        if (Input.GetKeyDown(triggerKey))
        {
            Debug.Log("Start IK calculation");
            ProcessTargetPosition(leftTargetPose.position, true);
            // ProcessTargetPosition(rightTargetPose.position, false);
        }

    }



    // receive and process the target position
    public void ProcessTargetPosition(Vector3 newTargetPosition, bool isLeftArm)
    {
        Vector3 offset = isLeftArm ? offsetLeft : offsetRight;
        Transform baseTransform = isLeftArm ? ur5BaseLeft : ur5BaseRight;

        // convert the target position to the base coordinate system
        Vector3 targetPositionRelative = ConvertToBaseCoordinates(newTargetPosition + offset, baseTransform);

        // build the translation data
        List<float> translation = new List<float>
        {
            targetPositionRelative.z,
            -targetPositionRelative.x,
            targetPositionRelative.y
        };

        // build the JSON data dictionary
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

        // send the inverse kinematics request
        StartCoroutine(SendIKRequest(data));
    }

    // send the inverse kinematics request coroutine
    private IEnumerator SendIKRequest(Dictionary<string, object> data)
    {
        using (UnityWebRequest request = new UnityWebRequest(url, "POST"))
        {
            string jsonData = JsonConvert.SerializeObject(data);
            request.uploadHandler = new UploadHandlerRaw(System.Text.Encoding.UTF8.GetBytes(jsonData));
            request.downloadHandler = new DownloadHandlerBuffer();
            request.SetRequestHeader("Content-Type", "application/json");

            //Debug.Log("sent JSON data: " + jsonData);

            yield return request.SendWebRequest();

            if (request.result == UnityWebRequest.Result.Success)
            {
                //Debug.Log("request successful, response data: " + request.downloadHandler.text);
                ProcessResponse(request.downloadHandler.text);
            }
            else
            {
                Debug.LogError("IK request failed, response code: " + request.responseCode);
            }
        }
    }

    // process the server response
    private void ProcessResponse(string jsonResponse)
    {
        JObject result = JObject.Parse(jsonResponse);

        if (result["success"].Value<bool>())
        {
            List<float> jointAnglesDegrees = ConvertToDegrees(result["q"].ToObject<List<float>>());
            OnTargetJointAnglesUpdated?.Invoke(jointAnglesDegrees);
            Debug.Log("IK angles: " + string.Join(", ", jointAnglesDegrees));
        }
        else
        {
            Debug.Log("IK calculation failed");
        }
    }

    // convert the target position to the base coordinate system
    Vector3 ConvertToBaseCoordinates(Vector3 targetPosition, Transform baseTransform)
    {
        Vector3 relativePosition = targetPosition - baseTransform.position;
        Vector3 result = Quaternion.Inverse(baseTransform.rotation) * relativePosition;
        //Debug.Log("relative position to the base: " + result);
        return result;
    }

    // convert the radians to the normalized angle
    private List<float> ConvertToDegrees(List<float> jointAnglesRadians)
    {
        List<float> jointAnglesDegrees = new List<float>();
        foreach (float radian in jointAnglesRadians)
        {
            float degree = radian * (180f / Mathf.PI);
            degree = agentMovement.NormalizeAngle(degree);
            jointAnglesDegrees.Add(degree);
        }
        //Debug.Log("joint angles (radians to degrees): " + string.Join(", ", jointAnglesDegrees));
        return jointAnglesDegrees;
    }
}