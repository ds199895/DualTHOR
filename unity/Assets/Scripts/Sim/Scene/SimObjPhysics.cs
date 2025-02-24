using System;
using System.Collections.Generic;
using UnityEngine;

public class SimObjPhysics : MonoBehaviour
{
    [Header("Unique String ID of this Object In Scene")]
    [SerializeField]
    private string objectID = string.Empty;

    [Header("Object Type")]
    [SerializeField]
    private SimObjType type = SimObjType.Undefined;

    [Header("Primary Property (Must Have only 1)")]
    [SerializeField]
    private SimObjPrimaryProperty primaryProperty;

    [Header("Additional Properties (Can Have Multiple)")]
    [SerializeField]
    private SimObjSecondaryProperty[] secondaryProperties;



    //[Header("Transfer Point")]
    [SerializeField]
    private Transform transferPoint; // 物体的传送点

    //[Header("Interactable Points")]
    [SerializeField]
    private Transform[] interactablePoints; // 物体的可交互点

    //[Header("Visible Points")]
    [SerializeField]
    private Transform[] visiblePoints; // 物体的传送点
    // 可放置物体的触发盒
    [SerializeField]
    private GameObject[] receptacleTriggerBoxes;


    public List<GameObject> parentReceptacleObjects;


    public bool IsReceptacle { get; private set; }
    public bool IsPickupable { get; private set; }
    public bool IsMoveable { get; private set; }
    public bool IsStatic { get; private set; }
    public bool IsToggleable { get; private set; }
    public bool IsOpenable { get; private set; }
    public bool IsBreakable { get; private set; }
    public bool IsFillable { get; private set; }
    public bool IsDirtyable { get; private set; }
    public bool IsCookable { get; private set; }
    public bool IsSliceable { get; private set; }
    public bool CanBeUsedUp { get; private set; }

    public string ObjectID => objectID;
    public SimObjType Type => type;
    public SimObjPrimaryProperty PrimaryProperty => primaryProperty;
    public SimObjSecondaryProperty[] SecondaryProperties => secondaryProperties;
    public Transform TransferPoint => transferPoint;
    public Transform[] InteractablePoints => interactablePoints;
    public Transform[] VisiblePoints => visiblePoints;

    void Start()
    {
        objectID = gameObject.name;
        InitializeInteractablePoints();
        InitializeTransferPoint();

        InitializeVisiblePoints();
        InitializeProperties();
    }

    //初始化传送点
    private void InitializeTransferPoint()
    {
        // 创建一个新的 GameObject 作为 transferPoint
        GameObject newTransferPoint = new GameObject("TransferPoint");
        
        newTransferPoint.transform.SetParent(transform);


        // 设置位置和旋转
        newTransferPoint.transform.position = interactablePoints[0].position + interactablePoints[0].forward * -0.5f;
        // newTransferPoint.transform.position = transform.position + transform.up * 0.5f;
        // newTransferPoint.transform.position = transform.position+transform.right*0.5f;
        newTransferPoint.transform.rotation = transform.rotation;

        // 将新的 Transform 赋值给 transferPoint
        transferPoint = newTransferPoint.transform;
    }

    //初始化可交互点
    private void InitializeInteractablePoints()
    {
        // 获取物体的所有 Renderer 组件
        Renderer[] renderers = GetComponentsInChildren<Renderer>();
        if (renderers.Length > 0)
        {
            // 初始化一个空的 Bounds
            Bounds bounds = renderers[0].bounds;
            
            // 合并所有 Renderer 的 Bounds
            foreach (Renderer renderer in renderers)
            {
                bounds.Encapsulate(renderer.bounds);
            }

            // 创建一个新的 GameObject 作为 interactablePoint
            GameObject newInteractablePoint = new GameObject("InteractablePoint");
            newInteractablePoint.transform.SetParent(transform);

            // 设置位置为几何中心
            newInteractablePoint.transform.position = bounds.center;
            newInteractablePoint.transform.rotation=transform.rotation;
            // 将新的 Transform 赋值给 interactablePoints
            interactablePoints = new Transform[] { newInteractablePoint.transform };
        }
        else
        {
            Debug.LogWarning($"{gameObject.name} 没有找到任何 Renderer 组件。");
        }
    }
    
    //初始化可看见点
    private void InitializeVisiblePoints()
    {
        GameObject visiblePointsObject = transform.Find("VisibilityPoints").gameObject;
        if (visiblePointsObject != null)
        {
            visiblePoints = new Transform[visiblePointsObject.transform.childCount];
            for (int i = 0; i < visiblePointsObject.transform.childCount; i++)
            {
                visiblePoints[i] = visiblePointsObject.transform.GetChild(i);
            }
        }
        else
        {
            Debug.LogWarning($"{gameObject.name} 没有找到名为 'VisibilityPoints' 的子物体。");
        }
    }

    private void InitializeProperties()
    {
        IsReceptacle = Array.IndexOf(secondaryProperties, SimObjSecondaryProperty.Receptacle) > -1 && receptacleTriggerBoxes != null;
        IsPickupable = primaryProperty == SimObjPrimaryProperty.CanPickup;
        IsMoveable = primaryProperty == SimObjPrimaryProperty.Moveable;
        IsStatic = primaryProperty == SimObjPrimaryProperty.Static;
        IsToggleable = GetComponent<CanToggleOnOff>() != null;
        IsOpenable = GetComponent<CanOpen_Object>() != null;
        IsBreakable = GetComponentInChildren<Break>() != null;
        IsFillable = GetComponent<Fill>() != null;
        IsCookable = GetComponent<CookObject>() != null;
        IsSliceable = GetComponent<SliceObject>() != null;
        CanBeUsedUp = GetComponent<UsedUp>() != null;
    }
    public bool HasSecondaryProperty(SimObjSecondaryProperty prop)
    {
        return Array.Exists(secondaryProperties, element => element == prop);
    }

    public List<string> ParentReceptacleObjectsIds()
    {
        List<string> objectIds = new();

        foreach (GameObject receptacle in parentReceptacleObjects)
        {
            if (receptacle.TryGetComponent<SimObjPhysics>(out SimObjPhysics sop))
            {
                objectIds.Add(sop.ObjectID);
            }
        }

        return objectIds;
    }

    //void OnDrawGizmos()
    //{
    //    // 检查可见点是否存在
    //    if (visiblePoints != null)
    //    {
    //        Gizmos.color = Color.yellow; // 设置 Gizmos 颜色为黄色
    //        foreach (Transform point in visiblePoints)
    //        {
    //            // 绘制黄色正方体在可见点的位置
    //            Gizmos.DrawCube(point.position, Vector3.one * 0.03f); // 0.1f是正方体大小，可以根据需要调整
    //        }
    //    }
    //}
}
