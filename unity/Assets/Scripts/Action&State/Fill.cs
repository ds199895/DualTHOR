using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using Random = UnityEngine.Random;
using DG.Tweening;

//用于管理游戏对象的填充状态
public class Fill : MonoBehaviour, IUniqueStateManager
{
    [SerializeField]
    protected GameObject WaterObject;
    [SerializeField]
    protected GameObject CoffeeObject;
    [SerializeField]
    protected GameObject WineObject;
    
    [SerializeField]
    protected float targetHeight; // 目标高度，可以根据需要调整
    [SerializeField]
    protected float animationDuration; // 动画持续时间，可以根据需要调整

    [SerializeField]
    public bool isFilling; //是否正在填充
    [SerializeField]
    public bool isFilled; //是否已经填满

    private string currentlyFilledWith;

    private Collider currentLiquidSourceCollider;//表示液体提供者的碰撞器

    // Liquids 字典存储这些液体的 GameObject 引用
    private readonly Dictionary<string, GameObject> liquids = new();
    private Coroutine raiseLiquidCoroutine;
    private Coroutine lowerLiquidCoroutine;
    //这些类型的物体，里面的液体可能会下降
    private readonly List<SimObjType> canDownObj = new()
    {
        SimObjType.Sink
    };
    private float originHeight;//可下降液体的初始高度


    public string FilledLiquid() => currentlyFilledWith;
    public Dictionary<string, GameObject> Liquids = new Dictionary<string, GameObject>();
    public void SaveState(ObjectState objectState)
    {
        objectState.fillState = new FillState
        {
            isFilled = isFilled,
            filledObj = liquids.Values.Select(liquid => liquid.activeSelf).ToArray(), // 保存每个液体对象的激活状态
            fillObjHeight=liquids.Values.Select(liquid => liquid.transform.localPosition.y).ToArray() // 保存每个液体对象的高度
        };
    }

    public void LoadState(ObjectState objectState)
    {
        if (objectState.fillState == null)
        {
            Debug.LogWarning("Fill state is null, skipping load");
            return;
        }

        isFilled = objectState.fillState.isFilled;

        // 确保数组长度匹配
        if (objectState.fillState.filledObj == null || objectState.fillState.fillObjHeight == null)
        {
            Debug.LogWarning("Fill state arrays are null, skipping load");
            return;
        }

        if (objectState.fillState.filledObj.Length != liquids.Count || 
            objectState.fillState.fillObjHeight.Length != liquids.Count)
        {
            Debug.LogWarning($"GameObject:{gameObject.name} Array length mismatch. filledObj: {objectState.fillState.filledObj?.Length}, " +
                           $"fillObjHeight: {objectState.fillState.fillObjHeight?.Length}, " +
                           $"liquids count: {liquids.Count}");
            return;
        }

        // 恢复填充物体的激活状态
        for (int i = 0; i < liquids.Count; i++)
        {
            if (i < objectState.fillState.filledObj.Length && i < objectState.fillState.fillObjHeight.Length)
            {
                liquids.Values.ElementAt(i).SetActive(objectState.fillState.filledObj[i]);
                liquids.Values.ElementAt(i).transform.localPosition = new Vector3(0, objectState.fillState.fillObjHeight[i], 0);
            }
        }
    }

    private void Start()
    {
#if UNITY_EDITOR
        PropertyValidator.ValidateProperty(gameObject, SimObjSecondaryProperty.CanBeFilled);
#endif
       
        liquids.Add("Water", WaterObject);
        liquids.Add("Coffee", CoffeeObject);
        liquids.Add("Wine", WineObject);

        if (canDownObj.Contains(GetComponent<SimObjPhysics>().Type))
        {
            originHeight = WaterObject.transform.localPosition.y; // 假设水对象的初始高度
        }
    }

    private void Update()
    {
        if (IsObjectTilted())
        {
            EmptyObject();
        }
        ProcessLiquidFilling();
    }

    //处理液体的填充
    private void ProcessLiquidFilling()
    {
        if (currentLiquidSourceCollider != null)
        {
            GameObject currentLiquidSource = currentLiquidSourceCollider.transform.parent.gameObject;
            if (currentLiquidSource.activeSelf)
            {
                if (!isFilling)
                {
                    //print("开始填充");
                    FillObject(currentLiquidSource.name);
                }
            }
            else
            {
                if (canDownObj.Contains(GetComponent<SimObjPhysics>().Type) && isFilling)
                {
                    //print("开始下降");
                    DownObject(currentLiquidSourceCollider.transform.parent.gameObject.name);
                }
                else
                {
                    StopRaisingLiquid();
                }
            }
        }

    }
    private bool IsObjectTilted()
    {
        // 检查对象局部y轴的旋转角度跟世界y轴的角度差是否超过 90 度
        return Vector3.Angle(transform.up, Vector3.up) > 90 && isFilled;
    }

    //液面上升
    public void FillObject(string liquidType)
    {
        if (!liquids.TryGetValue(liquidType, out GameObject liquidObject))
        {
            throw new ArgumentException("Unknown liquid: " + liquidType);
        }

        if (liquidObject == null)
        {
            throw new ArgumentException($"The liquid {liquidType} is not setup for this object.");
        }

        liquidObject.SetActive(true);
        raiseLiquidCoroutine = StartCoroutine(RaiseLiquidLevel(liquidObject, liquidType));
    }
    
    //液面下降
    public void DownObject(string liquidType)
    {
        if (!liquids.TryGetValue(liquidType, out GameObject liquidObject))
        {
            throw new ArgumentException("Unknown liquid: " + liquidType);
        }

        if (liquidObject == null)
        {
            throw new ArgumentException($"The liquid {liquidType} is not setup for this object.");
        }

        lowerLiquidCoroutine = StartCoroutine(LowerLiquidLevel(liquidObject));
    }

    //上升动画
    private IEnumerator RaiseLiquidLevel(GameObject liquidObject, string liquidType)
    {
        StopCoroutineIfNotNull(ref lowerLiquidCoroutine);
        isFilling = true;
        yield return new WaitForSeconds(1f);
        Vector3 originalPosition = liquidObject.transform.localPosition;
        Vector3 targetPosition = new(originalPosition.x, targetHeight, originalPosition.z);
        yield return AnimateLiquidPosition(liquidObject, originalPosition, targetPosition);
        isFilled = true;
        currentlyFilledWith = liquidType;
    }


    //下降动画
    private IEnumerator LowerLiquidLevel(GameObject liquidObject)
    {
        StopCoroutineIfNotNull(ref raiseLiquidCoroutine);
        isFilling = false;
        Vector3 originalPosition = liquidObject.transform.localPosition;
        Vector3 targetPosition = new(originalPosition.x, originHeight, originalPosition.z); // 假设水位下降到y=0
        yield return AnimateLiquidPosition(liquidObject, originalPosition, targetPosition);
        isFilled = false;
        liquidObject.SetActive(false);

    }

    //具体动画效果
    private IEnumerator AnimateLiquidPosition(GameObject liquidObject, Vector3 originalPosition, Vector3 targetPosition)
    {
        float elapsedTime = 0f;
        while (elapsedTime < animationDuration)
        {
            elapsedTime += Time.deltaTime;
            float t = elapsedTime / animationDuration;
            liquidObject.transform.localPosition = Vector3.Lerp(originalPosition, targetPosition, t);
            yield return null;
        }
        liquidObject.transform.localPosition = targetPosition; // 确保最终位置
    }

    public void StopRaisingLiquid()
    {
        if (raiseLiquidCoroutine != null)
        {
            print("停止填充");
            StopCoroutine(raiseLiquidCoroutine);
            raiseLiquidCoroutine = null; // 清空协程引用
            isFilling = false;
        }

    }

    public void EmptyObject()
    {
        foreach (GameObject liquid in liquids.Values)
        {
            liquid?.SetActive(false);
        }
        currentlyFilledWith = null;
        isFilled = false;
    }

    //停止非空的协程
    private void StopCoroutineIfNotNull(ref Coroutine coroutine)
    {
        if (coroutine != null)
        {
            StopCoroutine(coroutine);
            coroutine = null; // 清空协程引用
        }
    }

    private void OnTriggerEnter(Collider other)
    {
        if (other.CompareTag("Liquid"))
        {
            currentLiquidSourceCollider = other;
        }
    }

    private void OnTriggerExit(Collider other)
    {
        if (other == currentLiquidSourceCollider)
        {
            currentLiquidSourceCollider = null; // 清空热源的Collider记录
        }
    }
}
