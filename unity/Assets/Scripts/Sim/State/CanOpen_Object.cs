<<<<<<< HEAD
﻿using System.Collections;
using UnityEngine;

//1.用于管理可打开对象的打开和关闭行为，支持滑动、旋转和缩放三种运动类型
//2.通过 LerpPosition, LerpRotation, 和 LerpScale 协程实现平滑的动画过渡
//3.在对象打开或关闭过程中，检测是否发生碰撞，并根据情况重置对象状态
//4.在对象打开或关闭过程中，检测触发器碰撞，并根据情况重置对象状态
//5.=号键测试代码，按下时调用 Interact 方法
//6.triggerEnabled = true：对象会响应触发器碰撞。
//triggerEnabled = false：对象将忽略所有触发器碰撞。
public class CanOpen_Object : MonoBehaviour,IUniqueStateManager
{
    [Header("Moving Parts for this Object")]
    [SerializeField]
    private GameObject[] MovingParts;//可移动部件的数组，表示对象的可打开部分

    [Header("Animation Parameters")]
    [SerializeField]
    private Vector3[] openPositions;//分别表示打开和关闭状态下的位置、旋转

    [SerializeField]
    private Vector3[] closedPositions;

    [SerializeField]
    private float animationTime = 0.2f;//动画持续时间

    [SerializeField]
    private float currentOpenness = 0.0f; //当前打开程度，范围从 0.0 到 1.0

    [SerializeField]
    private bool isOpen = false;

    //todo:scale暂未用到，后续可能删除
    private enum MovementType { Slide, Rotate, Scale }

    [SerializeField]
    private MovementType movementType;

    private SimObjPhysics simObjPhysics;
    private Rigidbody rb;

    public bool IsOpen => isOpen;

    public void SaveState(ObjectState objectState)
    {
        objectState.openState = new OpenState
        {
            isOpen = isOpen,
            movingParts = MovingParts,
        };

    }

    public void LoadState(ObjectState objectState)
    {
        isOpen = objectState.openState.isOpen;
        // 根据开关状态选择相应的位置或旋转
        Vector3[] targetPositions = isOpen ? openPositions : closedPositions;
        for (int i = 0; i < MovingParts.Length; i++)
        {
            if (movementType == MovementType.Slide)
            {
                MovingParts[i].transform.localPosition = targetPositions[i];
            }
            else
            {
                MovingParts[i].transform.localRotation = Quaternion.Euler(targetPositions[i]);
            }
        }
    }

    void Start()
    {
#if UNITY_EDITOR
        PropertyValidator.ValidateProperty(gameObject, SimObjSecondaryProperty.CanOpen);
#endif

        simObjPhysics = GetComponent<SimObjPhysics>();
        rb = GetComponent<Rigidbody>();

        currentOpenness = isOpen ? 1.0f : 0.0f;
    }
    
    void Update()
    {
//#if UNITY_EDITOR
        if (Input.GetKeyDown(KeyCode.Equals))
        {
            Interact();
        }
//#endif
    }

    //处理对象的打开或关闭操作，支持多种参数配置
    public void Interact()
    {
        float targetOpenness = isOpen ? 0.0f : 1.0f; // 如果当前状态是打开的，目标打开程度为 0.0f，否则为 1.0f

        //如果可打开物体是可捡起物体，如尝试打开（书、盒子、笔记本等），在尝试打开或关闭之前，这些物体必须具有刚体 = false，否则它可能会穿过其他物体
        if (simObjPhysics.PrimaryProperty == SimObjPrimaryProperty.CanPickup)
        {
            rb.isKinematic = false;
        }

        StartCoroutine(AnimateOpenClose(targetOpenness));

        // 更新状态
        SetIsOpen(targetOpenness);
    }

    private IEnumerator AnimateOpenClose(float targetOpenness)
    {
        float elapsedTime = 0f;
        float initialOpenness = currentOpenness;
        while (elapsedTime < animationTime)
        {
            elapsedTime += Time.fixedDeltaTime;
            currentOpenness = Mathf.Clamp(Mathf.Lerp(initialOpenness, targetOpenness, elapsedTime / animationTime), 0f, 1f);

            for (int i = 0; i < MovingParts.Length; i++)
            {
                if (movementType == MovementType.Slide)
                {
                    MovingParts[i].transform.localPosition = Vector3.Lerp(closedPositions[i], openPositions[i], currentOpenness);
                }
                else
                {
                    MovingParts[i].transform.localRotation = Quaternion.Lerp(Quaternion.Euler(closedPositions[i]), Quaternion.Euler(openPositions[i]), currentOpenness);
                }
            }
            yield return null;
        }
    }

    //设置对象的打开状态
    private void SetIsOpen(float openness)
    {
        isOpen = openness > 0;
        currentOpenness = openness;
    }

    //同步子对象和父对象的位置和旋转
    public void SyncPosRot(GameObject child, GameObject parent)
    {
        child.transform.SetPositionAndRotation(parent.transform.position, parent.transform.rotation);
    }
}
=======
﻿using System.Collections;
using UnityEngine;

//1.用于管理可打开对象的打开和关闭行为，支持滑动、旋转和缩放三种运动类型
//2.通过 LerpPosition, LerpRotation, 和 LerpScale 协程实现平滑的动画过渡
//3.在对象打开或关闭过程中，检测是否发生碰撞，并根据情况重置对象状态
//4.在对象打开或关闭过程中，检测触发器碰撞，并根据情况重置对象状态
//5.=号键测试代码，按下时调用 Interact 方法
//6.triggerEnabled = true：对象会响应触发器碰撞。
//triggerEnabled = false：对象将忽略所有触发器碰撞。
public class CanOpen_Object : MonoBehaviour,IUniqueStateManager
{
    [Header("Moving Parts for this Object")]
    [SerializeField]
    private GameObject[] MovingParts;//可移动部件的数组，表示对象的可打开部分

    [Header("Animation Parameters")]
    [SerializeField]
    private Vector3[] openPositions;//分别表示打开和关闭状态下的位置、旋转

    [SerializeField]
    private Vector3[] closedPositions;

    [SerializeField]
    private float animationTime = 0.2f;//动画持续时间

    [SerializeField]
    private float currentOpenness = 0.0f; //当前打开程度，范围从 0.0 到 1.0

    [SerializeField]
    private bool isOpen = false;

    //todo:scale暂未用到，后续可能删除
    private enum MovementType { Slide, Rotate, Scale }

    [SerializeField]
    private MovementType movementType;

    private SimObjPhysics simObjPhysics;
    private Rigidbody rb;

    public bool IsOpen => isOpen;

    public void SaveState(ObjectState objectState)
    {
        objectState.openState = new OpenState
        {
            isOpen = isOpen,
            movingParts = MovingParts,
        };

    }

    public void LoadState(ObjectState objectState)
    {
        isOpen = objectState.openState.isOpen;
        // 根据开关状态选择相应的位置或旋转
        Vector3[] targetPositions = isOpen ? openPositions : closedPositions;
        for (int i = 0; i < MovingParts.Length; i++)
        {
            if (movementType == MovementType.Slide)
            {
                MovingParts[i].transform.localPosition = targetPositions[i];
            }
            else
            {
                MovingParts[i].transform.localRotation = Quaternion.Euler(targetPositions[i]);
            }
        }
    }

    void Start()
    {
#if UNITY_EDITOR
        PropertyValidator.ValidateProperty(gameObject, SimObjSecondaryProperty.CanOpen);
#endif

        simObjPhysics = GetComponent<SimObjPhysics>();
        rb = GetComponent<Rigidbody>();

        currentOpenness = isOpen ? 1.0f : 0.0f;
    }
    
    void Update()
    {
//#if UNITY_EDITOR
        if (Input.GetKeyDown(KeyCode.Equals))
        {
            Interact();
        }
//#endif
    }

    //处理对象的打开或关闭操作，支持多种参数配置
    public void Interact()
    {
        float targetOpenness = isOpen ? 0.0f : 1.0f; // 如果当前状态是打开的，目标打开程度为 0.0f，否则为 1.0f

        //如果可打开物体是可捡起物体，如尝试打开（书、盒子、笔记本等），在尝试打开或关闭之前，这些物体必须具有刚体 = false，否则它可能会穿过其他物体
        if (simObjPhysics.PrimaryProperty == SimObjPrimaryProperty.CanPickup)
        {
            rb.isKinematic = false;
        }

        StartCoroutine(AnimateOpenClose(targetOpenness));

        // 更新状态
        SetIsOpen(targetOpenness);
    }

    private IEnumerator AnimateOpenClose(float targetOpenness)
    {
        float elapsedTime = 0f;
        float initialOpenness = currentOpenness;
        while (elapsedTime < animationTime)
        {
            elapsedTime += Time.fixedDeltaTime;
            currentOpenness = Mathf.Clamp(Mathf.Lerp(initialOpenness, targetOpenness, elapsedTime / animationTime), 0f, 1f);

            for (int i = 0; i < MovingParts.Length; i++)
            {
                if (movementType == MovementType.Slide)
                {
                    MovingParts[i].transform.localPosition = Vector3.Lerp(closedPositions[i], openPositions[i], currentOpenness);
                }
                else
                {
                    MovingParts[i].transform.localRotation = Quaternion.Lerp(Quaternion.Euler(closedPositions[i]), Quaternion.Euler(openPositions[i]), currentOpenness);
                }
            }
            yield return null;
        }
    }

    //设置对象的打开状态
    private void SetIsOpen(float openness)
    {
        isOpen = openness > 0;
        currentOpenness = openness;
    }

    //同步子对象和父对象的位置和旋转
    public void SyncPosRot(GameObject child, GameObject parent)
    {
        child.transform.SetPositionAndRotation(parent.transform.position, parent.transform.rotation);
    }
}
>>>>>>> 0c14a5c8d787bef23f3133ad2b2203f5035105bb
