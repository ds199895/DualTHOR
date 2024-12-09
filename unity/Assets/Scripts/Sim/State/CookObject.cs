using System.Collections.Generic;
using UnityEngine;

//将物体放到热源附近，等待温度超过阈值，即可发生cook状态变化
public class CookObject : MonoBehaviour, IUniqueStateManager
{
    [Header("Objects that need Mat Swaps")]
    [SerializeField]
    private SwapObjList[] materialSwapObjects;
    [SerializeField]
    private float roomTemperature = 24f;
    [SerializeField]
    private float thresholdTemperature = 100f; // 自定义温度阈值
    [SerializeField]
    private float coolingRate = 1f; // 温度下降速率
    [SerializeField]
    private float currentTemperature;
    [SerializeField]
    private bool isCooked = false;
    private Collider currentHeatSourceCollider;
    public bool IsCooked => isCooked;

    public void SaveState(ObjectState objectState)
    {
        objectState.cookState = new CookState
        {
            isCooked = isCooked,
            temperature = currentTemperature,
            materials = new List<Material>(materialSwapObjects[0].MyObject.GetComponent<MeshRenderer>().materials)
        };
    }

    public void LoadState(ObjectState objectState)
    {
        isCooked = objectState.cookState.isCooked;
        currentTemperature = objectState.cookState.temperature;
        MeshRenderer meshRenderer = materialSwapObjects[0].MyObject.GetComponent<MeshRenderer>();
        meshRenderer.materials = objectState.cookState.materials.ToArray();
    }

    void Start()
    {
#if UNITY_EDITOR
        PropertyValidator.ValidateProperty(gameObject, SimObjSecondaryProperty.CanBeCooked);
#endif
        currentTemperature =roomTemperature;
    }

    void Update()
    {
#if UNITY_EDITOR
        if (Input.GetKeyDown(KeyCode.F))
        {
            Cook();
        }
#endif

        UpdateTemperature();
    }
    private void UpdateTemperature()
    {
        if (IsNearHeatSource())
        {
            currentTemperature += Time.deltaTime; // 温度随时间缓慢上升
            if (currentTemperature > thresholdTemperature&&!isCooked)
            {
                Cook(); // 调用烹饪函数以改变材质
            }
        }
        else
        {
            currentTemperature = Mathf.Max(currentTemperature - coolingRate * Time.deltaTime, roomTemperature);
        }
    }

    public void Cook()
    {
        //print("Cooking");
        if (materialSwapObjects.Length > 0)
        {
            MeshRenderer meshRenderer = materialSwapObjects[0].MyObject.GetComponent<MeshRenderer>();
            foreach (var swapObj in materialSwapObjects)
            {
                meshRenderer.materials = swapObj.OnMaterials;
            }
            isCooked = true;
        }
    }

    //GasBurnerFlames子物体的触发器和物体本身的active状态
    private bool IsNearHeatSource()
    {
        return currentHeatSourceCollider != null && currentHeatSourceCollider.transform.parent.gameObject.activeSelf;
    }


    private void OnTriggerEnter(Collider other)
    {
        if (other.CompareTag("HeatZone"))
        {
            currentHeatSourceCollider = other;
        }
    }

    private void OnTriggerExit(Collider other)
    {
        if (other == currentHeatSourceCollider)
        {
            currentHeatSourceCollider = null; // 清空热源的Collider记录
        }
    }

}
