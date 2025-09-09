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
    protected float targetHeight; // target height, can be adjusted as needed
    [SerializeField]
    protected float animationDuration; // animation duration, can be adjusted as needed

    [SerializeField]
    public bool isFilling; // whether the object is being filled
    [SerializeField]
    public bool isFilled; // whether the object is filled

    private string currentlyFilledWith;

    private Collider currentLiquidSourceCollider;// Collider representing the liquid provider

    // Liquids dictionary stores the GameObject references for these liquids
    private readonly Dictionary<string, GameObject> liquids = new();
    private Coroutine raiseLiquidCoroutine;
    private Coroutine lowerLiquidCoroutine;
    // These types of objects, their liquids may sink
    private readonly List<SimObjType> canDownObj = new()
    {
        SimObjType.Sink
    };
    private float originHeight;//the initial height of the liquid that can be lowered


    public string FilledLiquid() => currentlyFilledWith;
    public Dictionary<string, GameObject> Liquids = new Dictionary<string, GameObject>();
    public void SaveState(ObjectState objectState)
    {
        objectState.fillState = new FillState
        {
            isFilled = isFilled,
            filledObj = liquids.Values.Select(liquid => liquid.activeSelf).ToArray(), // save the activation state of each liquid object
            fillObjHeight=liquids.Values.Select(liquid => liquid.transform.localPosition.y).ToArray() // save the height of each liquid object
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

        // ensure the array length matches
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

        // restore the activation state of the filling object
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
            originHeight = WaterObject.transform.localPosition.y; // assume the initial height of the water object
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

    //process the filling of the liquid
    private void ProcessLiquidFilling()
    {
        if (currentLiquidSourceCollider != null)
        {
            GameObject currentLiquidSource = currentLiquidSourceCollider.transform.parent.gameObject;
            if (currentLiquidSource.activeSelf)
            {
                if (!isFilling)
                {
                    //print("Start filling");
                    FillObject(currentLiquidSource.name);
                }
            }
            else
            {
                if (canDownObj.Contains(GetComponent<SimObjPhysics>().Type) && isFilling)
                {
                    //print("Start lowering");
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
        // check if the angle difference between the local y-axis rotation of the object and the world y-axis is more than 90 degrees
        return Vector3.Angle(transform.up, Vector3.up) > 90 && isFilled;
    }

    //raise the liquid level
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
    
    //lower the liquid level
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

    //raise the liquid level animation
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


    //lower the liquid level animation
    private IEnumerator LowerLiquidLevel(GameObject liquidObject)
    {
        StopCoroutineIfNotNull(ref raiseLiquidCoroutine);
        isFilling = false;
        Vector3 originalPosition = liquidObject.transform.localPosition;
        Vector3 targetPosition = new(originalPosition.x, originHeight, originalPosition.z); // assume the liquid level is lowered to y=0
        yield return AnimateLiquidPosition(liquidObject, originalPosition, targetPosition);
        isFilled = false;
        liquidObject.SetActive(false);

    }

    //specific animation effect
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
        liquidObject.transform.localPosition = targetPosition; // ensure the final position
    }

    public void StopRaisingLiquid()
    {
        if (raiseLiquidCoroutine != null)
        {
            print("Stop filling");
            StopCoroutine(raiseLiquidCoroutine);
                raiseLiquidCoroutine = null; // clear the coroutine reference
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

    //stop non-empty coroutines
    private void StopCoroutineIfNotNull(ref Coroutine coroutine)
    {
        if (coroutine != null)
        {
            StopCoroutine(coroutine);
            coroutine = null; // clear the coroutine reference
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
            currentLiquidSourceCollider = null; // clear the collider record of the heat source
        }
    }
}
