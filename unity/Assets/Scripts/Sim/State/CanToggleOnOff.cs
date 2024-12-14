<<<<<<< HEAD
﻿using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class CanToggleOnOff : MonoBehaviour,IUniqueStateManager
{
    [Header("Moving Parts")]
    [SerializeField]
    private GameObject[] movingParts;//可移动部件的数组，表示对象的可开关部分

    [Header("Objects that need Mat Swaps")]
    [SerializeField]
    private SwapObjList[] materialSwapObjects;//需要切换材质的对象列表，每个对象包含 OnMaterials 和 OffMaterials 两个材质数组。

    [Header("Light Source to Enable or Disable")]
    [SerializeField]
    private Light[] lightSources;//灯光源的数组，表示对象的灯光部分。

    //常用于液体效果，如水龙头打开出现水，efffets[0]表示液体水平面效果，effects[1]表示水龙头流出水柱效果
    [Header("Effects/Objects to Enable or Disable")]
    [SerializeField]
    private GameObject[] effects;//效果和对象的数组，表示对象的其他可开关部分。

    [Header("Animation Parameters")]
    [SerializeField]
    private Vector3[] onPositions;//分别表示打开和关闭状态下的位置或旋转。
    [SerializeField]
    private Vector3[] offPositions;
    [SerializeField]
    private float animationTime;

    [SerializeField]
    private bool isOn; //表示对象的开关状态。



    private enum MovementType
    {
        Slide,
        Rotate
    };

    [SerializeField]
    private MovementType movementType;

    [SerializeField]
    private bool selfControlled = true;//表示对象的开关状态是否由自身控制。

    [SerializeField]
    private SimObjPhysics[] controlledSimObjects;//由该对象控制的 SimObjPhysics 对象数组。

    public bool IsOn => isOn;

    public void SaveState(ObjectState objectState)
    {
        objectState.toggleState = new ToggleState
        {
            isOn = isOn,
            materials = new List<Material>(), // 初始化材质列表
            movingParts = movingParts,
            lights = lightSources,
            effects = effects
        };
        // 获取所有材质并添加到列表中
        foreach (var swapObj in materialSwapObjects)
        {
            objectState.toggleState.materials.AddRange(isOn ? swapObj.OnMaterials : swapObj.OffMaterials);
        }
    }

    public void LoadState(ObjectState objectState)
    {
        isOn = objectState.toggleState.isOn;
        // 恢复材质
        foreach (var swapObj in materialSwapObjects)
        {
            swapObj.MyObject.GetComponent<MeshRenderer>().materials = isOn ? swapObj.OnMaterials : swapObj.OffMaterials;
        }
        // 恢复位置和旋转
        UpdateMovingParts();
        // 根据开关状态恢复灯光和效果
        UpdateLightAndEffects();
    }
    private void UpdateMovingParts()
    {
        Vector3[] targetPositions = isOn ? onPositions : offPositions;
        for (int i = 0; i < movingParts.Length; i++)
        {
            if (movementType == MovementType.Slide)
            {
                movingParts[i].transform.localPosition = targetPositions[i];
            }
            else if (movementType == MovementType.Rotate)
            {
                movingParts[i].transform.localRotation = Quaternion.Euler(targetPositions[i]);
            }
        }
    }

    private void UpdateLightAndEffects()
    {
        foreach (Light light in lightSources)
        {
            light.transform.gameObject.SetActive(isOn);
        }
        foreach (GameObject effect in effects)
        {
            effect.SetActive(isOn);
        }
    }

    public bool ReturnSelfControlled() => selfControlled;

    public SimObjPhysics[] ReturnControlledSimObjects() => controlledSimObjects;

    public bool IsTurnedOnOrOff() => isOn;

    // Use this for initialization
    void Start()
    {
#if UNITY_EDITOR
        PropertyValidator.ValidateProperty(gameObject, SimObjSecondaryProperty.CanToggleOnOff);
#endif
    }
   
    void Update()
    {
//#if UNITY_EDITOR
        if (Input.GetKeyDown(KeyCode.Minus))
        {
            Toggle();
        }
//#endif
    }

    //处理对象的开关操作，支持多种参数配置。
    public void Toggle()
    {
        if (!selfControlled)
        {
            return;
        }

        // 执行动画
        if (movingParts.Length > 0)
        {
            for (int i = 0; i < movingParts.Length; i++)
            {
                int initialOpenness = isOn ? 1 : 0;
                int desiredOpenness = isOn ? 0 : 1;

                if (movementType == MovementType.Slide)
                {
                    StartCoroutine(LerpPosition(movingParts, offPositions, onPositions, initialOpenness, desiredOpenness, animationTime));
                }
                else if (movementType == MovementType.Rotate)
                {
                    StartCoroutine(LerpRotation(movingParts, offPositions, onPositions, initialOpenness, desiredOpenness, animationTime));
                }
            }
        }
        SetIsOn();

    }

    private void SetIsOn()
    {
        isOn = !isOn; // 反转 isOn 的状态
        SetComponentsActive(isOn);
    }

    // 设置组件的激活状态
    private void SetComponentsActive(bool isActive)
    {
        foreach (Light light in lightSources)
        {
            light.gameObject.SetActive(isActive); // 设置灯光状态
        }

        foreach (GameObject effect in effects)
        {
            effect.SetActive(isActive); // 设置效果状态
        }

        foreach (SwapObjList swapObj in materialSwapObjects)
        {
            swapObj.MyObject.GetComponent<MeshRenderer>().materials = isActive ? swapObj.OnMaterials : swapObj.OffMaterials; // 设置材质
        }

        foreach (SimObjPhysics sop in controlledSimObjects)
        {
            sop.GetComponent<CanToggleOnOff>().isOn = isActive; // 设置受控对象状态
        }
    }
    
    // 平滑移动动画
    private  IEnumerator LerpPosition(GameObject[] movingParts, Vector3[] offLocalPositions, Vector3[] onLocalPositions, float initialOpenness, float desiredOpenness, float animationTime)
    {
        float elapsedTime = 0f;
        while (elapsedTime < animationTime)
        {
            elapsedTime += Time.deltaTime;
            float currentOpenness = Mathf.Clamp(initialOpenness + (desiredOpenness - initialOpenness) * (elapsedTime / animationTime), Mathf.Min(initialOpenness, desiredOpenness), Mathf.Max(initialOpenness, desiredOpenness));
            for (int i = 0; i < movingParts.Length; i++)
            {
                movingParts[i].transform.localPosition = Vector3.Lerp(offLocalPositions[i], onLocalPositions[i], currentOpenness);
            }
            yield return null; // 等待下一帧
        }
    }

    // 平滑旋转动画
    private  IEnumerator LerpRotation(GameObject[] movingParts, Vector3[] offLocalRotations, Vector3[] onLocalRotations, float initialOpenness, float desiredOpenness, float animationTime)
    {
        float elapsedTime = 0f;
        while (elapsedTime < animationTime)
        {
            elapsedTime += Time.fixedDeltaTime;
            float currentOpenness = Mathf.Clamp(initialOpenness + (desiredOpenness - initialOpenness) * (elapsedTime / animationTime), Mathf.Min(initialOpenness, desiredOpenness), Mathf.Max(initialOpenness, desiredOpenness));
            for (int i = 0; i < movingParts.Length; i++)
            {
                movingParts[i].transform.localRotation = Quaternion.Lerp(Quaternion.Euler(offLocalRotations[i]), Quaternion.Euler(onLocalRotations[i]), currentOpenness);
            }
            yield return null; // 等待下一帧
        }
    }

}
=======
﻿using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class CanToggleOnOff : MonoBehaviour,IUniqueStateManager
{
    [Header("Moving Parts")]
    [SerializeField]
    private GameObject[] movingParts;//可移动部件的数组，表示对象的可开关部分

    [Header("Objects that need Mat Swaps")]
    [SerializeField]
    private SwapObjList[] materialSwapObjects;//需要切换材质的对象列表，每个对象包含 OnMaterials 和 OffMaterials 两个材质数组。

    [Header("Light Source to Enable or Disable")]
    [SerializeField]
    private Light[] lightSources;//灯光源的数组，表示对象的灯光部分。

    //常用于液体效果，如水龙头打开出现水，efffets[0]表示液体水平面效果，effects[1]表示水龙头流出水柱效果
    [Header("Effects/Objects to Enable or Disable")]
    [SerializeField]
    private GameObject[] effects;//效果和对象的数组，表示对象的其他可开关部分。

    [Header("Animation Parameters")]
    [SerializeField]
    private Vector3[] onPositions;//分别表示打开和关闭状态下的位置或旋转。
    [SerializeField]
    private Vector3[] offPositions;
    [SerializeField]
    private float animationTime;

    [SerializeField]
    private bool isOn; //表示对象的开关状态。



    private enum MovementType
    {
        Slide,
        Rotate
    };

    [SerializeField]
    private MovementType movementType;

    [SerializeField]
    private bool selfControlled = true;//表示对象的开关状态是否由自身控制。

    [SerializeField]
    private SimObjPhysics[] controlledSimObjects;//由该对象控制的 SimObjPhysics 对象数组。

    public bool IsOn => isOn;

    public void SaveState(ObjectState objectState)
    {
        objectState.toggleState = new ToggleState
        {
            isOn = isOn,
            materials = new List<Material>(), // 初始化材质列表
            movingParts = movingParts,
            lights = lightSources,
            effects = effects
        };
        // 获取所有材质并添加到列表中
        foreach (var swapObj in materialSwapObjects)
        {
            objectState.toggleState.materials.AddRange(isOn ? swapObj.OnMaterials : swapObj.OffMaterials);
        }
    }

    public void LoadState(ObjectState objectState)
    {
        isOn = objectState.toggleState.isOn;
        // 恢复材质
        foreach (var swapObj in materialSwapObjects)
        {
            swapObj.MyObject.GetComponent<MeshRenderer>().materials = isOn ? swapObj.OnMaterials : swapObj.OffMaterials;
        }
        // 恢复位置和旋转
        UpdateMovingParts();
        // 根据开关状态恢复灯光和效果
        UpdateLightAndEffects();
    }
    private void UpdateMovingParts()
    {
        Vector3[] targetPositions = isOn ? onPositions : offPositions;
        for (int i = 0; i < movingParts.Length; i++)
        {
            if (movementType == MovementType.Slide)
            {
                movingParts[i].transform.localPosition = targetPositions[i];
            }
            else if (movementType == MovementType.Rotate)
            {
                movingParts[i].transform.localRotation = Quaternion.Euler(targetPositions[i]);
            }
        }
    }

    private void UpdateLightAndEffects()
    {
        foreach (Light light in lightSources)
        {
            light.transform.gameObject.SetActive(isOn);
        }
        foreach (GameObject effect in effects)
        {
            effect.SetActive(isOn);
        }
    }

    public bool ReturnSelfControlled() => selfControlled;

    public SimObjPhysics[] ReturnControlledSimObjects() => controlledSimObjects;

    public bool IsTurnedOnOrOff() => isOn;

    // Use this for initialization
    void Start()
    {
#if UNITY_EDITOR
        PropertyValidator.ValidateProperty(gameObject, SimObjSecondaryProperty.CanToggleOnOff);
#endif
    }
   
    void Update()
    {
//#if UNITY_EDITOR
        if (Input.GetKeyDown(KeyCode.Minus))
        {
            Toggle();
        }
//#endif
    }

    //处理对象的开关操作，支持多种参数配置。
    public void Toggle()
    {
        if (!selfControlled)
        {
            return;
        }

        // 执行动画
        if (movingParts.Length > 0)
        {
            for (int i = 0; i < movingParts.Length; i++)
            {
                int initialOpenness = isOn ? 1 : 0;
                int desiredOpenness = isOn ? 0 : 1;

                if (movementType == MovementType.Slide)
                {
                    StartCoroutine(LerpPosition(movingParts, offPositions, onPositions, initialOpenness, desiredOpenness, animationTime));
                }
                else if (movementType == MovementType.Rotate)
                {
                    StartCoroutine(LerpRotation(movingParts, offPositions, onPositions, initialOpenness, desiredOpenness, animationTime));
                }
            }
        }
        SetIsOn();

    }

    private void SetIsOn()
    {
        isOn = !isOn; // 反转 isOn 的状态
        SetComponentsActive(isOn);
    }

    // 设置组件的激活状态
    private void SetComponentsActive(bool isActive)
    {
        foreach (Light light in lightSources)
        {
            light.gameObject.SetActive(isActive); // 设置灯光状态
        }

        foreach (GameObject effect in effects)
        {
            effect.SetActive(isActive); // 设置效果状态
        }

        foreach (SwapObjList swapObj in materialSwapObjects)
        {
            swapObj.MyObject.GetComponent<MeshRenderer>().materials = isActive ? swapObj.OnMaterials : swapObj.OffMaterials; // 设置材质
        }

        foreach (SimObjPhysics sop in controlledSimObjects)
        {
            sop.GetComponent<CanToggleOnOff>().isOn = isActive; // 设置受控对象状态
        }
    }
    
    // 平滑移动动画
    private  IEnumerator LerpPosition(GameObject[] movingParts, Vector3[] offLocalPositions, Vector3[] onLocalPositions, float initialOpenness, float desiredOpenness, float animationTime)
    {
        float elapsedTime = 0f;
        while (elapsedTime < animationTime)
        {
            elapsedTime += Time.deltaTime;
            float currentOpenness = Mathf.Clamp(initialOpenness + (desiredOpenness - initialOpenness) * (elapsedTime / animationTime), Mathf.Min(initialOpenness, desiredOpenness), Mathf.Max(initialOpenness, desiredOpenness));
            for (int i = 0; i < movingParts.Length; i++)
            {
                movingParts[i].transform.localPosition = Vector3.Lerp(offLocalPositions[i], onLocalPositions[i], currentOpenness);
            }
            yield return null; // 等待下一帧
        }
    }

    // 平滑旋转动画
    private  IEnumerator LerpRotation(GameObject[] movingParts, Vector3[] offLocalRotations, Vector3[] onLocalRotations, float initialOpenness, float desiredOpenness, float animationTime)
    {
        float elapsedTime = 0f;
        while (elapsedTime < animationTime)
        {
            elapsedTime += Time.fixedDeltaTime;
            float currentOpenness = Mathf.Clamp(initialOpenness + (desiredOpenness - initialOpenness) * (elapsedTime / animationTime), Mathf.Min(initialOpenness, desiredOpenness), Mathf.Max(initialOpenness, desiredOpenness));
            for (int i = 0; i < movingParts.Length; i++)
            {
                movingParts[i].transform.localRotation = Quaternion.Lerp(Quaternion.Euler(offLocalRotations[i]), Quaternion.Euler(onLocalRotations[i]), currentOpenness);
            }
            yield return null; // 等待下一帧
        }
    }

}
>>>>>>> 0c14a5c8d787bef23f3133ad2b2203f5035105bb
