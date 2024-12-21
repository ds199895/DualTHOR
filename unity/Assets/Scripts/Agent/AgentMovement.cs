using System;
using System.Reflection;
using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using System.Linq;

public class AgentMovement : MonoBehaviour
{
    
    public enum RobotType
    {
        X1=0,
        H1=1,
        G1=2
    }
    public SceneManager sceneManager;
    public float moveSpeed = 1.0f;
    public float rotationSpeed = 90.0f;  // 每秒旋转的角度
    public GripperController gripperController; 
    public ArticulationBody[] articulationChain;
    public float stiffness = 10000f;    // 刚度
    public float damping = 100f;       // 阻尼
    public float forceLimit = 1000f;  // 力限制
    public float speed = 30f;         // 速度，单位：度/秒
    public float torque = 100f;       // 扭矩，单位：Nm
    public float acceleration = 10f;  // 加速度

    public Transform target;
    private Vector3 lastTargetPosition;
    private float positionChangeThreshold = 0.0001f; // 位置变化阈值
    public IK_X1 ikX1; 
    public IK_H1 ikH1;
    // public Transform pickPositionL;  // 夹取位置
    // public Transform placePositionL; // 放置位置
    // public Transform pickPositionR;  // 夹取位置
    // public Transform placePositionR; // 放置位置
    public List<float> targetJointAngles = new List<float> { 0, 0, 0, 0, 0, 0 }; // 初始值
    
    private string[] h1_left_arm_joints = new string[]
    {
        "left_shoulder_pitch_link",
        "left_shoulder_roll_link",
        "left_shoulder_yaw_link",
        "left_elbow_link",
    };
    private string[] h1_right_arm_joints = new string[]
    {
        "right_shoulder_pitch_link",
        "right_shoulder_roll_link",
        "right_shoulder_yaw_link",
        "right_elbow_link",
    };
    
    private string[] x1_left_arm_joints = new string[]
    {
        "left_shoulder_pitch_link",
        "left_shoulder_roll_link",
        "left_shoulder_yaw_link",
        "left_elbow_link",
    };
    private string[] x2_right_arm_joints = new string[]
    {
        "right_shoulder_pitch_link",
        "right_shoulder_roll_link",
        "right_shoulder_yaw_link",
        "right_elbow_link",
    };
    
    public List<ArticulationBody> leftArmJoints;  // 左臂关节
    public List<ArticulationBody> rightArmJoints; // 右臂关节

    private bool hasMovedToPosition = false; // toggle函数中标记是否已经到达目标位置
    private bool isTargetAnglesUpdated = false;
    private bool isManualControlEnabled = false;
    private float manualMoveSpeed = 5.0f;
    private float manualRotateSpeed = 60.0f;
    private float sprintMultiplier = 3f; // 加速倍数
    private float mouseSensitivity = 2.0f;
    private float verticalRotation = 0f;
    private Transform cameraTransform; // 相机Transform
    private float maxVerticalAngle = 80f; // 最大俯仰角度
    private bool isMouseUnlocked = false; // 标记是否按下了ESC以解锁鼠标

    [System.Serializable]
    public class JointAdjustment
    {
        public float angle;
        public Vector3 axis;
    }
    public List<JointAdjustment> adjustments = new List<JointAdjustment>
    {
        new JointAdjustment { angle = 0f, axis = Vector3.up },
        new JointAdjustment { angle = 90f, axis = Vector3.right },
        new JointAdjustment { angle = 0f, axis = Vector3.right },
        new JointAdjustment { angle = 90f, axis = Vector3.right },
        new JointAdjustment { angle = 0f, axis = Vector3.up },
        new JointAdjustment { angle = 0f, axis = Vector3.right }
    };
    private List<Vector3> defaultRotations = new List<Vector3>
    {
        new Vector3(0, 0, 0),
        new Vector3(90, 0, 0),
        new Vector3(0, 0, 0),
        new Vector3(90, 0, 0),
        new Vector3(0, 0, 0),
        new Vector3(0, 0, 0)
    };
    public List<JointAdjustment> rightAdjustments = new List<JointAdjustment>
    {
        new JointAdjustment { angle = 0f, axis = Vector3.up },
        new JointAdjustment { angle = 90f, axis = Vector3.right },
        new JointAdjustment { angle = 0f, axis = Vector3.right },
        new JointAdjustment { angle = 90f, axis = Vector3.right },
        new JointAdjustment { angle = 0f, axis = Vector3.up },
        new JointAdjustment { angle = 0f, axis = Vector3.right }
    };
    private List<Vector3> rightDefaultRotations = new List<Vector3>
    {
        new Vector3(0, 0, 0),
        new Vector3(90, 0, 0),
        new Vector3(0, 0, 0),
        new Vector3(90, 0, 0),
        new Vector3(0, 0, 0),
        new Vector3(0, 0, 0)
    };

    public RobotType CurrentRobotType = RobotType.X1;
    public List<GameObject> robots = new List<GameObject>();
    void Start()
    {



    }

    void Update()
    {
        if (Input.GetKeyDown(KeyCode.Alpha1))
        {
            StartCoroutine(ResetJoint(true)); // 同样，true表示使用左臂，false表示使用右臂
        }
        if (Input.GetKeyDown(KeyCode.Alpha2))
        {
            string targetObjectID = "Kitchen_Cup_01"; // 替换为目标物品的 ID
            TP(targetObjectID);
        }
        if (Input.GetKeyDown(KeyCode.Alpha3))
        {
            StartCoroutine(Pick("Kitchen_Cup_01", true)); // true表示使用左臂，false表示使用右臂
        }
        if (Input.GetKeyDown(KeyCode.Alpha4))
        {
            StartCoroutine(Place("Kitchen_Cup_01", true)); // 同样，true表示使用左臂，false表示使用右臂
        }
        if (Input.GetKeyDown(KeyCode.Alpha5))
        {
            StartCoroutine(Pick("Kitchen_Cup_01", false)); // true表示使用左臂，false表示使用右臂
        }
        if (Input.GetKeyDown(KeyCode.Alpha6))
        {
            StartCoroutine(Place("Kitchen_Cup_01", false)); // 同样，true表示使用左臂，false表示使用右臂
        }


        if (Input.GetKeyDown(KeyCode.Alpha7))
        {
            string targetObjectID = "Kitchen_Faucet_01"; // 替换为目标物品的 ID
            TP(targetObjectID);
        }
        if (Input.GetKeyDown(KeyCode.Alpha8))
        {
            StartCoroutine(Toggle("Kitchen_Faucet_01", true)); //
        }
        if (Input.GetKeyDown(KeyCode.Alpha9))
        {
            StartCoroutine(Toggle("Kitchen_Faucet_01", false)); //
        }

        if (Input.GetKeyDown(KeyCode.J))
        {
            string targetObjectID = "Kitchen_Fridge_01"; // 替换为目标物品的 ID
            TP(targetObjectID);
        }
        if (Input.GetKeyDown(KeyCode.K))
        {
            StartCoroutine(Open("Kitchen_Fridge_01", true)); // 
        }
        if (Input.GetKeyDown(KeyCode.L))
        {
            StartCoroutine(Open("Kitchen_Fridge_01", false)); // 
        }
        //if (Input.GetKeyDown(KeyCode.Alpha5))
        //{
        //    string targetObjectID = "Kitchen_StoveKnob_01"; // 替换为目标物品的 ID
        //    TP(targetObjectID);
        //}
        //if (Input.GetKeyDown(KeyCode.Alpha6))
        //{
        //    StartCoroutine(Toggle("Kitchen_StoveKnob_01")); // 
        //}
        //if (Input.GetKeyDown(KeyCode.Alpha7))
        //{
        //    string targetObjectID = "Kitchen_Cabinet_02"; // 替换为目标物品的 ID
        //    TP(targetObjectID);
        //}
        //if (Input.GetKeyDown(KeyCode.Alpha8))
        //{
        //    StartCoroutine(Open("Kitchen_Cabinet_02")); // 同样，
        //}
        //if (Input.GetKeyDown(KeyCode.Alpha0))
        //{
        //    StartCoroutine(ResetJoint(false)); // 同样，true表示使用左臂，false表示使用右臂
        //}


        if (Input.GetKeyDown(KeyCode.Z))        // Z键切换手动控制
        {
            isManualControlEnabled = !isManualControlEnabled;
            Cursor.visible = !isManualControlEnabled;
            Cursor.lockState = isManualControlEnabled ? CursorLockMode.Locked : CursorLockMode.None;

            if (isManualControlEnabled)
            {
                DisableArticulationBodies();
                isMouseUnlocked = false; // 确保进入控制模式时鼠标锁定
            }
            else
            {
                EnableArticulationBodies();
            }
        }
        // ESC键退出手动控制但不恢复ArticulationBodies
        if (isManualControlEnabled && Input.GetKeyDown(KeyCode.Escape))
        {
            isMouseUnlocked = true;
            Cursor.visible = true;
            Cursor.lockState = CursorLockMode.None;
        }
        // 点击屏幕重新进入手动控制
        if (isMouseUnlocked && Input.GetMouseButtonDown(0))
        {
            isMouseUnlocked = false;
            Cursor.visible = false;
            Cursor.lockState = CursorLockMode.Locked;
        }
        // 手动控制
        if (isManualControlEnabled && !isMouseUnlocked) ManualControl();
    }
    public void initGame()
    {
        articulationChain = GetComponentsInChildren<ArticulationBody>();

        if (articulationChain == null || articulationChain.Length == 0)
        {
            Debug.LogError("未找到任何关节，请确保该对象具有 ArticulationBody 组件！");
            return;
        }

        foreach (ArticulationBody joint in articulationChain)
        {
            ArticulationDrive drive = joint.xDrive;
            drive.stiffness = stiffness;
            drive.damping = damping;
            drive.forceLimit = forceLimit;
            joint.xDrive = drive;
        }

    }

    public void ExecuteActionWithCallback(UnityClient.ActionData actionData, Action callback)
    {
        Debug.Log("ExecuteActionWithCallback");
        // 获取方法
        MethodInfo method = typeof(AgentMovement).GetMethod(actionData.action, BindingFlags.Instance | BindingFlags.Public | BindingFlags.IgnoreCase);
        
        Debug.Log(method);
        // 如果方法不存在，执行回调并返回
        if (method == null)
        {
            Debug.LogWarning($"Unknown action: {actionData.action}");
            callback?.Invoke();
            return;
        }

        // 更新 lastAction
        sceneManager?.UpdateLastAction(actionData.action);

        // 使用传入的 successRate 来执行概率判断
        bool isSuccessful = Probability(actionData.successRate);
        sceneManager?.UpdateLastActionSuccess(isSuccessful, actionData.action);

        if (!isSuccessful)
        {
            Debug.LogWarning($"Action {actionData.action} failed due to random chance.");
            callback?.Invoke();
            return;
        }

        try
        {
            
            Debug.Log("ConstructArguments");

            Debug.Log(actionData);
            
            Debug.Log("test log1");
            Debug.Log(method.GetParameters());
            
            Debug.Log("test log2");
            // 构造参数并执行方法
            object[] args = ConstructArguments(method.GetParameters(), actionData);
            Debug.Log("test log3");
            Debug.Log(method.ReturnType);
            Debug.Log("test log4");
            if (method.ReturnType == typeof(IEnumerator))
            {
                Debug.Log("test log5");
                // 如果是协程方法，启动协程并在结束时调用回调
                StartCoroutine(ExecuteCoroutineAction(method, args, callback));
                Debug.Log("ExecuteCoroutineAction");
            }
            else
            {
                Debug.Log("非协程");
                // 非协程方法，立即调用并触发回调
                method.Invoke(this, args);
                callback?.Invoke();
                Debug.Log("立即调用并返回");
            }
        }
        catch (Exception ex)
        {
            Debug.LogError($"Error executing action {actionData.action}: {ex.Message}");
            callback?.Invoke();
        }
    }

    private IEnumerator ExecuteCoroutineAction(MethodInfo method, object[] args, Action callback)
    {
        Debug.Log($"Executing coroutine: {method.Name} with arguments: {string.Join(", ", args ?? new object[0])}");

        IEnumerator coroutine = null;

        try
        {
            coroutine = (IEnumerator)method.Invoke(this, args); // 启动协程方法
        }
        catch (Exception ex)
        {
            Debug.LogError($"Error invoking coroutine {method.Name}: {ex.Message}");
        }

        if (coroutine != null)
        {
            yield return StartCoroutine(coroutine); // 等待协程执行完成
        }
        else
        {
            Debug.LogError($"Coroutine method returned null: {method.Name}");
        }

        callback?.Invoke(); // 协程结束后触发回调
    }
    private object[] ConstructArguments(ParameterInfo[] parameters, UnityClient.ActionData actionData)
    {
        Debug.Log("parameters length : "+parameters.Length);
        if (parameters.Length == 0) return null;

        List<object> args = new List<object>();

        foreach (var param in parameters)
        {
            Debug.Log(param.Name.ToLower());
            // 根据参数名称显式映射
            switch (param.Name.ToLower())
            {
                case "stateid":
                    if (actionData.stateID != null)
                    {
                        args.Add(actionData.stateID);
                    }
                    else
                    {
                        Debug.LogError("State ID is not Valid");
                    }
                   
                    break;
                case "objectid":
                    if (actionData.objectID != null)
                    {
                        args.Add(actionData.objectID);
                    }
                    else
                    {
                        Debug.LogError("Object ID is Not Valid");
                    }
                    break;
                case "isleftarm":
                    Debug.Log("test log before arm");
                    Debug.Log(actionData.arm);
                    Debug.Log("test log after arm");
                    if (actionData.arm != null){
                        args.Add(actionData.arm.Equals("left", StringComparison.OrdinalIgnoreCase)); // 映射到 arm
                    }
                    else
                    {
                        args.Add(true); // 映射到 arm
                    }
                    break;
                case "magnitude":
                    if (actionData.Magnitude != null)
                    {
                        args.Add(actionData.Magnitude);
                    }
                    else
                    {
                        Debug.LogError("Magnitude is not Set");
                    }
                    break;
                default:
                    Debug.LogWarning($"Unsupported parameter: {param.Name} of type {param.ParameterType.Name}");
                    args.Add(null); // 默认值
                    break;
            }
        }
        Debug.Log("args length : " + args.Count);
        return args.ToArray();
    }


    public IEnumerator Toggle(string objectID, bool isLeftArm)
    {
        
        // 获取目标交互点和 Toggle 脚本
        Transform interactPoint = SceneManager.GetInteractablePoint(objectID);
        CanToggleOnOff toggleScript = interactPoint?.GetComponentInParent<CanToggleOnOff>();
        
        Debug.Log(interactPoint);
        Debug.Log(toggleScript);
        // 如果尚未到达目标位置，执行移动
        if (!hasMovedToPosition && interactPoint != null)
        {
            yield return StartCoroutine(MoveToPosition(interactPoint.position, isLeftArm));
            yield return new WaitForSeconds(1f);
            hasMovedToPosition = true; // 标记为已到达
        }

        // 切换开关
        toggleScript?.Toggle();
        yield return new WaitForSeconds(1f);
    }

    public IEnumerator Open(string objectID, bool isLeftArm)
    {
        // 获取目标交互点和 Open 脚本
        Transform interactPoint = SceneManager.GetInteractablePoint(objectID);
        CanOpen_Object openScript = interactPoint?.GetComponentInParent<CanOpen_Object>();

        // 如果尚未到达目标位置，执行移动
        if (!hasMovedToPosition && interactPoint != null)
        {
            yield return StartCoroutine(MoveToPosition(interactPoint.position, isLeftArm));
            yield return new WaitForSeconds(1f);
            hasMovedToPosition = true; // 标记为已到达
        }

        // 打开对象
        openScript?.Interact();
        yield return new WaitForSeconds(1f);
    }

    public void TP(string objectID)
    {
        // 查找物品的 TransferPoint
        Transform transferPoint = SceneManager.GetTransferPointByObjectID(objectID);

        if (transferPoint == null)
        {
            Debug.LogError($"TP action failed: objectID '{objectID}' not found.");
            return;
        }

        // 禁用机器人所有关节的 ArticulationBody
        DisableArticulationBodies();

        // 直接修改机器人的位置和旋转
        Debug.Log($"Robot directly transported to {objectID}'s TransferPoint: {transferPoint.position}");
        transform.position = transferPoint.position;
        transform.rotation = transferPoint.rotation;

        // 启用机器人所有关节的 ArticulationBody
        EnableArticulationBodies();

        Debug.Log($"Robot successfully transported to {objectID}'s TransferPoint");
    }

    public IEnumerator Pick(string objectID, bool isLeftArm)
    {
        Vector3 offset = new Vector3(0, 0.1f, 0);
        Transform pickPosition = SceneManager.GetInteractablePoint(objectID);

        if (pickPosition == null)
        {
            Debug.LogError($"未找到ID为 {objectID} 的物品的默认交互点，无法执行Pick动作");
            yield break;
        }

        Vector3 abovePickPosition = pickPosition.position + offset;

        // 移动到夹取位置上方
        Debug.Log($"移动到{(isLeftArm ? "左臂" : "右臂")}夹取位置上方: {abovePickPosition}");
        yield return StartCoroutine(MoveToPosition(abovePickPosition, isLeftArm));
        yield return new WaitForSeconds(1f);

        // 打开夹爪准备夹取
        Debug.Log($"打开{(isLeftArm ? "左臂" : "右臂")}夹爪准备夹取");
        gripperController.SetGripper(isLeftArm, true);
        yield return new WaitForSeconds(1f);

        // 下降到夹取位置
        Debug.Log($"下降到{(isLeftArm ? "左臂" : "右臂")}夹取位置: {pickPosition.position}");
        yield return StartCoroutine(MoveToPosition(pickPosition.position, isLeftArm));
        yield return new WaitForSeconds(1f);

        // 夹紧物体
        Debug.Log($"{(isLeftArm ? "左臂" : "右臂")}夹紧物体");
        gripperController.SetGripper(isLeftArm, false);
        yield return new WaitForSeconds(1f);
        

        Debug.Log($"移动到{(isLeftArm ? "左臂" : "右臂")}夹取位置上方: {abovePickPosition}");
        yield return StartCoroutine(MoveToPosition(abovePickPosition, isLeftArm));
        yield return new WaitForSeconds(1f);
        
        sceneManager.SetParent(gripperController.transform, objectID);
    }

    public IEnumerator Place(string objectID, bool isLeftArm)
    {
        // 根据是否是左臂设置偏移量
        Vector3 offset = isLeftArm ? new Vector3(0, 0.1f, -0.13f) : new Vector3(0, 0.1f, 0.13f);
        Transform pickPosition = SceneManager.GetInteractablePoint(objectID);

        if (pickPosition == null)
        {
            Debug.LogError($"未找到ID为 {objectID} 的物品的默认交互点，无法执行Place动作");
            yield break;
        }

        Vector3 placePosition = pickPosition.position + offset; // 基于Pick的位置偏移

        // 移动到放置位置
        Debug.Log($"移动至{(isLeftArm ? "左臂" : "右臂")}放置位置: {placePosition}");
        yield return MoveToPosition(placePosition, isLeftArm);
        yield return new WaitForSeconds(1f);

        // 打开夹爪放置物体
        Debug.Log($"打开{(isLeftArm ? "左臂" : "右臂")}夹爪放置物体");
        gripperController.SetGripper(isLeftArm, true);
        sceneManager.Release(objectID);
        yield return new WaitForSeconds(1f);
    }
    public IEnumerator ResetJoint(bool isLeftArm)
    {
        Debug.Log($"{(isLeftArm ? "左臂" : "右臂")}关节正在重置到初始位置...");

        List<float> initialAngles = new List<float>();
        var adjustments = isLeftArm ? this.adjustments : rightAdjustments;
        var joints = isLeftArm ? leftArmJoints : rightArmJoints;

        for (int i = 0; i < joints.Count; i++)
        {
            float initialAngle = (i == 0 || i == 4 || i == 3) ? -adjustments[i].angle : adjustments[i].angle;
            initialAngles.Add(initialAngle);
        }

        yield return StartCoroutine(SmoothUpdateJointAngles(initialAngles, 2f, isLeftArm));

        Debug.Log($"{(isLeftArm ? "左臂" : "右臂")}关节已成功重置！");

        hasMovedToPosition = false; // 标记为已到达

    }

    private void InitializeAdjustments(bool isLeftArm)
    {
        var joints = isLeftArm ? leftArmJoints : rightArmJoints;
        var defaultRotations = isLeftArm ? this.defaultRotations : rightDefaultRotations;
        var adjustments = isLeftArm ? this.adjustments : rightAdjustments;

        string logMessage = $"{(isLeftArm ? "左臂" : "右臂")}初始化关节调整信息：\n默认值:\n";

        for (int i = 0; i < joints.Count; i++)
        {
            var joint = joints[i];
            Vector3 initialRotation = joint.transform.localRotation.eulerAngles;

            // 规范化读取到的角度
            initialRotation.x = NormalizeAngle(initialRotation.x);
            initialRotation.y = NormalizeAngle(initialRotation.y);
            initialRotation.z = NormalizeAngle(initialRotation.z);

            Vector3 defaultRotation = defaultRotations[i];
            float adjustmentAngle = (i == 0 || i == 4)
                ? defaultRotation.y - initialRotation.y
                : defaultRotation.x - initialRotation.x;

            adjustmentAngle = NormalizeAngle(adjustmentAngle);
            adjustments[i].angle = adjustmentAngle;

            logMessage += $"关节 {i + 1} 默认旋转: {defaultRotation}\n" +
                          $"关节 {i + 1} 初始旋转: {initialRotation}\n" +
                          $"关节 {i + 1} 调整角度: {adjustmentAngle}\n";
        }

        Debug.Log(logMessage);
    }


    private void UpdateTargetJointAngles(List<float> updatedAngles)//事件
    {
        targetJointAngles = updatedAngles;
        isTargetAnglesUpdated = true; // 标记角度已更新
    }

    private IEnumerator MoveToPosition(Vector3 position, bool isLeftArm)
    {
        // 请求计算目标角度
        // ikClient.ProcessTargetPosition(position, isLeftArm);
        if (CurrentRobotType == RobotType.X1)
        {
            ikX1.ProcessTargetPosition(position, isLeftArm);
            // 等待目标角度更新
            yield return new WaitUntil(() => isTargetAnglesUpdated);

            //Debug.Log("MoveToPosition 中的目标角度: " + string.Join(", ", targetJointAngles));

            // 重置标记
            isTargetAnglesUpdated = false;

            yield return StartCoroutine(SmoothUpdateJointAngles(targetJointAngles, 2f, isLeftArm));

            // 清理状态，防止后续动作受影响
            targetJointAngles.Clear();
        }else if (CurrentRobotType == RobotType.H1)
        {
            ikH1.ProcessTargetPosition(position, isLeftArm);
            
        }



    }

    public IEnumerator SmoothUpdateJointAngles(List<float> targetJointAngles, float duration, bool isLeftArm)
    {
        //Debug.Log("进入 SmoothUpdateJointAngles 时的目标角度: " + string.Join(", ", targetJointAngles));

        List<float> startAngles = new List<float>();
        var joints = isLeftArm ? leftArmJoints : rightArmJoints;
        var adjustments = isLeftArm ? this.adjustments : rightAdjustments;

        foreach (var joint in joints)
        {
            startAngles.Add(NormalizeAngle(joint.xDrive.target));
        }

        float elapsedTime = 0f;

        while (elapsedTime < duration)
        {
            elapsedTime += Time.deltaTime;
            float t = Mathf.Clamp01(elapsedTime / duration);

            for (int i = 0; i < joints.Count; i++)
            {
                var joint = joints[i];
                var drive = joint.xDrive;
                //关节1和5是Y轴，且反向调整
                float adjustedAngle = NormalizeAngle(targetJointAngles[i] + ((i == 0 || i == 4) ? -adjustments[i].angle : adjustments[i].angle));

                float interpolatedAngle = Mathf.Lerp(startAngles[i], adjustedAngle, t);

                drive.target = NormalizeAngle(interpolatedAngle);
                joint.xDrive = drive;

                //Debug.Log($"插值中: 关节 {i + 1}, 初始={startAngles[i]}, 调整后目标={adjustedAngle}, 插值={interpolatedAngle}, xDrive={drive.target}");
            }

            yield return null;
        }

        for (int i = 0; i < joints.Count; i++)
        {
            var joint = joints[i];
            var drive = joint.xDrive;

            float finalAdjustedAngle = NormalizeAngle(targetJointAngles[i] + ((i == 0 || i == 4) ? -adjustments[i].angle : adjustments[i].angle));

            drive.target = finalAdjustedAngle;
            joint.xDrive = drive;

            //Debug.Log($"关节 {i + 1} 最终目标角度 (度): {finalAdjustedAngle}");
        }
    }


    // 平滑移动的协程，改为使用局部坐标系的方向
    private IEnumerator SmoothMove(Vector3 localDirection, float magnitude, float duration)
    {
        DisableArticulationBodies();

        Vector3 startPosition = transform.position;
        Vector3 targetPosition = startPosition + transform.TransformDirection(localDirection) * moveSpeed * magnitude;

        float elapsedTime = 0f;

        while (elapsedTime < duration)
        {
            transform.position = Vector3.Lerp(startPosition, targetPosition, elapsedTime / duration);
            elapsedTime += Time.deltaTime;
            yield return null;
        }

        transform.position = targetPosition; // 确保到达目标位置
        EnableArticulationBodies();
    }

    // 平滑旋转的协程，使用局部坐标系的方向
    private IEnumerator SmoothRotate(Vector3 rotationAxis, float magnitude, float duration)
    {
        DisableArticulationBodies();

        // 使用旋转角度逐步累加方式
        float targetAngle = rotationSpeed * magnitude;
        float elapsedTime = 0f;

        while (elapsedTime < duration)
        {
            float stepAngle = (targetAngle / duration) * Time.deltaTime; // 按每帧增量调整旋转角度
            transform.Rotate(rotationAxis, stepAngle, Space.Self);

            elapsedTime += Time.deltaTime;
            yield return null;
        }

        EnableArticulationBodies();
    }

    public IEnumerator MoveAhead(float Magnitude, Action callback = null)
    {
        yield return SmoothMove(Vector3.forward, Magnitude, 1.0f); 
        callback?.Invoke(); 
    }

    public IEnumerator MoveRight(float Magnitude, Action callback = null)
    {
        yield return SmoothMove(Vector3.right, Magnitude, 1.0f);
        callback?.Invoke();
    }

    public IEnumerator MoveBack(float Magnitude, Action callback = null)
    {
        yield return SmoothMove(Vector3.back, Magnitude, 1.0f);
        callback?.Invoke();
    }

    public IEnumerator MoveLeft(float Magnitude, Action callback = null)
    {
        yield return SmoothMove(Vector3.left, Magnitude, 1.0f);
        callback?.Invoke();
    }
    public IEnumerator MoveUp(float Magnitude, Action callback = null)
    {
        yield return SmoothMove(Vector3.up, Magnitude * 0.1f, 1.0f);
        callback?.Invoke();
    }

    public IEnumerator MoveDown(float Magnitude, Action callback = null)
    {
        yield return SmoothMove(Vector3.down, Magnitude * 0.1f, 1.0f);
        callback?.Invoke();
    }
    public IEnumerator RotateRight(float Magnitude, Action callback = null)
    {
        yield return SmoothRotate(Vector3.up, Mathf.Abs(Magnitude), 1.0f);
        callback?.Invoke();
    }

    public IEnumerator RotateLeft(float Magnitude, Action callback = null)
    {
        yield return SmoothRotate(Vector3.up, -Mathf.Abs(Magnitude), 1.0f);
        callback?.Invoke();
    }
    public void Undo()
    {
        DisableArticulationBodies();
        sceneManager.Undo();
        EnableArticulationBodies();
    }
    public void Redo()
    {
        DisableArticulationBodies();
        sceneManager.Redo();
        EnableArticulationBodies();
    }

    // 调用 SceneManager 的 LoadStateByIndex 方法
    public void LoadState(string stateID)
    {
        Debug.Log($"Attempting to load scene state with ID: {stateID}");
        DisableArticulationBodies();
        sceneManager.LoadStateByIndex(stateID);
        EnableArticulationBodies();
    }
    public void DisableArticulationBodies()
    {
        foreach (ArticulationBody body in articulationChain)
        {
            body.enabled = false;
        }
    }
    public void EnableArticulationBodies()
    {
        foreach (ArticulationBody body in articulationChain)
        {
            body.enabled = true;
        }
    }
    public float NormalizeAngle(float angle)
    {
        while (angle > 180) angle -= 360;
        while (angle < -180) angle += 360;
        return angle;
    }
    private bool Probability(float successRate)
    {
        return UnityEngine.Random.value < successRate; // 使用传入的 successRate 进行判断
    }
    private void ManualControl()
    {
        // ALT键按下时显示鼠标用于UI交互
        if (Input.GetKey(KeyCode.LeftAlt))
        {
            Cursor.visible = true;
            Cursor.lockState = CursorLockMode.None;
        }
        else if (!isMouseUnlocked) // ALT释放后重新锁定鼠标
        {
            Cursor.visible = false;
            Cursor.lockState = CursorLockMode.Locked;
        }

        // 获取基础移动输入
        Vector3 moveDirection = new Vector3(
            Input.GetKey(KeyCode.D) ? 1 : Input.GetKey(KeyCode.A) ? -1 : 0,
            0,
            Input.GetKey(KeyCode.W) ? 1 : Input.GetKey(KeyCode.S) ? -1 : 0
        );

        // 计算实际移动速度（按住Shift加速）
        float currentSpeed = manualMoveSpeed * (Input.GetKey(KeyCode.LeftShift) ? sprintMultiplier : 1.0f);

        // 应用移动
        if (moveDirection != Vector3.zero)
            transform.position += transform.TransformDirection(moveDirection.normalized) * currentSpeed * Time.deltaTime;

        // 获取鼠标输入
        float mouseX = Input.GetAxis("Mouse X") * mouseSensitivity;
        float mouseY = Input.GetAxis("Mouse Y") * mouseSensitivity;

        // 更新垂直旋转角度
        verticalRotation -= mouseY; // 注意这里是减号，因为Unity的坐标系统中向上是正方向
        verticalRotation = Mathf.Clamp(verticalRotation, -maxVerticalAngle, maxVerticalAngle);

        // 应用旋转
        transform.Rotate(Vector3.up * mouseX, Space.World); // 整个物体的水平旋转
        cameraTransform.localRotation = Quaternion.Euler(verticalRotation, 0, 0); // 只旋转相机的上下视角
    }

    public void LoadRobot(string robotType)
    {
        Debug.Log($"Loading robot of type: {robotType}");
        foreach (var robot in robots)
        {
            robot.SetActive(false);
        }

        switch (robotType.ToLower())
        {
            case "x1":
                robots[0].SetActive(true);
                initGame();
                cameraTransform = Camera.main.transform;
                CurrentRobotType = RobotType.X1;
                InitializeAdjustments(true);
                InitializeAdjustments(false);
                ikX1.OnTargetJointAnglesUpdated += UpdateTargetJointAngles;
                break;
            case "h1":
                robots[1].SetActive(true);
                initGame();
                cameraTransform = Camera.main.transform;
                CurrentRobotType = RobotType.H1;
                
                ikH1.IniitTarget();
                // InitializeAdjustments(true);
                // InitializeAdjustments(false);
                // ikClient.OnTargetJointAnglesUpdated += UpdateTargetJointAngles;
                break;
            case "g1":
                robots[2].SetActive(true);
                CurrentRobotType = RobotType.G1;
                break;
            default:
                Debug.LogError($"Unknown robot type: {robotType}");
                break;
        }
    }
}