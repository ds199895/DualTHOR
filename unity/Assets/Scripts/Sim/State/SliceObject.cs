using UnityEngine;

// 1.该脚本管理对象的切割行为，当对象被切割时，会生成一个新的对象（通常是切割后的碎片）。
public class SliceObject : MonoBehaviour, IUniqueStateManager
{
    [Header("Object To Change To")]
    [SerializeField]
    private string objectToChangeTo; // 切割后生成的新对象的预制体。

    [SerializeField]
    private bool isSliced = false;

    private SceneStateManager sceneManager;

    public bool IsSliced => isSliced;


    public void SaveState(ObjectState objectState)
    {
        objectState.slicedState = new SlicedState { isSliced = isSliced };
    }

    public void LoadState(ObjectState objectState)
    {
        isSliced = objectState.slicedState.isSliced;
    }

    void Start()
    {
        sceneManager = GameObject.Find("SceneManager").GetComponent<SceneStateManager>();

#if UNITY_EDITOR
        PropertyValidator.ValidateProperty(gameObject, SimObjSecondaryProperty.CanBeSliced);
        if (string.IsNullOrEmpty(objectToChangeTo))
        {
            Debug.LogError($"{gameObject.transform.name} is missing Object To Change To!");
        }
#endif
    }

    void Update()
    {
        if (Input.GetKeyDown(KeyCode.H))
        {
            print("SliceObject.Slice()");
            Slice();
        }
    }

    //切割物体
    public void Slice()
    {
        if (isSliced)
        {
            return;
        }

        if (!sceneManager.SimObjectsDict.TryGetValue(objectToChangeTo, out GameObject resultObject))
        {
            return; // 如果未找到对应的对象，直接返回
        }

        print(resultObject.name);
        ActivateResultObject(resultObject);
        isSliced = true;

        if (gameObject.GetComponent<SimObjPhysics>().HasSecondaryProperty(SimObjSecondaryProperty.CanBeCooked) && 
            gameObject.GetComponent<CookObject>().IsCooked)
        {
            CookChildObjects(resultObject);
        }
        gameObject.SetActive(false);

    }

    private void ActivateResultObject(GameObject resultObject)
    {
        resultObject.SetActive(true);
        //激活resultobject的所有子物体
        foreach (Transform t in resultObject.transform)
        {
            t.gameObject.SetActive(true);
        }
        resultObject.transform.SetPositionAndRotation(transform.position, transform.rotation);
        //resultObject.transform.parent = GameObject.Find("Objects").transform;
    }

    private void CookChildObjects(GameObject resultObject)
    {
        foreach (Transform t in resultObject.transform)
        {
            t.GetComponent<CookObject>().Cook();
        }
    }
}
