<<<<<<< HEAD
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
    private Transform transferPoint; // ����Ĵ��͵�

    //[Header("Interactable Points")]
    [SerializeField]
    private Transform[] interactablePoints; // ����Ŀɽ�����

    //[Header("Visible Points")]
    [SerializeField]
    private Transform[] visiblePoints; // ����Ĵ��͵�
    // �ɷ�������Ĵ�����
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
        InitializeTransferPoint();
        InitializeInteractablePoints();
        InitializeVisiblePoints();
        InitializeProperties();
    }

    //��ʼ�����͵�
    private void InitializeTransferPoint()
    {
        Transform foundTransferPoint = transform.Find("TransferPoint");
        if (foundTransferPoint != null)
        {
            transferPoint = foundTransferPoint;
        }
        else
        {
            Debug.LogWarning($"{gameObject.name} û���ҵ���Ϊ 'TransferPoint' �������塣");
        }
    }

    //��ʼ���ɽ�����
    private void InitializeInteractablePoints()
    {
        Transform foundInteractablePoint = transform.Find("InteractablePoints");
        if (foundInteractablePoint != null)
        {
            interactablePoints = new Transform[foundInteractablePoint.childCount];
            for (int i = 0; i < foundInteractablePoint.childCount; i++)
            {
                interactablePoints[i] = foundInteractablePoint.GetChild(i);
            }
        }
        else
        {
            Debug.LogWarning($"{gameObject.name} û���ҵ���Ϊ 'InteractablePoints' �������塣");
        }
    }
    
    //��ʼ���ɿ�����
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
            Debug.LogWarning($"{gameObject.name} û���ҵ���Ϊ 'VisibilityPoints' �������塣");
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
    //    // ���ɼ����Ƿ����
    //    if (visiblePoints != null)
    //    {
    //        Gizmos.color = Color.yellow; // ���� Gizmos ��ɫΪ��ɫ
    //        foreach (Transform point in visiblePoints)
    //        {
    //            // ���ƻ�ɫ�������ڿɼ����λ��
    //            Gizmos.DrawCube(point.position, Vector3.one * 0.03f); // 0.1f���������С�����Ը�����Ҫ����
    //        }
    //    }
    //}
}
=======
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
    private Transform transferPoint; // ����Ĵ��͵�

    //[Header("Interactable Points")]
    [SerializeField]
    private Transform[] interactablePoints; // ����Ŀɽ�����

    //[Header("Visible Points")]
    [SerializeField]
    private Transform[] visiblePoints; // ����Ĵ��͵�
    // �ɷ�������Ĵ�����
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
        InitializeTransferPoint();
        InitializeInteractablePoints();
        InitializeVisiblePoints();
        InitializeProperties();
    }

    //��ʼ�����͵�
    private void InitializeTransferPoint()
    {
        Transform foundTransferPoint = transform.Find("TransferPoint");
        if (foundTransferPoint != null)
        {
            transferPoint = foundTransferPoint;
        }
        else
        {
            Debug.LogWarning($"{gameObject.name} û���ҵ���Ϊ 'TransferPoint' �������塣");
        }
    }

    //��ʼ���ɽ�����
    private void InitializeInteractablePoints()
    {
        Transform foundInteractablePoint = transform.Find("InteractablePoints");
        if (foundInteractablePoint != null)
        {
            interactablePoints = new Transform[foundInteractablePoint.childCount];
            for (int i = 0; i < foundInteractablePoint.childCount; i++)
            {
                interactablePoints[i] = foundInteractablePoint.GetChild(i);
            }
        }
        else
        {
            Debug.LogWarning($"{gameObject.name} û���ҵ���Ϊ 'InteractablePoints' �������塣");
        }
    }
    
    //��ʼ���ɿ�����
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
            Debug.LogWarning($"{gameObject.name} û���ҵ���Ϊ 'VisibilityPoints' �������塣");
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
    //    // ���ɼ����Ƿ����
    //    if (visiblePoints != null)
    //    {
    //        Gizmos.color = Color.yellow; // ���� Gizmos ��ɫΪ��ɫ
    //        foreach (Transform point in visiblePoints)
    //        {
    //            // ���ƻ�ɫ�������ڿɼ����λ��
    //            Gizmos.DrawCube(point.position, Vector3.one * 0.03f); // 0.1f���������С�����Ը�����Ҫ����
    //        }
    //    }
    //}
}
>>>>>>> 0c14a5c8d787bef23f3133ad2b2203f5035105bb
