using System;
using System.Reflection;
using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using Agent;
using Unity.Collections;
using Unity.Robotics.UrdfImporter;
using UnityEngine.SceneManagement;
using NUnit.Framework.Constraints;
using System.IO;
using Newtonsoft.Json;
using Unity.VisualScripting;


public class AgentMovement : MonoBehaviour
{
    

    public SceneStateManager sceneManager;
    public float moveSpeed = 1.0f;
    public float rotationSpeed = 90.0f;  // 每秒旋转的角度
    public GripperController gripperController; 
    public HandController handController;

    public ArticulationBody[] articulationChain;
    public List<GameObject> collidedObjects = new List<GameObject>();
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

    // private PhysicsScene physicsScene;

    public List<float> targetJointAngles = new List<float> { 0, 0, 0, 0, 0, 0 }; // 初始值
    private List<float> initialJointAngles = new List<float>();
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
    public ArticulationBody rootArt;
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

    public bool collisionDetected=false;

    public string collisionA;
    public string collisionB;

    private string robottype="";

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

    private List<ArticulationDrive> defaultDrives = new List<ArticulationDrive>();
    private List<ArticulationDrive> tempDrives = new List<ArticulationDrive>();


    public class ActionMap{
        public Dictionary<string,ActionState> actionmap;
    }

    public class ActionState{
        public Dictionary<string,float>actionstate;
    }

    public Dictionary<string,ActionMap> propertyMap;

    // 添加用于跟踪最后一次移动是否成功的公共变量
    public bool lastMoveSuccessful = true;

    // 在类的成员变量部分添加
    // 用于记录当前正在交互的物体ID
    private string currentInteractingObjectID = string.Empty;
    private List<string> ignoredCollisionObjects = new List<string>();

    void Start()
    {
       Loadpropertymap();
    }


    private void Loadpropertymap(){
        string path = Path.Combine(Application.streamingAssetsPath, "ActionOutcomeConfig.json");
        if (File.Exists(path))
        {
            string json = File.ReadAllText(path);
            var actionOutcomeConfig = JsonConvert.DeserializeObject<Dictionary<string, Dictionary<string, Dictionary<string, float>>>>(json);
            propertyMap = actionOutcomeConfig.ToDictionary(
                category => category.Key,
                category => new ActionMap
                {
                    actionmap = category.Value.ToDictionary(
                        action => action.Key,
                        action => new ActionState
                        {
                            actionstate = new Dictionary<string, float>(action.Value)
                        }
                    )
                }
            );

            // 打印调试信息
            // Debug.Log("Loaded ActionOutcomeConfig:");
            // foreach (var category in propertyMap)
            // {
            //     Debug.Log($"Category: {category.Key}");
            //     foreach (var action in category.Value.actionmap)
            //     {
            //         Debug.Log($"  Action: {action.Key}");
            //         foreach (var outcome in action.Value.actionstate)
            //         {
            //             Debug.Log($"    Outcome: {outcome.Key}, Probability: {outcome.Value}");
            //         }
            //     }
            // }
        }
        else
        {
            Debug.LogError("ErrorConfig.json not found!");
        }
    }
    public void SetRobot(){
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

        // 记录默认的 xDrive 值
        defaultDrives.Clear();
        foreach (ArticulationBody body in articulationChain)
        {
            if (body.jointType != ArticulationJointType.FixedJoint)
            {
                defaultDrives.Add(body.xDrive);
            }
            if(body.name=="root"){
                Debug.Log("get root link");
                sceneManager.root=body;
                rootArt=body;
            }
            if (body.jointType == ArticulationJointType.RevoluteJoint) // 仅在 RevoluteJoint 类型时执行
            {
                Transform collisionsTransform = body.transform.Find("Collisions");
                if (collisionsTransform != null)
                {
                    foreach (Collider collider in collisionsTransform.GetComponentsInChildren<Collider>())
                    {
                        GameObject colliderObj = collider.gameObject;

                        // // // 添加刚体组件
                        Rigidbody rb = colliderObj.GetComponent<Rigidbody>();
                        if (rb == null)
                        {
                            rb = colliderObj.AddComponent<Rigidbody>();
                            // rb.isKinematic = true; // 设置为运动学刚体以避免物理引擎影响
                            rb.constraints = RigidbodyConstraints.FreezeAll; // 冻结所有位置和旋转
                        }

                        // 添加碰撞处理器
                        //colliderObj.AddComponent<CollisionReporter>();
                        // 添加碰撞处理器
                        CollisionHandler handler = colliderObj.AddComponent<CollisionHandler>();
                        // Debug.Log("add collision Handler");
                        handler.OnCollisionEnterEvent += (collision) => HandleCollision(body, collision, colliderObj);
                    }
                }
            }
        }
    }



    // 修改HandleCollision方法，添加colliderObj参数
    private void HandleCollision(ArticulationBody articulationBody, Collision collision, GameObject colliderObj)
    {
        // Debug.Log($"碰撞检测: ArticulationBody: {articulationBody.name}, Collider: {colliderObj.name}, 碰撞对象: {collision.gameObject.name}");
        
        // 添加到碰撞列表
        if (!collidedObjects.Contains(collision.gameObject))
        {
            collidedObjects.Add(collision.gameObject);
        }

        foreach(var obj in sceneManager.ObjectsInOperation){
            if(collidedObjects.Contains(obj.gameObject)){

            }else{
                Debug.Log("Haven't interacted with object "+obj.gameObject.name);
            }
        }
        
        // 检查是否是与当前交互物体的碰撞，使用RobotCollisionManager的判断逻辑
        bool isInteractingWithTarget = false;
        
        // 如果RobotCollisionManager存在，使用它的判断
        if (RobotCollisionManager.Instance != null)
        {
            isInteractingWithTarget = RobotCollisionManager.Instance.IsInteractingObject(collision.gameObject);
            if (isInteractingWithTarget)
            {
                Debug.Log($"与交互目标物体碰撞，不触发失败 (由RobotCollisionManager判断)");
            }
        }
        else
        {
            // 作为备份，保留原来的判断逻辑
            SimObjPhysics collisionPhysics = collision.gameObject.GetComponent<SimObjPhysics>();
            if (collisionPhysics != null && 
                (collisionPhysics.ObjectID == currentInteractingObjectID || 
                 ignoredCollisionObjects.Contains(collisionPhysics.ObjectID)))
            {
                isInteractingWithTarget = true;
                Debug.Log($"与交互目标物体碰撞，ID: {collisionPhysics.ObjectID}, 不触发失败 (由本地判断)");
            }
        }
        
        if(!collisionDetected && !isInteractingWithTarget){
            for(int i=0;i<collidedObjects.Count;i++){
                if(!sceneManager.ObjectsInOperation.Contains(collidedObjects[i])){
                    // Debug.Log($"检测到异常碰撞: ArticulationBody: {articulationBody.name}, Collider: {colliderObj.name}, 碰撞对象: {collision.gameObject.name}");
                    collisionDetected = true;
                    collisionA = articulationBody.name;
                    collisionB = collision.gameObject.name;
                    
                    // 通知RobotCollisionManager记录这个碰撞
                    if (RobotCollisionManager.Instance != null)
                    {
                        RobotCollisionManager.Instance.ReportCollision(articulationBody, collision.gameObject);
                    }
                }
            }
        }
    }

    public void ClearCollisions(){
        // 清理物理碰撞状态
        collisionDetected = false;
        collisionA = string.Empty;
        collisionB = string.Empty;
        
        // 清理碰撞物体列表
        collidedObjects.Clear();
        
        // 清理当前交互物体ID
        currentInteractingObjectID = string.Empty;
        
        // 清理RobotCollisionManager中的碰撞状态
        if (RobotCollisionManager.Instance != null)
        {
            RobotCollisionManager.Instance.ClearAllCollisions();
            Debug.Log("已清理RobotCollisionManager中的所有碰撞");
        }
        
        // 清理射线碰撞状态
        // 查找当前机器人的碰撞检测器
        GameObject currentRobot = CurrentRobotType == RobotType.H1 ? robots[1] : robots[0];
        if (currentRobot != null)
        {
            StructureCollisionDetector detector = currentRobot.GetComponent<StructureCollisionDetector>();
            if (detector != null)
            {
                // 使用公共方法清理碰撞状态
                detector.ClearCollisionState();
            }
        }
        
        // 清理所有机器人部件的碰撞状态
        foreach (var robot in robots)
        {
            if (robot != null && robot.activeSelf)
            {
                // 查找并清理所有碰撞报告器
                CollisionReporter[] reporters = robot.GetComponentsInChildren<CollisionReporter>(true);
                foreach (var reporter in reporters)
                {
                    // 在此处，可能需要添加一个方法来重置碰撞状态
                    // 或者直接处理关联的碰撞处理器
                    if (reporter != null)
                    {
                        Debug.Log($"重置碰撞报告器: {reporter.gameObject.name}");
                    }
                }
                
                // 查找所有关节并检查碰撞
                ArticulationBody[] joints = robot.GetComponentsInChildren<ArticulationBody>(true);
                foreach (var joint in joints)
                {
                    // 重置所有关节的碰撞状态
                    if (joint != null)
                    {
                        joint.gameObject.SendMessage("ClearCollision", null, SendMessageOptions.DontRequireReceiver);
                    }
                }
            }
        }
        
        Debug.Log("所有碰撞状态已清理");
    }

    public void Update(){
        if (Input.GetKeyDown(KeyCode.A))
        {
            Debug.Log("update drawer position");
            var drawer2=GameObject.Find("Kitchen_Drawer_02");
            Debug.Log("find drawer2 "+drawer2.name);
            drawer2.transform.position=new Vector3(0.6376251f,0.5475f,-0.65f);
            drawer2.transform.rotation=Quaternion.Euler(0f,-90f,0f);

            var drawer1=GameObject.Find("Kitchen_Drawer_01");
            Debug.Log("find drawer1 "+drawer1.name);
            drawer1.transform.position=new Vector3(0.6376251f,0.7556f,-0.65f);
            drawer1.transform.rotation=Quaternion.Euler(0f,-90f,0f);

            var drawer3=GameObject.Find("Kitchen_Drawer_03");
            Debug.Log("find drawer3 "+drawer3.name);
            drawer3.transform.position=new Vector3(0.6376251f,0.26875f,-0.65f);
            drawer3.transform.rotation=Quaternion.Euler(0f,-90f,0f);
            
            
        }



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

            // joint.gameObject.AddComponent<CollisionHandler>().OnCollisionEnterEvent += (collision) => HandleCollision(joint, collision);
  
        }

    }

    public void ExecuteActionWithCallback(UnityClient.ActionData actionData, Action<JsonData> callback)
    {
        
        Debug.Log($"Executing action: {actionData.action} with arm: {actionData.arm}, objectID: {actionData.objectID}, magnitude: {actionData.magnitude}");
        JsonData jsonData = new JsonData();

        // 先在场景状态管理器中更新最后执行的动作名称
        sceneManager.UpdateLastAction(actionData.action);

        // 根据动作类型和物体ID，先检查动作成功率
        bool shouldExecuteAction = true;
        string errorMessage = string.Empty;
        string targetState = string.Empty;

        // 检查是否是需要进行成功率检查的动作类型
        if (NeedsSuccessRateCheck(actionData.action))
        {
            // 获取动作配置信息
            var config = sceneManager.GetActionConfig(actionData.action, actionData.objectID);

            // 随机判断成功或失败
            float randomValue = UnityEngine.Random.value;
            // float randomValue = 0.98f;
            if (randomValue > config.successRate)
            {
                // 如果随机值大于成功率，则动作失败
                shouldExecuteAction = false;
                
                (errorMessage, targetState) = config.GetRandomErrorMessage();
                Debug.Log($"动作 {actionData.action} 成功率检查结果: 失败，错误消息: {errorMessage}，目标状态: {targetState}");
            }
            else
            {
                // 如果随机值小于等于成功率，则动作成功
                shouldExecuteAction = true;
                Debug.Log($"动作 {actionData.action} 成功率检查结果: 成功");
            }
        }
        
        if (!shouldExecuteAction)
        {
            // 如果动作不应执行，但有目标状态，尝试根据目标状态调用对应方法
            if (!string.IsNullOrEmpty(targetState) && !string.IsNullOrEmpty(actionData.objectID))
            {
                // 获取目标物体
                GameObject targetObject = null;
                if (sceneManager.SimObjectsDict.TryGetValue(actionData.objectID, out targetObject) && targetObject != null)
                {
                    bool stateExecuted = false;
                    
                    // 根据目标状态找到相应的组件
                    IStateComponent stateComponent = null;
                    
                    switch (targetState.ToLower())
                    {
                        case "broken":
                            stateComponent = targetObject.GetComponent<Break>();
                            break;
                        case "dirty":
                            stateComponent = targetObject.GetComponent<Dirty>();
                            break;
                        case "spilled":
                            stateComponent = targetObject.GetComponent<Spill>();
                            break;
                        // 可以添加更多状态...
                    }
                    
                    // 如果找到了相应的组件，执行状态变化
                    if (stateComponent != null)
                    {
                        stateComponent.Execute();
                        stateExecuted = true;
                        Debug.Log($"自动执行了物体 {actionData.objectID} 的 {targetState} 状态变化");
                    }
                    
                    // 如果成功执行了状态变化，更新结果
                    if (stateExecuted)
                    {
                        jsonData.success = true;
                        jsonData.msg = $"已自动处理物体状态变为: {targetState}";
                        callback?.Invoke(jsonData);
                        return;
                    }
                }
            }
            
            // 如果无法自动处理状态变化，返回原始失败结果
            jsonData.success = false;
            jsonData.msg = errorMessage;
            
            // 更新动作执行状态
            sceneManager.UpdateLastActionSuccess(actionData.action, actionData.objectID);
            
            // 调用回调函数
            callback?.Invoke(jsonData);
            return;
        }

        // 以下是原有的动作处理逻辑
        try
        {
            Type thisType = this.GetType();
            
            // 检查参数是否有效
            if (string.IsNullOrEmpty(actionData.action))
            {
                Debug.LogError("Action name cannot be null or empty.");
                jsonData.success = false;
                jsonData.msg = "Action name cannot be null or empty.";
                callback?.Invoke(jsonData);
                return;
            }

            // 方法名处理
            string methodName = actionData.action;

            // 尝试获取方法信息
            MethodInfo method = thisType.GetMethod(methodName, BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance | BindingFlags.IgnoreCase);
            
            if (method == null)
            {
                Debug.LogError($"Method {methodName} not found in {thisType.Name}.");
                jsonData.success = false;
                jsonData.msg = $"Method {methodName} not found.";
                callback?.Invoke(jsonData);
                return;
            }

            ClearCollidedObjects(); // 清除之前的碰撞对象
            
            // 获取方法参数
            ParameterInfo[] parameters = method.GetParameters();
            object[] args = ConstructArguments(parameters, actionData);

            // 设置最后动作结果为成功（默认）
            lastMoveSuccessful = true;

            // 根据方法返回类型决定调用方式
            if (method.ReturnType == typeof(IEnumerator))
            {
                // 协程方法
                if (method.Name.Equals("Open", StringComparison.OrdinalIgnoreCase))
                {
                    StartCoroutine(Open((string)args[0], (bool)args[1], callback));
                }
                else if (method.Name.Equals("Pick", StringComparison.OrdinalIgnoreCase))
                {
                    StartCoroutine(Pick((string)args[0], (bool)args[1], callback));
                }
                else if (method.Name.Equals("Place", StringComparison.OrdinalIgnoreCase))
                {
                    StartCoroutine(Place((string)args[0], (bool)args[1], callback));
                }
                else if (method.Name.Equals("Toggle", StringComparison.OrdinalIgnoreCase))
                {
                    StartCoroutine(Toggle((string)args[0], (bool)args[1], callback));
                }
                else
                {
                    StartCoroutine(ExecuteCoroutineAction(method, args, actionData, callback));
                }
            }
            else if (method.ReturnType == typeof(JsonData))
            {
                // 直接返回JsonData的方法
                jsonData = (JsonData)method.Invoke(this, args);
                callback?.Invoke(jsonData);
            }
            else
            {
                // 其他方法（假设返回bool或void）
                object result = method.Invoke(this, args);
                
                if (result is bool boolResult)
                {
                    jsonData.success = boolResult;
                    jsonData.msg = boolResult ? "Action executed successfully." : "Action failed.";
                }
                else
                {
                    jsonData.success = true;
                    jsonData.msg = "Action executed.";
                }
                
                // 更新动作执行状态
                bool actionSuccess = sceneManager.UpdateLastActionSuccess(actionData.action, actionData.objectID);
                jsonData.success = actionSuccess;
                
                if (!actionSuccess)
                {
                    jsonData.msg = sceneManager.GetCurrentSceneStateA2T().agent.errorMessage;
                }
                
                callback?.Invoke(jsonData);
            }
        }
        catch (Exception e)
        {
            Debug.LogError($"Error executing action: {e.Message}\n{e.StackTrace}");
            jsonData.success = false;
            jsonData.msg = $"Error: {e.Message}";
            callback?.Invoke(jsonData);
        }
    }
    
    // 添加：判断是否需要进行成功率检查的辅助方法
    private bool NeedsSuccessRateCheck(string actionName)
    {
        if (string.IsNullOrEmpty(actionName))
            return false;
            
        // 列出不需要进行成功率检查的动作类型
        string[] actionsToSkip = {
            "undo", "redo", "loadstate", "loadrobot", 
            "resetpose", "resetscene", "getcurstate", "resetstate"
        };
        
        // 检查是否是需要跳过的动作
        foreach (var skipAction in actionsToSkip)
        {
            if (actionName.ToLower().Equals(skipAction.ToLower()))
                return false;
        }
            
        // 列出需要进行成功率检查的动作类型
        string[] actionsToCheck = {
            "pick", "place", "toggle", "open",
            "slice", "break", "fill", "empty",
            "cook", "clean", "move", "rotate"
        };
        
        // 检查动作名称是否在列表中（不区分大小写）
        foreach (var action in actionsToCheck)
        {
            if (actionName.ToLower().Contains(action.ToLower()))
                return true;
        }
        
        // 默认情况下不检查
        return false;
    }

    // 定义JsonData类来存储返回结果
    [Serializable]
    public class JsonData
    {
        public bool success;
        public string msg;
    }

    private IEnumerator ExecuteCoroutineAction(MethodInfo method, object[] args, UnityClient.ActionData actionData, Action<JsonData> callback)
    {
        Debug.Log($"Starting coroutine action: {actionData.action}");
        JsonData jsonData = new JsonData();
        IEnumerator coroutine = null;
        
        try
        {
            // 调用协程方法但不等待其完成
            coroutine = (IEnumerator)method.Invoke(this, args);
        }
        catch (Exception e)
        {
            Debug.LogError($"Error in coroutine action {actionData.action}: {e.Message}\n{e.StackTrace}");
            jsonData.success = false;
            jsonData.msg = $"Error: {e.Message}";
            callback?.Invoke(jsonData);
            yield break;
        }
        
        // 在try-catch块外执行协程
        if (coroutine != null)
        {
            // 执行协程直到完成
            while (coroutine.MoveNext())
            {
                yield return coroutine.Current;
            }
            
            // 检查协程的最终结果
            if (coroutine.Current is JsonData coroutineResult)
            {
                jsonData = coroutineResult;
            }
            else
            {
                // 如果协程没有返回特定结果，则认为成功
                jsonData.success = true;
                jsonData.msg = $"Action {actionData.action} completed successfully.";
            }
            
            Debug.Log($"Coroutine action completed: {actionData.action}, initial success: {jsonData.success}");
        }
        else
        {
            // 如果协程为空
            jsonData.success = false;
            jsonData.msg = "无法执行动作：协程初始化失败";
            Debug.LogError($"协程初始化失败: {actionData.action}");
        }
        
        // 调用回调函数，传递结果
        callback?.Invoke(jsonData);
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
                    if (actionData.magnitude != null)
                    {
                        Debug.Log("magnitude: "+actionData.magnitude);
                        args.Add(actionData.magnitude);
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


    public IEnumerator Toggle(string objectID, bool isLeftArm, Action<JsonData> callback)
    {
        // 记录当前交互对象
        SetCurrentInteractingObject(objectID);
        
        // 执行前检查是否已存在碰撞
        if (collisionDetected || (RobotCollisionManager.Instance != null && RobotCollisionManager.Instance.HasAnyCollision()))
        {
            Debug.LogWarning("Toggle动作开始前已检测到碰撞，但将继续执行");
            // 不再中断操作
        }
        
        // 标记为需要检查碰撞，但继续执行
        collisionDetected = false;
        
        // 重置状态标记
        hasMovedToPosition = false;

        // 获取对象的交互点
        Transform interactablePoint = SceneStateManager.GetInteractablePoint(objectID);
        if (interactablePoint == null)
        {
            Debug.LogError($"未找到ID为 {objectID} 的物品的交互点");
            // 新增：构造失败结果并回调
            JsonData jsonData = new JsonData();
            jsonData.success = false;
            jsonData.msg = $"未找到ID为 {objectID} 的物品的交互点";
            callback?.Invoke(jsonData);
            yield break;
        }

        // 机械臂移动到该位置
        yield return StartCoroutine(ArmMovetoPosition(interactablePoint.position, isLeftArm));

        // 检查移动过程中是否发生碰撞
        if (collisionDetected || (RobotCollisionManager.Instance != null && RobotCollisionManager.Instance.HasAnyCollision()))
        {
            Debug.LogWarning("移动到交互点时发生碰撞，但将继续执行");
            // 不再中断操作
        }

        // 等待一小段时间
        yield return new WaitForSeconds(0.5f);
        
        // 清理标记
        collisionDetected = false;
        collisionA = "";
        collisionB = "";

        // 获取夹爪控制器
        GripperController gripperController = GetComponent<GripperController>();

        // 提前检查夹爪是否到达目标位置
        Transform gripperTransform = null;
        if (isLeftArm && gripperController.currentLeftLeftGripper != null)
        {
            gripperTransform = gripperController.currentLeftLeftGripper.transform;
        }
        else if (!isLeftArm && gripperController.currentRightLeftGripper != null)
        {
            gripperTransform = gripperController.currentRightLeftGripper.transform;
        }
        
        bool reachedTargetPosition = false;
        if (gripperTransform != null)
        {
            // 检查夹爪是否在交互点附近(10厘米以内)
            float distance = Vector3.Distance(gripperTransform.position, interactablePoint.position);
            reachedTargetPosition = distance < 0.3f;
            Debug.Log($"Toggle操作: 夹爪到交互点距离为 {distance}米");
        }
        
        // 如果夹爪没有到达目标位置，提前结束协程
        if (!reachedTargetPosition)
        {
            Debug.LogError($"Toggle操作失败：夹爪未到达目标位置");
            lastMoveSuccessful = false;
            
            // 更新状态管理器中的动作结果
            if (sceneManager != null)
            {
                sceneManager.UpdateLastActionSuccess("toggle");
                // 设置错误消息
                if (sceneManager.GetCurrentSceneStateA2T() != null && sceneManager.GetCurrentSceneStateA2T().agent != null)
                {
                    sceneManager.GetCurrentSceneStateA2T().agent.errorMessage = "夹爪未到达指定位置";
                }
            }
            // 新增：构造失败结果并回调
            JsonData jsonData = new JsonData();
            jsonData.success = false;
            jsonData.msg = "夹爪未到达指定位置";
            callback?.Invoke(jsonData);
            yield break;
        }

        // 检查Toggle操作是否成功（独立于碰撞）
        bool toggleSuccess = reachedTargetPosition;
        
        // 尝试查找物体并切换状态（无论是否成功都尝试切换，以避免卡住）
        SimObjPhysics[] allObjects = FindObjectsOfType<SimObjPhysics>();
        foreach (SimObjPhysics obj in allObjects)
        {
            if (obj.ObjectID == objectID && obj.IsToggleable)
            {
                CanToggleOnOff toggleComponent = obj.GetComponent<CanToggleOnOff>();
                if (toggleComponent != null)
                {
                    toggleComponent.Toggle();
                    Debug.Log($"物体 {objectID} 的状态已切换为: {(toggleComponent.isOn ? "开启" : "关闭")}");
                    
                    // 如果物理上切换了状态，则视为成功（优先于位置判断）
                    toggleSuccess = true;
                }
                break;
            }
        }
        
        if (toggleSuccess)
        {
            Debug.Log($"Toggle操作成功：物体 {objectID} 状态已切换");
        }
        else
        {
            Debug.LogWarning($"Toggle操作失败：夹爪未到达目标物体 {objectID} 位置附近");
        }
        
        // 标记已完成
        hasMovedToPosition = true;
        
        yield return StartCoroutine(ToggleSuccess(objectID, isLeftArm));
        
        // 不管是否有碰撞，都根据Toggle成功判断返回结果
        lastMoveSuccessful = toggleSuccess;
        
        // 更新状态管理器中的动作结果
        if (sceneManager != null)
        {
            sceneManager.UpdateLastActionSuccess("toggle");
        }
        // 新增：成功时回调
        JsonData successData = new JsonData();
        successData.success = toggleSuccess;
        successData.msg = toggleSuccess ? "Toggle操作成功" : "Toggle操作失败";
        callback?.Invoke(successData);
    }

    public IEnumerator Open(string objectID, bool isLeftArm, Action<JsonData> callback)
    {
        // 记录当前交互对象
        SetCurrentInteractingObject(objectID);
        
        // 执行前检查是否已存在碰撞
        if (collisionDetected || (RobotCollisionManager.Instance != null && RobotCollisionManager.Instance.HasAnyCollision()))
        {
            Debug.LogWarning("Open动作开始前已检测到碰撞，但将继续执行");
            // 不再中断操作
        }
        
        // 标记为需要检查碰撞，但继续执行
        collisionDetected = false;
        
        // 重置状态标记
        hasMovedToPosition = false;

        // 获取对象的交互点
        Transform interactablePoint = SceneStateManager.GetInteractablePoint(objectID);
        if (interactablePoint == null)
        {
            Debug.LogError($"未找到ID为 {objectID} 的物品的交互点");
            // 新增：构造失败结果并回调
            JsonData jsonData = new JsonData();
            jsonData.success = false;
            jsonData.msg = $"未找到ID为 {objectID} 的物品的交互点";
            callback?.Invoke(jsonData);
            yield break; // 这个检查仍然保留，因为缺少交互点无法继续
        }

        // 机械臂移动到该位置
        yield return StartCoroutine(ArmMovetoPosition(interactablePoint.position, isLeftArm));

        // 检查移动过程中是否发生碰撞
        if (collisionDetected || (RobotCollisionManager.Instance != null && RobotCollisionManager.Instance.HasAnyCollision()))
        {
            Debug.LogWarning("移动到交互点时发生碰撞，但将继续执行");
            // 不再中断操作
        }

        // 等待一小段时间
        yield return new WaitForSeconds(0.5f);
        
        // 清理标记
        collisionDetected = false;
        collisionA = "";
        collisionB = "";

        // 获取夹爪控制器
        GripperController gripperController = GetComponent<GripperController>();
        
        // 提前检查夹爪是否到达目标位置
        Transform gripperTransform = null;
        if (isLeftArm && gripperController.currentLeftLeftGripper != null)
        {
            gripperTransform = gripperController.currentLeftLeftGripper.transform;
        }
        else if (!isLeftArm && gripperController.currentRightLeftGripper != null)
        {
            gripperTransform = gripperController.currentRightLeftGripper.transform;
        }
        
        bool reachedTargetPosition = false;
        if (gripperTransform != null)
        {
            // 检查夹爪是否在交互点附近(10厘米以内)
            float distance = Vector3.Distance(gripperTransform.position, interactablePoint.position);
            reachedTargetPosition = distance < 0.3f;
            Debug.Log($"Open操作: 夹爪到交互点距离为 {distance}米");
        }
        
        // 如果夹爪没有到达目标位置，提前结束协程
        if (!reachedTargetPosition)
        {
            Debug.LogError($"Open操作失败：夹爪未到达目标位置");
            lastMoveSuccessful = false;
            
            // 更新状态管理器中的动作结果
            if (sceneManager != null)
            {
                sceneManager.UpdateLastActionSuccess("open");
                // 设置错误消息
                if (sceneManager.GetCurrentSceneStateA2T() != null && sceneManager.GetCurrentSceneStateA2T().agent != null)
                {
                    sceneManager.GetCurrentSceneStateA2T().agent.errorMessage = "夹爪未到达指定位置";
                }
            }
            // 新增：构造失败结果并回调
            JsonData jsonData = new JsonData();
            jsonData.success = false;
            jsonData.msg = "夹爪未到达指定位置";
            callback?.Invoke(jsonData);
            yield break;
        }

        // 检查Open操作是否成功（独立于碰撞）
        bool openSuccess = reachedTargetPosition;
        
        // 尝试查找物体并打开（无论是否成功都尝试打开，以避免卡住）
        SimObjPhysics[] allObjects = FindObjectsOfType<SimObjPhysics>();
        foreach (SimObjPhysics obj in allObjects)
        {
            if (obj.ObjectID == objectID && obj.IsOpenable)
            {
                CanOpen_Object openComponent = obj.GetComponent<CanOpen_Object>();
                if (openComponent != null)
                {
                    bool wasOpen = openComponent.isOpen;
                    openComponent.Interact();
                    
                    // 如果状态变化了，则视为成功
                    if (wasOpen != openComponent.isOpen)
                    {
                        openSuccess = true;
                        Debug.Log($"物体 {objectID} 已被{(openComponent.isOpen ? "打开" : "关闭")}");
                    }
                    else if (openComponent.isOpen)
                    {
                        Debug.Log($"物体 {objectID} 已经处于打开状态");
                    }
                    break;
                }
            }
        }
        
        if (openSuccess)
        {
            Debug.Log($"Open操作成功：物体 {objectID} 状态已更改");
        }
        else
        {
            Debug.LogWarning($"Open操作失败：夹爪未到达目标物体 {objectID} 位置附近或状态未变化");
        }
        
        // 标记已完成
        hasMovedToPosition = true;
        
        yield return StartCoroutine(OpenSuccess(objectID, isLeftArm));
        
        // 不管是否有碰撞，都根据Open成功判断返回结果
        lastMoveSuccessful = openSuccess;
        
        // 更新状态管理器中的动作结果
        if (sceneManager != null)
        {
            sceneManager.UpdateLastActionSuccess("open");
        }
        // 新增：成功时回调
        JsonData successData = new JsonData();
        successData.success = openSuccess;
        successData.msg = openSuccess ? "Open操作成功" : "Open操作失败";
        callback?.Invoke(successData);
    }



    public IEnumerator Lift(string objectID){

        // 查找物品的 TransferPoint
        Transform[] liftPoints = SceneStateManager.GetLiftPoints(objectID);

        if (liftPoints == null)
        {
            Debug.LogError($"Lift action failed: objectID '{objectID}' not found.");
            yield break;
        }

        // 左臂移动到liftPoints[0]
        yield return StartCoroutine(ArmMovetoPosition(liftPoints[0].position, true));

        yield return new WaitForSeconds(1f);
        // 右臂移动到liftPoints[1]
        yield return StartCoroutine(ArmMovetoPosition(liftPoints[1].position, false));

        yield return new WaitForSeconds(1f);
               // 检测夹爪是否到达目标位置
        Transform leftGripperTransform = null;
        Transform rightGripperTransform = null;
        if (gripperController.currentLeftLeftGripper != null) 
        {
            leftGripperTransform = gripperController.currentLeftLeftGripper.transform;
        } 
        if (gripperController.currentRightLeftGripper != null) 
        {
            rightGripperTransform = gripperController.currentRightLeftGripper.transform;
        }
            
        // 检查夹爪是否在目标位置附近(10厘米范围内)
        bool reachedTargetPosition = false;
        float leftDistance = 0;
        float rightDistance = 0;

        

        if (leftGripperTransform != null)
        {
            leftDistance = Vector3.Distance(leftGripperTransform.position, liftPoints[0].position);
            Debug.Log($"左夹爪到目标点距离: {leftDistance}米");
        }
        if (rightGripperTransform != null)
        {
            rightDistance = Vector3.Distance(rightGripperTransform.position, liftPoints[1].position);

            Debug.Log($"右夹爪到目标点距离: {rightDistance}米");
        }

        if (leftDistance < 0.3f && rightDistance < 0.3f)
        {
            reachedTargetPosition = true;
        }
        
        // 如果没有到达目标位置，结束协程并返回错误信息
        if (!reachedTargetPosition)
        {
            Debug.LogError($"Lift操作失败：夹爪未到达操作位置");
            lastMoveSuccessful = false;
            
            // 更新状态管理器中的动作结果
            if (sceneManager != null)
            {
                sceneManager.UpdateLastActionSuccess("lift");
                // 设置错误消息
                if (sceneManager.GetCurrentSceneStateA2T() != null && sceneManager.GetCurrentSceneStateA2T().agent != null)
                {
                    sceneManager.GetCurrentSceneStateA2T().agent.errorMessage = "夹爪未到达指定位置";
                }
            }

            yield break;
        }


        sceneManager.SetParent(gripperController.currentLeftLeftGripper.transform, objectID);

        yield return new WaitForSeconds(1f);


        // // 左臂移动到liftPoints[0]
        // yield return StartCoroutine(ArmMovetoPosition(liftPoints[0].position, true));

        // yield return new WaitForSeconds(1f);

        // // 右臂移动到liftPoints[1]
        // yield return StartCoroutine(ArmMovetoPosition(liftPoints[1].position, false));

        // yield return new WaitForSeconds(1f);

        //  // 检查夹爪是否在目标位置附近(10厘米范围内)
        // bool lastLiftSuccess = false;

        // if (leftGripperTransform != null)
        // {
        //     leftDistance = Vector3.Distance(leftGripperTransform.position, liftPoints[0].position);
        //     Debug.Log($"左夹爪到目标点距离: {leftDistance}米");
        // }
        // if (rightGripperTransform != null)
        // {
        //     rightDistance = Vector3.Distance(rightGripperTransform.position, liftPoints[1].position);

        //     Debug.Log($"右夹爪到目标点距离: {rightDistance}米");
        // }

        // if (leftDistance < 0.3f && rightDistance < 0.3f)
        // {
        //     lastLiftSuccess = true;
        // }
        bool lastLiftSuccess = true;
        
        // 如果没有到达目标位置，结束协程并返回错误信息
        if (!lastLiftSuccess)
        {
            Debug.LogError($"Lift操作失败：夹爪未到达目标位置");
            lastMoveSuccessful = false;
            
            // 更新状态管理器中的动作结果
            if (sceneManager != null)
            {
                sceneManager.UpdateLastActionSuccess("lift");
                // 设置错误消息
                if (sceneManager.GetCurrentSceneStateA2T() != null && sceneManager.GetCurrentSceneStateA2T().agent != null)
                {
                    sceneManager.GetCurrentSceneStateA2T().agent.errorMessage = "夹爪未到达指定位置";
                }
            }

            yield break;
        }

        yield return new WaitForSeconds(1f);

    }
    public JsonData TP(string objectID)
    {
        // 查找物品的 TransferPoint
        Transform transferPoint = SceneStateManager.GetTransferPointByObjectID(objectID);

        if (transferPoint == null)
        {
            Debug.LogError($"TP action failed: objectID '{objectID}' not found.");
            
            return new JsonData{success = false, msg = "未找到物品的传送点"};
        }

        // 开始执行传送
        StartCoroutine(TransferToPose(transferPoint));

        Debug.Log($"Robot successfully transported to {objectID}'s TransferPoint");
        return new JsonData{success = true, msg = "传送成功"};
    }

    public IEnumerator Pick(string objectID, bool isLeftArm, Action<JsonData> callback)
    {
        // 设置当前交互物体ID
        SetCurrentInteractingObject(objectID);
        
        // 执行前检查是否已存在碰撞
        if (collisionDetected || (RobotCollisionManager.Instance != null && RobotCollisionManager.Instance.HasAnyCollision()))
        {
            Debug.LogWarning("Pick动作开始前已检测到碰撞，但将继续执行");
            // 不再中断操作
        }

        // 清理之前的碰撞状态
        ClearCollisions();
        // 重新设置当前交互物体ID，因为ClearCollisions会清除它
        SetCurrentInteractingObject(objectID);
        
        // 重置状态标记
        hasMovedToPosition = false;
        
        if (CurrentRobotType == RobotType.X1)
        {
            Vector3 offset = new Vector3(0, 0.3f, 0);
            Transform pickPosition = SceneStateManager.GetInteractablePoint(objectID);

            if (pickPosition == null)
            {
                Debug.LogError($"未找到ID为 {objectID} 的物品的默认交互点，无法执行Pick动作");
                // 新增：构造失败结果并回调
                JsonData jsonData = new JsonData();
                jsonData.success = false;
                jsonData.msg = $"未找到ID为 {objectID} 的物品的默认交互点";
                callback?.Invoke(jsonData);
                yield break;
            }

            Vector3 abovePickPosition = pickPosition.position + offset;

            // 移动到夹取位置上方
            Debug.Log($"移动到{(isLeftArm ? "左臂" : "右臂")}夹取位置上方: {abovePickPosition}");
            yield return StartCoroutine(ArmMovetoPosition(abovePickPosition, isLeftArm));
            
            // 检查移动过程中是否发生碰撞
            if (collisionDetected || (RobotCollisionManager.Instance != null && RobotCollisionManager.Instance.HasAnyCollision()))
            {
                if (collisionDetected)
                {
                    Debug.LogWarning($"移动到物体上方时发生碰撞，碰撞关节: {collisionA}，碰撞物体: {collisionB}，但将继续执行");
                }
                else if (RobotCollisionManager.Instance != null)
                {
                    var collisions = RobotCollisionManager.Instance.GetAllNonInteractingCollisions();
                    if (collisions.Count > 0)
                    {
                        string collisionInfo = string.Join("\n", collisions.Select(c => $"关节: {c.Key.name}, 碰撞物体: {c.Value.name}"));
                        Debug.LogWarning($"移动到物体上方时发生碰撞 (由RobotCollisionManager报告):\n{collisionInfo}\n但将继续执行");
                    }
                    else
                    {
                        Debug.LogWarning("移动到物体上方时发生碰撞 (由RobotCollisionManager报告)，但未找到具体碰撞详情，将继续执行");
                    }
                }
                // 不再中断操作
            }
            
            yield return new WaitForSeconds(1f);

            // 打开夹爪准备夹取
            Debug.Log($"打开{(isLeftArm ? "左臂" : "右臂")}夹爪准备夹取");
            gripperController.SetRobotGripper(RobotType.X1, isLeftArm, true);
            yield return new WaitForSeconds(1f);

            var center = pickPosition.position + new Vector3(0,0.05f,0);

            // 下降到夹取位置
            Debug.Log($"下降到{(isLeftArm ? "左臂" : "右臂")}夹取位置: {center}");
            yield return StartCoroutine(ArmMovetoPosition(center, isLeftArm));
            
            // 检查下降过程中是否发生碰撞
            if (collisionDetected || (RobotCollisionManager.Instance != null && RobotCollisionManager.Instance.HasAnyCollision()))
            {
                Debug.LogWarning("下降到夹取位置时发生碰撞，但将继续执行");
                // 不再中断操作
            }
            
            yield return new WaitForSeconds(1f);

            // 检测夹爪是否到达目标位置
            Transform gripperTransform = null;
            if (isLeftArm && gripperController.currentLeftLeftGripper != null) 
            {
                gripperTransform = gripperController.currentLeftLeftGripper.transform;
            } 
            else if (!isLeftArm && gripperController.currentRightLeftGripper != null) 
            {
                gripperTransform = gripperController.currentRightLeftGripper.transform;
            }
            
            // 检查夹爪是否在目标位置附近(10厘米范围内)
            bool reachedTargetPosition = false;
            if (gripperTransform != null)
            {
                float distance = Vector3.Distance(gripperTransform.position, center);
                reachedTargetPosition = distance < 0.3f;
                Debug.Log($"夹爪到目标点距离: {distance}米");
            }
            
            // 如果没有到达目标位置，结束协程并返回错误信息
            if (!reachedTargetPosition)
            {
                Debug.LogError($"Pick操作失败：夹爪未到达目标位置");
                lastMoveSuccessful = false;
                
                // 更新状态管理器中的动作结果
                if (sceneManager != null)
                {
                    sceneManager.UpdateLastActionSuccess("pick");
                    // 设置错误消息
                    if (sceneManager.GetCurrentSceneStateA2T() != null && sceneManager.GetCurrentSceneStateA2T().agent != null)
                    {
                        sceneManager.GetCurrentSceneStateA2T().agent.errorMessage = "夹爪未到达指定位置";
                    }
                }
                // 新增：构造失败结果并回调
                JsonData jsonData = new JsonData();
                jsonData.success = false;
                jsonData.msg = "夹爪未到达指定位置";
                callback?.Invoke(jsonData);
                yield break;
            }

            // 夹紧物体
            Debug.Log($"{(isLeftArm ? "左臂" : "右臂")}夹紧物体");
            gripperController.SetRobotGripper(RobotType.X1, isLeftArm, false);
            yield return new WaitForSeconds(1f);
            if (isLeftArm)
            {
                sceneManager.SetParent(gripperController.leftArmLeftGripper.transform, objectID);
            }
            else
            {
                sceneManager.SetParent(gripperController.rightArmLeftGripper.transform, objectID);
            }
          
            // 提升物体
            Debug.Log($"移动到{(isLeftArm ? "左臂" : "右臂")}夹取位置上方: {abovePickPosition}");
            yield return StartCoroutine(ArmMovetoPosition(abovePickPosition, isLeftArm));
            
            // 检查提升过程中是否发生碰撞
            if (collisionDetected || (RobotCollisionManager.Instance != null && RobotCollisionManager.Instance.HasAnyCollision()))
            {
                Debug.LogWarning("提升物体时发生碰撞，但将继续执行");
            }
            
            yield return new WaitForSeconds(1f);
            

              // 调整物体的旋转以保持与世界坐标正交
            AdjustRotationToWorldAxes(objectID);
        }
        else if (CurrentRobotType == RobotType.H1)
        {
            
            Transform interactablePoint = SceneStateManager.GetInteractablePoint(objectID);
            Transform transferPoint = SceneStateManager.GetTransferPointByObjectID(objectID);
            
            // 检查移动过程中是否发生碰撞
            if (collisionDetected || (RobotCollisionManager.Instance != null && RobotCollisionManager.Instance.HasAnyCollision()))
            {
                Debug.LogWarning("移动机器人位置时发生碰撞，但将继续执行");
                // 不再中断操作
            }

            if (interactablePoint == null)
            {
                Debug.LogError($"未找到ID为 {objectID} 的物品的默认交互点，无法执行Pick动作");
                // 新增：构造失败结果并回调
                JsonData jsonData = new JsonData();
                jsonData.success = false;
                jsonData.msg = $"未找到ID为 {objectID} 的物品的默认交互点";
                callback?.Invoke(jsonData);
                yield break;
            }

            Vector3 pickPosition = interactablePoint.position + interactablePoint.forward * -0.1f + interactablePoint.up * 0.1f;
            Vector3 frontPickPosition = pickPosition + interactablePoint.up * 0.1f;

            // 移动到夹取位置前方
            Debug.Log($"移动到{(isLeftArm ? "左臂" : "右臂")}夹取位置前方: {frontPickPosition}");
            yield return StartCoroutine(ArmMovetoPosition(frontPickPosition, isLeftArm));
            
            // 检查移动过程中是否发生碰撞
            if (collisionDetected || (RobotCollisionManager.Instance != null && RobotCollisionManager.Instance.HasAnyCollision()))
            {
                Debug.LogWarning("移动到物体前方时发生碰撞，但将继续执行");
                // 不再中断操作
            }
            
            yield return new WaitForSeconds(1f);

            // 打开夹爪准备夹取
            Debug.Log($"打开{(isLeftArm ? "左臂" : "右臂")}夹爪准备夹取");
            gripperController.SetRobotGripper(RobotType.H1, isLeftArm, true);
            yield return new WaitForSeconds(1f);

            // 下降到夹取位置
            Debug.Log($"下降到{(isLeftArm ? "左臂" : "右臂")}夹取位置: {pickPosition}");
            yield return StartCoroutine(ArmMovetoPosition(pickPosition, isLeftArm));
            
            // 检查下降过程中是否发生碰撞
            if (collisionDetected || (RobotCollisionManager.Instance != null && RobotCollisionManager.Instance.HasAnyCollision()))
            {
                Debug.LogWarning("下降到夹取位置时发生碰撞，但将继续执行");
                // 不再中断操作
            }
            
            yield return new WaitForSeconds(1f);
            
            // 检查夹爪是否到达目标位置
            Transform h1GripperTransform = null;
            if (isLeftArm && gripperController.h1_leftArmLeftGripper != null)
            {
                h1GripperTransform = gripperController.h1_leftArmLeftGripper.transform;
            }
            else if (!isLeftArm && gripperController.h1_rightArmLeftGripper != null)
            {
                h1GripperTransform = gripperController.h1_rightArmLeftGripper.transform;
            }
            
            // 检查夹爪是否在目标位置附近(10厘米范围内)
            bool h1ReachedTargetPosition = false;
            if (h1GripperTransform != null)
            {
                float distance = Vector3.Distance(h1GripperTransform.position, pickPosition);
                h1ReachedTargetPosition = distance < 0.3f;
                Debug.Log($"H1夹爪到目标点距离: {distance}米");
            }
            
            // 如果没有到达目标位置，结束协程并返回错误信息
            if (!h1ReachedTargetPosition)
            {
                Debug.LogError($"H1 Pick操作失败：夹爪未到达目标位置");
                lastMoveSuccessful = false;
                
                // 更新状态管理器中的动作结果
                if (sceneManager != null)
                {
                    sceneManager.UpdateLastActionSuccess("pick");
                    // 设置错误消息
                    if (sceneManager.GetCurrentSceneStateA2T() != null && sceneManager.GetCurrentSceneStateA2T().agent != null)
                    {
                        sceneManager.GetCurrentSceneStateA2T().agent.errorMessage = "夹爪未到达指定位置";
                    }
                }
                // 新增：构造失败结果并回调
                JsonData jsonData = new JsonData();
                jsonData.success = false;
                jsonData.msg = "夹爪未到达指定位置";
                callback?.Invoke(jsonData);
                yield break;
            }
            
            // 夹紧物体
            Debug.Log($"{(isLeftArm ? "左臂" : "右臂")}夹紧物体");
            gripperController.SetRobotGripper(RobotType.H1, isLeftArm, false);
            yield return new WaitForSeconds(1f);
            
            if (isLeftArm)
            {
                sceneManager.SetParent(gripperController.h1_leftArmLeftGripper.transform, objectID);
            }
            else
            {
                sceneManager.SetParent(gripperController.h1_rightArmLeftGripper.transform, objectID);
            }

            Debug.Log($"移动到{(isLeftArm ? "左臂" : "右臂")}夹取位置前方: {frontPickPosition}");
            yield return StartCoroutine(ArmMovetoPosition(frontPickPosition, isLeftArm));
            
            // 检查退回过程中是否发生碰撞
            if (collisionDetected || (RobotCollisionManager.Instance != null && RobotCollisionManager.Instance.HasAnyCollision()))
            {
                Debug.LogWarning("退回时发生碰撞，但将继续执行");
            }
            
            yield return new WaitForSeconds(1f);

            // 调整物体的旋转以保持与世界坐标正交
            AdjustRotationToWorldAxes(objectID);
        }
        
        // 检查是否成功抓取物体（独立于碰撞）
        bool pickSuccess = false;
        GameObject pickedObject = null;
        
        // 查找物体对象
        SimObjPhysics[] allObjects = FindObjectsOfType<SimObjPhysics>();
        foreach (SimObjPhysics obj in allObjects)
        {
            if (obj.ObjectID == objectID)
            {
                pickedObject = obj.gameObject;
                break;
            }
        }
        
        // 判断是否成功抓取：检查物体是否是夹爪的子物体
        if (pickedObject != null)
        {
            Transform parent = pickedObject.transform.parent;
            if (parent != null && parent.CompareTag("Hand"))
            {
                pickSuccess = true;
                Debug.Log($"Pick操作成功：物体 {objectID} 已成为夹爪的子物体");
            }
            else
            {
                Debug.LogWarning($"Pick操作失败：物体 {objectID} 不是夹爪的子物体");
            }
        }
        else
        {
            Debug.LogError($"Pick操作失败：找不到物体 {objectID}");
        }
        
        // 标记已完成
        hasMovedToPosition = true;
        
        yield return StartCoroutine(PickSuccess(objectID, isLeftArm));
        
        // 不管是否有碰撞，都根据抓取成功判断返回结果
        lastMoveSuccessful = pickSuccess;
        
        // 更新状态管理器中的动作结果
        if (sceneManager != null)
        {
            sceneManager.UpdateLastActionSuccess("pick");
        }
        // 新增：成功时回调
        JsonData successData = new JsonData();
        successData.success = pickSuccess;
        successData.msg = pickSuccess ? "Pick操作成功" : "Pick操作失败";
        callback?.Invoke(successData);
    }

    public IEnumerator Place(string objectID, bool isLeftArm, Action<JsonData> callback)
    {
        // 记录当前交互对象
        SetCurrentInteractingObject(objectID);
        
        // 执行前检查是否已存在碰撞
        if (collisionDetected || (RobotCollisionManager.Instance != null && RobotCollisionManager.Instance.HasAnyCollision()))
        {
            Debug.LogWarning("Place动作开始前已检测到碰撞，但将继续执行");
            // 不再中断操作
        }

        // 将碰撞对象添加到忽略列表
        AddIgnoredCollisionObject(objectID);
        
        // 标记为需要检查碰撞，但继续执行
        collisionDetected = false;
        
        // 重置状态标记
        hasMovedToPosition = false;

        // 获取对象的传送点
        Transform get_trans = SceneStateManager.GetTransferPointByObjectID(objectID);
        Vector3 transferPoint =new Vector3(get_trans.position.x,get_trans.position.y+0.05f,get_trans.position.z);

        if (transferPoint == null)
        {
            Debug.LogError($"未找到ID为 {objectID} 的物品传送点");
            // 新增：构造失败结果并回调
            JsonData jsonData = new JsonData();
            jsonData.success = false;
            jsonData.msg = $"未找到ID为 {objectID} 的物品传送点";
            callback?.Invoke(jsonData);
            yield break;
        }

        // 机械臂移动到该位置
        yield return StartCoroutine(ArmMovetoPosition(transferPoint, isLeftArm));

        // 检查移动过程中是否发生碰撞
        if (collisionDetected || (RobotCollisionManager.Instance != null && RobotCollisionManager.Instance.HasAnyCollision()))
        {
            Debug.LogWarning("移动到放置位置时发生碰撞，但将继续执行");
            // 不再中断操作
        }

        // 获取夹爪控制器
        GripperController gripperController = GetComponent<GripperController>();
        
        // 提前检查夹爪是否到达目标位置
        bool reachedTargetPosition = false;
        if (isLeftArm && gripperController.currentLeftLeftGripper != null)
        {
            reachedTargetPosition = Vector3.Distance(gripperController.currentLeftLeftGripper.transform.position, transferPoint) < 0.3f;
            Debug.Log($"Place操作: 左夹爪到放置点距离为 {Vector3.Distance(gripperController.currentLeftLeftGripper.transform.position, transferPoint)}米");
        }
        else if (!isLeftArm && gripperController.currentRightLeftGripper != null)
        {
            reachedTargetPosition = Vector3.Distance(gripperController.currentRightLeftGripper.transform.position, transferPoint) < 0.3f;
            Debug.Log($"Place操作: 右夹爪到放置点距离为 {Vector3.Distance(gripperController.currentRightLeftGripper.transform.position, transferPoint)}米");
        }
        
        // 如果夹爪没有到达目标位置，提前结束协程
        if (!reachedTargetPosition)
        {
            Debug.LogError($"Place操作失败：夹爪未到达目标位置");
            lastMoveSuccessful = false;
            
            // 更新状态管理器中的动作结果
            if (sceneManager != null)
            {
                sceneManager.UpdateLastActionSuccess("place");
                // 设置错误消息
                if (sceneManager.GetCurrentSceneStateA2T() != null && sceneManager.GetCurrentSceneStateA2T().agent != null)
                {
                    sceneManager.GetCurrentSceneStateA2T().agent.errorMessage = "夹爪未到达指定位置";
                }
            }
            // 新增：构造失败结果并回调
            JsonData jsonData = new JsonData();
            jsonData.success = false;
            jsonData.msg = "夹爪未到达指定位置";
            callback?.Invoke(jsonData);
            yield break;
        }

        // 打开夹爪
        if (gripperController != null)
        {
            if (CurrentRobotType == RobotType.X1)
            {
                gripperController.SetRobotGripper(RobotType.X1, isLeftArm, true); // true表示打开夹爪
            }
            else if (CurrentRobotType == RobotType.H1)
            {
                gripperController.SetRobotGripper(RobotType.H1, isLeftArm, true);
            }
        }

        // 等待夹爪打开
        yield return new WaitForSeconds(0.5f);

        // 释放对象
        sceneManager.Release(objectID);
        
        // 清理标记
        collisionDetected = false;
        collisionA = "";
        collisionB = "";

        // 检查是否成功放置物体（独立于碰撞）
        bool placeSuccess = false;
        GameObject placedObject = null;
        bool isReleased = true;
        
        // 查找物体对象
        SimObjPhysics[] allObjects = FindObjectsOfType<SimObjPhysics>();
        foreach (SimObjPhysics obj in allObjects)
        {
            if (obj.ObjectID == objectID)
            {
                placedObject = obj.gameObject;
                break;
            }
        }
        
        // 检查物体是否已从夹爪释放
        if (placedObject != null)
        {
            Transform parent = placedObject.transform.parent;
            if (parent != null && parent.CompareTag("Hand"))
            {
                isReleased = false;
                Debug.LogWarning($"Place操作失败：物体 {objectID} 仍然是夹爪的子物体");
            }
        }
        
        // 判断放置是否成功：夹爪到达目标位置且物体已释放
        placeSuccess = reachedTargetPosition && isReleased;
        if (placeSuccess)
        {
            Debug.Log($"Place操作成功：夹爪已到达目标位置且物体 {objectID} 已释放");
        }
        else
        {
            Debug.LogWarning($"Place操作失败：夹爪到达目标位置={reachedTargetPosition}，物体已释放={isReleased}");
        }
        
        // 标记已完成
        hasMovedToPosition = true;
        
        yield return StartCoroutine(PlaceSuccess(objectID, isLeftArm));
        
        // 不管是否有碰撞，都根据放置成功判断返回结果
        lastMoveSuccessful = placeSuccess;
        
        // 更新状态管理器中的动作结果
        if (sceneManager != null)
        {
            sceneManager.UpdateLastActionSuccess("place");
        }
        
        // 清除当前交互对象
        sceneManager.RemoveOperation(objectID);
        // 新增：成功时回调
        JsonData successData = new JsonData();
        successData.success = placeSuccess;
        successData.msg = placeSuccess ? "Place操作成功" : "Place操作失败";
        callback?.Invoke(successData);
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

        hasMovedToPosition = false; // 标记为未到达位置
    }

    private void UpdateTargetJointAngles(List<float> updatedAngles)//事件
    {
        targetJointAngles = updatedAngles;
        isTargetAnglesUpdated = true; // 标记角度已更新
    }

    private IEnumerator ArmMovetoPosition(Vector3 position, bool isLeftArm)
    {
        // 请求计算目标角度
        // ikClient.ProcessTargetPosition(position, isLeftArm);
        if (CurrentRobotType == RobotType.X1)
        {
            ikX1.ProcessTargetPosition(position, isLeftArm);
            // 等待目标角度更新
            yield return new WaitUntil(() => isTargetAnglesUpdated);

            //Debug.Log("ArmMovetoPosition 中的目标角度: " + string.Join(", ", targetJointAngles));

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




    private IEnumerator TransferToPose(Transform transfer)
    {
        // 请求计算目标角度
        // rootArt.TeleportRoot(transfer.position, transfer.rotation);
        // transform.position = transfer.position; // 确保到达目标位置

        Vector3 startPosition = transform.position;
        Vector3 targetPosition = transfer.position;
         
        Quaternion originRot= transform.rotation;
        float elapsedTime = 0f;

        while (elapsedTime < 0.5f)
        {
            Vector3 pos_temp = Vector3.Lerp(startPosition, targetPosition, elapsedTime / 0.5f);

            rootArt.TeleportRoot(pos_temp, transfer.rotation);
            transform.position = pos_temp;
            elapsedTime += Time.deltaTime;
            yield return null;
        }

        rootArt.TeleportRoot(targetPosition, transfer.rotation);
        transform.position = targetPosition; // 确保到达目标位置

    }
    private IEnumerator MoveToPosition(Vector3 targetPosition)
    {
        // 请求计算目标角度
        // rootArt.TeleportRoot(targetPosition, transform.rotation);
        // transform.position = position; // 确保到达目标位置

        Vector3 startPosition = transform.position;

         
        Quaternion originRot= transform.rotation;
        float elapsedTime = 0f;

        while (elapsedTime < 0.5f)
        {
            Vector3 pos_temp = Vector3.Lerp(startPosition, targetPosition, elapsedTime / 0.5f);

            rootArt.TeleportRoot(pos_temp, originRot);
            transform.position = pos_temp;
            elapsedTime += Time.deltaTime;
            yield return null;
        }

        rootArt.TeleportRoot(targetPosition, originRot);
        transform.position = targetPosition; // 确保到达目标位置

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
        
        GameObject cur_robot=robots[0];
        if(CurrentRobotType==RobotType.H1){
            cur_robot=robots[1];
        }else{
            cur_robot=robots[0];
        }
        Vector3 startPosition = transform.position;
        Vector3 targetPosition = startPosition + transform.TransformDirection(localDirection) * moveSpeed * magnitude;
         
        Quaternion originRot= transform.rotation;
        float elapsedTime = 0f;
        
        // 获取StructureCollisionDetector组件
        StructureCollisionDetector collisionDetector = cur_robot.GetComponent<StructureCollisionDetector>();
        Debug.Log("collideStructure scripts: "+cur_robot.name);
        
        // 检查碰撞检测器是否存在
        if (collisionDetector == null)
        {
            Debug.LogError("未找到StructureCollisionDetector组件！尝试添加...");
            collisionDetector = cur_robot.AddComponent<StructureCollisionDetector>();
            collisionDetector.robotBodyTransform = transform;
            collisionDetector.raycastDistance = 0.3f; // 设置较小的默认值
        }
        
        // 设置当前移动方向 - 这是关键修改
        Vector3 worldMoveDirection = transform.TransformDirection(localDirection);
        collisionDetector.SetMoveDirection(worldMoveDirection);
        
        // 输出详细的调试信息
        collisionDetector.LogDebugInfo("移动开始");
        
        // 确保碰撞检测器正确设置
        if (collisionDetector != null)
        {
            // 检查碰撞检测器的主体引用是否正确
            if (collisionDetector.robotBodyTransform != transform)
            {
                collisionDetector.robotBodyTransform = transform;
                collisionDetector.ResetPosition();
                Debug.Log("已更新碰撞检测器的机器人主体引用");
            }
            
            // 强制立即同步位置
            collisionDetector.transform.position = transform.position;
            collisionDetector.transform.rotation = transform.rotation;
        }
        
        bool collideStructure = collisionDetector.CollideStructure;
        
        // 使用增强的射线检测功能检查移动路径是否安全
        float moveDistance = Vector3.Distance(startPosition, targetPosition);
        bool isSafeToMove = collisionDetector.IsSafeToMoveInDirection(localDirection, moveDistance * 1.1f);
        
        // 输出调试信息
        Debug.Log($"移动信息 - 方向: {localDirection}, 距离: {moveDistance:F3}, 安全: {isSafeToMove}");
        Debug.Log($"起点: {startPosition}, 目标点: {targetPosition}");
        
        // 检查是否有任何现有碰撞，如果有，则不允许移动
        if (collisionDetected || (RobotCollisionManager.Instance != null && RobotCollisionManager.Instance.HasAnyCollision()))
        {
            Debug.LogWarning("检测到现有碰撞，无法开始移动");
            // 设置移动成功标志为false
            lastMoveSuccessful = false;
            yield break;
        }
        
        if (!isSafeToMove)
        {
            // 检查碰撞物体
            GameObject obstacle = collisionDetector.GetLastDetectedObstacle();
            if (obstacle != null)
            {
                Debug.LogWarning($"移动路径上检测到障碍物: {obstacle.name}，位置: {obstacle.transform.position}");
                
                // 检查障碍物是否真的在移动方向上
                Vector3 directionToObstacle = obstacle.transform.position - transform.position;
                float angleToObstacle = Vector3.Angle(worldMoveDirection, directionToObstacle);
                
                Debug.Log($"与障碍物的角度: {angleToObstacle}度");
                
                // 如果障碍物在移动方向后方（角度>90度），可能是误检测
                if (angleToObstacle > 90f)
                {
                    Debug.LogWarning($"障碍物位于移动方向后方，可能是误检测，尝试继续移动");
                    isSafeToMove = true;
                }
                else
                {
                    // 明确标记为移动失败
                    lastMoveSuccessful = false;
                    yield break;
                }
            }
            else
            {
                // 如果没有具体障碍物但仍然不安全，拒绝移动
                Debug.LogWarning("移动路径不安全，无法开始移动");
                lastMoveSuccessful = false;
                yield break;
            }
        }

        // 进行平滑移动
        while (elapsedTime < duration && !collisionDetector.CollideStructure)
        {
            Vector3 pos_temp = Vector3.Lerp(startPosition, targetPosition, elapsedTime / duration);
            
            // 检查当前位置到下一个位置的短距离移动是否安全
            Vector3 nextMoveDirection = pos_temp - transform.position;
            float nextMoveDistance = nextMoveDirection.magnitude;
            
            if (nextMoveDistance > 0.01f && !collisionDetector.IsSafeToMoveInDirection(nextMoveDirection, nextMoveDistance * 1.2f))
            {
                Debug.LogWarning($"移动过程中检测到障碍物，停止移动，当前进度: {elapsedTime/duration:P0}");
                // 如果移动中断，则标记为失败
                lastMoveSuccessful = false;
                break;
            }
            
            // 移动机器人主体
            transform.position = pos_temp;
            rootArt.TeleportRoot(transform.position, originRot);
            
            // 确保碰撞检测器与机器人同步移动
            if (collisionDetector != null && collisionDetector.transform.position != transform.position)
            {
                // 如果位置偏移太大，强制同步
                float positionDiff = Vector3.Distance(collisionDetector.transform.position, transform.position);
                if (positionDiff > 0.05f)
                {
                    Debug.LogWarning($"检测到碰撞检测器位置与机器人不一致，相差: {positionDiff}，强制同步");
                    collisionDetector.transform.position = transform.position;
                    collisionDetector.transform.rotation = transform.rotation;
                    collisionDetector.ResetPosition();
                }
            }
            
            elapsedTime += Time.deltaTime;
            yield return null;
        }

        // 如果移动过程中被中断，则保持失败状态
        if (collisionDetector.CollideStructure)
        {
            Debug.LogWarning("检测到碰撞，移动已中断");
            lastMoveSuccessful = false;
            yield break;
        }

        // 最终位置安全检查
        if (!collisionDetector.CollideStructure)
        {
            // 检查最后的移动是否安全
            Vector3 finalMoveDirection = targetPosition - transform.position;
            float finalMoveDistance = finalMoveDirection.magnitude;
            Debug.Log("finalMoveDistance: "+finalMoveDistance);
            if (finalMoveDistance > 0.01f)
            {
                if(collisionDetector.IsSafeToMoveInDirection(finalMoveDirection, finalMoveDistance)){

                    transform.position = targetPosition;
                    rootArt.TeleportRoot(transform.position, originRot);
                    
                    // 确保碰撞检测器跟随最终位置
                    if (collisionDetector != null)
                    {
                        collisionDetector.transform.position = transform.position;
                        collisionDetector.transform.rotation = transform.rotation;
                    }
                    
                    Debug.Log("已安全到达目标位置");
                    // 成功完成整个移动
                    lastMoveSuccessful = true;
                }else{
                    Debug.LogWarning("最终位置可能不安全，保持当前位置");
                    lastMoveSuccessful = false;
                    yield break;
                }

            }else{
                transform.position = targetPosition;
                rootArt.TeleportRoot(transform.position, originRot);
                
                // 确保碰撞检测器跟随最终位置
                if (collisionDetector != null)
                {
                    collisionDetector.transform.position = transform.position;
                    collisionDetector.transform.rotation = transform.rotation;
                }
                
                Debug.Log("已安全到达目标位置");
                // 成功完成整个移动
                lastMoveSuccessful = true;
            }
        }
        else
        {
            Debug.LogWarning("检测到碰撞，移动已中断");
            lastMoveSuccessful = false;
            yield break;
        }

        // 确保机器人位置与物理引擎中的实际位置保持一致
        transform.position = cur_robot.transform.position;
        
        // 最后再次确保碰撞检测器与机器人位置同步
        if (collisionDetector != null)
        {
            collisionDetector.transform.position = transform.position;
            collisionDetector.transform.rotation = transform.rotation;
        }
        
        // 清理移动方向，移动结束后不再需要检测
        collisionDetector.SetMoveDirection(Vector3.zero);
        
        // 输出详细的调试信息
        collisionDetector.LogDebugInfo("移动结束");
        
        // 这里设置一个公共变量，告知调用者移动是否成功
        lastMoveSuccessful = lastMoveSuccessful;
    }

    // 平滑旋转的协程，使用局部坐标系的方向
    private IEnumerator SmoothRotate(Vector3 rotationAxis, float magnitude, float duration)
    {
        float targetAngle = rotationSpeed * magnitude;
        Vector3 currentPosition = transform.position;
        Quaternion startRotation = transform.rotation;
        Quaternion targetRotation = startRotation * Quaternion.AngleAxis(targetAngle, rotationAxis);
        float elapsedTime = 0f;

        while (elapsedTime < duration)
        {
            float t = elapsedTime / duration;
            Quaternion currentRotation = Quaternion.Lerp(startRotation, targetRotation, t);
            rootArt.TeleportRoot(currentPosition, currentRotation);
            transform.rotation = currentRotation;
            elapsedTime += Time.deltaTime;
            yield return null;
        }

        rootArt.TeleportRoot(currentPosition, targetRotation);
        transform.rotation = targetRotation;
        yield return new WaitForSeconds(1f);
    }

    public IEnumerator MoveAhead(float Magnitude)
    {
        // 确保每次移动前重置状态
        lastMoveSuccessful = true;
        
        yield return SmoothMove(Vector3.forward, Magnitude, 1.0f); 
    }

    public IEnumerator MoveRight(float Magnitude)
    {
        // 确保每次移动前重置状态
        lastMoveSuccessful = true;
        
        yield return SmoothMove(Vector3.right, Magnitude, 1.0f);
    }

    public IEnumerator MoveBack(float Magnitude)
    {
        // 确保每次移动前重置状态
        lastMoveSuccessful = true;
        
        yield return SmoothMove(Vector3.back, Magnitude, 1.0f);
    }

    public IEnumerator MoveLeft(float Magnitude)
    {
        // 确保每次移动前重置状态
        lastMoveSuccessful = true;
        
        yield return SmoothMove(Vector3.left, Magnitude, 1.0f);
    }
    
    public IEnumerator MoveUp(float Magnitude)
    {
        // 确保每次移动前重置状态
        lastMoveSuccessful = true;
        
        yield return SmoothMove(Vector3.up, Magnitude * 0.1f, 1.0f);
    }

    public IEnumerator MoveDown(float Magnitude)
    {
        // 确保每次移动前重置状态
        lastMoveSuccessful = true;
        
        yield return SmoothMove(Vector3.down, Magnitude * 0.1f, 1.0f);
    }
    
    public IEnumerator RotateRight(float Magnitude)
    {
        yield return SmoothRotate(Vector3.up, Mathf.Abs(Magnitude), 1.0f);
    }

    public IEnumerator RotateLeft(float Magnitude)
    {
        yield return SmoothRotate(Vector3.up, -Mathf.Abs(Magnitude), 1.0f);
    }
    public bool Undo()
    {
        
        // DisableArticulationBodies();
        bool result=sceneManager.Undo();
        // EnableArticulationBodies();
        return result;
    }
    public bool Redo()
    {
        // DisableArticulationBodies();
        bool result=sceneManager.Redo();
        // EnableArticulationBodies();
        return result;
    }

    // 调用 SceneStateManager 的 LoadStateByIndex 方法
    public bool LoadState(string stateID)
    {
        Debug.Log($"Attempting to load scene state with ID: {stateID}");
        // DisableArticulationBodies();
        bool result=sceneManager.LoadStateByIndex(stateID);
        // EnableArticulationBodies();
        return result;
    }
    public void DisableArticulationBodies()
    {
        // 保存当前的 xDrive 值
        tempDrives.Clear();
        foreach (ArticulationBody body in articulationChain)
        {
            if (body.jointType != ArticulationJointType.FixedJoint)
            {
                tempDrives.Add(body.xDrive);
            }
        }

        // 设置为默认的 xDrive 值
        int i = 0;
        foreach (ArticulationBody body in articulationChain)
        {
            if (body.jointType != ArticulationJointType.FixedJoint)
            {
                if (i < defaultDrives.Count)
                {
                    body.xDrive = defaultDrives[i];
                    i++;
                }
            }
        }

        // 禁用 ArticulationBody
        foreach (ArticulationBody body in articulationChain)
        {
            body.enabled = false;
        }
    }

    public void EnableArticulationBodies()
    {
        // 启用 ArticulationBody
        foreach (ArticulationBody body in articulationChain)
        {
            body.enabled = true;
        }

        // 恢复之前保存的 xDrive 值
        int i = 0;
        foreach (ArticulationBody body in articulationChain)
        {
            if (body.jointType != ArticulationJointType.FixedJoint)
            {
                if (i < tempDrives.Count)
                {
                    body.xDrive = tempDrives[i];
                    i++;
                }
            }
        }
    }
    public float NormalizeAngle(float angle)
    {
        while (angle > 180f) angle -= 360f;
        while (angle < -180f) angle += 360f;
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
    public bool LoadScene(string scene,string robottype)
    {
        Debug.Log($"Loading scene: {scene}, robot Type:{robottype}");
        
        this.robottype=robottype;
        try{
            SceneManager.LoadScene(scene);
        }catch(Exception e){
            Debug.Log(e);
            return false;
        }
        return true;
    }
    private void OnSceneLoaded(Scene scene, LoadSceneMode mode)
    {
        // 在场景加载完成后查找 AgentMovement 并调用 LoadRobot 方法
        Debug.Log("scene loaded");
        AgentMovement agentMovement = FindObjectOfType<AgentMovement>();
        if (agentMovement != null)
        {
            agentMovement.LoadRobot(this.robottype);
        }
        else
        {
            Debug.LogWarning("AgentMovement not found in the loaded scene.");
        }

        // 取消注册事件以避免重复调用
        SceneManager.sceneLoaded -= OnSceneLoaded;
    }
    public bool LoadRobot(string robotType)
    {
        Debug.Log($"Loading robot of type: {robotType}");
        foreach (var robot in robots)
        {
            robot.SetActive(false);
        }

        GameObject activeRobot = null;

        switch (robotType.ToLower())
        {
            case "x1":
                activeRobot = robots[0];
                activeRobot.SetActive(true);
                SetRobot();
                cameraTransform = Camera.main.transform;
                CurrentRobotType = RobotType.X1;
                InitializeAdjustments(true);
                InitializeAdjustments(false);
                ikX1.OnTargetJointAnglesUpdated += UpdateTargetJointAngles;
                sceneManager.getObjectsInView.viewDistance=1.5f;
                
                // 确保碰撞检测管理器存在
                EnsureCollisionDetectorManagerExists();
                
                // 配置X1机器人的碰撞检测器
                StructureCollisionDetector detector = activeRobot.GetComponent<StructureCollisionDetector>();
                if (detector != null)
                {
                    detector.robotBodyTransform = transform; // 确保引用正确的机器人主体
                    detector.ResetPosition(); // 重置位置和偏移量
                    CollisionDetectorManager.Instance.RegisterDetector(detector);
                    Debug.Log("已配置X1机器人的碰撞检测器");
                }
                else
                {
                    detector = activeRobot.AddComponent<StructureCollisionDetector>();
                    detector.robotBodyTransform = transform;
                    // 碰撞检测管理器会自动配置其他参数
                    Debug.Log("为X1机器人添加并配置碰撞检测器");
                }
                break;
            
            case "h1":
                activeRobot = robots[1];
                activeRobot.SetActive(true);
                Debug.Log("Set robot!");
                SetRobot();
                cameraTransform = Camera.main.transform;
                CurrentRobotType = RobotType.H1;
                ikH1.InitBodies();
                ikH1.IniitTarget();
                sceneManager.getObjectsInView.viewDistance=2.0f;
                
                // 确保碰撞检测管理器存在
                EnsureCollisionDetectorManagerExists();
                
                // 将X1的射线距离设置得更大一些
                if (CollisionDetectorManager.Instance != null)
                {
                    CollisionDetectorManager.Instance.SetRayDistance(0.3f);
                }
                
                // 配置H1机器人的碰撞检测器
                detector = activeRobot.GetComponent<StructureCollisionDetector>();
                if (detector != null)
                {
                    detector.robotBodyTransform = transform; // 确保引用正确的机器人主体
                    detector.ResetPosition(); // 重置位置和偏移量
                    CollisionDetectorManager.Instance.RegisterDetector(detector);
                    Debug.Log("已配置H1机器人的碰撞检测器");
                }
                else
                {
                    detector = activeRobot.AddComponent<StructureCollisionDetector>();
                    detector.robotBodyTransform = transform;
                    // 碰撞检测管理器会自动配置其他参数
                    Debug.Log("为H1机器人添加并配置碰撞检测器");
                }
                break;
            
            case "g1":
                activeRobot = robots[2];
                activeRobot.SetActive(true);
                CurrentRobotType = RobotType.G1;
                
                // 确保碰撞检测管理器存在
                EnsureCollisionDetectorManagerExists();
                
                // 配置G1机器人的碰撞检测器
                detector = activeRobot.GetComponent<StructureCollisionDetector>();
                if (detector != null)
                {
                    detector.robotBodyTransform = transform; // 确保引用正确的机器人主体
                    detector.ResetPosition(); // 重置位置和偏移量
                    CollisionDetectorManager.Instance.RegisterDetector(detector);
                    Debug.Log("已配置G1机器人的碰撞检测器");
                }
                else
                {
                    detector = activeRobot.AddComponent<StructureCollisionDetector>();
                    detector.robotBodyTransform = transform;
                    // 碰撞检测管理器会自动配置其他参数
                    Debug.Log("为G1机器人添加并配置碰撞检测器");
                }
                break;
            
            default:
                Debug.LogError($"Unknown robot type: {robotType}");
                return false;
        }
        
        // 确保碰撞检测器立即更新位置
        if (activeRobot != null)
        {
            // 让碰撞检测器立即与机器人位置同步
            StructureCollisionDetector activeDetector = activeRobot.GetComponent<StructureCollisionDetector>();
            if (activeDetector != null)
            {
                // 强制立即更新位置
                activeDetector.transform.position = transform.position;
                activeDetector.transform.rotation = transform.rotation;
                // 再次调用重置位置以确保正确的偏移计算
                activeDetector.ResetPosition();
                
                // 输出检测器信息进行调试
                activeDetector.LogDebugInfo("机器人加载后");
            }
            
            // 重置所有检测器
            if (CollisionDetectorManager.Instance != null)
            {
                CollisionDetectorManager.Instance.ResetAllDetectors();
                CollisionDetectorManager.Instance.LogAllDetectorsInfo();
            }
        }
        
        return true;
    }

    // 确保碰撞检测管理器存在
    private void EnsureCollisionDetectorManagerExists()
    {
        if (CollisionDetectorManager.Instance == null)
        {
            GameObject managerObj = new GameObject("CollisionDetectorManager");
            CollisionDetectorManager manager = managerObj.AddComponent<CollisionDetectorManager>();
            manager.defaultRayDistance = 0.3f;  // 设置较小的默认检测距离
            manager.disableCollisionDetection = false;
            Debug.Log("创建了碰撞检测管理器");
        }
    }

    public bool ResetPose(){
        // if(CurrentRobotType==RobotType.H1){
        //     Debug.Log("reset h1 pose!");
        //     ikH1.ResetTarget();
        // }
        ikH1.ResetTarget();
        return true;
    }


    // 在 AgentMovement 类中添加一个方法来处理碰撞
    private void HandleCollision(ArticulationBody articulationBody, Collision collision)
    {
        Debug.Log($"ArticulationBody {articulationBody.name} 碰撞到 {collision.gameObject.name}");
    }
    // 碰撞检测方法
    private void OnCollisionEnter(Collision collision)
    {
        // 遍历所有的 ArticulationBody
        foreach (ArticulationBody articulationBody in articulationChain)
        {
            // 获取当前 ArticulationBody 的所有碰撞体
            Collider[] colliders = articulationBody.GetComponents<Collider>();

            // 检查碰撞体是否与其他物体发生碰撞
            foreach (Collider collider in colliders)
            {
                if (collider.bounds.Intersects(collision.collider.bounds))
                {
                    // 如果碰撞的物体不是 ArticulationBody，则记录
                    if (collision.gameObject.GetComponent<ArticulationBody>() == null)
                    {
                        if (!collidedObjects.Contains(collision.gameObject))
                        {
                            collidedObjects.Add(collision.gameObject);
                            Debug.Log($"与物体 {collision.gameObject.name} 发生碰撞");
                        }
                    }
                }
            }
        }
    }
        // 可选：清空碰撞记录
    public void ClearCollidedObjects()
    {
        collidedObjects.Clear();
    }

    private void AdjustRotationToWorldAxes(string objectID)
    {
        // 查找对应ID的物体
        SimObjPhysics[] allObjects = FindObjectsOfType<SimObjPhysics>();
        foreach (SimObjPhysics obj in allObjects)
        {
            if (obj.ObjectID == objectID)
            {
                // 将物体的旋转调整为与世界坐标轴对齐
                obj.transform.rotation = Quaternion.identity;
                return;
            }
        }
        
        Debug.LogWarning($"未找到ID为 {objectID} 的物品，无法调整旋转");
    }

    private List<float> savedTargets = new List<float>();

    public void SaveArticulationBodyTargets()
    {
        savedTargets.Clear();
        foreach (ArticulationBody body in articulationChain)
        {
            if (body.jointType != ArticulationJointType.FixedJoint)
            {
                savedTargets.Add(body.xDrive.target);
            }
        }
    }

    public void RestoreArticulationBodyTargets()
    {
        int i = 0;
        foreach (ArticulationBody body in articulationChain)
        {
            if (body.jointType != ArticulationJointType.FixedJoint)
            {
                if (i < savedTargets.Count)
                {
                    var drive = body.xDrive;
                    drive.target = savedTargets[i];
                    body.xDrive = drive;
                    i++;
                }
            }
        }
    }

    private List<float> savedAngles = new List<float>();

    public void SaveArticulationBodyAngles()
    {
        savedAngles.Clear();
        foreach (ArticulationBody body in articulationChain)
        {
            if (body.jointType != ArticulationJointType.FixedJoint)
            {
                savedAngles.Add(body.jointPosition[0]);
            }
        }
    }

    public void RestoreArticulationBodyAngles()
    {
        int i = 0;
        foreach (ArticulationBody body in articulationChain)
        {
            if (body.jointType != ArticulationJointType.FixedJoint)
            {
                if (i < savedAngles.Count)
                {
                    var drive = body.xDrive;
                    drive.target = savedAngles[i];
                    body.xDrive = drive;
                    i++;
                }
            }
        }
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

    private Vector3 CalculateOffset(Transform target)
    {
        // 计算机器人和目标物体之间的相对位置
        Vector3 relativePosition = target.position - transform.position;

        // 根据相对位置的z轴值判断offset的z轴是否为负
        float zOffset = relativePosition.z < 0 ? -0.1f : 0.1f;

        // 返回计算后的offset
        return new Vector3(0, 0f, zOffset);
    }

    // 在Pick方法的最后添加成功标记
    private IEnumerator PickSuccess(string objectID, bool isLeftArm)
    {
        Debug.Log($"Pick操作完成处理：物体ID {objectID}, 使用{(isLeftArm ? "左臂" : "右臂")}");
        
        // 等待一帧确保所有状态更新
        yield return null;
        
        // 标记交互完成
        hasMovedToPosition = true;
        
        // 清除所有碰撞状态
        ClearCollisions();
        
        Debug.Log("Pick操作完全结束");
    }
    
    // 在Place方法的最后添加成功标记
    private IEnumerator PlaceSuccess(string objectID, bool isLeftArm)
    {
        Debug.Log($"Place操作完成处理：物体ID {objectID}, 使用{(isLeftArm ? "左臂" : "右臂")}");
        
        // 等待一帧确保所有状态更新
        yield return null;
        
        // 标记交互完成
        hasMovedToPosition = true;
        
        // 清除所有碰撞状态
        ClearCollisions();
        
        Debug.Log("Place操作完全结束");
    }
    
    // 在Toggle方法的最后添加成功标记
    private IEnumerator ToggleSuccess(string objectID, bool isLeftArm)
    {
        Debug.Log($"Toggle操作完成处理：物体ID {objectID}, 使用{(isLeftArm ? "左臂" : "右臂")}");
        
        // 等待一帧确保所有状态更新
        yield return null;
        
        // 标记交互完成
        hasMovedToPosition = true;
        
        // 清除所有碰撞状态
        ClearCollisions();
        
        Debug.Log("Toggle操作完全结束");
    }
    
    // 在Open方法的最后添加成功标记
    private IEnumerator OpenSuccess(string objectID, bool isLeftArm)
    {
        Debug.Log($"Open操作完成处理：物体ID {objectID}, 使用{(isLeftArm ? "左臂" : "右臂")}");
        
        // 等待一帧确保所有状态更新
        yield return null;
        
        // 标记交互完成
        hasMovedToPosition = true;
        
        // 清除所有碰撞状态
        ClearCollisions();
        
        Debug.Log("Open操作完全结束");
    }

    // 设置当前交互物体ID的方法
    public void SetCurrentInteractingObject(string objectID)
    {
        currentInteractingObjectID = objectID;
        Debug.Log($"设置当前交互物体ID: {objectID}");
        
        // 同步更新RobotCollisionManager
        if (RobotCollisionManager.Instance != null)
        {
            RobotCollisionManager.Instance.SetCurrentInteractingObject(objectID);
        }
    }
    
    // 添加忽略碰撞的物体ID
    public void AddIgnoredCollisionObject(string objectID)
    {
        if (!string.IsNullOrEmpty(objectID) && !ignoredCollisionObjects.Contains(objectID))
        {
            ignoredCollisionObjects.Add(objectID);
            Debug.Log($"添加忽略碰撞物体ID: {objectID}");
            
            // 同步更新RobotCollisionManager
            if (RobotCollisionManager.Instance != null)
            {
                RobotCollisionManager.Instance.AddIgnoredCollisionObject(objectID);
            }
        }
    }
    
    // 清空忽略列表
    public void ClearIgnoredCollisionObjects()
    {
        ignoredCollisionObjects.Clear();
        currentInteractingObjectID = string.Empty;
        Debug.Log("已清空忽略碰撞物体列表");
        
        // 同步更新RobotCollisionManager
        if (RobotCollisionManager.Instance != null)
        {
            RobotCollisionManager.Instance.ClearIgnoredCollisionObjects();
        }
    }

    // 检查指定物体ID是否为当前交互物体或在忽略列表中
    public bool IsCurrentInteractingObject(string objectID)
    {
        if (string.IsNullOrEmpty(objectID))
            return false;
            
        // 检查当前交互物体
        if (objectID == currentInteractingObjectID)
            return true;
            
        // 检查忽略列表
        if (ignoredCollisionObjects.Contains(objectID))
            return true;
            
        return false;
    }
}