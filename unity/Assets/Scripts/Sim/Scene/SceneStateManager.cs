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
    [Header("场景中所有模拟物体")]
    [SerializeField]
    private List<GameObject> simObjects = new();//场景中所有模拟物体，包括破碎和切碎后生成的物体，用于unity编辑器中可见
    [Header("场景中所有可交互物体")]
    [SerializeField]
    private List<GameObject> interactableObjects = new();//场景中所有可交互物体
    //[SerializeField]
    [Header("视野范围内能交互物体")]

    public List<GameObject> canInteractableObjects = new();//视野范围内所有可交互物体
    [Header("场景中所有可传送位置")]

    [SerializeField]
    private List<GameObject> transferPoints = new();//场景中所有可交互物体的传送位置
    //[SerializeField]
    [Header("视野范围内能交互物体的传送位置")]

    public List<GameObject> canTransferPoints = new();//视野范围内可交互物体的传送位置

    [Header("可交互物体的可操作位置")]
    [SerializeField]
    private List<GameObject> interactablePoints = new();//场景中所有可交互物体的可操作位置

    private readonly Dictionary<string, GameObject> simObjectsDict = new();//场景中所有模拟物体字典，用于快速查找
    //private readonly Dictionary<string, GameObject> canInteractableObjectsDict = new();//场景中所有模拟物体字典，用于快速查找

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

    // 定义ActionConfig类
    [Serializable]
    public class ActionConfig
    {
        public float successRate;
        public Dictionary<string, ErrorEffectConfig> errorMessages;
        // 添加基于物体状态的配置
        public Dictionary<string, ObjectStateConfig> objectStateConfigs;

        public override string ToString()
        {
            string errorMessagesString = string.Join(", ", errorMessages.Select(kv => $"{kv.Key}: {kv.Value.probability}"));
            return $"Success Rate: {successRate}, Error Messages: [{errorMessagesString}]";
        }
    }

    // 新增错误效果配置类，用于存储错误消息对应的概率和状态效果
    [Serializable]
    public class ErrorEffectConfig
    {
        public float probability; // 概率
        public string targetState; // 目标状态
        
        // 构造函数，方便从旧配置转换
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

    // 新增ObjectStateConfig类，用于存储物体特定状态的成功率配置
    [Serializable]
    public class ObjectStateConfig
    {
        public string objectType; // 物体类型 (cup, plate等)
        public string stateCondition; // 状态条件 (filled, broken, open等)
        public float successRate; // 该状态下的成功率
        public Dictionary<string, ErrorEffectConfig> errorMessages; // 该状态下的错误消息及概率
    }

    void Start()
    {
        LoadActionConfigs();

        // 查找并填充可交互物体列表
        FillList(simObjects, new[] { "Interactable", "DynamicAdd" });
        // 查找并填充可交互物体字典
        FillDict(simObjectsDict, new[] { "Interactable", "DynamicAdd" });
        

        // 查找并填充可交互物体列表
        FillList(interactableObjects, new[] { "Interactable"});

        // 查找并填充传送位置列表
        FillList(transferPoints, new[] { "TransferPoint" });

        // 查找并填充可操作位置列表
        FillList(interactablePoints, new[] { "InteractablePoint" });

        GameObject[] dynamicAdds = GameObject.FindGameObjectsWithTag("DynamicAdd");
        foreach (GameObject obj in dynamicAdds)
        {
            obj.SetActive(false);
        }
        

        
        StartCoroutine(DelayedSave());

        // 保存初始状态
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
                Debug.Log("动作配置文件加载成功");
            }
            catch (Exception e)
            {
                Debug.LogError($"动作配置文件加载失败: {e.Message}");
                actionConfigs = CreateDefaultActionConfigs();
            }
        }
        else
        {
            Debug.LogError("ErrorConfig.json not found!");
            // 创建默认配置
            actionConfigs = CreateDefaultActionConfigs();
        }
    }

    // 新增方法：创建默认配置
    private Dictionary<string, ActionConfig> CreateDefaultActionConfigs()
    {
        var configs = new Dictionary<string, ActionConfig>();
        
        // 添加默认Pick动作配置
        var pickConfig = new ActionConfig
        {
            successRate = 0.91f,
            errorMessages = new Dictionary<string, ErrorEffectConfig> 
            {
                { "抓取失败，物体掉落", new ErrorEffectConfig(0.05f, "") },
                { "无法抓取到物体", new ErrorEffectConfig(0.04f, "") }
            },
            objectStateConfigs = new Dictionary<string, ObjectStateConfig>()
        };
        
        // 为cup添加特殊状态配置
        var filledCupConfig = new ObjectStateConfig
        {
            objectType = "Cup",
            stateCondition = "isFilled",
            successRate = 0.91f,
            errorMessages = new Dictionary<string, ErrorEffectConfig>
            {
                { "抓取失败，杯子碎裂", new ErrorEffectConfig(0.02f, "broken") },
                { "无法抓取到杯子", new ErrorEffectConfig(0.03f, "") },
                { "抓取失败，杯子内容物泼洒", new ErrorEffectConfig(0.04f, "spilled") }
            }
        };
        
        pickConfig.objectStateConfigs.Add("Cup_filled", filledCupConfig);
        configs.Add("pick", pickConfig);
        
        // 可以添加更多默认配置...
        
        return configs;
    }

    // 简化的配置结果类
    public class ActionConfigResult
    {
        public float successRate;
        public Dictionary<string, ErrorEffectConfig> errorMessages;
        
        public (string message, string effectState) GetRandomErrorMessage() {  
            float total = errorMessages.Values.Sum(e => e.probability);  
            if(total <= 0) return ("未知错误", "");  

            float randomValue = UnityEngine.Random.value;  
            float cumulative = 0;  
  
            foreach (var error in errorMessages) {  
                float normalizedProb = error.Value.probability / total; // 归一化  
                cumulative += normalizedProb;  
                if(randomValue <= cumulative) {  
                    return (error.Key, error.Value.targetState);  
                }  
            }  
            return ("未知错误", "");  
        }  

        public override string ToString()
        {
            return $"Success Rate: {successRate}, Error Messages: [{string.Join(", ", errorMessages.Select(kv => $"{kv.Key}: {kv.Value}"))}]";
        }
    }

    // 新增：根据物体状态获取动作配置
    public ActionConfigResult GetActionConfigByObjectState(string actionType, SimObjPhysics targetObj)
    {
        ActionConfigResult result = new ActionConfigResult
        {
            successRate = 0.95f, // 默认高成功率
            errorMessages = new Dictionary<string, ErrorEffectConfig>
            {
                { "操作失败", new ErrorEffectConfig(0.05f, "") }
            }
        };
        
        if (actionConfigs == null || !actionConfigs.TryGetValue(actionType.ToLower(), out ActionConfig config))
        {
            Debug.LogWarning($"未找到动作类型的配置: {actionType}，使用默认配置");
            return result;
        }
        
        // 先使用基础配置
        result.successRate = config.successRate;
        result.errorMessages = new Dictionary<string, ErrorEffectConfig>(config.errorMessages);
        
        // 如果没有目标物体或没有对象状态配置，使用基础配置
        if (targetObj == null || config.objectStateConfigs == null || config.objectStateConfigs.Count == 0)
        {
            return result;
        }
        Debug.Log($"尝试根据物体状态查找特定配置");

        // 尝试根据物体状态查找特定配置
        foreach (var stateConfig in config.objectStateConfigs)
        {

            Debug.Log(targetObj.objType);
            Debug.Log($"检查物体类型: {targetObj.Type} 与配置的物体类型: {stateConfig.Value.objectType}");
            // 检查物体类型是否匹配
            if (!targetObj.Type.ToString().Equals(stateConfig.Value.objectType, StringComparison.OrdinalIgnoreCase))
                continue;
                
            Debug.Log($"检查物体 {targetObj.ObjectID} 的状态: {stateConfig.Value.stateCondition}");
            // 检查物体状态是否匹配
            bool stateMatches = false;
            switch (stateConfig.Value.stateCondition)
            {
                case "isFilled":
                    Debug.Log($"检查物体 {targetObj.ObjectID} 的填充状态: {targetObj.GetComponent<Fill>().isFilled}");
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
                    stateMatches = true; // 默认状态总是匹配
                    break;
                // 可以添加更多状态检查...
            }
            
            // 如果状态匹配，使用特定配置
            if (stateMatches)
            {
                result.successRate = stateConfig.Value.successRate;
                result.errorMessages = stateConfig.Value.errorMessages;
                Debug.Log($"使用物体 {targetObj.ObjectID} 的特定状态配置: {stateConfig.Value.stateCondition}");
                break;
            }
        }
        
        return result;
    }

    // 修改：返回动作配置信息，而不执行随机判断
    public ActionConfigResult GetActionConfig(string actionType, string objectID)
    {
        // 查找目标物体
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
        
        // 获取基于物体状态的配置
        ActionConfigResult config = GetActionConfigByObjectState(actionType, targetObj);
        Debug.Log($"Action config: {config}");
        Debug.Log($"Action config successRate: {config.successRate}");
        
        // 返回完整的配置信息，由AgentMovement决定是否成功
        return config;
    }
    
    // 保留兼容现有代码，但不执行随机判断
    public (bool success, string errorMessage, string targetState) CheckActionSuccess(string actionType, string objectID)
    {
        // 获取配置信息
        var config = GetActionConfig(actionType, objectID);
        
        // 为了与现有代码兼容，返回一个默认的结果
        // 实际的成功/失败判断由AgentMovement执行
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
                    currentAgent.errorMessage = "移动被障碍物阻挡或无法安全执行";
                    return false;
                }
            }
            
            // 不再进行动作成功率检查，由AgentMovement负责
            return true;
        }
        else
        {
            Debug.LogWarning("No state history to update lastActionSuccess or errorMessage.");
            return false;
        }
    }

    // 填充列表的方法
    private void FillList(List<GameObject> list, string[] tags)
    {
        foreach (string tag in tags)
        {
            GameObject[] objects = GameObject.FindGameObjectsWithTag(tag); // 查找所有带有当前标签的物体

            foreach (var obj in objects)
            {
                if (tag == "TransferPoint") // 判断是否是 TransferPoint 标签
                {
                    if (obj.transform.parent != null) // 检查物体是否有父物体
                    {
                        list.Add(obj.transform.parent.gameObject); // 添加父物体
                    }
                }
                else
                {
                    list.Add(obj); // 添加其他标签的物体本身
                }
            }
        }
    }

    // 填充字典的方法
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
        
        
        //if (Input.GetKey(KeyCode.LeftControl) && Input.GetKeyDown(KeyCode.Alpha1))
        //{
        //    SaveCurrentState(); 
        //}
        //if (Input.GetKey(KeyCode.LeftControl) && Input.GetKeyDown(KeyCode.Alpha2))
        //{
        //    Undo(); 
        //}
        //if (Input.GetKey(KeyCode.LeftControl) && Input.GetKeyDown(KeyCode.Alpha3))
        //{
        //    Redo(); 
        //}
        //if (Input.GetKeyDown(KeyCode.Alpha1))
        //{
        //    SaveCurrentState();
        //}
        //if (Input.GetKeyDown(KeyCode.Alpha2))
        //{
        //    Undo();
        //}
        //if ( Input.GetKeyDown(KeyCode.Alpha3))
        //{
        //    Redo();
        //}
    }
    
    public void AdjustRotationToWorldAxes(Transform objectTransform)
    {

        if (objectTransform != null)
        {
            // 将物体的旋转调整为与世界坐标轴对齐
            objectTransform.rotation = Quaternion.identity;
        }
    }

    public void SaveCurrentState()
    {
        Debug.Log($"Before SaveCurrentState - currentStateIndex: {currentStateIndex}");
        
        #region 保存版本控制的场景信息
        // 保存当前状态
        SceneState state = new()
        {
            id = currentStateIndex + 1,
            agentPosition = agent.transform.position,
            agentRotation = agent.transform.rotation,
            
            objects = new ObjectState[simObjects.Count]
        };
        
        // 保存机器人关节角度
        AgentMovement agentMovement = agent.GetComponent<AgentMovement>();
        if (agentMovement != null && agentMovement.articulationChain != null)
        {
            state.jointAngles = new List<float>();
            state.gripperAngles = new List<float>();
            
            // 保存所有关节角度
            foreach (ArticulationBody joint in agentMovement.articulationChain)
            {
                if (joint.jointType != ArticulationJointType.FixedJoint)
                {
                    // 只检查dofCount是否大于0
                    if (joint.dofCount > 0)
                    {
                        state.jointAngles.Add(joint.jointPosition[0]);
                    }
                    else
                    {
                        // 如果关节未初始化，添加默认值0
                        state.jointAngles.Add(0f);
                        Debug.LogWarning($"关节 {joint.name} 未找到有效的位置信息，使用默认值0");
                    }
                }
            }
            
            // 保存爪子角度
            if (agentMovement.gripperController != null)
            {
                // 获取当前左臂爪子
                ArticulationBody leftArmLeftGripper = agentMovement.gripperController.currentLeftLeftGripper;
                ArticulationBody leftArmRightGripper = agentMovement.gripperController.currentLeftRightGripper;
                
                // 获取当前右臂爪子
                ArticulationBody rightArmLeftGripper = agentMovement.gripperController.currentRightLeftGripper;
                ArticulationBody rightArmRightGripper = agentMovement.gripperController.currentRightRightGripper;
                
                // 保存左臂爪子角度
                if (leftArmLeftGripper != null)
                {
                    if (leftArmLeftGripper.dofCount > 0)
                    {
                        state.gripperAngles.Add(leftArmLeftGripper.jointPosition[0]);
                    }
                    else
                    {
                        state.gripperAngles.Add(0f);
                        Debug.LogWarning($"左臂左爪 {leftArmLeftGripper.name} 未找到有效的位置信息，使用默认值0");
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
                        Debug.LogWarning($"左臂右爪 {leftArmRightGripper.name} 未找到有效的位置信息，使用默认值0");
                    }
                }
                
                // 保存右臂爪子角度
                if (rightArmLeftGripper != null)
                {
                    if (rightArmLeftGripper.dofCount > 0)
                    {
                        state.gripperAngles.Add(rightArmLeftGripper.jointPosition[0]);
                    }
                    else
                    {
                        state.gripperAngles.Add(0f);
                        Debug.LogWarning($"右臂左爪 {rightArmLeftGripper.name} 未找到有效的位置信息，使用默认值0");
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
                        Debug.LogWarning($"右臂右爪 {rightArmRightGripper.name} 未找到有效的位置信息，使用默认值0");
                    }
                }
            }
        }
        
        stateIndexText.text = "CurrentIndex: " + state.id;

        // 保存每个 simObject 的状态
        for (int i = 0; i < simObjects.Count; i++)
        {
            state.objects[i] = SaveObjectState(simObjects[i]);
        }
        #endregion

        #region 保存返回给python的场景信息
        SceneStateA2T stateA2T = new()
        {
            id = currentStateIndex + 1,
            objects = new ObjectStateA2T[interactableObjects.Count],
            agent = new AgentStateA2T()
            {
                name = agent.name,
                position = agent.transform.position,
                rotation = agent.transform.rotation,
                lastAction = stateHistoryA2T.Count > 0 ? stateHistoryA2T[currentStateIndex].agent.lastAction : "idle", // 默认值
                lastActionSuccess = stateHistoryA2T.Count > 0 ? stateHistoryA2T[currentStateIndex].agent.lastActionSuccess : false,
                errorMessage = stateHistoryA2T.Count > 0 ? stateHistoryA2T[currentStateIndex].agent.errorMessage : string.Empty
            },
            reachablePositons = canTransferPoints.Select(t => t.transform.position).ToArray()
        };
        
        // 保存机器人关节角度到A2T状态
        if (agentMovement != null && agentMovement.articulationChain != null)
        {
            stateA2T.agent.jointAngles = new List<float>();
            stateA2T.agent.gripperAngles = new List<float>();
            
            // 保存所有关节角度
            foreach (ArticulationBody joint in agentMovement.articulationChain)
            {
                if (joint.jointType != ArticulationJointType.FixedJoint)
                {
                    // 只检查dofCount是否大于0
                    if (joint.dofCount > 0)
                    {
                        stateA2T.agent.jointAngles.Add(joint.jointPosition[0]);
                    }
                    else
                    {
                        // 如果关节未初始化，添加默认值0
                        stateA2T.agent.jointAngles.Add(0f);
                        Debug.LogWarning($"关节 {joint.name} 未找到有效的位置信息，使用默认值0");
                    }
                }
            }
            
            // 保存爪子角度
            if (agentMovement.gripperController != null)
            {
                // 获取当前左臂爪子
                ArticulationBody leftArmLeftGripper = agentMovement.gripperController.currentLeftLeftGripper;
                ArticulationBody leftArmRightGripper = agentMovement.gripperController.currentLeftRightGripper;
                
                // 获取当前右臂爪子
                ArticulationBody rightArmLeftGripper = agentMovement.gripperController.currentRightLeftGripper;
                ArticulationBody rightArmRightGripper = agentMovement.gripperController.currentRightRightGripper;
                
                // 保存左臂爪子角度
                if (leftArmLeftGripper != null)
                {
                    if (leftArmLeftGripper.dofCount > 0)
                    {
                        stateA2T.agent.gripperAngles.Add(leftArmLeftGripper.jointPosition[0]);
                    }
                    else
                    {
                        stateA2T.agent.gripperAngles.Add(0f);
                        Debug.LogWarning($"左臂左爪 {leftArmLeftGripper.name} 未找到有效的位置信息，使用默认值0");
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
                        Debug.LogWarning($"左臂右爪 {leftArmRightGripper.name} 未找到有效的位置信息，使用默认值0");
                    }
                }
                
                // 保存右臂爪子角度
                if (rightArmLeftGripper != null)
                {
                    if (rightArmLeftGripper.dofCount > 0)
                    {
                        stateA2T.agent.gripperAngles.Add(rightArmLeftGripper.jointPosition[0]);
                    }
                    else
                    {
                        stateA2T.agent.gripperAngles.Add(0f);
                        Debug.LogWarning($"右臂左爪 {rightArmLeftGripper.name} 未找到有效的位置信息，使用默认值0");
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
                        Debug.LogWarning($"右臂右爪 {rightArmRightGripper.name} 未找到有效的位置信息，使用默认值0");
                    }
                }
            }
        }

        for (int i = 0; i < interactableObjects.Count; i++)
        {
            stateA2T.objects[i] = SaveInteractableObjectState(interactableObjects[i]);
        }
        #endregion

        // 确保当前状态索引不超过历史记录
        if (currentStateIndex < stateHistory.Count - 1)
        {
            stateHistory.RemoveRange(currentStateIndex + 1, stateHistory.Count - (currentStateIndex + 1));
            stateHistoryA2T.RemoveRange(currentStateIndex + 1, stateHistoryA2T.Count - (currentStateIndex + 1));
        }
        stateHistory.Add(state);
        stateHistoryA2T.Add(stateA2T);
        maxStateIndexText.text= "MaxIndex:" + (stateHistory.Count-1).ToString();
        // 输出场景状态
        //print(JsonUtility.ToJson(state));
        print(JsonUtility.ToJson(stateA2T));
        currentStateIndex++;
        Debug.Log($"After SaveCurrentState - currentStateIndex: {currentStateIndex}");
        Debug.Log($"StateHistory count: {stateHistory.Count}");
    }

    // 保存单个物体的状态
    private ObjectState SaveObjectState(GameObject obj)
    {
        ObjectState objectState = new()
        {
            name = obj.name,
            position = obj.transform.localPosition,
            rotation = obj.transform.localRotation,
            isActive = obj.activeSelf,
            isPickedUp = obj.transform.parent != null && obj.transform.parent.CompareTag("Hand"), // 检查父物体是否为Hand
        };

        // 保存可序列化状态
        IUniqueStateManager[] savables = obj.GetComponents<IUniqueStateManager>();
        foreach (var savable in savables)
        {
            savable.SaveState(objectState); // 保存状态
        }

        return objectState;
    }

    // 保存可交互物体的状态
    private ObjectStateA2T SaveInteractableObjectState(GameObject obj)
    {
        SimObjPhysics sop = obj.GetComponent<SimObjPhysics>();
        // Debug.Log("obj: "+obj);
        // Debug.Log("sop: "+sop);
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
            
            // 先恢复所有交互物体的从属关系
            RestorePickedObjectsParent();
            
            LoadState(stateHistory[currentStateIndex], stateHistoryA2T[currentStateIndex]);
            return true;
        }
        return false;
    }

    // 恢复所有交互物体的从属关系
    private void RestorePickedObjectsParent()
    {
        Debug.Log("开始恢复所有被拿起的物体...");
        List<Transform> possibleGrippers = new List<Transform>();
        
        // 1. 查找所有带Hand标签的物体（整个手部）
        GameObject[] hands = GameObject.FindGameObjectsWithTag("Hand");
        foreach (GameObject hand in hands)
        {
            possibleGrippers.Add(hand.transform);
        }
        
        // 2. 查找GripperController组件中的所有夹爪关节
        AgentMovement agentMovement = agent.GetComponent<AgentMovement>();
        if (agentMovement != null && agentMovement.gripperController != null)
        {
            GripperController gc = agentMovement.gripperController;
            
            // 添加X1机器人夹爪
            if (gc.leftArmLeftGripper != null) possibleGrippers.Add(gc.leftArmLeftGripper.transform);
            if (gc.leftArmRightGripper != null) possibleGrippers.Add(gc.leftArmRightGripper.transform);
            if (gc.rightArmLeftGripper != null) possibleGrippers.Add(gc.rightArmLeftGripper.transform);
            if (gc.rightArmRightGripper != null) possibleGrippers.Add(gc.rightArmRightGripper.transform);
            
            // 添加H1机器人夹爪
            if (gc.h1_leftArmLeftGripper != null) possibleGrippers.Add(gc.h1_leftArmLeftGripper.transform);
            if (gc.h1_leftArmRightGripper != null) possibleGrippers.Add(gc.h1_leftArmRightGripper.transform);
            if (gc.h1_rightArmLeftGripper != null) possibleGrippers.Add(gc.h1_rightArmLeftGripper.transform);
            if (gc.h1_rightArmRightGripper != null) possibleGrippers.Add(gc.h1_rightArmRightGripper.transform);
            
            // 添加当前激活的夹爪
            if (gc.currentLeftLeftGripper != null) possibleGrippers.Add(gc.currentLeftLeftGripper.transform);
            if (gc.currentLeftRightGripper != null) possibleGrippers.Add(gc.currentLeftRightGripper.transform);
            if (gc.currentRightLeftGripper != null) possibleGrippers.Add(gc.currentRightLeftGripper.transform);
            if (gc.currentRightRightGripper != null) possibleGrippers.Add(gc.currentRightRightGripper.transform);
        }
        
        // 3. 额外查找可能的手指关节（按名称）
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
        
        Debug.Log($"找到 {possibleGrippers.Count} 个可能的夹爪/手指位置");
        
        // 遍历所有可能的夹爪，检查子物体
        int releasedCount = 0;
        foreach (Transform gripper in possibleGrippers)
        {
            // 跳过null引用
            if (gripper == null) continue;
            
            Debug.Log($"检查夹爪/手指: {gripper.name}，子物体数量: {gripper.childCount}");
            
            // 获取该夹爪下的所有子物体
            for (int i = gripper.childCount - 1; i >= 0; i--)
            {
                Transform child = gripper.GetChild(i);
                SimObjPhysics simObj = child.GetComponent<SimObjPhysics>();
                
                // 只处理具有SimObjPhysics组件的物体（交互物体）
                if (simObj != null)
                {
                    Debug.Log($"Undo/Redo: 将物体 {child.name} (ID: {simObj.ObjectID}) 从夹爪 {gripper.name} 释放");
                    releasedCount++;
                    
                    // 彻底释放物体，确保物体回归ObjectsParent
                    Vector3 currentPosition = child.position; // 记录当前世界坐标位置
                    Quaternion currentRotation = child.rotation; // 记录当前世界旋转
                    
                    // 断开与夹爪的连接，设置为场景根物体
                    child.SetParent(ObjectsParent);
                    
                    // 恢复物理属性之前确保位置不变
                    child.position = currentPosition;
                    child.rotation = currentRotation;
                    
                    // 恢复物理属性
                    Rigidbody rigidbody = child.GetComponent<Rigidbody>();
                    if (rigidbody != null)
                    {
                        // 先设为运动学以确保位置不变
                        rigidbody.isKinematic = true;
                        
                        // 调整旋转为世界坐标系
                        AdjustRotationToWorldAxes(child);
                        
                        // 完全恢复物理
                        rigidbody.isKinematic = false;
                        rigidbody.useGravity = true;
                        rigidbody.detectCollisions = true;
                        rigidbody.linearVelocity = Vector3.zero; // 清除速度
                        rigidbody.angularVelocity = Vector3.zero; // 清除角速度
                    }
                    
                    // 从操作列表中移除
                    if (ObjectsInOperation.Contains(child.gameObject))
                    {
                        ObjectsInOperation.Remove(child.gameObject);
                        Debug.Log($"已从操作列表中移除物体: {child.name}");
                    }
                }
            }
        }
        
        // 更新Agent组件中的currentInteractingObjectID
        AgentMovement agentMovementForClear = agent.GetComponent<AgentMovement>();
        if (agentMovementForClear != null)
        {
            // 通知AgentMovement清除当前交互物体
            agentMovementForClear.ClearIgnoredCollisionObjects();
        }
        
        Debug.Log($"恢复操作完成，共释放了 {releasedCount} 个物体回到场景");
    }

    public bool Redo()
    {
        if (currentStateIndex < stateHistory.Count - 1)
        {
            currentStateIndex++;
            
            // 先恢复所有交互物体的从属关系
            RestorePickedObjectsParent();
            
            LoadState(stateHistory[currentStateIndex], stateHistoryA2T[currentStateIndex]);
            return true;
        }
        return false;
    }

    // 加载指定索引的状态
    public bool LoadStateByIndex(string indexText)
    {
        if (int.TryParse(indexText, out int index))
        {
            if (index >= 0 && index <= stateHistory.Count-1) // 检查索引是否在有效范围内
            {
                currentStateIndex = index; // 索引从0开始，用户输入从1开始
                
                // 先恢复所有交互物体的从属关系
                RestorePickedObjectsParent();
                
                LoadState(stateHistory[currentStateIndex], stateHistoryA2T[currentStateIndex]);
                return true;
            }
            else
            {
                Debug.LogWarning("输入的索引超出范围！");
            }
        }
        else
        {
            Debug.LogWarning("无效的索引输入！");
        }
        return false;
    }

    private void LoadState(SceneState state)
    {
        //string sceneStateJson = JsonUtility.ToJson(state);
        //print(sceneStateJson);
        // 更新场景状态和相关信息
        // agent.transform.SetPositionAndRotation(state.agentPosition, state.agentRotation);
        root.TeleportRoot(state.agentPosition,state.agentRotation);
        stateIndexText.text = "CurrentIndex: " + state.id;

        // 恢复关节角度
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
            
            // 恢复爪子角度
            if (state.gripperAngles != null && state.gripperAngles.Count > 0 && agentMovement.gripperController != null)
            {
                // 获取当前左臂爪子
                ArticulationBody leftArmLeftGripper = agentMovement.gripperController.currentLeftLeftGripper;
                ArticulationBody leftArmRightGripper = agentMovement.gripperController.currentLeftRightGripper;
                
                // 获取当前右臂爪子
                ArticulationBody rightArmLeftGripper = agentMovement.gripperController.currentRightLeftGripper;
                ArticulationBody rightArmRightGripper = agentMovement.gripperController.currentRightRightGripper;
                
                // 恢复左臂爪子角度
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
                
                // 恢复右臂爪子角度
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

        // 还原其他物体的状态
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
        // 更新场景状态和相关信息

        print("Load state");

        root.TeleportRoot(state.agentPosition,state.agentRotation);
        agent.transform.position = state.agentPosition;
        agent.transform.rotation = state.agentRotation;

        stateIndexText.text = "CurrentIndex: " + state.id;

        // 恢复关节角度
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
            
            // 恢复爪子角度
            if (state.gripperAngles != null && state.gripperAngles.Count > 0 && agentMovement.gripperController != null)
            {
                // 获取当前左臂爪子
                ArticulationBody leftArmLeftGripper = agentMovement.gripperController.currentLeftLeftGripper;
                ArticulationBody leftArmRightGripper = agentMovement.gripperController.currentLeftRightGripper;
                
                // 获取当前右臂爪子
                ArticulationBody rightArmLeftGripper = agentMovement.gripperController.currentRightLeftGripper;
                ArticulationBody rightArmRightGripper = agentMovement.gripperController.currentRightRightGripper;
                
                // 恢复左臂爪子角度
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
                
                // 恢复右臂爪子角度
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

        // 还原其他物体的状态
        foreach (ObjectState objectState in state.objects)
        {
            LoadObjectState(objectState);
        }

        
    }

    private void LoadObjectState(ObjectState objectState)
    {
        // 查找动态物体
        if (simObjectsDict.TryGetValue(objectState.name, out GameObject obj))
        {
            // 先处理父级关系
            // 1. 如果物体当前在手中，但在保存的状态中不是被拿起的，则将其释放
            if (obj.transform.parent != null && obj.transform.parent.CompareTag("Hand") && !objectState.isPickedUp)
            {
                Debug.Log($"LoadObjectState: 将物体 {obj.name} 从手部释放到场景中");
                obj.transform.SetParent(ObjectsParent);
                
                // 从操作列表中移除
                if (ObjectsInOperation.Contains(obj))
                {
                    ObjectsInOperation.Remove(obj);
                }
            }
            // 2. 如果物体在保存的状态中是被拿起的，但当前不在手中，这种情况应该由Agent的Pick操作来处理
            // 我们只恢复位置，不改变父级关系
            
            // 设置活动状态
            obj.SetActive(objectState.isActive);

            // 处理物理状态和位置
            HandlePhysicsState(obj, objectState);

            // 恢复其他状态（通过IUniqueStateManager接口）
            IUniqueStateManager[] savables = obj.GetComponents<IUniqueStateManager>();
            foreach (var savable in savables)
            {
                savable.LoadState(objectState);
            }
            
            // 再次检查刚体状态，确保根据父级关系正确设置
            // Rigidbody rb = obj.GetComponent<Rigidbody>();
            // if (rb != null)
            // {
            //     if (obj.transform.parent != null && obj.transform.parent.CompareTag("Hand"))
            //     {
            //         // 如果在手中，禁用物理
            //         rb.isKinematic = true;
            //         rb.useGravity = false;
            //         rb.detectCollisions = false;
            //     }
            //     else if (!objectState.isPickedUp)
            //     {
            //         // 如果不在手中且保存状态也不是被拿起，启用物理
            //         rb.isKinematic = false;
            //         rb.useGravity = true;
            //         rb.detectCollisions = true;
            //     }
            // }
        }
        else
        {
            Debug.LogWarning($"无法找到物体: {objectState.name}");
        }
    }

    private void HandlePhysicsState(GameObject obj, ObjectState objectState)
    {
        // 无论物体类型如何，都先暂停物理
        Rigidbody rb = obj.GetComponent<Rigidbody>();
        bool hadRigidbody = rb != null;
        bool wasKinematic = hadRigidbody && rb.isKinematic;
        bool usedGravity = hadRigidbody && rb.useGravity;
        bool detectCollisions = hadRigidbody && rb.detectCollisions;
        
        // 暂时禁用物理以确保位置设置正确
        if (hadRigidbody)
        {

            rb.isKinematic = true;
            rb.useGravity = false;
            rb.detectCollisions = false;
            
            // 清除所有现有速度和角速度
            rb.linearVelocity = Vector3.zero;
            rb.angularVelocity = Vector3.zero;
        }
        
        // 设置位置和旋转
        obj.transform.SetLocalPositionAndRotation(objectState.position, objectState.rotation);
        
        // 确保物理系统与新的transform同步
        Physics.SyncTransforms();
        
        // 根据物体类型恢复物理属性
        SimObjPhysics simObj = obj.GetComponent<SimObjPhysics>();
        if (hadRigidbody)
        {
            // 如果物体目前被拿起（在手中），保持kinematic=true
            if (obj.transform.parent != null && obj.transform.parent.CompareTag("Hand"))
            {
                rb.isKinematic = true;
                rb.useGravity = false;
                rb.detectCollisions = false;
            }
            else if (simObj != null && simObj.PrimaryProperty == SimObjPrimaryProperty.CanPickup)
            {
                // 对于可拾取物体，我们需要确保它们的位置稳定后再恢复物理
                // 先进行位置固定，然后延迟恢复物理
                StartCoroutine(SafelyRestorePhysics(rb, false, true, true));
            }
            else
            {
                if(simObj.PrimaryProperty != SimObjPrimaryProperty.Static)
                {

                    // 其他物体恢复原有状态
                    rb.isKinematic = wasKinematic;
                    rb.useGravity = usedGravity;
                    rb.detectCollisions = detectCollisions;
                }

            }
            
            // 确保所有速度都被重置
            rb.linearVelocity = Vector3.zero;
            rb.angularVelocity = Vector3.zero;
        }
        
        // 记录调试信息
        Debug.Log($"物体 {obj.name} 位置已恢复到: {objectState.position}, 旋转: {objectState.rotation.eulerAngles}");
    }
    
    // 安全地恢复物理属性的协程
    private IEnumerator SafelyRestorePhysics(Rigidbody rb, bool isKinematic, bool useGravity, bool detectCollisions)
    {
        if (rb == null) yield break;
        
        // 等待一帧，确保所有位置设置都已完成
        yield return null;
        
        // 再次确保物理系统与transform同步
        Physics.SyncTransforms();
        
        // 确保速度为零
        rb.linearVelocity = Vector3.zero;
        rb.angularVelocity = Vector3.zero;
        
        // 先恢复碰撞检测但保持kinematic状态
        rb.detectCollisions = detectCollisions;
        yield return new WaitForFixedUpdate();
        
        // 然后恢复重力但保持kinematic状态
        rb.useGravity = useGravity;
        yield return new WaitForFixedUpdate();
        
        // 最后恢复kinematic状态
        rb.isKinematic = isKinematic;
        
        // 再次确保速度为零，防止位置突变
        rb.linearVelocity = Vector3.zero;
        rb.angularVelocity = Vector3.zero;
        
        Debug.Log($"物体 {rb.gameObject.name} 的物理属性已安全恢复");
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

        Debug.LogWarning($"未找到ID为 {objectID} 的物品");
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
                    Debug.LogWarning($"物品 {objectID} 没有可用的交互点");
                    return null;
                }
            }
        }

        Debug.LogWarning($"未找到ID为 {objectID} 的物品");
        return null;
    }

    public void SetParent(Transform parent, string objectID)
    {
        Debug.Log($"SetParent: 将物体 {objectID} 设置为 {parent.name} 的子物体");
        
        // 确保父物体有Hand标签，便于后续恢复时查找
        if (!parent.CompareTag("Hand"))
        {
            try
            {
                parent.tag = "Hand";
                Debug.Log($"已为夹爪 {parent.name} 添加Hand标签");
            }
            catch (Exception e)
            {
                Debug.LogWarning($"无法为夹爪添加Hand标签: {e.Message}");
            }
        }
        
        SimObjPhysics[] allObjects = FindObjectsOfType<SimObjPhysics>();

        foreach (SimObjPhysics obj in allObjects)
        {
            if (obj.ObjectID == objectID)
            {
                Debug.Log($"找到物体 {obj.name} (ID: {objectID})，设置其父物体为 {parent.name}");
                
                ObjectsInOperation.Add(obj.gameObject);
                obj.transform.SetParent(parent);
                Rigidbody rigidbody = obj.GetComponent<Rigidbody>();
                if (rigidbody != null)
                {
                    rigidbody.isKinematic = true; // 设置为运动学模式
                    rigidbody.useGravity = false; // 禁用重力
                    rigidbody.detectCollisions = false; // 禁用碰撞检测
                }
                
                Debug.Log($"物体 {obj.name} 已成功设置为 {parent.name} 的子物体，并禁用了物理");
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
                    
                    rigidbody.isKinematic = false; // 恢复物理运动
                    rigidbody.useGravity = true; // 启用重力
                    rigidbody.detectCollisions = true; // 启用碰撞检测
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
            
            // 检查碰撞对象是否为当前交互物体
            bool isInteractingObject = false;
            
            // 获取AgentMovement组件
            AgentMovement agentMovement = FindObjectOfType<AgentMovement>();
            if (agentMovement != null)
            {
                // 尝试判断collisionB是否为交互物体
                SimObjPhysics[] allObjects = FindObjectsOfType<SimObjPhysics>();
                foreach (SimObjPhysics obj in allObjects)
                {
                    if (obj.gameObject.name == collisionB)
                    {
                        // 从AgentMovement获取当前交互物体信息
                        // 假设AgentMovement有一个方法来检查物体是否为当前交互物体
                        if (agentMovement.IsCurrentInteractingObject(obj.ObjectID))
                        {
                            isInteractingObject = true;
                            Debug.Log($"碰撞物体 {collisionB} 是当前交互物体，不视为错误碰撞");
                            break;
                        }
                    }
                }
            }
            
            // 如果不是交互物体，则视为失败
            if (!isInteractingObject)
            {
                currentAgent.lastActionSuccess = false;
                currentAgent.errorMessage = $"Collision Detected. Joint {collisionA} collision with object {collisionB}";
            }
            else
            {
                // 如果是交互物体，仍然记录碰撞但不视为失败
                Debug.Log($"检测到与交互物体的碰撞，不标记为失败: {collisionB}");
            }
        }
        else
        {
            Debug.LogWarning("No state history to update lastActionSuccess or errorMessage.");
        }
    }
}
