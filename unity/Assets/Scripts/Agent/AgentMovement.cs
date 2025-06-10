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
    public float rotationSpeed = 90.0f;  // Angle per second
    public GripperController gripperController; 
    public HandController handController;

    public ArticulationBody[] articulationChain;
    public List<GameObject> collidedObjects = new List<GameObject>();
    public float stiffness = 10000f;    // stiffness
    public float damping = 100f;       // damping
    public float forceLimit = 1000f;  // force limit
    public float speed = 30f;         // speed, unit: degree/second
    public float torque = 100f;       // torque, unit: Nm
    public float acceleration = 10f;  // acceleration

    public Transform target;
    private Vector3 lastTargetPosition;
    private float positionChangeThreshold = 0.0001f; // position change threshold
    public IK_X1 ikX1; 
    public IK_H1 ikH1;


    public List<float> targetJointAngles = new List<float> { 0, 0, 0, 0, 0, 0 }; // initial value
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
    public List<ArticulationBody> leftArmJoints;  // left arm joints
    public List<ArticulationBody> rightArmJoints; // right arm joints

    private bool hasMovedToPosition = false; // toggle function to mark if the target position has been reached
    private bool isTargetAnglesUpdated = false;
    private bool isManualControlEnabled = false;
    private float manualMoveSpeed = 5.0f;
    private float manualRotateSpeed = 60.0f;
    private float sprintMultiplier = 3f; // sprint multiplier
    private float mouseSensitivity = 2.0f;
    private float verticalRotation = 0f;
    private Transform cameraTransform; // camera transform
    private float maxVerticalAngle = 80f; // maximum vertical angle
    private bool isMouseUnlocked = false; // mark if ESC is pressed to unlock mouse

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
            Debug.LogError("No joints found, please ensure the object has ArticulationBody component!");
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

        // record default xDrive values
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
            if (body.jointType == ArticulationJointType.RevoluteJoint) // only execute when RevoluteJoint type
            {
                Transform collisionsTransform = body.transform.Find("Collisions");
                if (collisionsTransform != null)
                {
                    foreach (Collider collider in collisionsTransform.GetComponentsInChildren<Collider>())
                    {
                        GameObject colliderObj = collider.gameObject;

                        // // // add rigidbody component
                        Rigidbody rb = colliderObj.GetComponent<Rigidbody>();
                        if (rb == null)
                        {
                            rb = colliderObj.AddComponent<Rigidbody>();
                            rb.constraints = RigidbodyConstraints.FreezeAll; // freeze all positions and rotations
                        }

                        CollisionHandler handler = colliderObj.AddComponent<CollisionHandler>();
                        // Debug.Log("add collision Handler");
                        handler.OnCollisionEnterEvent += (collision) => HandleCollision(body, collision, colliderObj);
                    }
                }
            }
        }
    }



    // modify HandleCollision method, add colliderObj parameter
    private void HandleCollision(ArticulationBody articulationBody, Collision collision, GameObject colliderObj)
    {
        // Debug.Log($"碰撞检测: ArticulationBody: {articulationBody.name}, Collider: {colliderObj.name}, 碰撞对象: {collision.gameObject.name}");
        
        // add to collision list
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
        
        // check if it is a collision with the current interacting object, use RobotCollisionManager's judgment logic
        bool isInteractingWithTarget = false;
        
        // if RobotCollisionManager exists, use its judgment
        if (RobotCollisionManager.Instance != null)
        {
            isInteractingWithTarget = RobotCollisionManager.Instance.IsInteractingObject(collision.gameObject);
            if (isInteractingWithTarget)
            {
                Debug.Log($"Collision with interacting object, not trigger failure (judged by RobotCollisionManager)");
            }
        }
        else
        {
            // as a backup, keep the original judgment logic
            SimObjPhysics collisionPhysics = collision.gameObject.GetComponent<SimObjPhysics>();
            if (collisionPhysics != null && 
                (collisionPhysics.ObjectID == currentInteractingObjectID || 
                 ignoredCollisionObjects.Contains(collisionPhysics.ObjectID)))
            {
                isInteractingWithTarget = true;
                Debug.Log($"Collision with interacting object, ID: {collisionPhysics.ObjectID}, not trigger failure (judged by local)");
            }
        }
        
        if(!collisionDetected && !isInteractingWithTarget){
            for(int i=0;i<collidedObjects.Count;i++){
                if(!sceneManager.ObjectsInOperation.Contains(collidedObjects[i])){
                    // Debug.Log($"Detect abnormal collision: ArticulationBody: {articulationBody.name}, Collider: {colliderObj.name}, collision object: {collision.gameObject.name}");
                    collisionDetected = true;
                    collisionA = articulationBody.name;
                    collisionB = collision.gameObject.name;
                    
                    // notify RobotCollisionManager to record this collision
                    if (RobotCollisionManager.Instance != null)
                    {
                        RobotCollisionManager.Instance.ReportCollision(articulationBody, collision.gameObject);
                    }
                }
            }
        }
    }

    public void ClearCollisions(){
        // clear physical collision state
        collisionDetected = false;
        collisionA = string.Empty;
        collisionB = string.Empty;
        
        // clear collision object list
        collidedObjects.Clear();
        
        // clear current interacting object ID
        currentInteractingObjectID = string.Empty;
        
        // clear collision state in RobotCollisionManager
        if (RobotCollisionManager.Instance != null)
        {
            RobotCollisionManager.Instance.ClearAllCollisions();
            Debug.Log("已清理RobotCollisionManager中的所有碰撞");
        }
        
        // clear raycast collision state
        // find current robot's collision detector
        GameObject currentRobot = CurrentRobotType == RobotType.H1 ? robots[1] : robots[0];
        if (currentRobot != null)
        {
            StructureCollisionDetector detector = currentRobot.GetComponent<StructureCollisionDetector>();
            if (detector != null)
            {
                // use public method to clear collision state
                detector.ClearCollisionState();
            }
        }
        
        // clear collision state of all robot parts
        foreach (var robot in robots)
        {
            if (robot != null && robot.activeSelf)
            {
                // find and clear all collision reporters
                CollisionReporter[] reporters = robot.GetComponentsInChildren<CollisionReporter>(true);
                foreach (var reporter in reporters)
                {
                    // here, you may need to add a method to reset the collision state
                    // or directly handle the associated collision processor
                    if (reporter != null)
                    {
                        Debug.Log($"Reset collision reporter: {reporter.gameObject.name}");
                    }
                }
                
                // find all joints and check collisions
                ArticulationBody[] joints = robot.GetComponentsInChildren<ArticulationBody>(true);
                foreach (var joint in joints)
                {
                    // reset all joint's collision state
                    if (joint != null)
                    {
                        joint.gameObject.SendMessage("ClearCollision", null, SendMessageOptions.DontRequireReceiver);
                    }
                }
            }
        }
        
        Debug.Log("All collision states have been cleared");
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
            Debug.LogError("No joints found, please ensure the object has ArticulationBody component!");
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

        // update the last executed action name in scene state manager
        sceneManager.UpdateLastAction(actionData.action);

        // check action success rate based on action type and objectID
        bool shouldExecuteAction = true;
        string errorMessage = string.Empty;
        string targetState = string.Empty;

        // check if the action needs to be checked for success rate
        if (NeedsSuccessRateCheck(actionData.action))
        {
            // get action config information
            var config = sceneManager.GetActionConfig(actionData.action, actionData.objectID);

            // random judge success or failure
            float randomValue = UnityEngine.Random.value;
            // float randomValue = 0.98f;
            if (randomValue > config.successRate)
            {
                // if random value is greater than success rate, the action fails
                shouldExecuteAction = false;
                
                (errorMessage, targetState) = config.GetRandomErrorMessage();
                Debug.Log($"Action {actionData.action} success rate check result: failed, error message: {errorMessage}, target state: {targetState}");
            }
            else
            {
                // if random value is less than or equal to success rate, the action succeeds
                shouldExecuteAction = true;
                Debug.Log($"Action {actionData.action} success rate check result: success");
            }
        }
        
        if (!shouldExecuteAction)
        {
            // if the action should not be executed, but has target state, try to call the corresponding method based on the target state
            if (!string.IsNullOrEmpty(targetState) && !string.IsNullOrEmpty(actionData.objectID))
            {
                // get target object
                GameObject targetObject = null;
                if (sceneManager.SimObjectsDict.TryGetValue(actionData.objectID, out targetObject) && targetObject != null)
                {
                    bool stateExecuted = false;
                    
                    // find the corresponding component based on the target state
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
                        // can add more states...
                    }
                    
                    // if the corresponding component is found, execute the state change
                    if (stateComponent != null)
                    {
                        stateComponent.Execute();
                        stateExecuted = true;
                        Debug.Log($"Automatically executed the {targetState} state change of object {actionData.objectID}");
                    }
                    
                    // if the state change is successfully executed, update the result
                    if (stateExecuted)
                    {
                        jsonData.success = true;
                        jsonData.msg = $"Automatically processed the object state to: {targetState}";
                        callback?.Invoke(jsonData);
                        return;
                    }
                }
            }
            
            // if the state change cannot be automatically processed, return the original failure result
            jsonData.success = false;
            jsonData.msg = errorMessage;
            
            // update the action execution status
            sceneManager.UpdateLastActionSuccess(actionData.action, actionData.objectID);
            
            // call the callback function
            callback?.Invoke(jsonData);
            return;
        }

        // the following is the original action processing logic
        try
        {
            Type thisType = this.GetType();
            
            // check if the parameters are valid
            if (string.IsNullOrEmpty(actionData.action))
            {
                Debug.LogError("Action name cannot be null or empty.");
                jsonData.success = false;
                jsonData.msg = "Action name cannot be null or empty.";
                callback?.Invoke(jsonData);
                return;
            }

            // method name processing
            string methodName = actionData.action;

            // try to get method information
            MethodInfo method = thisType.GetMethod(methodName, BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance | BindingFlags.IgnoreCase);
            
            if (method == null)
            {
                Debug.LogError($"Method {methodName} not found in {thisType.Name}.");
                jsonData.success = false;
                jsonData.msg = $"Method {methodName} not found.";
                callback?.Invoke(jsonData);
                return;
            }

            ClearCollidedObjects(); // clear previous collided objects
            
            // get method parameters
            ParameterInfo[] parameters = method.GetParameters();
            object[] args = ConstructArguments(parameters, actionData);

            // set the last action result to success (default)
            lastMoveSuccessful = true;

            // decide the calling method based on the return type of the method
            if (method.ReturnType == typeof(IEnumerator))
            {
                // coroutine method
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
                    Debug.Log("place args length: "+args.Length);
                    for(int i=0;i<args.Length;i++){
                        Debug.Log($"place args {i}: {args[i]}");
                    }
                
                    StartCoroutine(Place((string)args[0], (bool)args[1], callback, (string)args[3]));
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
                // method that directly returns JsonData
                jsonData = (JsonData)method.Invoke(this, args);
                callback?.Invoke(jsonData);
            }
            else
            {
                // other methods (assume return bool or void)
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
                
                // update the action execution status
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
    
    // add: auxiliary method to determine if success rate check is needed
    private bool NeedsSuccessRateCheck(string actionName)
    {
        if (string.IsNullOrEmpty(actionName))
            return false;
            
        // list the action types that do not need to be checked for success rate
        string[] actionsToSkip = {
            "undo", "redo", "loadstate", "loadrobot", 
            "resetpose", "resetscene", "getcurstate", "resetstate"
        };
        
        // check if the action is in the list of actions to skip
        foreach (var skipAction in actionsToSkip)
        {
            if (actionName.ToLower().Equals(skipAction.ToLower()))
                return false;
        }
            
        // list the action types that need to be checked for success rate
        string[] actionsToCheck = {
            "pick", "place", "toggle", "open",
            "slice", "break", "fill", "empty",
            "cook", "clean", "move", "rotate"
        };
        
        // check if the action name is in the list (not case-sensitive)
        foreach (var action in actionsToCheck)
        {
            if (actionName.ToLower().Contains(action.ToLower()))
                return true;
        }
        
        // by default, do not check
        return false;
    }

    // define JsonData class to store the return result
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
            // call the coroutine method but do not wait for it to complete
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
        
        // execute the coroutine outside the try-catch block
        if (coroutine != null)
        {
            // execute the coroutine until it is completed
            while (coroutine.MoveNext())
            {
                yield return coroutine.Current;
            }
            
            // check the final result of the coroutine
            if (coroutine.Current is JsonData coroutineResult)
            {
                jsonData = coroutineResult;
            }
            else
            {
                // if the coroutine does not return a specific result, consider it successful
                jsonData.success = true;
                jsonData.msg = $"Action {actionData.action} completed successfully.";
            }
            
            Debug.Log($"Coroutine action completed: {actionData.action}, initial success: {jsonData.success}");
        }
        else
        {
            // if the coroutine is empty
            jsonData.success = false;
            jsonData.msg = "无法执行动作：协程初始化失败";
            Debug.LogError($"协程初始化失败: {actionData.action}");
        }
        
        // call the callback function, pass the result
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
            // explicitly map according to parameter name
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
                case "container":
                    if (!string.IsNullOrEmpty(actionData.container))
                    {
                        Debug.Log("container: " + actionData.container);
                        args.Add(actionData.container);
                    }
                    else
                    {
                        args.Add(null); // Default to null for optional container parameter
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
        // record the current interacting object
        SetCurrentInteractingObject(objectID);
        
        // check if there is a collision before execution
        if (collisionDetected || (RobotCollisionManager.Instance != null && RobotCollisionManager.Instance.HasAnyCollision()))
        {
            Debug.LogWarning("Toggle action started with collision, but will continue execution");
            // do not interrupt the operation
        }
        
        // mark as needing collision check, but continue execution
        collisionDetected = false;
        
        // reset the state flag
        hasMovedToPosition = false;

        // get the interacting point of the object
        Transform interactablePoint = SceneStateManager.GetInteractablePoint(objectID);
        if (interactablePoint == null)
        {
            Debug.LogError($"未找到ID为 {objectID} 的物品的交互点");
            // add: construct the failure result and callback
            JsonData jsonData = new JsonData();
            jsonData.success = false;
            jsonData.msg = $"未找到ID为 {objectID} 的物品的交互点";
            callback?.Invoke(jsonData);
            yield break;
        }

        // move the arm to the position
        yield return StartCoroutine(ArmMovetoPosition(interactablePoint.position, isLeftArm));

        // check if there is a collision during the movement
        if (collisionDetected || (RobotCollisionManager.Instance != null && RobotCollisionManager.Instance.HasAnyCollision()))
        {
            Debug.LogWarning("移动到交互点时发生碰撞，但将继续执行");
            // do not interrupt the operation
        }

        // wait for a short period of time
        yield return new WaitForSeconds(0.5f);
        
        // clean the flag
        collisionDetected = false;
        collisionA = "";
        collisionB = "";

        // get the gripper controller
        GripperController gripperController = GetComponent<GripperController>();

        // check if the gripper has reached the target position
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
            // check if the gripper is near the interacting point (within 10 cm)
            float distance = Vector3.Distance(gripperTransform.position, interactablePoint.position);
            reachedTargetPosition = distance < 0.3f;
            Debug.Log($"Toggle operation: gripper to interacting point distance is {distance} meters");
        }
        
        // if the gripper has not reached the target position, end the coroutine early
        if (!reachedTargetPosition)
        {
            Debug.LogError($"Toggle operation failed: gripper not reached the target position");
            lastMoveSuccessful = false;
            
            // update the action result in the scene state manager
            if (sceneManager != null)
            {
                sceneManager.UpdateLastActionSuccess("toggle");
                // set the error message
                if (sceneManager.GetCurrentSceneStateA2T() != null && sceneManager.GetCurrentSceneStateA2T().agent != null)
                {
                    sceneManager.GetCurrentSceneStateA2T().agent.errorMessage = "Gripper not reached the target position";
                }
            }
            // add: construct the failure result and callback
            JsonData jsonData = new JsonData();
            jsonData.success = false;
            jsonData.msg = "Gripper not reached the target position";
            callback?.Invoke(jsonData);
            yield break;
        }

        // check if the Toggle operation is successful (independent of collision)
        bool toggleSuccess = reachedTargetPosition;
        
        // try to find the object and switch the state (try to switch regardless of success, to avoid getting stuck)
        SimObjPhysics[] allObjects = FindObjectsOfType<SimObjPhysics>();
        foreach (SimObjPhysics obj in allObjects)
        {
            if (obj.ObjectID == objectID && obj.IsToggleable)
            {
                CanToggleOnOff toggleComponent = obj.GetComponent<CanToggleOnOff>();
                if (toggleComponent != null)
                {
                    toggleComponent.Toggle();
                    Debug.Log($"The state of object {objectID} has been switched to: {(toggleComponent.isOn ? "on" : "off")}");
                    
                    // if the physical state has been switched, it is considered successful (priority over position judgment)
                    toggleSuccess = true;
                }
                break;
            }
        }
        
        if (toggleSuccess)
        {
            Debug.Log($"Toggle operation successful: object {objectID} state has been switched");
        }
        else
        {
            Debug.LogWarning($"Toggle operation failed: gripper not reached the target object {objectID} position nearby");
        }
        
        // mark as completed
        hasMovedToPosition = true;
        
        yield return StartCoroutine(ToggleSuccess(objectID, isLeftArm));
        
        // regardless of whether there is a collision, return the result based on the success of the Toggle operation
        lastMoveSuccessful = toggleSuccess;
        
        // update the action result in the scene state manager
        if (sceneManager != null)
        {
            sceneManager.UpdateLastActionSuccess("toggle");
        }
        // add: callback when successful
        JsonData successData = new JsonData();
        successData.success = toggleSuccess;
        successData.msg = toggleSuccess ? "Toggle operation successful" : "Toggle operation failed";
        callback?.Invoke(successData);
    }

    public IEnumerator Open(string objectID, bool isLeftArm, Action<JsonData> callback)
    {
        // record the current interacting object
        SetCurrentInteractingObject(objectID);
        
        // check if there is a collision before execution
        if (collisionDetected || (RobotCollisionManager.Instance != null && RobotCollisionManager.Instance.HasAnyCollision()))
        {
            Debug.LogWarning("Open action started with collision, but will continue execution");
            // do not interrupt the operation
        }
        
        // mark as needing collision check, but continue execution
        collisionDetected = false;
        
        // reset the state flag
        hasMovedToPosition = false;

        // get the interacting point of the object
        Transform interactablePoint = SceneStateManager.GetInteractablePoint(objectID);
        if (interactablePoint == null)
        {
            Debug.LogError($"未找到ID为 {objectID} 的物品的交互点");
            // add: construct the failure result and callback
            JsonData jsonData = new JsonData();
            jsonData.success = false;
            jsonData.msg = $"未找到ID为 {objectID} 的物品的交互点";
            callback?.Invoke(jsonData);
            yield break; // this check is still retained because the missing interacting point cannot continue
        }

        // move the arm to the position
        yield return StartCoroutine(ArmMovetoPosition(interactablePoint.position, isLeftArm));

        // check if there is a collision during the movement
        if (collisionDetected || (RobotCollisionManager.Instance != null && RobotCollisionManager.Instance.HasAnyCollision()))
        {
            Debug.LogWarning("移动到交互点时发生碰撞，但将继续执行");
            // do not interrupt the operation
        }

        // wait for a short period of time
        yield return new WaitForSeconds(0.5f);
        
        // clean the flag
        collisionDetected = false;
        collisionA = "";
        collisionB = "";

        // get the gripper controller
        GripperController gripperController = GetComponent<GripperController>();
        
        // check if the gripper has reached the target position
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
            // check if the gripper is near the interacting point (within 10 cm)
            float distance = Vector3.Distance(gripperTransform.position, interactablePoint.position);
            reachedTargetPosition = distance < 0.3f;
            Debug.Log($"Open operation: gripper to interacting point distance is {distance} meters");
        }
        
        // if the gripper has not reached the target position, end the coroutine early
        if (!reachedTargetPosition)
        {
            Debug.LogError($"Open operation failed: gripper not reached the target position");
            lastMoveSuccessful = false;
            
            // update the action result in the scene state manager
            if (sceneManager != null)
            {
                sceneManager.UpdateLastActionSuccess("open");
                // set the error message
                if (sceneManager.GetCurrentSceneStateA2T() != null && sceneManager.GetCurrentSceneStateA2T().agent != null)
                {
                    sceneManager.GetCurrentSceneStateA2T().agent.errorMessage = "Gripper not reached the target position";
                }
            }
            // add: construct the failure result and callback
            JsonData jsonData = new JsonData();
            jsonData.success = false;
            jsonData.msg = "Gripper not reached the target position";
            callback?.Invoke(jsonData);
            yield break;
        }

        // check if the Open operation is successful (independent of collision)
        bool openSuccess = reachedTargetPosition;
        
        // try to find the object and open it (try to open regardless of success, to avoid getting stuck)
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
                    
                    // if the state has changed, it is considered successful
                    if (wasOpen != openComponent.isOpen)
                    {
                        openSuccess = true;
                        Debug.Log($"Object {objectID} has been {(openComponent.isOpen ? "opened" : "closed")}");
                    }
                    else if (openComponent.isOpen)
                    {
                        Debug.Log($"Object {objectID} is already open");
                    }
                    break;
                }
            }
        }
        
        if (openSuccess)
        {
            Debug.Log($"Open operation successful: object {objectID} state has been changed");
        }
        else
        {
            Debug.LogWarning($"Open operation failed: gripper not reached the target object {objectID} position nearby or state not changed");
        }
        
        // mark as completed
        hasMovedToPosition = true;
        
        yield return StartCoroutine(OpenSuccess(objectID, isLeftArm));
        
        // regardless of whether there is a collision, return the result based on the success of the Open operation
        lastMoveSuccessful = openSuccess;
        
        // update the action result in the scene state manager
        if (sceneManager != null)
        {
            sceneManager.UpdateLastActionSuccess("open");
        }
        // add: callback when successful
        JsonData successData = new JsonData();
        successData.success = openSuccess;
        successData.msg = openSuccess ? "Open operation successful" : "Open operation failed";
        callback?.Invoke(successData);
    }



    public IEnumerator Lift(string objectID){

        // find the lift points of the object
        Transform[] liftPoints = SceneStateManager.GetLiftPoints(objectID);

        if (liftPoints == null)
        {
            Debug.LogError($"Lift action failed: objectID '{objectID}' not found.");
            yield break;
        }

        // move the left arm to liftPoints[0]
        yield return StartCoroutine(ArmMovetoPosition(liftPoints[0].position, true));

        yield return new WaitForSeconds(1f);
        // move the right arm to liftPoints[1]
        yield return StartCoroutine(ArmMovetoPosition(liftPoints[1].position, false));

        yield return new WaitForSeconds(1f);
        // check if the gripper has reached the target position
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
            
        // check if the gripper is near the target position (within 10 cm)
        bool reachedTargetPosition = false;
        float leftDistance = 0;
        float rightDistance = 0;

        

        if (leftGripperTransform != null)
        {
            leftDistance = Vector3.Distance(leftGripperTransform.position, liftPoints[0].position);
            Debug.Log($"Left gripper to target point distance: {leftDistance} meters");
        }
        if (rightGripperTransform != null)
        {
            rightDistance = Vector3.Distance(rightGripperTransform.position, liftPoints[1].position);

            Debug.Log($"Right gripper to target point distance: {rightDistance} meters");
        }

        if (leftDistance < 0.3f && rightDistance < 0.3f)
        {
            reachedTargetPosition = true;
        }
        
        // if the gripper has not reached the target position, end the coroutine and return the error information
        if (!reachedTargetPosition)
        {
            Debug.LogError($"Lift operation failed: gripper not reached the target position");
            lastMoveSuccessful = false;
            
            // update the action result in the scene state manager
            if (sceneManager != null)
            {
                sceneManager.UpdateLastActionSuccess("lift");
                // set the error message
                if (sceneManager.GetCurrentSceneStateA2T() != null && sceneManager.GetCurrentSceneStateA2T().agent != null)
                {
                    sceneManager.GetCurrentSceneStateA2T().agent.errorMessage = "Gripper not reached the target position";
                }
            }

            yield break;
        }


        sceneManager.SetParent(gripperController.currentLeftLeftGripper.transform, objectID);

        yield return new WaitForSeconds(1f);

        bool lastLiftSuccess = true;
        
        // if the gripper has not reached the target position, end the coroutine and return the error information
        if (!lastLiftSuccess)
        {
            Debug.LogError($"Lift operation failed: gripper not reached the target position");
            lastMoveSuccessful = false;
            
            // update the action result in the scene state manager
            if (sceneManager != null)
            {
                sceneManager.UpdateLastActionSuccess("lift");
                // set the error message
                if (sceneManager.GetCurrentSceneStateA2T() != null && sceneManager.GetCurrentSceneStateA2T().agent != null)
                {
                    sceneManager.GetCurrentSceneStateA2T().agent.errorMessage = "Gripper not reached the target position";
                }
            }

            yield break;
        }

        yield return new WaitForSeconds(1f);

    }
    public JsonData TP(string objectID)
    {
        // find the transfer point of the object
        Transform transferPoint = SceneStateManager.GetTransferPointByObjectID(objectID);

        if (transferPoint == null)
        {
            Debug.LogError($"TP action failed: objectID '{objectID}' not found.");
            
            return new JsonData{success = false, msg = "cannot find the transfer point of the object"};
        }

        // start the transfer
        StartCoroutine(TransferToPose(transferPoint));

        Debug.Log($"Robot successfully transported to {objectID}'s TransferPoint");
        return new JsonData{success = true, msg = "Transfer successful"};
    }

    public IEnumerator Pick(string objectID, bool isLeftArm, Action<JsonData> callback)
    {
        // set the current interacting object ID
        SetCurrentInteractingObject(objectID);
        
        // check if there is a collision before execution
        if (collisionDetected || (RobotCollisionManager.Instance != null && RobotCollisionManager.Instance.HasAnyCollision()))
        {
            Debug.LogWarning("Pick action started with collision, but will continue execution");
            // do not interrupt the operation
        }

        // Clear the previous collision state
        ClearCollisions();
        // reset the current interacting object ID, because ClearCollisions will clear it
        SetCurrentInteractingObject(objectID);
        
        // reset the state flag
        hasMovedToPosition = false;
        
        if (CurrentRobotType == RobotType.X1)
        {
            Vector3 offset = new Vector3(0, 0.3f, 0);
            Transform pickPosition = SceneStateManager.GetInteractablePoint(objectID);

            if (pickPosition == null)
            {
                Debug.LogError($"cannot find the default interacting point of the objectID {objectID}, cannot execute the Pick action");
                // construct the failure result and callback
                JsonData jsonData = new JsonData();
                jsonData.success = false;
                jsonData.msg = $"cannot find the default interacting point of the objectID {objectID}";
                callback?.Invoke(jsonData);
                yield break;
            }

            Vector3 abovePickPosition = pickPosition.position + offset;

            // move to the above of the pick position
            Debug.Log($"move to the above of the pick position: {abovePickPosition}");
            yield return StartCoroutine(ArmMovetoPosition(abovePickPosition, isLeftArm));
            
            // check if there is a collision during the movement
            if (collisionDetected || (RobotCollisionManager.Instance != null && RobotCollisionManager.Instance.HasAnyCollision()))
            {
                if (collisionDetected)
                {
                    Debug.LogWarning($"collision occurred when moving to the above of the pick position, collision joint: {collisionA}, collision object: {collisionB}, but will continue execution");
                }
                else if (RobotCollisionManager.Instance != null)
                {
                    var collisions = RobotCollisionManager.Instance.GetAllNonInteractingCollisions();
                    if (collisions.Count > 0)
                    {
                        string collisionInfo = string.Join("\n", collisions.Select(c => $"joint: {c.Key.name}, collision object: {c.Value.name}"));
                        Debug.LogWarning($"collision occurred when moving to the above of the pick position (reported by RobotCollisionManager):\n{collisionInfo}\nbut will continue execution");
                    }
                    else
                    {
                        Debug.LogWarning("collision occurred when moving to the above of the pick position (reported by RobotCollisionManager), but no specific collision details found, will continue execution");
                    }
                }
                // do not interrupt the operation
            }
            
            yield return new WaitForSeconds(1f);

            // open the gripper to prepare for picking
            Debug.Log($"open the gripper to prepare for picking");
            gripperController.SetRobotGripper(RobotType.X1, isLeftArm, true);
            yield return new WaitForSeconds(1f);

            var center = pickPosition.position + new Vector3(0,0.1f,0);

            // move to the pick position
            Debug.Log($"move to the pick position: {center}");
            yield return StartCoroutine(ArmMovetoPosition(center, isLeftArm));
            
            // check if there is a collision during the movement
            if (collisionDetected || (RobotCollisionManager.Instance != null && RobotCollisionManager.Instance.HasAnyCollision()))
            {
                Debug.LogWarning("collision occurred when moving to the pick position, but will continue execution");
                // do not interrupt the operation
            }
            
            yield return new WaitForSeconds(1f);

            // check if the gripper has reached the target position
            Transform gripperTransform = null;
            if (isLeftArm && gripperController.currentLeftLeftGripper != null) 
            {
                gripperTransform = gripperController.currentLeftLeftGripper.transform;
            } 
            else if (!isLeftArm && gripperController.currentRightLeftGripper != null) 
            {
                gripperTransform = gripperController.currentRightLeftGripper.transform;
            }
            
            // check if the gripper is near the target position (within 10 cm)
            bool reachedTargetPosition = false;
            if (gripperTransform != null)
            {
                float distance = Vector3.Distance(gripperTransform.position, center);
                reachedTargetPosition = distance < 0.3f;
                Debug.Log($"gripper to target point distance: {distance} meters");
            }
            
            // if the gripper has not reached the target position, end the coroutine and return the error information
            if (!reachedTargetPosition)
            {
                Debug.LogError($"Pick operation failed: gripper not reached the target position");
                lastMoveSuccessful = false;
                
                // update the action result in the scene state manager
                if (sceneManager != null)
                {
                    sceneManager.UpdateLastActionSuccess("pick");
                    // set the error message
                    if (sceneManager.GetCurrentSceneStateA2T() != null && sceneManager.GetCurrentSceneStateA2T().agent != null)
                    {
                        sceneManager.GetCurrentSceneStateA2T().agent.errorMessage = "gripper not reached the target position";
                    }
                }
                // add: construct the failure result and callback
                JsonData jsonData = new JsonData();
                jsonData.success = false;
                jsonData.msg = "gripper not reached the target position";
                callback?.Invoke(jsonData);
                yield break;
            }

            // pick the object
            Debug.Log($"{(isLeftArm ? "left arm" : "right arm")} pick the object");
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
          
            // move to the above of the pick position
            Debug.Log($"move to the above of the pick position: {abovePickPosition}");
            yield return StartCoroutine(ArmMovetoPosition(abovePickPosition, isLeftArm));
            
            // check if there is a collision during the movement
            if (collisionDetected || (RobotCollisionManager.Instance != null && RobotCollisionManager.Instance.HasAnyCollision()))
            {
                Debug.LogWarning("collision occurred when moving to the above of the pick position, but will continue execution");
            }
            
            yield return new WaitForSeconds(1f);
            

            // adjust the rotation of the object to keep it orthogonal to the world axes
            AdjustRotationToWorldAxes(objectID);
        }
        else if (CurrentRobotType == RobotType.H1)
        {
            
            Transform interactablePoint = SceneStateManager.GetInteractablePoint(objectID);
            Transform transferPoint = SceneStateManager.GetTransferPointByObjectID(objectID);
            
            // check if there is a collision during the movement
            if (collisionDetected || (RobotCollisionManager.Instance != null && RobotCollisionManager.Instance.HasAnyCollision()))
            {
                Debug.LogWarning("collision occurred when moving the robot position, but will continue execution");
                // do not interrupt the operation
            }

            if (interactablePoint == null)
            {
                Debug.LogError($"未找到ID为 {objectID} 的物品的默认交互点，无法执行Pick动作");
                // add: construct the failure result and callback
                JsonData jsonData = new JsonData();
                jsonData.success = false;
                jsonData.msg = $"未找到ID为 {objectID} 的物品的默认交互点";
                callback?.Invoke(jsonData);
                yield break;
            }

            Vector3 pickPosition = interactablePoint.position + interactablePoint.forward * -0.1f + interactablePoint.up * 0.1f;
            Vector3 frontPickPosition = pickPosition + interactablePoint.up * 0.1f;

            // move to the front of the pick position
            Debug.Log($"move to the front of the pick position: {frontPickPosition}");
            yield return StartCoroutine(ArmMovetoPosition(frontPickPosition, isLeftArm));
            
            // check if there is a collision during the movement
            if (collisionDetected || (RobotCollisionManager.Instance != null && RobotCollisionManager.Instance.HasAnyCollision()))
            {
                Debug.LogWarning("collision occurred when moving to the front of the pick position, but will continue execution");
                // do not interrupt the operation
            }
            
            yield return new WaitForSeconds(1f);

            // open the gripper to prepare for picking
            Debug.Log($"open the {(isLeftArm ? "left arm" : "right arm")} gripper to prepare for picking");
            gripperController.SetRobotGripper(RobotType.H1, isLeftArm, true);
            yield return new WaitForSeconds(1f);

            // move to the pick position
            Debug.Log($"move to the pick position: {pickPosition}");
            yield return StartCoroutine(ArmMovetoPosition(pickPosition, isLeftArm));
            
            // check if there is a collision during the movement
            if (collisionDetected || (RobotCollisionManager.Instance != null && RobotCollisionManager.Instance.HasAnyCollision()))
            {
                Debug.LogWarning("collision occurred when moving to the pick position, but will continue execution");
                // do not interrupt the operation
            }
            
            yield return new WaitForSeconds(1f);
            
            // check if the gripper has reached the target position
            Transform h1GripperTransform = null;
            if (isLeftArm && gripperController.h1_leftArmLeftGripper != null)
            {
                h1GripperTransform = gripperController.h1_leftArmLeftGripper.transform;
            }
            else if (!isLeftArm && gripperController.h1_rightArmLeftGripper != null)
            {
                h1GripperTransform = gripperController.h1_rightArmLeftGripper.transform;
            }
            
            // check if the gripper is near the target position (within 10 cm)
            bool h1ReachedTargetPosition = false;
            if (h1GripperTransform != null)
            {
                float distance = Vector3.Distance(h1GripperTransform.position, pickPosition);
                h1ReachedTargetPosition = distance < 0.3f;
                Debug.Log($"H1 gripper to target point distance: {distance} meters");
            }
            
            // if the gripper has not reached the target position, end the coroutine and return the error information
            if (!h1ReachedTargetPosition)
            {
                Debug.LogError($"H1 Pick operation failed: gripper not reached the target position");
                lastMoveSuccessful = false;
                
                // update the action result in the scene state manager
                if (sceneManager != null)
                {
                    sceneManager.UpdateLastActionSuccess("pick");
                    // set the error message
                    if (sceneManager.GetCurrentSceneStateA2T() != null && sceneManager.GetCurrentSceneStateA2T().agent != null)
                    {
                        sceneManager.GetCurrentSceneStateA2T().agent.errorMessage = "gripper not reached the target position";
                    }
                }
                // add: construct the failure result and callback
                JsonData jsonData = new JsonData();
                jsonData.success = false;
                jsonData.msg = "gripper not reached the target position";
                callback?.Invoke(jsonData);
                yield break;
            }
            
            // grip the object
            Debug.Log($"{(isLeftArm ? "left arm" : "right arm")} grip the object");
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

            Debug.Log($"move to the front of the pick position: {frontPickPosition}");
            yield return StartCoroutine(ArmMovetoPosition(frontPickPosition, isLeftArm));
            
            // check if there is a collision during the movement
            if (collisionDetected || (RobotCollisionManager.Instance != null && RobotCollisionManager.Instance.HasAnyCollision()))
            {
                Debug.LogWarning("collision occurred when moving back, but will continue execution");
            }
            
            yield return new WaitForSeconds(1f);

            // adjust the rotation of the object to keep it orthogonal to the world axes
            AdjustRotationToWorldAxes(objectID);
        }
        
        // check if the object has been picked (independent of collisions)
        bool pickSuccess = false;
        GameObject pickedObject = null;
        
        // find the object
        SimObjPhysics[] allObjects = FindObjectsOfType<SimObjPhysics>();
        foreach (SimObjPhysics obj in allObjects)
        {
            if (obj.ObjectID == objectID)
            {
                pickedObject = obj.gameObject;
                break;
            }
        }
        
        // check if the object has been picked (independent of collisions)
        if (pickedObject != null)
        {
            Transform parent = pickedObject.transform.parent;
            if (parent != null && parent.CompareTag("Hand"))
            {
                pickSuccess = true;
                Debug.Log($"Pick operation successful: object {objectID} has become the child of the gripper");
            }
            else
            {
                Debug.LogWarning($"Pick operation failed: object {objectID} is not the child of the gripper");
            }
        }
        else
        {
            Debug.LogError($"Pick operation failed: cannot find the object {objectID}");
        }
        
        // mark the operation as completed
        hasMovedToPosition = true;
        
        yield return StartCoroutine(PickSuccess(objectID, isLeftArm));
        
        // regardless of whether there is a collision, return the result based on the success of the pick
        lastMoveSuccessful = pickSuccess;
        
        // update the action result in the scene state manager
        if (sceneManager != null)
        {
            sceneManager.UpdateLastActionSuccess("pick");
        }
        // add: construct the success result and callback
        JsonData successData = new JsonData();
        successData.success = pickSuccess;
        successData.msg = pickSuccess ? "Pick operation successful" : "Pick operation failed";
        callback?.Invoke(successData);
    }

    public IEnumerator Place(string objectID, bool isLeftArm, Action<JsonData> callback, string container = null)
    {
        // set the current interacting object
        SetCurrentInteractingObject(objectID);
        
        // check if there is a collision before execution
        if (collisionDetected || (RobotCollisionManager.Instance != null && RobotCollisionManager.Instance.HasAnyCollision()))
        {
            Debug.LogWarning("Place action started with collision, but will continue execution");
            // do not interrupt the operation
        }

        // add the collision object to the ignore list
        AddIgnoredCollisionObject(objectID);
        
        // mark the operation as needing collision check, but continue execution
        collisionDetected = false;
        
        // reset the state flag
        hasMovedToPosition = false;

        Vector3 targetPosition;
        Debug.Log($"Placing object {objectID} in container {container}");
        // If container is specified, move to container's interactive point first
        if (!string.IsNullOrEmpty(container))
        {
            Debug.Log($"Moving to container {container} interactive point...");
            Transform containerInteractivePoint = SceneStateManager.GetInteractablePoint(container);
            if (containerInteractivePoint != null)
            {
                // Apply vertical offset to the container's interactive point
                targetPosition = containerInteractivePoint.position + Vector3.up * 0.3f; // 10cm vertical offset
                Debug.Log($"Moving to container {container} interactive point: {targetPosition}");
            }
            else
            {
                Debug.LogError($"Cannot find interactive point for container: {container}");
                JsonData jsonData = new JsonData();
                jsonData.success = false;
                jsonData.msg = $"Cannot find interactive point for container: {container}";
                callback?.Invoke(jsonData);
                yield break;
            }
        }
        else
        {
            // Original logic: get the transfer point of the object
            Transform get_trans = SceneStateManager.GetTransferPointByObjectID(objectID);
            if (get_trans == null)
            {
                Debug.LogError($"未找到ID为 {objectID} 的物品传送点");
                JsonData jsonData = new JsonData();
                jsonData.success = false;
                jsonData.msg = $"未找到ID为 {objectID} 的物品传送点";
                callback?.Invoke(jsonData);
                yield break;
            }
            targetPosition = new Vector3(get_trans.position.x, get_trans.position.y + 0.05f, get_trans.position.z);
        }

        // move the robot arm to this position
        yield return StartCoroutine(ArmMovetoPosition(targetPosition, isLeftArm));

        // check if there is a collision during the movement
        if (collisionDetected || (RobotCollisionManager.Instance != null && RobotCollisionManager.Instance.HasAnyCollision()))
        {
            Debug.LogWarning("collision occurred when moving to the placement position, but will continue execution");
            // do not interrupt the operation
        }

        // get the gripper controller
        GripperController gripperController = GetComponent<GripperController>();
        
        // check if the gripper has reached the target position
        bool reachedTargetPosition = false;
        if (isLeftArm && gripperController.currentLeftLeftGripper != null)
        {
            reachedTargetPosition = Vector3.Distance(gripperController.currentLeftLeftGripper.transform.position, targetPosition) < 0.5f;
            Debug.Log($"Place operation: left gripper to placement point distance is {Vector3.Distance(gripperController.currentLeftLeftGripper.transform.position, targetPosition)} meters");
        }
        else if (!isLeftArm && gripperController.currentRightLeftGripper != null)
        {
            reachedTargetPosition = Vector3.Distance(gripperController.currentRightLeftGripper.transform.position, targetPosition) < 0.5f;
            Debug.Log($"Place operation: right gripper to placement point distance is {Vector3.Distance(gripperController.currentRightLeftGripper.transform.position, targetPosition)} meters");
        }
        
        // if the gripper has not reached the target position, end the coroutine
        if (!reachedTargetPosition)
        {
            Debug.LogError($"Place operation failed: gripper not reached the target position");
            lastMoveSuccessful = false;
            
            // update the action result in the scene state manager
            if (sceneManager != null)
            {
                sceneManager.UpdateLastActionSuccess("place");
                // set the error message
                if (sceneManager.GetCurrentSceneStateA2T() != null && sceneManager.GetCurrentSceneStateA2T().agent != null)
                {
                    sceneManager.GetCurrentSceneStateA2T().agent.errorMessage = "gripper not reached the target position";
                }
            }
            // add: construct the failure result and callback
            JsonData jsonData = new JsonData();
            jsonData.success = false;
            jsonData.msg = "gripper not reached the target position";
            callback?.Invoke(jsonData);
            yield break;
        }

        // open the gripper
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

        // wait for the gripper to open
        yield return new WaitForSeconds(0.5f);

        // release the object
        sceneManager.Release(objectID);
        
        // clean the flag
        collisionDetected = false;
        collisionA = "";
        collisionB = "";

        // check if the object has been placed (independent of collisions)
        bool placeSuccess = false;
        GameObject placedObject = null;
        bool isReleased = true;
        
        // find the object
        SimObjPhysics[] allObjects = FindObjectsOfType<SimObjPhysics>();
        foreach (SimObjPhysics obj in allObjects)
        {
            if (obj.ObjectID == objectID)
            {
                placedObject = obj.gameObject;
                break;
            }
        }
        
        // check if the object has been released from the gripper
        if (placedObject != null)
        {
            Transform parent = placedObject.transform.parent;
            if (parent != null && parent.CompareTag("Hand"))
            {
                isReleased = false;
                Debug.LogWarning($"Place operation failed: object {objectID} is still the child of the gripper");
            }
        }
        
        // check if the placement is successful: the gripper has reached the target position and the object has been released
        placeSuccess = reachedTargetPosition && isReleased;
        
        // If container is specified, also check if the object is properly placed in the container
        if (!string.IsNullOrEmpty(container) && placeSuccess)
        {
            // Wait a bit for physics to settle
            yield return new WaitForSeconds(0.5f);
            
            bool containerPlaceSuccess = CheckIfReceptacle(objectID, container);
            placeSuccess = placeSuccess && containerPlaceSuccess;
            
            if (!containerPlaceSuccess)
            {
                Debug.LogWarning($"Container placement failed: object {objectID} is not properly placed in container {container}");
            }
        }
        
        if (placeSuccess)
        {
            Debug.Log($"Place operation successful: the gripper has reached the target position and the object {objectID} has been released");
        }
        else
        {
            Debug.LogWarning($"Place operation failed: the gripper has reached the target position={reachedTargetPosition}, the object has been released={isReleased}");
        }
        
        // mark the operation as completed
        hasMovedToPosition = true;
        
        yield return StartCoroutine(PlaceSuccess(objectID, isLeftArm, container));
        
        // regardless of whether there is a collision, return the result based on the success of the place
        lastMoveSuccessful = placeSuccess;
        
        // update the action result in the scene state manager
        if (sceneManager != null)
        {
            sceneManager.UpdateLastActionSuccess("place");
        }
        
        // clear the current interacting object
        sceneManager.RemoveOperation(objectID);
        // add: construct the success result and callback
        JsonData successData = new JsonData();
        successData.success = placeSuccess;
        successData.msg = placeSuccess ? "Place operation successful" : "Place operation failed";
        callback?.Invoke(successData);
    }

    public IEnumerator ResetJoint(bool isLeftArm)
    {
        Debug.Log($"{(isLeftArm ? "left arm" : "right arm")} joint is being reset to the initial position...");

        List<float> initialAngles = new List<float>();
        var adjustments = isLeftArm ? this.adjustments : rightAdjustments;
        var joints = isLeftArm ? leftArmJoints : rightArmJoints;

        for (int i = 0; i < joints.Count; i++)
        {
            float initialAngle = (i == 0 || i == 4 || i == 3) ? -adjustments[i].angle : adjustments[i].angle;
            initialAngles.Add(initialAngle);
        }

        yield return StartCoroutine(SmoothUpdateJointAngles(initialAngles, 2f, isLeftArm));

        Debug.Log($"{(isLeftArm ? "left arm" : "right arm")} joint has been successfully reset!");

        hasMovedToPosition = false; // mark as not reached the position
    }

    private void UpdateTargetJointAngles(List<float> updatedAngles)//event
    {
        targetJointAngles = updatedAngles;
        isTargetAnglesUpdated = true; // mark the angles as updated
    }

    private IEnumerator ArmMovetoPosition(Vector3 position, bool isLeftArm)
    {
        // request to calculate the target angles
        // ikClient.ProcessTargetPosition(position, isLeftArm);
        if (CurrentRobotType == RobotType.X1)
        {
            ikX1.ProcessTargetPosition(position, isLeftArm);
            // wait for the target angles to be updated
            yield return new WaitUntil(() => isTargetAnglesUpdated);

            //Debug.Log("ArmMovetoPosition Target Angles: " + string.Join(", ", targetJointAngles));

            // reset the flag
            isTargetAnglesUpdated = false;

            yield return StartCoroutine(SmoothUpdateJointAngles(targetJointAngles, 2f, isLeftArm));

            // clean the state, prevent the subsequent actions from being affected
            targetJointAngles.Clear();
        }else if (CurrentRobotType == RobotType.H1)
        {
            ikH1.ProcessTargetPosition(position, isLeftArm);
            
        }
    }




    private IEnumerator TransferToPose(Transform transfer)
    {
        // request to calculate the target angles
        // rootArt.TeleportRoot(transfer.position, transfer.rotation);
        // transform.position = transfer.position; // ensure to reach the target position

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
        transform.position = targetPosition; // ensure to reach the target position

    }

    public IEnumerator SmoothUpdateJointAngles(List<float> targetJointAngles, float duration, bool isLeftArm)
    {
        // output debug information
        Debug.Log("Target angles when entering SmoothUpdateJointAngles: " + string.Join(", ", targetJointAngles));

        List<float> startAngles = new List<float>();
        var joints = isLeftArm ? leftArmJoints : rightArmJoints;
        var adjustments = isLeftArm ? this.adjustments : rightAdjustments;

        foreach (var joint in joints)
        {
            startAngles.Add(NormalizeAngle(joint.xDrive.target));
        }

        // output the starting angles information
        Debug.Log("Starting joint angles: " + string.Join(", ", startAngles));

        float elapsedTime = 0f;

        while (elapsedTime < duration)
        {
            elapsedTime += Time.deltaTime;
            float t = Mathf.Clamp01(elapsedTime / duration);

            for (int i = 0; i < joints.Count; i++)
            {
                var joint = joints[i];
                var drive = joint.xDrive;
                
                // calculate the adjusted target angle
                float adjustedAngle = NormalizeAngle(targetJointAngles[i] + ((i == 0 || i == 4) ? -adjustments[i].angle : adjustments[i].angle));
                
                // calculate the shortest path angle difference
                float currentAngle = startAngles[i];
                float angleDiff = adjustedAngle - currentAngle;
                
                // check if there is a shorter path
                if (angleDiff > 180f)
                {
                    adjustedAngle -= 360f;
                }
                else if (angleDiff < -180f)
                {
                    adjustedAngle += 360f;
                }
                
                float interpolatedAngle = Mathf.Lerp(startAngles[i], adjustedAngle, t);

                drive.target = NormalizeAngle(interpolatedAngle);
                joint.xDrive = drive;
            }

            yield return null;
        }

        // set the final angle
        for (int i = 0; i < joints.Count; i++)
        {
            var joint = joints[i];
            var drive = joint.xDrive;
            
            float finalAdjustedAngle = NormalizeAngle(targetJointAngles[i] + ((i == 0 || i == 4) ? -adjustments[i].angle : adjustments[i].angle));
            
            // ensure to use the shortest path
            float currentAngle = startAngles[i];
            float angleDiff = finalAdjustedAngle - currentAngle;
            
            if (angleDiff > 180f)
            {
                finalAdjustedAngle -= 360f;
            }
            else if (angleDiff < -180f)
            {
                finalAdjustedAngle += 360f;
            }

            drive.target = NormalizeAngle(finalAdjustedAngle);
            joint.xDrive = drive;

            Debug.Log($"Joint {i + 1} final target angle: {NormalizeAngle(finalAdjustedAngle)}");
        }
    }


    // coroutine for smooth movement, using the local coordinate system direction
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
        
        // get the StructureCollisionDetector component
        StructureCollisionDetector collisionDetector = cur_robot.GetComponent<StructureCollisionDetector>();
        Debug.Log("collideStructure scripts: "+cur_robot.name);
        
        // check if the StructureCollisionDetector component exists
        if (collisionDetector == null)
        {
            Debug.LogError("StructureCollisionDetector component not found! Trying to add...");
            collisionDetector = cur_robot.AddComponent<StructureCollisionDetector>();
            collisionDetector.robotBodyTransform = transform;
            collisionDetector.raycastDistance = 0.3f; // set a smaller default value
        }
        
        // set the current moving direction - this is the key modification
        Vector3 worldMoveDirection = transform.TransformDirection(localDirection);
        collisionDetector.SetMoveDirection(worldMoveDirection);
        
        // output detailed debug information
        collisionDetector.LogDebugInfo("Movement started");
        
        // ensure the StructureCollisionDetector is correctly set
        if (collisionDetector != null)
        {
            // check if the reference of the robot body of the StructureCollisionDetector is correct
            if (collisionDetector.robotBodyTransform != transform)
            {
                collisionDetector.robotBodyTransform = transform;
                collisionDetector.ResetPosition();
                Debug.Log("The reference of the robot body of the StructureCollisionDetector has been updated");
            }
            
            // force to synchronize the position immediately
            collisionDetector.transform.position = transform.position;
            collisionDetector.transform.rotation = transform.rotation;
        }
        
        bool collideStructure = collisionDetector.CollideStructure;
        
        // use the enhanced raycast detection function to check if the moving path is safe
        float moveDistance = Vector3.Distance(startPosition, targetPosition);
        bool isSafeToMove = collisionDetector.IsSafeToMoveInDirection(localDirection, moveDistance * 1.1f);
        
        // output debug information
        Debug.Log($"Movement information - direction: {localDirection}, distance: {moveDistance:F3}, safe: {isSafeToMove}");
        Debug.Log($"Starting point: {startPosition}, target point: {targetPosition}");
        
        // check if there is any existing collision, if so, do not allow movement
        if (collisionDetected || (RobotCollisionManager.Instance != null && RobotCollisionManager.Instance.HasAnyCollision()))
        {
            Debug.LogWarning("Existing collision detected, cannot start movement");
            // set the movement success flag to false
            lastMoveSuccessful = false;
            yield break;
        }
        
        if (!isSafeToMove)
        {
            // check the collision object
            GameObject obstacle = collisionDetector.GetLastDetectedObstacle();
            if (obstacle != null)
            {
                Debug.LogWarning($"Movement path detected obstacle: {obstacle.name}, position: {obstacle.transform.position}");
                
                // check if the obstacle is really in the moving direction
                Vector3 directionToObstacle = obstacle.transform.position - transform.position;
                float angleToObstacle = Vector3.Angle(worldMoveDirection, directionToObstacle);
                
                Debug.Log($"Angle to obstacle: {angleToObstacle} degrees");
                
                // if the obstacle is behind the moving direction (angle>90 degrees), it may be a false detection
                if (angleToObstacle > 90f)
                {
                    Debug.LogWarning($"The obstacle is behind the moving direction, it may be a false detection, trying to continue movement");
                    isSafeToMove = true;
                }
                else
                {
                    // clearly mark as movement failed
                    lastMoveSuccessful = false;
                    yield break;
                }
            }
            else
            {
                // if there is no specific obstacle but still unsafe, reject movement
                Debug.LogWarning("Movement path is unsafe, cannot start movement");
                lastMoveSuccessful = false;
                yield break;
            }
        }

        // smooth movement
        while (elapsedTime < duration && !collisionDetector.CollideStructure)
        {
            Vector3 pos_temp = Vector3.Lerp(startPosition, targetPosition, elapsedTime / duration);
            
            // check if the short distance movement from the current position to the next position is safe
            Vector3 nextMoveDirection = pos_temp - transform.position;
            float nextMoveDistance = nextMoveDirection.magnitude;
            
            if (nextMoveDistance > 0.01f && !collisionDetector.IsSafeToMoveInDirection(nextMoveDirection, nextMoveDistance * 1.2f))
            {
                Debug.LogWarning($"Movement interrupted, current progress: {elapsedTime/duration:P0}");
                // if the movement is interrupted, mark as failed
                lastMoveSuccessful = false;
                break;
            }
            
            // move the robot body
            transform.position = pos_temp;
            rootArt.TeleportRoot(transform.position, originRot);
            
            // ensure the StructureCollisionDetector moves with the robot
            if (collisionDetector != null && collisionDetector.transform.position != transform.position)
            {
                // if the position offset is too large, force synchronization
                float positionDiff = Vector3.Distance(collisionDetector.transform.position, transform.position);
                if (positionDiff > 0.05f)
                {
                    Debug.LogWarning($"CollisionDetector position is inconsistent with the robot, offset: {positionDiff}, force synchronization");
                    collisionDetector.transform.position = transform.position;
                    collisionDetector.transform.rotation = transform.rotation;
                    collisionDetector.ResetPosition();
                }
            }
            
            elapsedTime += Time.deltaTime;
            yield return null;
        }

        // if the movement is interrupted, keep the failed status
        if (collisionDetector.CollideStructure)
        {
            Debug.LogWarning("Collision detected, movement interrupted");
            lastMoveSuccessful = false;
            yield break;
        }

        // final position safety check
        if (!collisionDetector.CollideStructure)
        {
            // check if the final movement is safe
            Vector3 finalMoveDirection = targetPosition - transform.position;
            float finalMoveDistance = finalMoveDirection.magnitude;
            Debug.Log("finalMoveDistance: "+finalMoveDistance);
            if (finalMoveDistance > 0.01f)
            {
                if(collisionDetector.IsSafeToMoveInDirection(finalMoveDirection, finalMoveDistance)){

                    transform.position = targetPosition;
                    rootArt.TeleportRoot(transform.position, originRot);
                    
                    // ensure the StructureCollisionDetector follows the final position
                    if (collisionDetector != null)
                    {
                        collisionDetector.transform.position = transform.position;
                        collisionDetector.transform.rotation = transform.rotation;
                    }
                    
                    Debug.Log("Successfully reached the target position");
                    // successfully completed the entire movement
                    lastMoveSuccessful = true;
                }else{
                    Debug.LogWarning("The final position may be unsafe, keep the current position");
                    lastMoveSuccessful = false;
                    yield break;
                }

            }else{
                transform.position = targetPosition;
                rootArt.TeleportRoot(transform.position, originRot);
                
                // ensure the StructureCollisionDetector follows the final position
                if (collisionDetector != null)
                {
                    collisionDetector.transform.position = transform.position;
                    collisionDetector.transform.rotation = transform.rotation;
                }
                
                Debug.Log("Successfully reached the target position");
                // successfully completed the entire movement
                lastMoveSuccessful = true;
            }
        }
        else
        {
            Debug.LogWarning("Collision detected, movement interrupted");
            lastMoveSuccessful = false;
            yield break;
        }

        // ensure the robot position is consistent with the actual position in the physical engine
        transform.position = cur_robot.transform.position;
        
        // finally ensure the StructureCollisionDetector is synchronized with the robot position
        if (collisionDetector != null)
        {
            collisionDetector.transform.position = transform.position;
            collisionDetector.transform.rotation = transform.rotation;
        }
        
        // clean the moving direction, no need to detect after movement
        collisionDetector.SetMoveDirection(Vector3.zero);
        
        // output detailed debug information
        collisionDetector.LogDebugInfo("Movement ended");
        
        // set a public variable to notify the caller if the movement is successful
        lastMoveSuccessful = lastMoveSuccessful;
    }

    // coroutine for smooth rotation, using the local coordinate system direction
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
        // reset the status before each movement
        lastMoveSuccessful = true;
        
        yield return SmoothMove(Vector3.forward, Magnitude, 1.0f); 
    }

    public IEnumerator MoveRight(float Magnitude)
    {
        // reset the status before each movement
        lastMoveSuccessful = true;
        
        yield return SmoothMove(Vector3.right, Magnitude, 1.0f);
    }

    public IEnumerator MoveBack(float Magnitude)
    {
        // reset the status before each movement
        lastMoveSuccessful = true;
        
        yield return SmoothMove(Vector3.back, Magnitude, 1.0f);
    }

    public IEnumerator MoveLeft(float Magnitude)
    {
        // reset the status before each movement
        lastMoveSuccessful = true;
        
        yield return SmoothMove(Vector3.left, Magnitude, 1.0f);
    }
    
    public IEnumerator MoveUp(float Magnitude)
    {
        // reset the status before each movement
        lastMoveSuccessful = true;
        
        yield return SmoothMove(Vector3.up, Magnitude * 0.1f, 1.0f);
    }

    public IEnumerator MoveDown(float Magnitude)
    {
        // reset the status before each movement
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
        

        bool result=sceneManager.Undo();

        return result;
    }
    public bool Redo()
    {

        bool result=sceneManager.Redo();

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


    public float NormalizeAngle(float angle)
    {
        while (angle > 180f) angle -= 360f;
        while (angle < -180f) angle += 360f;
        return angle;
    }
    private bool Probability(float successRate)
    {
        return UnityEngine.Random.value < successRate; // use the传入的 successRate 进行判断
    }
    private void ManualControl()
    {
        // ALT key is pressed to show the mouse for UI interaction
        if (Input.GetKey(KeyCode.LeftAlt))
        {
            Cursor.visible = true;
            Cursor.lockState = CursorLockMode.None;
        }
        else if (!isMouseUnlocked) // ALT released to re-lock the mouse
        {
            Cursor.visible = false;
            Cursor.lockState = CursorLockMode.Locked;
        }

        // get the basic movement input
        Vector3 moveDirection = new Vector3(
            Input.GetKey(KeyCode.D) ? 1 : Input.GetKey(KeyCode.A) ? -1 : 0,
            0,
            Input.GetKey(KeyCode.W) ? 1 : Input.GetKey(KeyCode.S) ? -1 : 0
        );

        // calculate the actual moving speed (hold Shift to accelerate)
        float currentSpeed = manualMoveSpeed * (Input.GetKey(KeyCode.LeftShift) ? sprintMultiplier : 1.0f);

        // apply the movement
        if (moveDirection != Vector3.zero)
            transform.position += transform.TransformDirection(moveDirection.normalized) * currentSpeed * Time.deltaTime;

        // get the mouse input
        float mouseX = Input.GetAxis("Mouse X") * mouseSensitivity;
        float mouseY = Input.GetAxis("Mouse Y") * mouseSensitivity;

        // update the vertical rotation angle
        verticalRotation -= mouseY; // note that it is a minus sign, because in Unity's coordinate system, up is the positive direction
        verticalRotation = Mathf.Clamp(verticalRotation, -maxVerticalAngle, maxVerticalAngle);

        // apply the rotation
        transform.Rotate(Vector3.up * mouseX, Space.World); // the horizontal rotation of the whole object
        cameraTransform.localRotation = Quaternion.Euler(verticalRotation, 0, 0); // only rotate the camera's up and down view
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
        // find the AgentMovement and call the LoadRobot method after the scene is loaded
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

        // unregister the event to avoid repeated calls
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

                gripperController.InitializeGripper(RobotType.X1);
                
                // ensure the collision detector manager exists
                EnsureCollisionDetectorManagerExists();
                
                // configure the collision detector of the X1 robot
                StructureCollisionDetector detector = activeRobot.GetComponent<StructureCollisionDetector>();
                if (detector != null)
                {
                    detector.robotBodyTransform = transform; // ensure the reference to the correct robot body
                    detector.ResetPosition(); // reset the position and offset
                    CollisionDetectorManager.Instance.RegisterDetector(detector);
                    Debug.Log("configured the collision detector for the X1 robot");
                }
                else
                {
                    detector = activeRobot.AddComponent<StructureCollisionDetector>();
                    detector.robotBodyTransform = transform;
                    // the collision detector manager will automatically configure other parameters
                    Debug.Log("add and configure the collision detector for the X1 robot");
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
                gripperController.InitializeGripper(RobotType.H1);
                // ensure the collision detector manager exists
                EnsureCollisionDetectorManagerExists();
                
                // set the ray distance of the X1 robot larger
                if (CollisionDetectorManager.Instance != null)
                {
                    CollisionDetectorManager.Instance.SetRayDistance(0.3f);
                }
                
                // configure the collision detector of the H1 robot
                detector = activeRobot.GetComponent<StructureCollisionDetector>();
                if (detector != null)
                {
                    detector.robotBodyTransform = transform; // ensure the reference to the correct robot body
                    detector.ResetPosition(); // reset the position and offset
                    CollisionDetectorManager.Instance.RegisterDetector(detector);
                    Debug.Log("configured the collision detector for the H1 robot");
                }
                else
                {
                    detector = activeRobot.AddComponent<StructureCollisionDetector>();
                    detector.robotBodyTransform = transform;
                    // the collision detector manager will automatically configure other parameters
                    Debug.Log("add and configure the collision detector for the H1 robot");
                }
                break;
            
            case "g1":
                activeRobot = robots[2];
                activeRobot.SetActive(true);
                CurrentRobotType = RobotType.G1;
                
                // ensure the collision detector manager exists
                EnsureCollisionDetectorManagerExists();
                
                // configure the collision detector of the G1 robot
                detector = activeRobot.GetComponent<StructureCollisionDetector>();
                if (detector != null)
                {
                    detector.robotBodyTransform = transform; // ensure the reference to the correct robot body
                    detector.ResetPosition(); // reset the position and offset
                    CollisionDetectorManager.Instance.RegisterDetector(detector);
                    Debug.Log("configured the collision detector for the G1 robot");
                }
                else
                {
                    detector = activeRobot.AddComponent<StructureCollisionDetector>();
                    detector.robotBodyTransform = transform;
                    // the collision detector manager will automatically configure other parameters
                    Debug.Log("add and configure the collision detector for the G1 robot");
                }
                break;
            
            default:
                Debug.LogError($"Unknown robot type: {robotType}");
                return false;
        }
        
        // ensure the collision detector updates the position immediately
        if (activeRobot != null)
        {
            // asynchronously update the position of the collision detector
            StructureCollisionDetector activeDetector = activeRobot.GetComponent<StructureCollisionDetector>();
            if (activeDetector != null)
            {
                // force to update the position immediately
                activeDetector.transform.position = transform.position;
                activeDetector.transform.rotation = transform.rotation;
                // call the reset position again to ensure the correct offset calculation
                activeDetector.ResetPosition();
                
                // output the detector information for debugging
                activeDetector.LogDebugInfo("after the robot is loaded");
            }
            
            // reset all detectors
            if (CollisionDetectorManager.Instance != null)
            {
                CollisionDetectorManager.Instance.ResetAllDetectors();
                CollisionDetectorManager.Instance.LogAllDetectorsInfo();
            }
        }
        
        return true;
    }

    // ensure the collision detector manager exists
    private void EnsureCollisionDetectorManagerExists()
    {
        if (CollisionDetectorManager.Instance == null)
        {
            GameObject managerObj = new GameObject("CollisionDetectorManager");
            CollisionDetectorManager manager = managerObj.AddComponent<CollisionDetectorManager>();
            manager.defaultRayDistance = 0.3f;  // set a smaller default detection distance
            manager.disableCollisionDetection = false;
            Debug.Log("created the collision detector manager");
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


    // add a method to handle collisions in the AgentMovement class
    private void HandleCollision(ArticulationBody articulationBody, Collision collision)
    {
        Debug.Log($"ArticulationBody {articulationBody.name} collided with {collision.gameObject.name}");
    }
    // collision detection method
    private void OnCollisionEnter(Collision collision)
    {
        // iterate through all the ArticulationBody
        foreach (ArticulationBody articulationBody in articulationChain)
        {
            // get all the colliders of the current ArticulationBody
            Collider[] colliders = articulationBody.GetComponents<Collider>();

            // check if the collider collides with other objects
            foreach (Collider collider in colliders)
            {
                if (collider.bounds.Intersects(collision.collider.bounds))
                {
                    // if the collided object is not an ArticulationBody, record it
                    if (collision.gameObject.GetComponent<ArticulationBody>() == null)
                    {
                        if (!collidedObjects.Contains(collision.gameObject))
                        {
                            collidedObjects.Add(collision.gameObject);
                            Debug.Log($"collided with {collision.gameObject.name}");
                        }
                    }
                }
            }
        }
    }
        // optional: clear the collision record
    public void ClearCollidedObjects()
    {
        collidedObjects.Clear();
    }

    private void AdjustRotationToWorldAxes(string objectID)
    {
        // find the object with the corresponding ID
        SimObjPhysics[] allObjects = FindObjectsOfType<SimObjPhysics>();
        foreach (SimObjPhysics obj in allObjects)
        {
            if (obj.ObjectID == objectID)
            {
                // adjust the rotation of the object to align with the world axes
                obj.transform.rotation = Quaternion.identity;
                return;
            }
        }
        
        Debug.LogWarning($"no object found with ID {objectID}, cannot adjust the rotation");
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

        string logMessage = $"{(isLeftArm ? "left arm" : "right arm")} initializes the joint adjustment information:\ndefault value:\n";

        for (int i = 0; i < joints.Count; i++)
        {
            var joint = joints[i];
            Vector3 initialRotation = joint.transform.localRotation.eulerAngles;

            // normalize the read angle
            initialRotation.x = NormalizeAngle(initialRotation.x);
            initialRotation.y = NormalizeAngle(initialRotation.y);
            initialRotation.z = NormalizeAngle(initialRotation.z);

            Vector3 defaultRotation = defaultRotations[i];
            float adjustmentAngle = (i == 0 || i == 4)
                ? defaultRotation.y - initialRotation.y
                : defaultRotation.x - initialRotation.x;

            adjustmentAngle = NormalizeAngle(adjustmentAngle);
            adjustments[i].angle = adjustmentAngle;

            logMessage += $"joint {i + 1} default rotation: {defaultRotation}\n" +
                         $"joint {i + 1} initial rotation: {initialRotation}\n" +
                         $"joint {i + 1} adjustment angle: {adjustmentAngle}\n";
        }

        Debug.Log(logMessage);
    }

    private Vector3 CalculateOffset(Transform target)
    {
        // calculate the relative position between the robot and the target object
        Vector3 relativePosition = target.position - transform.position;

        // determine if the z-axis offset is negative based on the z-axis value of the relative position
        float zOffset = relativePosition.z < 0 ? -0.1f : 0.1f;

        // return the calculated offset
        return new Vector3(0, 0f, zOffset);
    }

    // add a success mark at the end of the Pick method
    private IEnumerator PickSuccess(string objectID, bool isLeftArm)
    {
        Debug.Log($"Pick operation completed: object ID {objectID}, using {(isLeftArm ? "left arm" : "right arm")}");
        
        // wait for one frame to ensure all states are updated
        yield return null;
        
        // mark the interaction as completed
        hasMovedToPosition = true;
        
        // clear all collision states
        ClearCollisions();
        
        Debug.Log("Pick operation completed");
    }
    
    // add a success mark at the end of the Place method
    private IEnumerator PlaceSuccess(string objectID, bool isLeftArm, string container = null)
    {
        Debug.Log($"Place operation completed: object ID {objectID}, using {(isLeftArm ? "left arm" : "right arm")}");
        
        // wait for one frame to ensure all states are updated
        yield return null;
        
        // mark the interaction as completed
        hasMovedToPosition = true;
        
        // clear all collision states
        ClearCollisions();
        
        // Check if the object was successfully placed in the container
        bool containerPlaceSuccess = true; // Default to true for non-container placements
        if (!string.IsNullOrEmpty(container))
        {
            // Wait a bit more for physics to settle when placing in container
            yield return new WaitForSeconds(0.5f);
            
            containerPlaceSuccess = CheckIfReceptacle(objectID, container);
            Debug.Log($"Object {objectID} was {(containerPlaceSuccess ? "" : "not ")}successfully placed in container {container}");
            
            // Update the overall place success based on container placement
            if (!containerPlaceSuccess)
            {
                Debug.LogWarning($"Container placement failed: object {objectID} is not properly placed in container {container}");
            }
        }
        
        Debug.Log("Place operation completed");
        
        // Return the container placement result for use in the main Place method
        // Note: This is handled through the existing placeSuccess logic in the Place method
    }
    
    // add a success mark at the end of the Toggle method
    private IEnumerator ToggleSuccess(string objectID, bool isLeftArm)
    {
        Debug.Log($"Toggle operation completed: object ID {objectID}, using {(isLeftArm ? "left arm" : "right arm")}");
        
        // wait for one frame to ensure all states are updated
        yield return null;
        
        // mark the interaction as completed
        hasMovedToPosition = true;
        
        // clear all collision states
        ClearCollisions();
        
        Debug.Log("Toggle operation completed");
    }
    
    // add a success mark at the end of the Open method
    private IEnumerator OpenSuccess(string objectID, bool isLeftArm)
    {
        Debug.Log($"Open operation completed: object ID {objectID}, using {(isLeftArm ? "left arm" : "right arm")}");
        
        // wait for one frame to ensure all states are updated
        yield return null;
        
        // mark the interaction as completed
        hasMovedToPosition = true;
        
        // clear all collision states
        ClearCollisions();
        
        Debug.Log("Open operation completed");
    }

    // set the current interacting object ID method
    public void SetCurrentInteractingObject(string objectID)
    {
        currentInteractingObjectID = objectID;
        Debug.Log($"set the current interacting object ID: {objectID}");
        
        // synchronize the update of RobotCollisionManager
        if (RobotCollisionManager.Instance != null)
        {
            RobotCollisionManager.Instance.SetCurrentInteractingObject(objectID);
        }
    }
    
    // add a method to ignore collision objects
    public void AddIgnoredCollisionObject(string objectID)
    {
        if (!string.IsNullOrEmpty(objectID) && !ignoredCollisionObjects.Contains(objectID))
        {
            ignoredCollisionObjects.Add(objectID);
            Debug.Log($"add the ignored collision object ID: {objectID}");
            
            // synchronize the update of RobotCollisionManager
            if (RobotCollisionManager.Instance != null)
            {
                RobotCollisionManager.Instance.AddIgnoredCollisionObject(objectID);
            }
        }
    }
    
    // clear the ignored collision objects
    public void ClearIgnoredCollisionObjects()
    {
        ignoredCollisionObjects.Clear();
        currentInteractingObjectID = string.Empty;
        Debug.Log("clear the ignored collision objects");
        
        // synchronize the update of RobotCollisionManager
        if (RobotCollisionManager.Instance != null)
        {
            RobotCollisionManager.Instance.ClearIgnoredCollisionObjects();
        }
    }

    // check if the specified object ID is the current interacting object or in the ignored list
    public bool IsCurrentInteractingObject(string objectID)
    {
        if (string.IsNullOrEmpty(objectID))
            return false;
            
        // check the current interacting object
        if (objectID == currentInteractingObjectID)
            return true;
            
        // check the ignored list
        if (ignoredCollisionObjects.Contains(objectID))
            return true;
            
        return false;
    }

    // Check if an object is successfully placed in a receptacle container
    private bool CheckIfReceptacle(string objectID, string containerID)
    {
        if (string.IsNullOrEmpty(objectID) || string.IsNullOrEmpty(containerID))
            return false;

        // Find the container object
        SimObjPhysics[] allObjects = FindObjectsOfType<SimObjPhysics>();
        SimObjPhysics containerObject = null;
        SimObjPhysics placedObject = null;

        foreach (SimObjPhysics obj in allObjects)
        {
            if (obj.ObjectID == containerID)
            {
                containerObject = obj;
            }
            if (obj.ObjectID == objectID)
            {
                placedObject = obj;
            }
        }

        if (containerObject == null || placedObject == null)
        {
            Debug.LogWarning($"Cannot find container {containerID} or object {objectID}");
            return false;
        }

        // Check if the container has Contains components (receptacle trigger boxes)
        Contains[] containsComponents = containerObject.GetComponentsInChildren<Contains>();
        
        if (containsComponents.Length == 0)
        {
            Debug.LogWarning($"Container {containerID} has no receptacle trigger boxes");
            return false;
        }

        // Check if the placed object is inside any of the container's receptacle trigger boxes
        foreach (Contains contains in containsComponents)
        {
            List<string> containedObjectIDs = contains.CurrentlyContainedObjectIDs();
            if (containedObjectIDs.Contains(objectID))
            {
                Debug.Log($"Object {objectID} is successfully placed in container {containerID}");
                return true;
            }
        }

        Debug.Log($"Object {objectID} is not inside container {containerID}");
        return false;
    }
}