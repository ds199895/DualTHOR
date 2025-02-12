// using UnityEngine;
// using UnityEngine.Networking;
// using System.Collections;
// using System.Collections.Generic;
// using System.Linq;
// using Newtonsoft.Json;

// public class IK_H1 : IKBase
// {
//     [System.Serializable]
//     public class IKRequest
//     {
//         public float[][] left_pose;    // 4x4 矩阵
//         public float[][] right_pose;   // 4x4 矩阵
//         public float[] motorstate;     // 当前关节角度
//         public float[] motorV;         // 当前关节角速度
//     }

//     [System.Serializable]
//     public class IKResponse
//     {
//         public bool success;
//         public float[] q;      // 计算得到的关节角度
//         public float[] tau;    // 计算得到的关节力矩
//     }
//     public ArticulationBody[] joints; // 机器人的关节

   
//     private string serverUrl = "http://localhost:5000/ik";

//     // 左右手目标位姿
//     public Transform leftTargetPose;
//     public Transform rightTargetPose;

//     public Transform baseTransform;

//     // 插值相关参数
//     private float[] currentJointAngles;
//     private float[] targetJointAngles;
//     private float interpolationTime = 0.5f; // 插值时间（秒）
//     private float currentInterpolationTime = 0f;
//     private bool isInterpolating = false;

//     // 键盘控制
//     public KeyCode triggerKey = KeyCode.Space; // 触发IK的按键

//     void Start()
//     {
//         // 确保所有关节都已赋值
//         if (joints == null || joints.Length == 0)
//         {
//             Debug.LogError("请设置机器人关节！");
//             enabled = false;
//             return;
//         }
//         // 初始化当前关节角度
//         currentJointAngles = joints.Select(j => j.jointPosition[0] * Mathf.Rad2Deg).ToArray();
//         targetJointAngles = currentJointAngles.ToArray();
//     }

//     void Update()
//     {
//         // 检测键盘输入
//         if (Input.GetKeyDown(triggerKey))
//         {
//             Debug.Log("开始IK计算");
//             // 转换目标位置到基座坐标系
//             float[][] left_target_matrix = ConvertTargetToBaseMatrix(leftTargetPose, baseTransform);
//             float[][] right_target_matrix = ConvertTargetToBaseMatrix(rightTargetPose, baseTransform);

//             // 构建请求数据
//             var request = new IKRequest
//             {
//                 left_pose = left_target_matrix,
//                 right_pose = right_target_matrix,
//                 motorstate = joints.Select(j => j.jointPosition[0]).ToArray(),
//                 motorV = joints.Select(j => j.jointVelocity[0]).ToArray()
//             };
//             StartCoroutine(SendIKRequest(request));
//         }

//         // 执行插值
//         if (isInterpolating)
//         {
//             UpdateJointInterpolation();
//         }
//     }


//     // 将目标位置转换为基座坐标系
//     Vector3 ConvertToBaseCoordinates(Vector3 targetPosition, Transform baseTransform)
//     {
//         Vector3 relativePosition = targetPosition - baseTransform.position;
//         Vector3 result = Quaternion.Inverse(baseTransform.rotation) * relativePosition;
//         //Debug.Log("相对于基座的目标位置: " + result);
//         return result;
//     }
    
//     private void UpdateJointInterpolation()
//     {
//         if (currentInterpolationTime >= interpolationTime)
//         {
//             isInterpolating = false;
//             return;
//         }

//         currentInterpolationTime += Time.deltaTime;
//         float t = currentInterpolationTime / interpolationTime;
//         t = Mathf.SmoothStep(0, 1, t); // 使用平滑插值

//         // 更新每个关节的角度
//         for (int i = 0; i < joints.Length; i++)
//         {
//             float interpolatedAngle = Mathf.LerpAngle(currentJointAngles[i], targetJointAngles[i], t);
//             var drive = joints[i].xDrive;
//             drive.target = interpolatedAngle;
//             joints[i].xDrive = drive;
//         }
//     }

//     IEnumerator SendIKRequest(IKRequest request)
//     {
//         string jsonData = JsonConvert.SerializeObject(request);

//         using (UnityWebRequest www = new UnityWebRequest(serverUrl, "POST"))
//         {
//             byte[] jsonToSend = new System.Text.UTF8Encoding().GetBytes(jsonData);
//             www.uploadHandler = new UploadHandlerRaw(jsonToSend);
//             www.downloadHandler = new DownloadHandlerBuffer();
//             www.SetRequestHeader("Content-Type", "application/json");

//             yield return www.SendWebRequest();

//             if (www.result == UnityWebRequest.Result.Success)
//             {
//                 var response = JsonConvert.DeserializeObject<IKResponse>(www.downloadHandler.text);
                
//                 if (response.success)
//                 {
//                     // 保存当前关节角度作为插值起点
//                     currentJointAngles = joints.Select(j => j.jointPosition[0] * Mathf.Rad2Deg).ToArray();
                    
//                     // 设置目标关节角度
//                     targetJointAngles = response.q.Select(angle => angle * Mathf.Rad2Deg).ToArray();
                    
//                     // 重置插值参数
//                     currentInterpolationTime = 0f;
//                     isInterpolating = true;

//                     Debug.Log("开始插值到新的目标位置");
//                 }
//                 else
//                 {
//                     Debug.LogWarning("IK求解失败");
//                 }
//             }
//             else
//             {
//                 Debug.LogError($"请求失败: {www.error}. URL: {serverUrl}");
//             }
//         }
//     }

//     private float[][] TransformToMatrix(Vector3 position, Quaternion rotation)
//     {
//         Matrix4x4 matrix = Matrix4x4.TRS(position, rotation, Vector3.one);
//         return new float[][]
//         {
//             new float[] { matrix.m00, matrix.m01, matrix.m02, matrix.m03 },
//             new float[] { matrix.m10, matrix.m11, matrix.m12, matrix.m13 },
//             new float[] { matrix.m20, matrix.m21, matrix.m22, matrix.m23 },
//             new float[] { matrix.m30, matrix.m31, matrix.m32, matrix.m33 }
//         };
//     }

//     private float[][] ConvertTargetToBaseMatrix(Transform targetPose, Transform baseTransform)
//     {
//         // 将目标位置转换为基座坐标系
//         Vector3 relativePosition = targetPose.position - baseTransform.position;
//         Vector3 basePosition = Quaternion.Inverse(baseTransform.rotation) * relativePosition;

//         // 使用IKClient中的方式构建translation
//         List<float> translation = new List<float>
//         {
//             basePosition.z,
//             -basePosition.x,
//             basePosition.y
//         };

//         // 将目标旋转转换为基座坐标系
//         Quaternion baseRotation = Quaternion.Inverse(baseTransform.rotation) * targetPose.rotation;

//         // 将Quaternion转换为Matrix4x4
//         Matrix4x4 rotationMatrix = Matrix4x4.Rotate(baseRotation);

//         // 生成4x4变换矩阵
//         return new float[][]
//         {
//             new float[] { rotationMatrix.m00, rotationMatrix.m01, rotationMatrix.m02, translation[0] },
//             new float[] { rotationMatrix.m10, rotationMatrix.m11, rotationMatrix.m12, translation[1] },
//             new float[] { rotationMatrix.m20, rotationMatrix.m21, rotationMatrix.m22, translation[2] },
//             new float[] { 0, 0, 0, 1 }
//         };
//     }

//     // 辅助方法：标准化角度到 [-180, 180] 范围
//     private float NormalizeAngle(float angle)
//     {
//         while (angle > 180f) angle -= 360f;
//         while (angle < -180f) angle += 360f;
//         return angle;
//     }

//     public void IniitTarget()
//     {
//         // 转换目标位置到基座坐标系
//         float[][] left_target_matrix = ConvertTargetToBaseMatrix(leftTargetPose, baseTransform);
//         float[][] right_target_matrix = ConvertTargetToBaseMatrix(rightTargetPose, baseTransform);

//         // 构建请求数据
//         var request = new IKRequest
//         {
//             left_pose = left_target_matrix,
//             right_pose = right_target_matrix,
//             motorstate = joints.Select(j => j.jointPosition[0]).ToArray(),
//             motorV = joints.Select(j => j.jointVelocity[0]).ToArray()
//         };
//         StartCoroutine(SendIKRequest(request));
//     }

//     public override void ProcessTargetPosition(Vector3 newTargetPosition, bool isLeftArm)
//     {
//         // // 选择目标位姿和基座变换
//         // Transform targetPose = isLeftArm ? leftTargetPose : rightTargetPose;
//         // Transform baseTransform = this.baseTransform;
//         //
//         // // 转换目标位置到基座坐标系
//         // Vector3 targetPositionRelative = ConvertToBaseCoordinates(newTargetPosition, baseTransform);
//         //
//         // // 构建目标位姿矩阵
//         // float[][] targetMatrix = TransformToMatrix(targetPositionRelative, targetPose.rotation);
//         //
//         // // 构建请求数据
//         // var request = new IKRequest
//         // {
//         //     left_pose = isLeftArm ? targetMatrix : null,
//         //     right_pose = isLeftArm ? null : targetMatrix,
//         //     motorstate = joints.Select(j => j.jointPosition[0]).ToArray(),
//         //     motorV = joints.Select(j => j.jointVelocity[0]).ToArray()
//         // };
//         //
//         // // 发送反向运动学请求
//         // StartCoroutine(SendIKRequest(request));
//         if (isLeftArm)
//         {
//             leftTargetPose.position = newTargetPosition; 
//         }
//         else
//         {
//             rightTargetPose.position = newTargetPosition;
//         }
//         float[][] left_target_matrix = ConvertTargetToBaseMatrix(leftTargetPose, baseTransform);
//         float[][] right_target_matrix = ConvertTargetToBaseMatrix(rightTargetPose, baseTransform);

//         // 构建请求数据
//         var request = new IKRequest
//         {
//             left_pose = left_target_matrix,
//             right_pose = right_target_matrix,
//             motorstate = joints.Select(j => j.jointPosition[0]).ToArray(),
//             motorV = joints.Select(j => j.jointVelocity[0]).ToArray()
//         };
//         StartCoroutine(SendIKRequest(request));
//     }
// } 