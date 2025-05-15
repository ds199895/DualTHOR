using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Networking;
using Newtonsoft.Json;
using System.Linq;
using System;

public class IK_X1 : IKBase
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

    private string url = "http://localhost:5000/ik";
    public ArticulationBody[] joints; // 机器人的关节
    
    // 左右手目标位姿
    public Transform leftTargetPose;
    public Transform rightTargetPose;
    
    public Transform baseTransform; // 机器人基座
    
    // 插值相关参数
    private float[] currentJointAngles;
    private float[] targetJointAngles;
    private float interpolationTime = 0.5f; // 插值时间（秒）
    private float currentInterpolationTime = 0f;
    private bool isInterpolating = false;
    
    // 键盘控制
    public KeyCode triggerKey = KeyCode.Space; // 触发IK的按键
    
    public float[] initialAngles;
    public bool inited = false;
    
    // 添加事件，用于兼容AgentMovement的使用方式
    public event Action<List<float>> OnTargetJointAnglesUpdated;
    
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
            Debug.Log("开始IK计算");
            // 转换目标位置到基座坐标系
            float[][] left_target_matrix = ConvertTargetToBaseMatrix(leftTargetPose, baseTransform);
            float[][] right_target_matrix = ConvertTargetToBaseMatrix(rightTargetPose, baseTransform);

            // 构建请求数据 - 交换左右手目标位姿
            var request = new IKRequest
            {
                left_pose = right_target_matrix,   // 交换：将右手目标矩阵传给left_pose
                right_pose = left_target_matrix,   // 交换：将左手目标矩阵传给right_pose
                motorstate = joints.Select(j => j.jointPosition[0]).ToArray(),
                motorV = joints.Select(j => j.jointVelocity[0]).ToArray()
            };
            StartCoroutine(SendIKRequest(request));
        }

        // 执行插值
        if (isInterpolating)
        {
            UpdateJointInterpolation();
        }
    }
    
    // 将目标位置转换为基座坐标系
    Vector3 ConvertToBaseCoordinates(Vector3 targetPosition, Transform baseTransform)
    {
        Vector3 relativePosition = targetPosition - baseTransform.position;
        Vector3 result = Quaternion.Inverse(baseTransform.rotation) * relativePosition;
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
        t = Mathf.SmoothStep(0, 1, t); // 使用平滑插值

        // 更新每个关节的角度
        for (int i = 0; i < joints.Length && i < targetJointAngles.Length; i++)
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

        using (UnityWebRequest www = new UnityWebRequest(url, "POST"))
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

                    if(!inited){
                        initialAngles = response.q.Select(angle => angle * Mathf.Rad2Deg).ToArray();
                        inited = true;
                    }
                    
                    // 重置插值参数
                    currentInterpolationTime = 0f;
                    isInterpolating = true;

                    // 触发事件，通知AgentMovement目标角度已更新（仅作为通知用途）
                    if (OnTargetJointAnglesUpdated != null)
                    {
                        List<float> anglesList = new List<float>(targetJointAngles);
                        OnTargetJointAnglesUpdated.Invoke(anglesList);
                    }

                    Debug.Log("开始插值到新的目标位置");
                }
                else
                {
                    Debug.LogWarning("IK求解失败");
                }
            }
            else
            {
                Debug.LogError($"请求失败: {www.error}. URL: {url}");
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
        // 将目标位置转换为基座坐标系
        Vector3 relativePosition = targetPose.position - baseTransform.position;
        Vector3 basePosition = Quaternion.Inverse(baseTransform.rotation) * relativePosition;

        // 构建translation
        List<float> translation = new List<float>
        {
           
            basePosition.z,
            -basePosition.x,
            basePosition.y
        };

        // 将目标旋转转换为基座坐标系
        Quaternion baseRotation = Quaternion.Inverse(baseTransform.rotation) * targetPose.rotation;

        // 将Quaternion转换为Matrix4x4
        Matrix4x4 rotationMatrix = Matrix4x4.Rotate(baseRotation);

        // 生成4x4变换矩阵
        return new float[][]
        {
            new float[] { rotationMatrix.m00, rotationMatrix.m01, rotationMatrix.m02, translation[0] },
            new float[] { rotationMatrix.m10, rotationMatrix.m11, rotationMatrix.m12, translation[1] },
            new float[] { rotationMatrix.m20, rotationMatrix.m21, rotationMatrix.m22, translation[2] },
            new float[] { 0, 0, 0, 1 }
        };
    }
    
    public void InitTarget()
    {
        Debug.Log("初始化X1目标位置");
        
        // 确保关节已经初始化
        if (joints == null || joints.Length == 0)
        {
            Debug.LogError("关节未初始化，请先调用InitBodies方法");
            return;
        }
        
        // 转换目标位置到基座坐标系
        float[][] left_target_matrix = ConvertTargetToBaseMatrix(leftTargetPose, baseTransform);
        float[][] right_target_matrix = ConvertTargetToBaseMatrix(rightTargetPose, baseTransform);

        // 构建请求数据 - 交换左右手目标位姿
        var request = new IKRequest
        {
            left_pose = left_target_matrix,   // 交换：将右手目标矩阵传给left_pose
            right_pose = right_target_matrix,   // 交换：将左手目标矩阵传给right_pose
            motorstate = joints.Select(j => j.jointPosition[0]).ToArray(),
            motorV = joints.Select(j => j.jointVelocity[0]).ToArray()
        };
        StartCoroutine(SendIKRequest(request));
    }
    
    public void ResetTarget()
    {
        if (initialAngles != null)
        {
            // 保存当前关节角度作为插值起点
            currentJointAngles = joints.Select(j => j.jointPosition[0] * Mathf.Rad2Deg).ToArray();
            
            // 设置目标为初始角度
            targetJointAngles = initialAngles.ToArray();
            
            // 重置插值参数
            currentInterpolationTime = 0f;
            isInterpolating = true;
            
            // 触发事件，通知AgentMovement目标角度已更新（仅作为通知用途）
            if (OnTargetJointAnglesUpdated != null)
            {
                List<float> anglesList = new List<float>(initialAngles);
                OnTargetJointAnglesUpdated.Invoke(anglesList);
            }
            
            Debug.Log("开始重置到初始位置");
        }
        else
        {
            Debug.LogWarning("未找到初始角度数据，无法重置位置");
        }
    }
    
    // 辅助方法：标准化角度到 [-180, 180] 范围
    public float NormalizeAngle(float angle)
    {
        while (angle > 180f) angle -= 360f;
        while (angle < -180f) angle += 360f;
        return angle;
    }
    
    public override void ProcessTargetPosition(Vector3 newTargetPosition, bool isLeftArm)
    {
        if (isLeftArm)
        {
            leftTargetPose.position = newTargetPosition; 
        }
        else
        {
            rightTargetPose.position = newTargetPosition;
        }
        
        float[][] left_target_matrix = ConvertTargetToBaseMatrix(leftTargetPose, baseTransform);
        float[][] right_target_matrix = ConvertTargetToBaseMatrix(rightTargetPose, baseTransform);

        // 构建请求数据 - 交换左右手目标位姿
        var request = new IKRequest
        {
            left_pose = right_target_matrix,   // 交换：将右手目标矩阵传给left_pose
            right_pose = left_target_matrix,   // 交换：将左手目标矩阵传给right_pose
            motorstate = joints.Select(j => j.jointPosition[0]).ToArray(),
            motorV = joints.Select(j => j.jointVelocity[0]).ToArray()
        };
        StartCoroutine(SendIKRequest(request));
        
        // 不再需要触发事件，因为所有处理都在内部完成
        // 事件仍然保留用于兼容性，但它仅是为了通知而不是为了控制
    }

    public void InitBodies() {
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
        
        // 打印关节顺序，方便调试
        Debug.Log("机器人关节顺序:");
        for (int i = 0; i < joints.Length; i++) {
            Debug.Log($"关节 {i}: {joints[i].name}");
        }
        
        Debug.Log("X1机器人关节初始化完成");
    }
}