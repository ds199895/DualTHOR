using UnityEngine;
using System.Collections.Generic;
using TMPro;
using System.Collections;
using System.Linq;
using System;

public class SceneManager : MonoBehaviour
{
    private readonly List<SceneState> stateHistory = new();
    private readonly List<SceneStateA2T> stateHistoryA2T = new();

    private int currentStateIndex=-1;
    [SerializeField]
    private GameObject agent;

    [SerializeField]
    TextMeshProUGUI stateIndexText;
    [SerializeField]
    TextMeshProUGUI maxStateIndexText;
    [Header("����������ģ������")]
    [SerializeField]
    private List<GameObject> simObjects = new();//����������ģ�����壬�����������������ɵ����壬����unity�༭���пɼ�
    [Header("���������пɽ�������")]
    [SerializeField]
    private List<GameObject> interactableObjects = new();//���������пɽ�������
    //[SerializeField]
    [Header("��Ұ��Χ���ܽ�������")]

    public List<GameObject> canInteractableObjects = new();//��Ұ��Χ�����пɽ�������
    [Header("���������пɴ���λ��")]

    [SerializeField]
    private List<GameObject> transferPoints = new();//���������пɽ�������Ĵ���λ��
    //[SerializeField]
    [Header("��Ұ��Χ���ܽ�������Ĵ���λ��")]

    public List<GameObject> canTransferPoints = new();//��Ұ��Χ�ڿɽ�������Ĵ���λ��

    [Header("�ɽ�������Ŀɲ���λ��")]
    [SerializeField]
    private List<GameObject> interactablePoints = new();//���������пɽ�������Ŀɲ���λ��

    private readonly Dictionary<string, GameObject> simObjectsDict = new();//����������ģ�������ֵ䣬���ڿ��ٲ���
    //private readonly Dictionary<string, GameObject> canInteractableObjectsDict = new();//����������ģ�������ֵ䣬���ڿ��ٲ���

    public List<GameObject> TransferPoints => transferPoints;
    //public List<GameObject> CanTransferPoints => canTransferPoints;

    public Dictionary<string, GameObject> SimObjectsDict => simObjectsDict;
    //public Dictionary<string , GameObject> CanInteractableObjectsDict => canInteractableObjectsDict;

    [SerializeField]
    private GetObjectsInView getObjectsInView;
    void Start()
    {
        // ���Ҳ����ɽ��������б�
        FillList(simObjects, new[] { "Interactable", "DynamicAdd" });
        // ���Ҳ����ɽ��������ֵ�
        FillDict(simObjectsDict, new[] { "Interactable", "DynamicAdd" });
        

        // ���Ҳ����ɽ��������б�
        FillList(interactableObjects, new[] { "Interactable"});

        // ���Ҳ���䴫��λ���б�
        FillList(transferPoints, new[] { "TransferPoint" });

        // ���Ҳ����ɲ���λ���б�
        FillList(interactablePoints, new[] { "InteractablePoint" });

        GameObject[] dynamicAdds = GameObject.FindGameObjectsWithTag("DynamicAdd");
        foreach (GameObject obj in dynamicAdds)
        {
            obj.SetActive(false);
        }

        StartCoroutine(DelayedSave());
    }


    // ����б�ķ���
    private void FillList(List<GameObject> list, string[] tags)
    {
        foreach (string tag in tags)
        {
            GameObject[] objects = GameObject.FindGameObjectsWithTag(tag); // �������д��е�ǰ��ǩ������

            foreach (var obj in objects)
            {
                if (tag == "TransferPoint") // �ж��Ƿ��� TransferPoint ��ǩ
                {
                    if (obj.transform.parent != null) // ��������Ƿ��и�����
                    {
                        list.Add(obj.transform.parent.gameObject); // ��Ӹ�����
                    }
                }
                else
                {
                    list.Add(obj); // ���������ǩ�����屾��
                }
            }
        }
    }

    // ����ֵ�ķ���
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

    public void SaveCurrentState()
    {
        #region ����汾���Ƶĳ�����Ϣ
        // ���浱ǰ״̬
        SceneState state = new()
        {
            id = currentStateIndex + 1,
            agentPosition = agent.transform.position,
            agentRotation = agent.transform.rotation,
            objects = new ObjectState[simObjects.Count]
        };
        stateIndexText.text = "CurrentIndex: " + state.id;

        // ����ÿ�� simObject ��״̬
        for (int i = 0; i < simObjects.Count; i++)
        {
            state.objects[i] = SaveObjectState(simObjects[i]);
        }
        #endregion

        #region ���淵�ظ�python�ĳ�����Ϣ
        SceneStateA2T stateA2T = new()
        {
            id = currentStateIndex + 1,
            objects = new ObjectStateA2T[interactableObjects.Count],
            agent = new AgentStateA2T()
            {
                name = agent.name,
                position = agent.transform.position,
                rotation = agent.transform.rotation,
                lastAction = stateHistoryA2T.Count > 0 ? stateHistoryA2T[currentStateIndex].agent.lastAction : "idle", // Ĭ��ֵ
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

        // ȷ����ǰ״̬������������ʷ��¼
        if (currentStateIndex < stateHistory.Count - 1)
        {
            stateHistory.RemoveRange(currentStateIndex + 1, stateHistory.Count - (currentStateIndex + 1));
            stateHistoryA2T.RemoveRange(currentStateIndex + 1, stateHistoryA2T.Count - (currentStateIndex + 1));
        }
        stateHistory.Add(state);
        stateHistoryA2T.Add(stateA2T);
        maxStateIndexText.text= "MaxIndex:" + (stateHistory.Count-1).ToString();
        // �������״̬
        //print(JsonUtility.ToJson(state));
        print(JsonUtility.ToJson(stateA2T));
        currentStateIndex++;
    }

    // ���浥�������״̬
    private ObjectState SaveObjectState(GameObject obj)
    {
        ObjectState objectState = new()
        {
            name = obj.name,
            position = obj.transform.position,
            rotation = obj.transform.rotation,
            isActive = obj.activeSelf,
        };

        // ��������л�״̬
        IUniqueStateManager[] savables = obj.GetComponents<IUniqueStateManager>();
        foreach (var savable in savables)
        {
            savable.SaveState(objectState); // ����״̬
        }

        return objectState;
    }

    // ����ɽ��������״̬
    private ObjectStateA2T SaveInteractableObjectState(GameObject obj)
    {
        SimObjPhysics sop = obj.GetComponent<SimObjPhysics>();
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
            isToggled = sop.IsToggleable ? (obj.GetComponent<CanToggleOnOff>().IsOn) : false,
            isBroken = sop.IsBreakable ? obj.GetComponent<Break>().Broken : false,
            isFilledWithLiquid = sop.IsFillable ? obj.GetComponent<Fill>().IsFilled : false,
            isUsedUp = sop.CanBeUsedUp ? obj.GetComponent<UsedUp>().IsUsedUp : false,
            isCooked = sop.IsCookable ? obj.GetComponent<CookObject>().IsCooked : false,
            isSliced = sop.IsSliceable ? obj.GetComponent<SliceObject>().IsSliced : false,
            isOpen = sop.IsOpenable ? obj.GetComponent<CanOpen_Object>().IsOpen : false,
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
    public void Undo()
    {
        if (currentStateIndex > 0)
        {
            currentStateIndex--;
            //LoadState(stateHistory[currentStateIndex]);
            LoadState(stateHistory[currentStateIndex],stateHistoryA2T[currentStateIndex]);
        }
    }

    public void Redo()
    {
        if (currentStateIndex < stateHistory.Count - 1)
        {
            currentStateIndex++;
            //LoadState(stateHistory[currentStateIndex]);
            LoadState(stateHistory[currentStateIndex], stateHistoryA2T[currentStateIndex]);
        }
    }

    // ����ָ��������״̬
    public void LoadStateByIndex(string indexText)
    {
        if (int.TryParse(indexText, out int index))
        {
            if (index >= 0 && index <= stateHistory.Count-1) // ��������Ƿ�����Ч��Χ��
            {
                currentStateIndex = index; // ������0��ʼ���û������1��ʼ
                LoadState(stateHistory[currentStateIndex], stateHistoryA2T[currentStateIndex]);
            }
            else
            {
                Debug.LogWarning("���������������Χ��");
            }
        }
        else
        {
            Debug.LogWarning("��Ч���������룡");
        }
    }

    private void LoadState(SceneState state)
    {
        //string sceneStateJson = JsonUtility.ToJson(state);
        //print(sceneStateJson);
        // ���³���״̬�������Ϣ
        agent.transform.SetPositionAndRotation(state.agentPosition, state.agentRotation);
        stateIndexText.text = "CurrentIndex: " + state.id;

        // ��ԭ���������״̬
        foreach (ObjectState objectState in state.objects)
        {
            LoadObjectState(objectState);
        }
    }

    private void LoadState(SceneState state, SceneStateA2T stateA2T)
    {
        //string sceneStateJson = JsonUtility.ToJson(state);
        //print(sceneStateJson);
        string sceneStateJsonA2T = JsonUtility.ToJson(stateA2T);
        print(sceneStateJsonA2T);
        // ���³���״̬�������Ϣ
        agent.transform.position = state.agentPosition;
        agent.transform.rotation = state.agentRotation;
        stateIndexText.text = "CurrentIndex: " + state.id;

        // ��ԭ���������״̬
        foreach (ObjectState objectState in state.objects)
        {
            LoadObjectState(objectState);
        }
    }

    private void LoadObjectState(ObjectState objectState)
    {
        // ���Ҷ�̬����
        if (simObjectsDict.TryGetValue(objectState.name, out GameObject obj))
        {
            obj.SetActive(objectState.isActive);

            // ��������״̬
            HandlePhysicsState(obj, objectState);
            //obj.transform.SetPositionAndRotation(objectState.position, objectState.rotation);

            // �ָ�״̬
            IUniqueStateManager[] savables = obj.GetComponents<IUniqueStateManager>();
            foreach (var savable in savables)
            {
                savable.LoadState(objectState); // �ָ�״̬
            }
        }
    }

    private void HandlePhysicsState(GameObject obj, ObjectState objectState)
    {
        SimObjPhysics simObj = obj.GetComponent<SimObjPhysics>();
        if (simObj != null && simObj.PrimaryProperty == SimObjPrimaryProperty.CanPickup) // ȷ�� simObj ��Ϊ��
        {
            if (obj.TryGetComponent<Rigidbody>(out Rigidbody rb))
            {
                rb.isKinematic = true; // ��ʱ��������Ϊ�˶�ѧ
                obj.transform.SetPositionAndRotation(objectState.position, objectState.rotation);
                rb.isKinematic = false; // �ָ������˶�
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

        Debug.LogWarning($"δ�ҵ�IDΪ {objectID} ����Ʒ");
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
                    Debug.LogWarning($"��Ʒ {objectID} û�п��õĽ�����");
                    return null;
                }
            }
        }

        Debug.LogWarning($"δ�ҵ�IDΪ {objectID} ����Ʒ");
        return null;
    }
    
    public void UpdateLastActionSuccess(bool isSuccessful, string actionType = null)
    {
        if (stateHistoryA2T.Count > 0)
        {
            var currentAgent = stateHistoryA2T[currentStateIndex].agent;

            currentAgent.lastActionSuccess = isSuccessful;

            if (!isSuccessful && actionType != null)
            {
                // ���Ի�ȡ��Ӧ�����Ĵ�����Ϣ
                ErrorMessage errorMessageComponent = FindObjectOfType<ErrorMessage>();
                if (errorMessageComponent != null && errorMessageComponent.errorMessage.ContainsKey(actionType))
                {
                    string[] possibleErrors = errorMessageComponent.GetErrorMessage(actionType);
                    currentAgent.errorMessage = possibleErrors[UnityEngine.Random.Range(0, possibleErrors.Length)];
                }
                else
                {
                    currentAgent.errorMessage = "Error message not defined for this action."; // �����޶�Ӧ������Ϣ
                }
            }
            else
            {
                currentAgent.errorMessage = string.Empty; // �ɹ�ʱ�޴�����Ϣ
            }
        }
        else
        {
            Debug.LogWarning("No state history to update lastActionSuccess or errorMessage.");
        }
    }
}
