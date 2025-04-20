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
    public class ActionConfig
    {
        public float successRate;
        public Dictionary<string, float> errorMessages;

        public override string ToString()
        {
            string errorMessagesString = string.Join(", ", errorMessages.Select(kv => $"{kv.Key}: {kv.Value}"));
            return $"Success Rate: {successRate}, Error Messages: [{errorMessagesString}]";
        }
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
            actionConfigs = JsonConvert.DeserializeObject<Dictionary<string, ActionConfig>>(json);

            // foreach(var config in actionConfigs){
            //     print(config.Value);
            // }
        }
        else
        {
            Debug.LogError("ErrorConfig.json not found!");
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
            position = obj.transform.position,
            rotation = obj.transform.rotation,
            isActive = obj.activeSelf,
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
            //LoadState(stateHistory[currentStateIndex]);
            LoadState(stateHistory[currentStateIndex],stateHistoryA2T[currentStateIndex]);
            return true;
        }
        return false;
    }

    public bool Redo()
    {
        if (currentStateIndex < stateHistory.Count - 1)
        {
            currentStateIndex++;
            //LoadState(stateHistory[currentStateIndex]);
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
            obj.SetActive(objectState.isActive);

            // 处理物理状态
            HandlePhysicsState(obj, objectState);
            //obj.transform.SetPositionAndRotation(objectState.position, objectState.rotation);

            // 恢复状态
            IUniqueStateManager[] savables = obj.GetComponents<IUniqueStateManager>();
            foreach (var savable in savables)
            {
                savable.LoadState(objectState); // 恢复状态
            }
        }
    }

    private void HandlePhysicsState(GameObject obj, ObjectState objectState)
    {
        SimObjPhysics simObj = obj.GetComponent<SimObjPhysics>();
        if (simObj != null && simObj.PrimaryProperty == SimObjPrimaryProperty.CanPickup) // 确保 simObj 不为空
        {
            if (obj.TryGetComponent<Rigidbody>(out Rigidbody rb))
            {
                rb.isKinematic = true; // 暂时将刚体设为运动学
                obj.transform.SetPositionAndRotation(objectState.position, objectState.rotation);
                rb.isKinematic = false; // 恢复物理运动
            }
        }
        else
        {
            obj.transform.SetPositionAndRotation(objectState.position, objectState.rotation);

        }
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
        SimObjPhysics[] allObjects = FindObjectsOfType<SimObjPhysics>();

        foreach (SimObjPhysics obj in allObjects)
        {
            if (obj.ObjectID == objectID)
            {
                ObjectsInOperation.Add(obj.gameObject);
                obj.transform.SetParent(parent);
                Rigidbody rigidbody = obj.GetComponent<Rigidbody>();
                if (rigidbody != null)
                {
                    rigidbody.isKinematic = true; // 设置为运动学模式
                    rigidbody.useGravity = false; // 禁用重力
                    rigidbody.detectCollisions = false; // 禁用碰撞检测
                }
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

    public void UpdateLastActionSuccessCollision(string collisionA,string collisionB)
    {
        if (stateHistoryA2T.Count > 0)
        {
            var currentAgent = stateHistoryA2T[currentStateIndex].agent;

            currentAgent.lastActionSuccess = false;


            // 尝试获取对应动作的错误信息

            currentAgent.errorMessage =$"Collision Detected. Joint {collisionA} collision with object {collisionB}";
        }
        else
        {
            Debug.LogWarning("No state history to update lastActionSuccess or errorMessage.");
        }
    }

    public bool UpdateLastActionSuccess(string actionType = null)
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
            
            // 如果没有特定的失败情况，使用配置文件中的概率处理
            if (actionConfigs.TryGetValue(actionType, out ActionConfig config))
            {
                float randomValue = UnityEngine.Random.value;
                if (randomValue < config.successRate)
                {
                    currentAgent.lastActionSuccess = true;
                    currentAgent.errorMessage = string.Empty;
                    return true;
                }
                else
                {
                    currentAgent.lastActionSuccess = false;
                    float cumulativeProbability = config.successRate;
                    foreach (var error in config.errorMessages)
                    {
                        cumulativeProbability += error.Value;
                        if (randomValue < cumulativeProbability)
                        {
                            currentAgent.errorMessage = error.Key;
                            break;
                        }
                    }
                    return false;
                }
            }
            else
            {
                Debug.LogWarning("No configuration found for action: " + actionType);
                currentAgent.lastActionSuccess = true;
                currentAgent.errorMessage =  "Error message not defined for this action, Default to Success.";
                return true;
            }
        }
        else
        {
            Debug.LogWarning("No state history to update lastActionSuccess or errorMessage.");
            return false;
        }
    }
}
