using UnityEngine;
using System.Collections.Generic;
using TMPro;
using System.Collections;
using System.Linq;
using System;
using System.IO;
using Newtonsoft.Json;
using Unity.VisualScripting;
public class SceneStateManager : MonoBehaviour
{
    

    private readonly List<SceneState> stateHistory = new();
    private readonly List<SceneStateA2T> stateHistoryA2T = new();

    private int currentStateIndex=-1;
    [SerializeField]
    private GameObject agent;

    [SerializeField]
    public ArticulationBody root;

    [SerializeField]
    TextMeshProUGUI stateIndexText;
    [SerializeField]
    TextMeshProUGUI maxStateIndexText;
    [Header("scene objects, including broken and sliced objects, visible in the unity editor")]
    [SerializeField]
    private List<GameObject> simObjects = new();//scene objects, including broken and sliced objects, visible in the unity editor
    [Header("scene objects that can be interacted with")]
    [SerializeField]
    private List<GameObject> interactableObjects = new();//scene objects that can be interacted with
    //[SerializeField]
    [Header("scene objects that can be interacted with in the view")]

    public List<GameObject> canInteractableObjects = new();//scene objects that can be interacted with in the view
    [Header("scene objects that can be transferred")]

    [SerializeField]
    private List<GameObject> transferPoints = new();//scene objects that can be transferred
    //[SerializeField]
    [Header("scene objects that can be interacted with in the view")]

    public List<GameObject> canTransferPoints = new();//scene objects that can be transferred in the view

    [Header("scene objects that can be operated")]
    [SerializeField]
    private List<GameObject> interactablePoints = new();//scene objects that can be operated

    private readonly Dictionary<string, GameObject> simObjectsDict = new();//scene objects dictionary, for quick lookup
    //private readonly Dictionary<string, GameObject> canInteractableObjectsDict = new();//scene objects dictionary, for quick lookup

    public List<GameObject> TransferPoints => transferPoints;
    //public List<GameObject> CanTransferPoints => canTransferPoints;

    public Dictionary<string, GameObject> SimObjectsDict => simObjectsDict;
    //public Dictionary<string , GameObject> CanInteractableObjectsDict => canInteractableObjectsDict;

    [SerializeField]
    public GetObjectsInView getObjectsInView;
    
    
    public List<GameObject> ObjectsInOperation=new  List<GameObject>();


    public Transform ObjectsParent = null;

    public CameraController camera_ctrl;

    public string ImagePath{
        get {
            return camera_ctrl != null ? camera_ctrl.imgeDir : string.Empty;
        }
        set {
            if (camera_ctrl != null) {
                camera_ctrl.imgeDir = value;
            }
        }
    }

    public Dictionary<string, ActionConfig> actionConfigs;

    // define ActionConfig class
    [Serializable]
    public class ActionConfig
    {
        public float successRate;
        public Dictionary<string, ErrorEffectConfig> errorMessages;
        // add object state based configuration
        public Dictionary<string, ObjectStateConfig> objectStateConfigs;

        public override string ToString()
        {
            string errorMessagesString = string.Join(", ", errorMessages.Select(kv => $"{kv.Key}: {kv.Value.probability}"));
            return $"Success Rate: {successRate}, Error Messages: [{errorMessagesString}]";
        }
    }

    // add error effect config class, for storing the probability and state effect of error messages
    [Serializable]
    public class ErrorEffectConfig
    {
        public float probability; // probability
        public string targetState; // target state
        
        // constructor, for easy conversion from old configuration
        public ErrorEffectConfig(float prob, string state = "")
        {
            probability = prob;
            targetState = state;
        }
        
        public override string ToString()
        {
            return $"{probability}(→{targetState})";
        }
    }

    // add object state config class, for storing the success rate configuration of specific object states
    [Serializable]
    public class ObjectStateConfig
    {
        public string objectType; // object type (cup, plate, etc.)
        public string stateCondition; // state condition (filled, broken, open, etc.)
        public float successRate; // success rate of this state
        public Dictionary<string, ErrorEffectConfig> errorMessages; // error messages and their probabilities of this state
    }

    void Start()
    {
        LoadActionConfigs();

        // find and fill the list of interactable objects
        FillList(simObjects, new[] { "Interactable", "DynamicAdd" });
        // find and fill the dictionary of interactable objects
        FillDict(simObjectsDict, new[] { "Interactable", "DynamicAdd" });
        

        // find and fill the list of interactable objects
        FillList(interactableObjects, new[] { "Interactable"});

        // find and fill the list of transfer points
        FillList(transferPoints, new[] { "TransferPoint" });

        // find and fill the list of interactable points
        FillList(interactablePoints, new[] { "InteractablePoint" });

        GameObject[] dynamicAdds = GameObject.FindGameObjectsWithTag("DynamicAdd");
        foreach (GameObject obj in dynamicAdds)
        {
            obj.SetActive(false);
        }
        

        
        StartCoroutine(DelayedSave());

        // save the initial state
        SaveCurrentState();
        Debug.Log($"Initial state saved - currentStateIndex: {currentStateIndex}");
    }
    
    private void LoadActionConfigs(){
        string path = Path.Combine(Application.streamingAssetsPath, "ErrorConfig.json");
        if (File.Exists(path))
        {
            string json = File.ReadAllText(path);
            try 
            {
                actionConfigs = JsonConvert.DeserializeObject<Dictionary<string, ActionConfig>>(json);
                Debug.Log("action config file loaded successfully");
            }
            catch (Exception e)
            {
                Debug.LogError($"failed to load action config file: {e.Message}");
                actionConfigs = CreateDefaultActionConfigs();
            }
        }
        else
        {
            Debug.LogError("ErrorConfig.json not found!");
            // create default configuration
            actionConfigs = CreateDefaultActionConfigs();
        }
    }

    // add method: create default configuration
    private Dictionary<string, ActionConfig> CreateDefaultActionConfigs()
    {
        var configs = new Dictionary<string, ActionConfig>();
        
        // add default pick action configuration
        var pickConfig = new ActionConfig
        {
            successRate = 0.91f,
            errorMessages = new Dictionary<string, ErrorEffectConfig> 
            {
                { "failed to pick up, object falls", new ErrorEffectConfig(0.05f, "") },
                { "failed to pick up object", new ErrorEffectConfig(0.04f, "") }
            },
            objectStateConfigs = new Dictionary<string, ObjectStateConfig>()
        };
        
        // add special state configuration for cup
        var filledCupConfig = new ObjectStateConfig
        {
            objectType = "Cup",
            stateCondition = "isFilled",
            successRate = 0.91f,
            errorMessages = new Dictionary<string, ErrorEffectConfig>
            {
                { "failed to pick up, cup broken", new ErrorEffectConfig(0.02f, "broken") },
                { "failed to pick up cup", new ErrorEffectConfig(0.03f, "") },
                { "failed to pick up, cup contents spilled", new ErrorEffectConfig(0.04f, "spilled") }
            }
        };
        
        pickConfig.objectStateConfigs.Add("Cup_filled", filledCupConfig);
        configs.Add("pick", pickConfig);
        
        // can add more default configurations...
        
        return configs;
    }

    // simplified configuration result class
    public class ActionConfigResult
    {
        public float successRate;
        public Dictionary<string, ErrorEffectConfig> errorMessages;
        
        public (string message, string effectState) GetRandomErrorMessage() {  
            float total = errorMessages.Values.Sum(e => e.probability);  
            if(total <= 0) return ("unknown error", "");  

            float randomValue = UnityEngine.Random.value;  
            float cumulative = 0;  
  
            foreach (var error in errorMessages) {  
                float normalizedProb = error.Value.probability / total; // 归一化  
                cumulative += normalizedProb;  
                if(randomValue <= cumulative) {  
                    return (error.Key, error.Value.targetState);  
                }  
            }  
            return ("unknown error", "");  
        }  

        public override string ToString()
        {
            return $"Success Rate: {successRate}, Error Messages: [{string.Join(", ", errorMessages.Select(kv => $"{kv.Key}: {kv.Value}"))}]";
        }
    }

    // add: get action config by object state
    public ActionConfigResult GetActionConfigByObjectState(string actionType, SimObjPhysics targetObj)
    {
        ActionConfigResult result = new ActionConfigResult
        {
            successRate = 0.95f, // default high success rate
            errorMessages = new Dictionary<string, ErrorEffectConfig>
            {
                { "operation failed", new ErrorEffectConfig(0.05f, "") }
            }
        };
        
        if (actionConfigs == null || !actionConfigs.TryGetValue(actionType.ToLower(), out ActionConfig config))
        {
            Debug.LogWarning($"no configuration found for action type: {actionType}, using default configuration");
            return result;
        }
        
        // use base configuration first
        result.successRate = config.successRate;
        result.errorMessages = new Dictionary<string, ErrorEffectConfig>(config.errorMessages);
        
        // if no target object or no object state configuration, use base configuration
        if (targetObj == null || config.objectStateConfigs == null || config.objectStateConfigs.Count == 0)
        {
            return result;
        }
        Debug.Log($"try to find specific configuration by object state");

        // try to find specific configuration by object state
        foreach (var stateConfig in config.objectStateConfigs)
        {

            // Debug.Log(targetObj.objType);
            Debug.Log($"check object type: {targetObj.Type} with configured object type: {stateConfig.Value.objectType}");
            // check if object type matches
            if (!targetObj.Type.ToString().Equals(stateConfig.Value.objectType, StringComparison.OrdinalIgnoreCase))
                continue;
                
            Debug.Log($"check object state: {targetObj.ObjectID} with configured object state: {stateConfig.Value.stateCondition}");
            // check if object state matches
            bool stateMatches = false;
            switch (stateConfig.Value.stateCondition)
            {
                case "isFilled":
                    Debug.Log($"check object {targetObj.ObjectID} fill state: {targetObj.GetComponent<Fill>().isFilled}");
                    stateMatches = targetObj.GetComponent<Fill>().isFilled;
                    break;
                case "isBroken":
                    stateMatches = targetObj.IsBreakable && targetObj.GetComponent<Break>().isBroken;
                    break;
                case "isOpen":
                    stateMatches = targetObj.IsOpenable && targetObj.GetComponent<CanOpen_Object>().isOpen;
                    break;
                case "isToggled":
                    stateMatches = targetObj.IsToggleable && targetObj.GetComponent<CanToggleOnOff>().isOn;
                    break;
                case "default":
                    stateMatches = true; // default state always matches
                    break;
                // can add more state checks...
            }
            
            // if state matches, use specific configuration
            if (stateMatches)
            {
                result.successRate = stateConfig.Value.successRate;
                result.errorMessages = stateConfig.Value.errorMessages;
                Debug.Log($"use specific state configuration for object {targetObj.ObjectID}: {stateConfig.Value.stateCondition}");
                break;
            }
        }
        
        return result;
    }

    // modify: return action config info, without random judgment
    public ActionConfigResult GetActionConfig(string actionType, string objectID)
    {
        // find target object
        SimObjPhysics targetObj = null;
        if (!string.IsNullOrEmpty(objectID))
        {
            SimObjPhysics[] allObjects = FindObjectsOfType<SimObjPhysics>();
            foreach (var obj in allObjects)
            {
                if (obj.ObjectID == objectID)
                {
                    targetObj = obj;
                    break;
                }
            }
        }
        
        // get action config by object state
        ActionConfigResult config = GetActionConfigByObjectState(actionType, targetObj);
        Debug.Log($"Action config: {config}");
        Debug.Log($"Action config successRate: {config.successRate}");
        
        // return full config info, let AgentMovement decide if success
        return config;
    }
    
    // 保留兼容现有代码，但不执行随机判断
    public (bool success, string errorMessage, string targetState) CheckActionSuccess(string actionType, string objectID)
    {
        // get config info
        var config = GetActionConfig(actionType, objectID);
        
        // for compatibility with existing code, return a default result
        // actual success/failure judgment is performed by AgentMovement
        return (true, string.Empty, string.Empty);
    }

    public bool UpdateLastActionSuccess(string actionType = null, string objectID = null)
    {
        if (stateHistoryA2T.Count > 0)
        {
            var currentAgent = stateHistoryA2T[currentStateIndex].agent;

            // 如果动作是移动相关且被标记为失败，直接返回失败结果
            if (actionType != null && (actionType.ToLower().Contains("move") || actionType.ToLower().Contains("rotate")))
            {
                // 检查是否有任何碰撞被报告或者移动动作失败
                AgentMovement agentMovement = FindObjectOfType<AgentMovement>();
                if (agentMovement != null && !agentMovement.lastMoveSuccessful)
                {
                    currentAgent.lastActionSuccess = false;
                    currentAgent.errorMessage = "moved blocked by obstacle or failed to move";
                    return false;
                }
            }
            
            // no longer check action success rate, let AgentMovement handle it
            return true;
        }
        else
        {
            Debug.LogWarning("No state history to update lastActionSuccess or errorMessage.");
            return false;
        }
    }

    // fill list method
    private void FillList(List<GameObject> list, string[] tags)
    {
        foreach (string tag in tags)
        {
            GameObject[] objects = GameObject.FindGameObjectsWithTag(tag); // find all objects with current tag

            foreach (var obj in objects)
            {
                if (tag == "TransferPoint") // check if it is TransferPoint tag
                {
                    if (obj.transform.parent != null) // check if object has parent
                    {
                        list.Add(obj.transform.parent.gameObject); // add parent object
                    }
                }
                else
                {
                    list.Add(obj); // add object itself
                }
            }
        }
    }

    // fill dictionary method
    private void FillDict(Dictionary<string, GameObject> dict, string[] tags)
    {
        foreach (string tag in tags)
        {
            GameObject[] objects = GameObject.FindGameObjectsWithTag(tag);
            foreach (GameObject obj in objects)
            {
                dict[obj.name] = obj;
            }
        }
    }

    private IEnumerator DelayedSave()
    {
        yield return new WaitForSeconds(1f);
        getObjectsInView.GetObjects();
        SaveCurrentState(); 
    }

    void Update()
    {
        foreach (GameObject obj in ObjectsInOperation)
        {
            AdjustRotationToWorldAxes(obj.transform);
        }
        
    }
    
    public void AdjustRotationToWorldAxes(Transform objectTransform)
    {

        if (objectTransform != null)
        {
            // adjust object rotation to align with world axes
            objectTransform.rotation = Quaternion.identity;
        }
    }

    public void SaveCurrentState()
    {
        Debug.Log($"Before SaveCurrentState - currentStateIndex: {currentStateIndex}");
        
        #region save version controlled scene info
        // save current state
        SceneState state = new()
        {
            id = currentStateIndex + 1,
            agentPosition = agent.transform.position,
            agentRotation = agent.transform.rotation,
            
            objects = new ObjectState[simObjects.Count]
        };
        
        // save robot joint angles
        AgentMovement agentMovement = agent.GetComponent<AgentMovement>();
        if (agentMovement != null && agentMovement.articulationChain != null)
        {
            state.jointAngles = new List<float>();
            state.gripperAngles = new List<float>();
            
            // save all joint angles
            foreach (ArticulationBody joint in agentMovement.articulationChain)
            {
                if (joint.jointType != ArticulationJointType.FixedJoint)
                {
                    // check if dofCount is greater than 0
                    if (joint.dofCount > 0)
                    {
                        state.jointAngles.Add(joint.jointPosition[0]);
                    }
                    else
                    {
                        // if joint is not initialized, add default value 0
                        state.jointAngles.Add(0f);
                        Debug.LogWarning($"joint {joint.name} not found valid position info, using default value 0");
                    }
                }
            }
            
            // save gripper angles
            if (agentMovement.gripperController != null)
            {
                // get current left arm gripper
                ArticulationBody leftArmLeftGripper = agentMovement.gripperController.currentLeftLeftGripper;
                ArticulationBody leftArmRightGripper = agentMovement.gripperController.currentLeftRightGripper;
                
                // get current right arm gripper
                ArticulationBody rightArmLeftGripper = agentMovement.gripperController.currentRightLeftGripper;
                ArticulationBody rightArmRightGripper = agentMovement.gripperController.currentRightRightGripper;
                
                // save left arm gripper angles
                if (leftArmLeftGripper != null)
                {
                    if (leftArmLeftGripper.dofCount > 0)
                    {
                        state.gripperAngles.Add(leftArmLeftGripper.jointPosition[0]);
                    }
                    else
                    {
                        state.gripperAngles.Add(0f);
                        Debug.LogWarning($"left arm left gripper {leftArmLeftGripper.name} not found valid position info, using default value 0");
                    }
                }
                
                if (leftArmRightGripper != null)
                {
                    if (leftArmRightGripper.dofCount > 0)
                    {
                        state.gripperAngles.Add(leftArmRightGripper.jointPosition[0]);
                    }
                    else
                    {
                        state.gripperAngles.Add(0f);
                        Debug.LogWarning($"left arm right gripper {leftArmRightGripper.name} not found valid position info, using default value 0");
                    }
                }
                
                // save right arm gripper angles
                if (rightArmLeftGripper != null)
                {
                    if (rightArmLeftGripper.dofCount > 0)
                    {
                        state.gripperAngles.Add(rightArmLeftGripper.jointPosition[0]);
                    }
                    else
                    {
                        state.gripperAngles.Add(0f);
                        Debug.LogWarning($"right arm left gripper {rightArmLeftGripper.name} not found valid position info, using default value 0");
                    }
                }
                
                if (rightArmRightGripper != null)
                {
                    if (rightArmRightGripper.dofCount > 0)
                    {
                        state.gripperAngles.Add(rightArmRightGripper.jointPosition[0]);
                    }
                    else
                    {
                        state.gripperAngles.Add(0f);
                        Debug.LogWarning($"right arm right gripper {rightArmRightGripper.name} not found valid position info, using default value 0");
                    }
                }
            }
        }
        
        // stateIndexText.text = "CurrentIndex: " + state.id;

        // save state of each simObject
        for (int i = 0; i < simObjects.Count; i++)
        {
            state.objects[i] = SaveObjectState(simObjects[i]);
        }
        #endregion

        #region save scene info returned to python
        SceneStateA2T stateA2T = new()
        {
            id = currentStateIndex + 1,
            objects = new ObjectStateA2T[interactableObjects.Count],
            agent = new AgentStateA2T()
            {
                name = agent.name,
                position = agent.transform.position,
                rotation = agent.transform.rotation,
                lastAction = stateHistoryA2T.Count > 0 ? stateHistoryA2T[currentStateIndex].agent.lastAction : "idle", // default value
                lastActionSuccess = stateHistoryA2T.Count > 0 ? stateHistoryA2T[currentStateIndex].agent.lastActionSuccess : false,
                errorMessage = stateHistoryA2T.Count > 0 ? stateHistoryA2T[currentStateIndex].agent.errorMessage : string.Empty
            },
            reachablePositons = canTransferPoints.Select(t => t.transform.position).ToArray()
        };
        
        // save robot joint angles to A2T state
        if (agentMovement != null && agentMovement.articulationChain != null)
        {
            stateA2T.agent.jointAngles = new List<float>();
            stateA2T.agent.gripperAngles = new List<float>();
            
            // save all joint angles
            foreach (ArticulationBody joint in agentMovement.articulationChain)
            {
                if (joint.jointType != ArticulationJointType.FixedJoint)
                {
                    // check if dofCount is greater than 0
                    if (joint.dofCount > 0)
                    {
                        stateA2T.agent.jointAngles.Add(joint.jointPosition[0]);
                    }
                    else
                    {
                        // if joint is not initialized, add default value 0
                        stateA2T.agent.jointAngles.Add(0f);
                        // Debug.LogWarning($"joint {joint.name} not found valid position info, using default value 0");
                    }
                }
            }
            
            // save gripper angles
            if (agentMovement.gripperController != null)
            {
                // get current left arm gripper
                ArticulationBody leftArmLeftGripper = agentMovement.gripperController.currentLeftLeftGripper;
                ArticulationBody leftArmRightGripper = agentMovement.gripperController.currentLeftRightGripper;
                
                // get current right arm gripper
                ArticulationBody rightArmLeftGripper = agentMovement.gripperController.currentRightLeftGripper;
                ArticulationBody rightArmRightGripper = agentMovement.gripperController.currentRightRightGripper;
                
                // save left arm gripper angles
                if (leftArmLeftGripper != null)
                {
                    if (leftArmLeftGripper.dofCount > 0)
                    {
                        stateA2T.agent.gripperAngles.Add(leftArmLeftGripper.jointPosition[0]);
                    }
                    else
                    {
                        stateA2T.agent.gripperAngles.Add(0f);
                        Debug.LogWarning($"left arm left gripper {leftArmLeftGripper.name} not found valid position info, using default value 0");
                    }
                }
                
                if (leftArmRightGripper != null)
                {
                    if (leftArmRightGripper.dofCount > 0)
                    {
                        stateA2T.agent.gripperAngles.Add(leftArmRightGripper.jointPosition[0]);
                    }
                    else
                    {
                        stateA2T.agent.gripperAngles.Add(0f);
                        Debug.LogWarning($"left arm right gripper {leftArmRightGripper.name} not found valid position info, using default value 0");
                    }
                }
                
                // save right arm gripper angles
                if (rightArmLeftGripper != null)
                {
                    if (rightArmLeftGripper.dofCount > 0)
                    {
                        stateA2T.agent.gripperAngles.Add(rightArmLeftGripper.jointPosition[0]);
                    }
                    else
                    {
                        stateA2T.agent.gripperAngles.Add(0f);
                        Debug.LogWarning($"right arm left gripper {rightArmLeftGripper.name} not found valid position info, using default value 0");
                    }
                }
                
                if (rightArmRightGripper != null)
                {
                    if (rightArmRightGripper.dofCount > 0)
                    {
                        stateA2T.agent.gripperAngles.Add(rightArmRightGripper.jointPosition[0]);
                    }
                    else
                    {
                        stateA2T.agent.gripperAngles.Add(0f);
                        Debug.LogWarning($"right arm right gripper {rightArmRightGripper.name} not found valid position info, using default value 0");
                    }
                }
            }
        }

        for (int i = 0; i < interactableObjects.Count; i++)
        {
            Debug.Log("interactableObjects[i]: "+interactableObjects[i]);
            stateA2T.objects[i] = SaveInteractableObjectState(interactableObjects[i]);
        }
        #endregion

        // ensure current state index does not exceed history
        if (currentStateIndex < stateHistory.Count - 1)
        {
            stateHistory.RemoveRange(currentStateIndex + 1, stateHistory.Count - (currentStateIndex + 1));
            stateHistoryA2T.RemoveRange(currentStateIndex + 1, stateHistoryA2T.Count - (currentStateIndex + 1));
        }
        stateHistory.Add(state);
        stateHistoryA2T.Add(stateA2T);
        // maxStateIndexText.text= "MaxIndex:" + (stateHistory.Count-1).ToString();
        // print scene state
        //print(JsonUtility.ToJson(state));
        print(JsonUtility.ToJson(stateA2T));
        currentStateIndex++;
        Debug.Log($"After SaveCurrentState - currentStateIndex: {currentStateIndex}");
        Debug.Log($"StateHistory count: {stateHistory.Count}");
    }

    // save state of single object
    private ObjectState SaveObjectState(GameObject obj)
    {
        ObjectState objectState = new()
        {
            name = obj.name,
            position = obj.transform.localPosition,
            rotation = obj.transform.localRotation,
            isActive = obj.activeSelf,
            isPickedUp = obj.transform.parent != null && obj.transform.parent.CompareTag("Hand"), // check if parent is Hand
        };

        // save serializable state
        IUniqueStateManager[] savables = obj.GetComponents<IUniqueStateManager>();
        foreach (var savable in savables)
        {
            savable.SaveState(objectState); // save state
        }

        return objectState;
    }

    // save state of interactable object
    private ObjectStateA2T SaveInteractableObjectState(GameObject obj)
    {
        SimObjPhysics sop = obj.GetComponent<SimObjPhysics>();
        // Debug.Log("obj: "+obj);
        // Debug.Log("sop: "+sop);
        // Debug.Log("obj.name: "+obj.name);

        // Debug.Log("sop.ObjectID: "+sop.ObjectID);
        // Debug.Log("sop.Type: "+sop.Type);
        // Debug.Log("sop.SecondaryProperties: "+sop.SecondaryProperties);
        // Debug.Log("sop.PrimaryProperty: "+sop.PrimaryProperty);
        // Debug.Log("sop.IsToggleable: "+sop.IsToggleable);
        // Debug.Log("sop.IsBreakable: "+sop.IsBreakable);
        // Debug.Log("sop.IsFillable: "+sop.IsFillable);
        // Debug.Log("sop.CanBeUsedUp: "+sop.CanBeUsedUp);
        // Debug.Log("sop.IsCookable: "+sop.IsCookable);
        // Debug.Log("sop.IsSliceable: "+sop.IsSliceable);
        // Debug.Log("sop.IsOpenable: "+sop.IsOpenable);
        // Debug.Log("sop.IsMoveable: "+sop.IsMoveable);
        // Debug.Log("sop.ParentReceptacleObjectsIds: "+sop.ParentReceptacleObjectsIds());
        // Debug.Log("obj.transform.parent.name: "+obj.transform.parent.name);



        ObjectStateA2T objectStateA2T = new()
        {
            name = obj.name,
            objectId = sop.ObjectID,
            objectType = sop.Type.ToString(),
            position = obj.transform.position,
            rotation = obj.transform.rotation,
            distance = Vector3.Distance(agent.transform.position, obj.transform.position),
            visible = canInteractableObjects.Contains(obj),
            receptacle = sop.SecondaryProperties.Contains(SimObjSecondaryProperty.Receptacle),
            toggleable = sop.IsToggleable,
            breakable = sop.IsBreakable,
            canFillWithLiquid = sop.IsFillable,
            canBeUsedUp = sop.CanBeUsedUp,
            cookable = sop.IsCookable,
            sliceable = sop.IsSliceable,
            openable = sop.IsOpenable,
            pickupable = sop.PrimaryProperty == SimObjPrimaryProperty.CanPickup,
            isMoveable = sop.IsMoveable,
            isPickedUp = obj.transform.parent.CompareTag("Hand"),
            isToggled = sop.IsToggleable ? (obj.GetComponent<CanToggleOnOff>().isOn) : false,
            isBroken = sop.IsBreakable ? obj.GetComponent<Break>().isBroken : false,
            isFilledWithLiquid = sop.IsFillable ? obj.GetComponent<Fill>().isFilled : false,
            isUsedUp = sop.CanBeUsedUp ? obj.GetComponent<UsedUp>().isUsedUp : false,
            isCooked = sop.IsCookable ? obj.GetComponent<CookObject>().isCooked : false,
            isSliced = sop.IsSliceable ? obj.GetComponent<SliceObject>().isSliced : false,
            isOpen = sop.IsOpenable ? obj.GetComponent<CanOpen_Object>().isOpen : false,
            parentReceptacles = sop.ParentReceptacleObjectsIds()
        };
        if (objectStateA2T.receptacle)
        {
            Contains contains = obj.GetComponentInChildren<Contains>();
            if (contains != null)
            {
                objectStateA2T.receptacleObjectIds = contains.CurrentlyContainedObjectIDs();
            }
        }
        return objectStateA2T;
    }

    public void UpdateLastAction(string action)
    {
        if (stateHistoryA2T.Count > 0)
        {
            stateHistoryA2T[currentStateIndex].agent.lastAction = action;
        }
        else
        {
            Debug.LogWarning("No state history to update lastAction.");
        }
    }
    public bool Undo()
    {
        if (currentStateIndex > 0)
        {
            currentStateIndex--;
            
            // restore parent relationship of all interactable objects
            RestorePickedObjectsParent();
            
            LoadState(stateHistory[currentStateIndex], stateHistoryA2T[currentStateIndex]);
            return true;
        }
        return false;
    }

    // restore parent relationship of all interactable objects
    private void RestorePickedObjectsParent()
    {
        Debug.Log("Start restoring all picked objects...");
        List<Transform> possibleGrippers = new List<Transform>();
        
        // 1. find all objects with Hand tag (whole hand)
        GameObject[] hands = GameObject.FindGameObjectsWithTag("Hand");
        foreach (GameObject hand in hands)
        {
            possibleGrippers.Add(hand.transform);
        }
        
        // 2. find all gripper joints in GripperController component
        AgentMovement agentMovement = agent.GetComponent<AgentMovement>();
        if (agentMovement != null && agentMovement.gripperController != null)
        {
            GripperController gc = agentMovement.gripperController;
            
            // add X1 robot gripper
            if (gc.leftArmLeftGripper != null) possibleGrippers.Add(gc.leftArmLeftGripper.transform);
            if (gc.leftArmRightGripper != null) possibleGrippers.Add(gc.leftArmRightGripper.transform);
            if (gc.rightArmLeftGripper != null) possibleGrippers.Add(gc.rightArmLeftGripper.transform);
            if (gc.rightArmRightGripper != null) possibleGrippers.Add(gc.rightArmRightGripper.transform);
            
            // add H1 robot gripper
            if (gc.h1_leftArmLeftGripper != null) possibleGrippers.Add(gc.h1_leftArmLeftGripper.transform);
            if (gc.h1_leftArmRightGripper != null) possibleGrippers.Add(gc.h1_leftArmRightGripper.transform);
            if (gc.h1_rightArmLeftGripper != null) possibleGrippers.Add(gc.h1_rightArmLeftGripper.transform);
            if (gc.h1_rightArmRightGripper != null) possibleGrippers.Add(gc.h1_rightArmRightGripper.transform);
            
            // add current active gripper
            if (gc.currentLeftLeftGripper != null) possibleGrippers.Add(gc.currentLeftLeftGripper.transform);
            if (gc.currentLeftRightGripper != null) possibleGrippers.Add(gc.currentLeftRightGripper.transform);
            if (gc.currentRightLeftGripper != null) possibleGrippers.Add(gc.currentRightLeftGripper.transform);
            if (gc.currentRightRightGripper != null) possibleGrippers.Add(gc.currentRightRightGripper.transform);
        }
        
        // 3. find possible finger joints (by name)
        string[] possibleGripperNames = new[] { 
            "hand_left_link", "hand_right_link", 
            "gripper_left", "gripper_right",
            "finger", "thumb", "claw", "pinch"
        };
        
        var allArticulationBodies = FindObjectsOfType<ArticulationBody>();
        foreach (var joint in allArticulationBodies)
        {
            foreach (var name in possibleGripperNames)
            {
                if (joint.name.ToLower().Contains(name.ToLower()))
                {
                    possibleGrippers.Add(joint.transform);
                    break;
                }
            }
        }
        
        Debug.Log($"found {possibleGrippers.Count} possible gripper/finger positions");
        
        // iterate through all possible grippers, check sub-objects
        int releasedCount = 0;
        foreach (Transform gripper in possibleGrippers)
        {
            // skip null reference
            if (gripper == null) continue;
            
            Debug.Log($"check gripper/finger: {gripper.name}, sub-objects count: {gripper.childCount}");
            
            // get all sub-objects of the gripper
            for (int i = gripper.childCount - 1; i >= 0; i--)
            {
                Transform child = gripper.GetChild(i);
                SimObjPhysics simObj = child.GetComponent<SimObjPhysics>();
                
                // only process objects with SimObjPhysics component (interactable objects)
                if (simObj != null)
                {
                    Debug.Log($"Undo/Redo: release object {child.name} (ID: {simObj.ObjectID}) from gripper {gripper.name}");
                    releasedCount++;
                    
                    // completely release object, ensure object returns to ObjectsParent
                    Vector3 currentPosition = child.position; // record current world position
                    Quaternion currentRotation = child.rotation; // record current world rotation
                    
                    // disconnect from gripper, set to scene root
                    child.SetParent(ObjectsParent);
                    
                    // ensure position before restoring physics
                    child.position = currentPosition;
                    child.rotation = currentRotation;
                    
                    // restore physics
                    Rigidbody rigidbody = child.GetComponent<Rigidbody>();
                    if (rigidbody != null)
                    {
                        // set to kinematic to ensure position
                        rigidbody.isKinematic = true;
                        
                        // adjust rotation to world axes
                        AdjustRotationToWorldAxes(child);
                        
                        // fully restore physics
                        rigidbody.isKinematic = false;
                        rigidbody.useGravity = true;
                        rigidbody.detectCollisions = true;
                        rigidbody.linearVelocity = Vector3.zero; // clear velocity
                        rigidbody.angularVelocity = Vector3.zero; // clear angular velocity
                    }
                    
                    // remove from operation list
                    if (ObjectsInOperation.Contains(child.gameObject))
                    {
                        ObjectsInOperation.Remove(child.gameObject);
                        Debug.Log($"removed object {child.name} from operation list");
                    }
                }
            }
        }
        
        // update currentInteractingObjectID in Agent component
        AgentMovement agentMovementForClear = agent.GetComponent<AgentMovement>();
        if (agentMovementForClear != null)
        {
            // notify AgentMovement to clear current interacting object
            agentMovementForClear.ClearIgnoredCollisionObjects();
        }
        
        Debug.Log($"restore operation completed, released {releasedCount} objects back to scene");
    }

    public bool Redo()
    {
        if (currentStateIndex < stateHistory.Count - 1)
        {
            currentStateIndex++;
            
            // restore parent relationship of all interactable objects
            RestorePickedObjectsParent();
            
            LoadState(stateHistory[currentStateIndex], stateHistoryA2T[currentStateIndex]);
            return true;
        }
        return false;
    }

    // load state by index
    public bool LoadStateByIndex(string indexText)
    {
        if (int.TryParse(indexText, out int index))
        {
            if (index >= 0 && index <= stateHistory.Count-1) // check if index is in valid range
            {
                currentStateIndex = index; // index starts from 0, user input starts from 1
                
                // restore parent relationship of all interactable objects
                RestorePickedObjectsParent();
                
                LoadState(stateHistory[currentStateIndex], stateHistoryA2T[currentStateIndex]);
                return true;
            }
            else
            {
                Debug.LogWarning("input index out of range!");
            }
        }
        else
        {
            Debug.LogWarning("invalid index input!");
        }
        return false;
    }

    private void LoadState(SceneState state)
    {
        //string sceneStateJson = JsonUtility.ToJson(state);
        //print(sceneStateJson);
        // update scene state and related information
        // agent.transform.SetPositionAndRotation(state.agentPosition, state.agentRotation);
        root.TeleportRoot(state.agentPosition,state.agentRotation);
        // stateIndexText.text = "CurrentIndex: " + state.id;

        // restore joint angles
        AgentMovement agentMovement = agent.GetComponent<AgentMovement>();
        if (agentMovement != null && agentMovement.articulationChain != null)
        {
            if (state.jointAngles != null && state.jointAngles.Count > 0)
            {
                int jointIndex = 0;
                foreach (ArticulationBody joint in agentMovement.articulationChain)
                {
                    if (joint.jointType != ArticulationJointType.FixedJoint && jointIndex < state.jointAngles.Count)
                    {
                        var drive = joint.xDrive;
                        drive.target = state.jointAngles[jointIndex];
                        joint.xDrive = drive;
                        jointIndex++;
                    }
                }
            }
            
            // restore gripper angles
            if (state.gripperAngles != null && state.gripperAngles.Count > 0 && agentMovement.gripperController != null)
            {
                // get current left arm gripper
                ArticulationBody leftArmLeftGripper = agentMovement.gripperController.currentLeftLeftGripper;
                ArticulationBody leftArmRightGripper = agentMovement.gripperController.currentLeftRightGripper;
                
                // get current right arm gripper
                ArticulationBody rightArmLeftGripper = agentMovement.gripperController.currentRightLeftGripper;
                ArticulationBody rightArmRightGripper = agentMovement.gripperController.currentRightRightGripper;
                
                // restore left arm gripper angle
                if (leftArmLeftGripper != null && state.gripperAngles.Count > 0)
                {
                    var drive = leftArmLeftGripper.xDrive;
                    drive.target = state.gripperAngles[0];
                    leftArmLeftGripper.xDrive = drive;
                }
                
                if (leftArmRightGripper != null && state.gripperAngles.Count > 1)
                {
                    var drive = leftArmRightGripper.xDrive;
                    drive.target = state.gripperAngles[1];
                    leftArmRightGripper.xDrive = drive;
                }
                
                // restore right arm gripper angle
                if (rightArmLeftGripper != null && state.gripperAngles.Count > 2)
                {
                    var drive = rightArmLeftGripper.xDrive;
                    drive.target = state.gripperAngles[2];
                    rightArmLeftGripper.xDrive = drive;
                }
                
                if (rightArmRightGripper != null && state.gripperAngles.Count > 3)
                {
                    var drive = rightArmRightGripper.xDrive;
                    drive.target = state.gripperAngles[3];
                    rightArmRightGripper.xDrive = drive;
                }
            }
        }

        // restore other objects' state
        foreach (ObjectState objectState in state.objects)
        {
            LoadObjectState(objectState);
        }
    }

    private void LoadState(SceneState state, SceneStateA2T stateA2T)
    {
        string sceneStateJson = JsonUtility.ToJson(state);
        print(sceneStateJson);
        string sceneStateJsonA2T = JsonUtility.ToJson(stateA2T);
        print(sceneStateJsonA2T);
        // update scene state and related information

        print("Load state");

        root.TeleportRoot(state.agentPosition,state.agentRotation);
        agent.transform.position = state.agentPosition;
        agent.transform.rotation = state.agentRotation;

        stateIndexText.text = "CurrentIndex: " + state.id;

        // restore joint angles
        AgentMovement agentMovement = agent.GetComponent<AgentMovement>();
        if (agentMovement != null && agentMovement.articulationChain != null)
        {
            if (state.jointAngles != null && state.jointAngles.Count > 0)
            {
                int jointIndex = 0;
                foreach (ArticulationBody joint in agentMovement.articulationChain)
                {
                    if (joint.jointType != ArticulationJointType.FixedJoint && jointIndex < state.jointAngles.Count)
                    {
                        var drive = joint.xDrive;
                        drive.target = state.jointAngles[jointIndex];
                        joint.xDrive = drive;
                        jointIndex++;
                    }
                }
            }
            
            // restore gripper angles
            if (state.gripperAngles != null && state.gripperAngles.Count > 0 && agentMovement.gripperController != null)
            {
                // get current left arm gripper
                ArticulationBody leftArmLeftGripper = agentMovement.gripperController.currentLeftLeftGripper;
                ArticulationBody leftArmRightGripper = agentMovement.gripperController.currentLeftRightGripper;
                
                // get current right arm gripper
                ArticulationBody rightArmLeftGripper = agentMovement.gripperController.currentRightLeftGripper;
                ArticulationBody rightArmRightGripper = agentMovement.gripperController.currentRightRightGripper;
                
                // restore left arm gripper angle
                if (leftArmLeftGripper != null && state.gripperAngles.Count > 0)
                {
                    var drive = leftArmLeftGripper.xDrive;
                    drive.target = state.gripperAngles[0];
                    leftArmLeftGripper.xDrive = drive;
                }
                
                if (leftArmRightGripper != null && state.gripperAngles.Count > 1)
                {
                    var drive = leftArmRightGripper.xDrive;
                    drive.target = state.gripperAngles[1];
                    leftArmRightGripper.xDrive = drive;
                }
                
                // restore right arm gripper angle
                if (rightArmLeftGripper != null && state.gripperAngles.Count > 2)
                {
                    var drive = rightArmLeftGripper.xDrive;
                    drive.target = state.gripperAngles[2];
                    rightArmLeftGripper.xDrive = drive;
                }
                
                if (rightArmRightGripper != null && state.gripperAngles.Count > 3)
                {
                    var drive = rightArmRightGripper.xDrive;
                    drive.target = state.gripperAngles[3];
                    rightArmRightGripper.xDrive = drive;
                }
            }
        }

        // restore other objects' state
        foreach (ObjectState objectState in state.objects)
        {
            LoadObjectState(objectState);
        }

        
    }

    private void LoadObjectState(ObjectState objectState)
    {
        // find dynamic object
        if (simObjectsDict.TryGetValue(objectState.name, out GameObject obj))
        {
            // first, handle parent relationship
            // 1. if object is in hand, but not picked up in saved state, release it
            if (obj.transform.parent != null && obj.transform.parent.CompareTag("Hand") && !objectState.isPickedUp)
            {
                Debug.Log($"LoadObjectState: release object {obj.name} from hand to scene");
                obj.transform.SetParent(ObjectsParent);
                
                // remove from operation list
                if (ObjectsInOperation.Contains(obj))
                {
                    ObjectsInOperation.Remove(obj);
                }
            }
            // 2. if object is picked up in saved state, but not in hand, this should be handled by Agent's Pick operation
            // we only restore position, not change parent relationship
            
            // set active state
            obj.SetActive(objectState.isActive);

            // handle physics state and position
            HandlePhysicsState(obj, objectState);

            // restore other states (through IUniqueStateManager interface)
            IUniqueStateManager[] savables = obj.GetComponents<IUniqueStateManager>();
            foreach (var savable in savables)
            {
                savable.LoadState(objectState);
            }
        }
        else
        {
            Debug.LogWarning($"cannot find object: {objectState.name}");
        }
    }

    private void HandlePhysicsState(GameObject obj, ObjectState objectState)
    {
        // regardless of object type, first pause physics
        Rigidbody rb = obj.GetComponent<Rigidbody>();
        bool hadRigidbody = rb != null;
        bool wasKinematic = hadRigidbody && rb.isKinematic;
        bool usedGravity = hadRigidbody && rb.useGravity;
        bool detectCollisions = hadRigidbody && rb.detectCollisions;
        
        // temporarily disable physics to ensure position setting is correct
        if (hadRigidbody)
        {

            rb.isKinematic = true;
            rb.useGravity = false;
            rb.detectCollisions = false;
            
            // clear all existing velocity and angular velocity
            rb.linearVelocity = Vector3.zero;
            rb.angularVelocity = Vector3.zero;
        }
        
        // set position and rotation
        obj.transform.SetLocalPositionAndRotation(objectState.position, objectState.rotation);
        
        // ensure physics system is synchronized with new transform
        Physics.SyncTransforms();
        
        // restore physics properties according to object type
        SimObjPhysics simObj = obj.GetComponent<SimObjPhysics>();
        if (hadRigidbody)
        {
            // if object is currently picked up (in hand), keep kinematic=true
            if (obj.transform.parent != null && obj.transform.parent.CompareTag("Hand"))
            {
                rb.isKinematic = true;
                rb.useGravity = false;
                rb.detectCollisions = false;
            }
            else if (simObj != null && simObj.PrimaryProperty == SimObjPrimaryProperty.CanPickup)
            {
                // for pickable objects, we need to ensure their position is stable before restoring physics
                // first, fix position, then delay restore physics
                StartCoroutine(SafelyRestorePhysics(rb, false, true, true));
            }
            else
            {
                if(simObj.PrimaryProperty != SimObjPrimaryProperty.Static)
                {

                    // restore original state of other objects
                    rb.isKinematic = wasKinematic;
                    rb.useGravity = usedGravity;
                    rb.detectCollisions = detectCollisions;
                }

            }
            
            // ensure all velocities are reset
            rb.linearVelocity = Vector3.zero;
            rb.angularVelocity = Vector3.zero;
        }
        
        // record debug information
        Debug.Log($"object {obj.name} position restored to: {objectState.position}, rotation: {objectState.rotation.eulerAngles}");
    }
    
    // Safely restore physics properties
    private IEnumerator SafelyRestorePhysics(Rigidbody rb, bool isKinematic, bool useGravity, bool detectCollisions)
    {
        if (rb == null) yield break;
        
        // wait for one frame, ensure all position settings are completed
        yield return null;
        
        // ensure physics system is synchronized with transform
        Physics.SyncTransforms();
        
        // ensure velocity is zero
        rb.linearVelocity = Vector3.zero;
        rb.angularVelocity = Vector3.zero;
        
        // first, restore collision detection but keep kinematic state
        rb.detectCollisions = detectCollisions;
        yield return new WaitForFixedUpdate();
        
        // then, restore gravity but keep kinematic state
        rb.useGravity = useGravity;
        yield return new WaitForFixedUpdate();
        
        // finally, restore kinematic state
        rb.isKinematic = isKinematic;
        
        // ensure velocity is zero
        rb.linearVelocity = Vector3.zero;
        rb.angularVelocity = Vector3.zero;
        
        Debug.Log($"object {rb.gameObject.name} physics properties restored");
    }


    public SceneState GetCurrentSceneState()
    {
        return stateHistory[currentStateIndex];  
    }

    public SceneStateA2T GetCurrentSceneStateA2T()
    {
        Debug.Log("currentIndex: "+currentStateIndex);
        return stateHistoryA2T[currentStateIndex];
    }

    public static Transform GetTransferPointByObjectID(string objectID)
    {
        SimObjPhysics[] allObjects = FindObjectsOfType<SimObjPhysics>();

        foreach (SimObjPhysics obj in allObjects)
        {
            if (obj.ObjectID == objectID)
            {
                return obj.TransferPoint;
            }
        }

        Debug.LogWarning($"cannot find object with ID: {objectID}");
        return null;
    }

    public static Transform GetInteractablePoint(string objectID)
    {
        SimObjPhysics[] allObjects = FindObjectsOfType<SimObjPhysics>();

        foreach (SimObjPhysics obj in allObjects)
        {
            if (obj.ObjectID == objectID)
            {
                if (obj.InteractablePoints != null && obj.InteractablePoints.Length > 0)
                {
                    return obj.InteractablePoints[0];
                }
                else
                {
                    Debug.LogWarning($"object {objectID} has no available interactable points");
                    return null;
                }
            }
        }

        Debug.LogWarning($"cannot find object with ID: {objectID}");
        return null;
    }


    public static Transform[] GetLiftPoints(string objectID)
    {
        SimObjPhysics[] allObjects = FindObjectsOfType<SimObjPhysics>();

        foreach (SimObjPhysics obj in allObjects)
        {
            if (obj.ObjectID == objectID)
            {
                return obj.LiftPoints;
            }
        }

        Debug.LogWarning($"cannot find object with ID: {objectID}");
        return null;
        
    }

    public void SetParent(Transform parent, string objectID)
    {
        Debug.Log($"SetParent: set object {objectID} as child of {parent.name}");
        
        // ensure parent has Hand tag, for later recovery
        if (!parent.CompareTag("Hand"))
        {
            try
            {
                parent.tag = "Hand";
                Debug.Log($"added Hand tag to {parent.name}");
            }
            catch (Exception e)
            {
                Debug.LogWarning($"cannot add Hand tag to {parent.name}: {e.Message}");
            }
        }
        
        SimObjPhysics[] allObjects = FindObjectsOfType<SimObjPhysics>();

        foreach (SimObjPhysics obj in allObjects)
        {
            if (obj.ObjectID == objectID)
            {
                Debug.Log($"found object {obj.name} (ID: {objectID}), set its parent to {parent.name}");
                
                ObjectsInOperation.Add(obj.gameObject);
                obj.transform.SetParent(parent);
                Rigidbody rigidbody = obj.GetComponent<Rigidbody>();
                if (rigidbody != null)
                {
                    rigidbody.isKinematic = true; // set to kinematic mode
                    rigidbody.useGravity = false; // disable gravity
                    rigidbody.detectCollisions = false; // disable collision detection
                }
                
                Debug.Log($"object {obj.name} successfully set as child of {parent.name}, and disabled physics");
            }
        }
    }

    public void Release(string objectID)
    {
        SimObjPhysics[] allObjects = FindObjectsOfType<SimObjPhysics>();

        foreach (SimObjPhysics obj in allObjects)
        {
            if (obj.ObjectID == objectID)
            {
                obj.transform.SetParent(ObjectsParent);
               
                Rigidbody rigidbody = obj.GetComponent<Rigidbody>();
                if (rigidbody != null)
                {
                    AdjustRotationToWorldAxes(obj.transform);
                    
                    rigidbody.isKinematic = false; // restore physics
                    rigidbody.useGravity = true; // enable gravity
                    rigidbody.detectCollisions = true; // enable collision detection
                }
                // ObjectsInOperation.Remove(obj);
            }
        }
    }

    public void RemoveOperation(string objectID)
    {
        SimObjPhysics[] allObjects = FindObjectsOfType<SimObjPhysics>();
        Transform objectTransform = null;
        foreach (SimObjPhysics obj in allObjects)
        {
            if (obj.ObjectID == objectID)
            {
                ObjectsInOperation.Remove(obj.gameObject);
            }
        }
    }

    public void UpdateLastActionSuccessCollision(string collisionA, string collisionB)
    {
        if (stateHistoryA2T.Count > 0)
        {
            var currentAgent = stateHistoryA2T[currentStateIndex].agent;
            
            // check if collision object is current interacting object
            bool isInteractingObject = false;
            
            // get AgentMovement component
            AgentMovement agentMovement = FindObjectOfType<AgentMovement>();
            if (agentMovement != null)
            {
                // try to determine if collisionB is current interacting object
                SimObjPhysics[] allObjects = FindObjectsOfType<SimObjPhysics>();
                foreach (SimObjPhysics obj in allObjects)
                {
                    if (obj.gameObject.name == collisionB)
                    {
                        // get current interacting object information from AgentMovement
                        // assume AgentMovement has a method to check if object is current interacting object
                        if (agentMovement.IsCurrentInteractingObject(obj.ObjectID))
                        {
                            isInteractingObject = true;
                            Debug.Log($"collision object {collisionB} is current interacting object, not considered as a collision");
                            break;
                        }
                    }
                }
            }
            
            // if not interacting object, consider as failure
            if (!isInteractingObject)
            {
                currentAgent.lastActionSuccess = false;
                currentAgent.errorMessage = $"Collision Detected. Joint {collisionA} collision with object {collisionB}";
            }
            else
            {
                // if interacting object, still record collision but not considered as failure
                Debug.Log($"detected collision with interacting object, not considered as failure: {collisionB}");
            }
        }
        else
        {
            Debug.LogWarning("No state history to update lastActionSuccess or errorMessage.");
        }
    }
}
