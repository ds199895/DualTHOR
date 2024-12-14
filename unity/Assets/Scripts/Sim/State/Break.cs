<<<<<<< HEAD
using System.Collections.Generic;
using UnityEngine;

public class Break : MonoBehaviour, IUniqueStateManager
{
    [SerializeField]
    private string prefabToSwapTo; // ÆÆËéºóÌæ»»µÄÔ¤ÖÆÌå
    [SerializeField]
    private float mass;
    [SerializeField]
    private float impulseThreshold; // ÆÆËéÐèÒªµÄãÐÖµ
    [SerializeField]
    private float squeezeThreshold; // ÄóµÄÁ¦¶ÈãÐÖµ
    [SerializeField]
    private float highFrictionImpulseOffset = 2.0f;// ¸ßÄ¦²ÁÇøÓòÏÂµÄ³å»÷Á¦ãÐÖµÆ«ÒÆ
    private float minImpulseThreshold; // ×îÐ¡³å»÷Á¦ãÐÖµ
    private float maxImpulseThreshold; // ×î´ó³å»÷Á¦ãÐÖµ
    [SerializeField]
    private float fallImpactForce ; // ÏÂÂäÊ±ÊÜµ½µÄ³å»÷Á¦
    [SerializeField]
    private float currentSqueezeForce; // µ±Ç°ÄóµÄÁ¦¶È

    private SceneManager sceneManager;

    [SerializeField]
    private bool broken; // ÊÇ·ñÒÑ¾­ÆÆËé
    [SerializeField]
    private bool isUnbreakable; // ÊÇ·ñ²»¿ÉÆÆËé
    [SerializeField]
    private bool isReadyToBreak = true; // ÊÇ·ñ×¼±¸ºÃÆÆËé

    // ²»»áµ¼ÖÂÆäËûÎïÌåÆÆËéµÄÎïÌåÀàÐÍÁÐ±í
    //todo£ººóÐø»¹ÐèÒª¾«¼òÀïÃæµÄÎïÌåÀàÐÍ
    private static readonly HashSet<SimObjType> TooSmallOrSoftToBreakOtherObjects = new()
    {
        SimObjType.TeddyBear,
        SimObjType.Pillow,
        SimObjType.Cloth,
        SimObjType.Bed,
        SimObjType.Bread,
        SimObjType.BreadSliced,
        SimObjType.Egg,
        SimObjType.EggShell,
        SimObjType.Omelette,
        SimObjType.EggCracked,
        SimObjType.LettuceSliced,
        SimObjType.TissueBox,
        SimObjType.Newspaper,
        SimObjType.TissueBoxEmpty,
        SimObjType.CreditCard,
        SimObjType.ToiletPaper,
        SimObjType.ToiletPaperRoll,
        SimObjType.SoapBar,
        SimObjType.Pen,
        SimObjType.Pencil,
        SimObjType.Towel,
        SimObjType.Watch,
        SimObjType.DishSponge,
        SimObjType.Tissue,
        SimObjType.CD,
        SimObjType.HandTowel
    };

    public void SaveState(ObjectState objectState)
    {
        objectState.breakState = new BreakState
        {
            isReadyToBreak = isReadyToBreak,
            broken = broken,
            isUnbreakable = isUnbreakable,
        };
    }

    public void LoadState(ObjectState objectState)
    {
        isReadyToBreak = objectState.breakState.isReadyToBreak;
        broken = objectState.breakState.broken;
        isUnbreakable = objectState.breakState.isUnbreakable;
    }


    public bool Broken => broken;

    private void Start()
    {
#if UNITY_EDITOR
        PropertyValidator.ValidateProperty(gameObject, SimObjSecondaryProperty.CanBreak);
#endif

        minImpulseThreshold = impulseThreshold;
        maxImpulseThreshold = impulseThreshold + highFrictionImpulseOffset;

        if (!TryGetComponent<Rigidbody>(out var rb)) return;
        
        // ¼ÆËã³å»÷Á¦
        fallImpactForce = CalculateFallImpactForce(rb);
        mass= rb.mass;
        sceneManager = GameObject.Find("SceneManager").GetComponent<SceneManager>();
    }

    //private void Update()
    //{
    //    // ´¦ÀíÄóµÄÊäÈë£¬¼Ù¶¨Ê¹ÓÃInput.GetButton()·½·¨À´Ä£ÄâÄóµÄÁ¦¶È¡£
    //    // ÕâÀïÐèÒª¸ù¾ÝÊµ¼ÊÊäÈëÏµÍ³½øÐÐµ÷Õû
    //    if (Input.GetKeyDown(KeyCode.B))
    //    {
    //        // ¼ÙÉèÍ¨¹ýÄ³ÖÖ·½Ê½»ñÈ¡ÄóµÄÁ¦¶È£¬ÕâÀïÊ¹ÓÃ0-1·¶Î§µÄÖµ×÷ÎªÊ¾Àý
    //        float squeezeValue = GetSqueezeInput(); // ÄãÐèÒªÊµÏÖÕâ¸ö·½·¨
    //        //print(squeezeValue);
    //        // ¸ù¾ÝÄóµÄÁ¦¶ÈÉèÖÃµ±Ç°ÄóÁ¦
    //        currentSqueezeForce = squeezeValue * 10.0f; // ¿É¸ù¾ÝÐèÒªµ÷ÕûËõ·ÅÒò×Ó
    //        if(currentSqueezeForce > squeezeThreshold)
    //        {
    //            if (isReadyToBreak)
    //            {
    //                isReadyToBreak = false;
    //                BreakObject();
    //            }
    //        }
    //    }
    //    //else
    //    //{
    //    //    currentSqueezeForce = 0;
    //    //}
    //}

    public float CalculateFallImpactForce(Rigidbody rb)
    {
        float gravity = Physics.gravity.y; // »ñÈ¡ÖØÁ¦¼ÓËÙ¶È (Í¨³£Îª -9.81)
        float height = transform.position.y / transform.parent.parent.localScale.y; // »¹Ô­¸ß¶È
        // ¸ù¾Ý¸ß¶ÈºÍÖØÁ¦¼ÆËã×îÖÕËÙ¶È
        float finalVelocity = Mathf.Sqrt(-2 * gravity * height); // ×¢ÒâÖØÁ¦ÊÇ¸ºÖµ£¬ËùÒÔÐèÒªÈ¡¸ººÅ
        // ¼ÆËã³å»÷Á¦
        return (rb.mass * finalVelocity) / 0.1f; // ±£³ÖÔ­À´µÄµ÷ÕûÒò×Ó
    }
    public float CalculateFallImpactForce(Rigidbody rb, Collider collider)
    {
        float gravity = Physics.gravity.y; // »ñÈ¡ÖØÁ¦¼ÓËÙ¶È (Í¨³£Îª -9.81)
        float heightDifference = (transform.position.y - collider.bounds.max.y)/ transform.parent.parent.localScale.y; // »ñÈ¡Åö×²ÌåÏÂ±ß½çµÄyÖµ
        // ¸ù¾Ý¸ß¶ÈºÍÖØÁ¦¼ÆËã×îÖÕËÙ¶È
        float finalVelocity = Mathf.Sqrt(-2 * gravity * heightDifference);
        // ¼ÆËã³å»÷Á¦
        return (rb.mass * finalVelocity) / 0.1f; // ±£³ÖÔ­À´µÄµ÷ÕûÒò×Ó
    }
    public float CalculateCollisionImpactForce(Collision col)
    {
        // »ñÈ¡Åö×²µÄÏà¶ÔËÙ¶È
        Vector3 collisionVelocity = col.relativeVelocity;

        // ¼ÆËã³å»÷Á¦£¬Ê¹ÓÃÎïÌåµÄÖÊÁ¿ºÍÅö×²Ê±µÄËÙ¶È
        float impactForce =mass * collisionVelocity.magnitude; // Ê¹ÓÃËÙ¶ÈµÄ´óÐ¡

        return impactForce; // ·µ»Ø¼ÆËã³öµÄ³å»÷Á¦
    }

    private void OnCollisionEnter(Collision col)
    {
        //col.impulse.magnitudeµ¥Î»ÎªÅ£¶Ù/Ãë£¬N/s£¬±íÊ¾Ò»Ãë¸Ä±äÁË¶àÉÙ¶¯Á¿
        float impactForce = CalculateCollisionImpactForce(col);
        //print("impactForce: " + impactForce);
        //float impactForce = CalculateFallImpactForce(GetComponent<Rigidbody>(), col.collider);
        //print("impactForce: " + impactForce);
        if (isUnbreakable || impactForce <= impulseThreshold) return;
        //if (isUnbreakable || fallImpactForce  <= impulseThreshold) return;

        //Èç¹ûÅö×²ÎïÌåÔÚ TooSmalOrSoftToBreakOtherObjects ÁÐ±íÖÐ£¬Ö±½Ó·µ»Ø
        SimObjPhysics collidedObject = col.transform.GetComponentInParent<SimObjPhysics>();
        if (collidedObject != null && TooSmallOrSoftToBreakOtherObjects.Contains(collidedObject.Type))
        {
            return;
        }

        if (isReadyToBreak)
        {
            isReadyToBreak = false;
            BreakObject();
        }
    }

    private void OnCollisionStay(Collision collision)
    {
        if (collision.gameObject.CompareTag("Robot")&& currentSqueezeForce > squeezeThreshold)
        {
            print("OnCollisionStay");

            if (isReadyToBreak)
                {
                    isReadyToBreak = false;
                    BreakObject();
                }
        }
    }
    public void BreakObject()
    {
        Rigidbody rb = gameObject.GetComponent<Rigidbody>();
        //ÊµÀý»¯ÐÂµÄÔ¤ÖÆÌå£¬²¢½«ÆäÉèÖÃÎªµ±Ç°ÎïÌåµÄÎ»ÖÃºÍÐý×ª
        if (!sceneManager.SimObjectsDict.TryGetValue(prefabToSwapTo, out GameObject breakedObject))
        {
            return; // Èç¹ûÎ´ÕÒµ½¶ÔÓ¦µÄ¶ÔÏó£¬Ö±½Ó·µ»Ø
        }
        breakedObject.transform.SetPositionAndRotation(transform.position, transform.rotation);
        breakedObject.SetActive(true);
        Breakdown breakdown = breakedObject.GetComponent<Breakdown>();
        breakdown.StartBreak();
        broken = true;

        //½«ÐÂÎïÌåµÄ¸ÕÌåËÙ¶ÈºÍ½ÇËÙ¶ÈÉèÖÃÎªµ±Ç°ÎïÌåµÄËÙ¶ÈºÍ½ÇËÙ¶È
        //ÎªÊ²Ã´ÉèÎª0.4f£¿
        foreach (Rigidbody subRb in breakedObject.GetComponentsInChildren<Rigidbody>())
        {
            subRb.velocity = rb.velocity * 0.4f;
            subRb.angularVelocity = rb.angularVelocity * 0.4f;
        }
        gameObject.SetActive(false);
    }

    //µ±ÎïÌå½øÈë¸ßÄ¦²ÁÇøÓòÊ±£¬Ôö¼Ó³å»÷Á¦ãÐÖµ
    public void OnTriggerEnter(Collider other)
    {
        if (other.CompareTag("HighFriction"))
        {
            impulseThreshold = maxImpulseThreshold;
            
        }
    }
   
    public void OnTriggerExit(Collider other)
    {
        if (other.CompareTag("HighFriction"))
        {
            impulseThreshold = minImpulseThreshold;

        }
    }

    private float GetSqueezeInput()
    {

        return 1; // Ö»ÊÇÊ¾Àý£¬Ìæ»»ÎªÕæÊµÄóÁ¦¶È»ñÈ¡Âß¼­
    }
}
=======
using System.Collections.Generic;
using UnityEngine;

public class Break : MonoBehaviour, IUniqueStateManager
{
    [SerializeField]
    private string prefabToSwapTo; // ï¿½ï¿½ï¿½ï¿½ï¿½ï¿½æ»»ï¿½ï¿½Ô¤ï¿½ï¿½ï¿½ï¿½
    [SerializeField]
    private float mass;
    [SerializeField]
    private float impulseThreshold; // ï¿½ï¿½ï¿½ï¿½ï¿½ï¿½Òªï¿½ï¿½ï¿½ï¿½Öµ
    [SerializeField]
    private float squeezeThreshold; // ï¿½ï¿½ï¿½ï¿½ï¿½ï¿½ï¿½ï¿½ï¿½Öµ
    [SerializeField]
    private float highFrictionImpulseOffset = 2.0f;// ï¿½ï¿½Ä¦ï¿½ï¿½ï¿½ï¿½ï¿½ï¿½ï¿½ÂµÄ³ï¿½ï¿½ï¿½ï¿½ï¿½ï¿½ÖµÆ«ï¿½ï¿½
    private float minImpulseThreshold; // ï¿½ï¿½Ð¡ï¿½ï¿½ï¿½ï¿½ï¿½ï¿½ï¿½Öµ
    private float maxImpulseThreshold; // ï¿½ï¿½ï¿½ï¿½ï¿½ï¿½ï¿½ï¿½ï¿½Öµ
    [SerializeField]
    private float fallImpactForce ; // ï¿½ï¿½ï¿½ï¿½Ê±ï¿½Üµï¿½ï¿½Ä³ï¿½ï¿½ï¿½ï¿½
    [SerializeField]
    private float currentSqueezeForce; // ï¿½ï¿½Ç°ï¿½ï¿½ï¿½ï¿½ï¿½ï¿½ï¿½

    private SceneManager sceneManager;

    [SerializeField]
    private bool broken; // ï¿½Ç·ï¿½ï¿½Ñ¾ï¿½ï¿½ï¿½ï¿½ï¿½
    [SerializeField]
    private bool isUnbreakable; // ï¿½Ç·ñ²»¿ï¿½ï¿½ï¿½ï¿½ï¿½
    [SerializeField]
    private bool isReadyToBreak = true; // ï¿½Ç·ï¿½×¼ï¿½ï¿½ï¿½ï¿½ï¿½ï¿½ï¿½ï¿½

    // ï¿½ï¿½ï¿½áµ¼ï¿½ï¿½ï¿½ï¿½ï¿½ï¿½ï¿½ï¿½ï¿½ï¿½ï¿½ï¿½ï¿½ï¿½ï¿½ï¿½ï¿½ï¿½ï¿½ï¿½ï¿½ï¿½ï¿½ï¿½Ð±ï¿½
    //todoï¿½ï¿½ï¿½ï¿½ï¿½ï¿½ï¿½ï¿½ï¿½ï¿½Òªï¿½ï¿½ï¿½ï¿½ï¿½ï¿½ï¿½ï¿½ï¿½ï¿½ï¿½ï¿½ï¿½ï¿½ï¿½ï¿½ï¿½
    private static readonly HashSet<SimObjType> TooSmallOrSoftToBreakOtherObjects = new()
    {
        SimObjType.TeddyBear,
        SimObjType.Pillow,
        SimObjType.Cloth,
        SimObjType.Bed,
        SimObjType.Bread,
        SimObjType.BreadSliced,
        SimObjType.Egg,
        SimObjType.EggShell,
        SimObjType.Omelette,
        SimObjType.EggCracked,
        SimObjType.LettuceSliced,
        SimObjType.TissueBox,
        SimObjType.Newspaper,
        SimObjType.TissueBoxEmpty,
        SimObjType.CreditCard,
        SimObjType.ToiletPaper,
        SimObjType.ToiletPaperRoll,
        SimObjType.SoapBar,
        SimObjType.Pen,
        SimObjType.Pencil,
        SimObjType.Towel,
        SimObjType.Watch,
        SimObjType.DishSponge,
        SimObjType.Tissue,
        SimObjType.CD,
        SimObjType.HandTowel
    };

    public void SaveState(ObjectState objectState)
    {
        objectState.breakState = new BreakState
        {
            isReadyToBreak = isReadyToBreak,
            broken = broken,
            isUnbreakable = isUnbreakable,
        };
    }

    public void LoadState(ObjectState objectState)
    {
        isReadyToBreak = objectState.breakState.isReadyToBreak;
        broken = objectState.breakState.broken;
        isUnbreakable = objectState.breakState.isUnbreakable;
    }


    public bool Broken => broken;

    private void Start()
    {
#if UNITY_EDITOR
        PropertyValidator.ValidateProperty(gameObject, SimObjSecondaryProperty.CanBreak);
#endif

        minImpulseThreshold = impulseThreshold;
        maxImpulseThreshold = impulseThreshold + highFrictionImpulseOffset;

        if (!TryGetComponent<Rigidbody>(out var rb)) return;
        
        // ï¿½ï¿½ï¿½ï¿½ï¿½ï¿½ï¿½ï¿½
        fallImpactForce = CalculateFallImpactForce(rb);
        mass= rb.mass;
        sceneManager = GameObject.Find("SceneManager").GetComponent<SceneManager>();
    }

    //private void Update()
    //{
    //    // ï¿½ï¿½ï¿½ï¿½ï¿½ï¿½ï¿½ï¿½ï¿½ï¿½ë£¬ï¿½Ù¶ï¿½Ê¹ï¿½ï¿½Input.GetButton()ï¿½ï¿½ï¿½ï¿½ï¿½ï¿½Ä£ï¿½ï¿½ï¿½ï¿½ï¿½ï¿½ï¿½ï¿½È¡ï¿½
    //    // ï¿½ï¿½ï¿½ï¿½ï¿½ï¿½Òªï¿½ï¿½ï¿½ï¿½Êµï¿½ï¿½ï¿½ï¿½ï¿½ï¿½ÏµÍ³ï¿½ï¿½ï¿½Ðµï¿½ï¿½ï¿½
    //    if (Input.GetKeyDown(KeyCode.B))
    //    {
    //        // ï¿½ï¿½ï¿½ï¿½Í¨ï¿½ï¿½Ä³ï¿½Ö·ï¿½Ê½ï¿½ï¿½È¡ï¿½ï¿½ï¿½ï¿½ï¿½ï¿½È£ï¿½ï¿½ï¿½ï¿½ï¿½Ê¹ï¿½ï¿½0-1ï¿½ï¿½Î§ï¿½ï¿½Öµï¿½ï¿½ÎªÊ¾ï¿½ï¿½
    //        float squeezeValue = GetSqueezeInput(); // ï¿½ï¿½ï¿½ï¿½ÒªÊµï¿½ï¿½ï¿½ï¿½ï¿½ï¿½ï¿½ï¿½ï¿½
    //        //print(squeezeValue);
    //        // ï¿½ï¿½ï¿½ï¿½ï¿½ï¿½ï¿½ï¿½ï¿½ï¿½ï¿½ï¿½ï¿½ï¿½Ãµï¿½Ç°ï¿½ï¿½ï¿½ï¿½
    //        currentSqueezeForce = squeezeValue * 10.0f; // ï¿½É¸ï¿½ï¿½ï¿½ï¿½ï¿½Òªï¿½ï¿½ï¿½ï¿½ï¿½ï¿½ï¿½ï¿½ï¿½ï¿½ï¿½ï¿½
    //        if(currentSqueezeForce > squeezeThreshold)
    //        {
    //            if (isReadyToBreak)
    //            {
    //                isReadyToBreak = false;
    //                BreakObject();
    //            }
    //        }
    //    }
    //    //else
    //    //{
    //    //    currentSqueezeForce = 0;
    //    //}
    //}

    public float CalculateFallImpactForce(Rigidbody rb)
    {
        float gravity = Physics.gravity.y; // ï¿½ï¿½È¡ï¿½ï¿½ï¿½ï¿½ï¿½ï¿½ï¿½Ù¶ï¿½ (Í¨ï¿½ï¿½Îª -9.81)
        float height = transform.position.y / transform.parent.parent.localScale.y; // ï¿½ï¿½Ô­ï¿½ß¶ï¿½
        // ï¿½ï¿½ï¿½Ý¸ß¶Èºï¿½ï¿½ï¿½ï¿½ï¿½ï¿½ï¿½ï¿½ï¿½ï¿½ï¿½ï¿½ï¿½ï¿½Ù¶ï¿½
        float finalVelocity = Mathf.Sqrt(-2 * gravity * height); // ×¢ï¿½ï¿½ï¿½ï¿½ï¿½ï¿½ï¿½Ç¸ï¿½Öµï¿½ï¿½ï¿½ï¿½ï¿½ï¿½ï¿½ï¿½ÒªÈ¡ï¿½ï¿½ï¿½ï¿½
        // ï¿½ï¿½ï¿½ï¿½ï¿½ï¿½ï¿½ï¿½
        return (rb.mass * finalVelocity) / 0.1f; // ï¿½ï¿½ï¿½ï¿½Ô­ï¿½ï¿½ï¿½Äµï¿½ï¿½ï¿½ï¿½ï¿½ï¿½ï¿½
    }
    public float CalculateFallImpactForce(Rigidbody rb, Collider collider)
    {
        float gravity = Physics.gravity.y; // ï¿½ï¿½È¡ï¿½ï¿½ï¿½ï¿½ï¿½ï¿½ï¿½Ù¶ï¿½ (Í¨ï¿½ï¿½Îª -9.81)
        float heightDifference = (transform.position.y - collider.bounds.max.y)/ transform.parent.parent.localScale.y; // ï¿½ï¿½È¡ï¿½ï¿½×²ï¿½ï¿½ï¿½Â±ß½ï¿½ï¿½yÖµ
        // ï¿½ï¿½ï¿½Ý¸ß¶Èºï¿½ï¿½ï¿½ï¿½ï¿½ï¿½ï¿½ï¿½ï¿½ï¿½ï¿½ï¿½ï¿½ï¿½Ù¶ï¿½
        float finalVelocity = Mathf.Sqrt(-2 * gravity * heightDifference);
        // ï¿½ï¿½ï¿½ï¿½ï¿½ï¿½ï¿½ï¿½
        return (rb.mass * finalVelocity) / 0.1f; // ï¿½ï¿½ï¿½ï¿½Ô­ï¿½ï¿½ï¿½Äµï¿½ï¿½ï¿½ï¿½ï¿½ï¿½ï¿½
    }
    public float CalculateCollisionImpactForce(Collision col)
    {
        // ï¿½ï¿½È¡ï¿½ï¿½×²ï¿½ï¿½ï¿½ï¿½ï¿½ï¿½Ù¶ï¿½
        Vector3 collisionVelocity = col.relativeVelocity;

        // ï¿½ï¿½ï¿½ï¿½ï¿½ï¿½ï¿½ï¿½ï¿½ï¿½Ê¹ï¿½ï¿½ï¿½ï¿½ï¿½ï¿½ï¿½ï¿½ï¿½ï¿½ï¿½ï¿½ï¿½ï¿½ï¿½×²Ê±ï¿½ï¿½ï¿½Ù¶ï¿½
        float impactForce =mass * collisionVelocity.magnitude; // Ê¹ï¿½ï¿½ï¿½Ù¶ÈµÄ´ï¿½Ð¡

        return impactForce; // ï¿½ï¿½ï¿½Ø¼ï¿½ï¿½ï¿½ï¿½ï¿½Ä³ï¿½ï¿½ï¿½ï¿½
    }

    private void OnCollisionEnter(Collision col)
    {
        //col.impulse.magnitudeï¿½ï¿½Î»ÎªÅ£ï¿½ï¿½/ï¿½ë£¬N/sï¿½ï¿½ï¿½ï¿½Ê¾Ò»ï¿½ï¿½Ä±ï¿½ï¿½Ë¶ï¿½ï¿½Ù¶ï¿½ï¿½ï¿½
        float impactForce = CalculateCollisionImpactForce(col);
        //print("impactForce: " + impactForce);
        //float impactForce = CalculateFallImpactForce(GetComponent<Rigidbody>(), col.collider);
        //print("impactForce: " + impactForce);
        if (isUnbreakable || impactForce <= impulseThreshold) return;
        //if (isUnbreakable || fallImpactForce  <= impulseThreshold) return;

        //ï¿½ï¿½ï¿½ï¿½ï¿½×²ï¿½ï¿½ï¿½ï¿½ï¿½ï¿½ TooSmalOrSoftToBreakOtherObjects ï¿½Ð±ï¿½ï¿½Ð£ï¿½Ö±ï¿½Ó·ï¿½ï¿½ï¿½
        SimObjPhysics collidedObject = col.transform.GetComponentInParent<SimObjPhysics>();
        if (collidedObject != null && TooSmallOrSoftToBreakOtherObjects.Contains(collidedObject.Type))
        {
            return;
        }

        if (isReadyToBreak)
        {
            isReadyToBreak = false;
            BreakObject();
        }
    }

    private void OnCollisionStay(Collision collision)
    {
        if (collision.gameObject.CompareTag("Robot")&& currentSqueezeForce > squeezeThreshold)
        {
            print("OnCollisionStay");

            if (isReadyToBreak)
                {
                    isReadyToBreak = false;
                    BreakObject();
                }
        }
    }
    public void BreakObject()
    {
        Rigidbody rb = gameObject.GetComponent<Rigidbody>();
        //Êµï¿½ï¿½ï¿½ï¿½ï¿½Âµï¿½Ô¤ï¿½ï¿½ï¿½å£¬ï¿½ï¿½ï¿½ï¿½ï¿½ï¿½ï¿½ï¿½ï¿½ï¿½Îªï¿½ï¿½Ç°ï¿½ï¿½ï¿½ï¿½ï¿½Î»ï¿½Ãºï¿½ï¿½ï¿½×ª
        if (!sceneManager.SimObjectsDict.TryGetValue(prefabToSwapTo, out GameObject breakedObject))
        {
            return; // ï¿½ï¿½ï¿½Î´ï¿½Òµï¿½ï¿½ï¿½Ó¦ï¿½Ä¶ï¿½ï¿½ï¿½Ö±ï¿½Ó·ï¿½ï¿½ï¿½
        }
        breakedObject.transform.SetPositionAndRotation(transform.position, transform.rotation);
        breakedObject.SetActive(true);
        Breakdown breakdown = breakedObject.GetComponent<Breakdown>();
        breakdown.StartBreak();
        broken = true;

        //ï¿½ï¿½ï¿½ï¿½ï¿½ï¿½ï¿½ï¿½Ä¸ï¿½ï¿½ï¿½ï¿½Ù¶ÈºÍ½ï¿½ï¿½Ù¶ï¿½ï¿½ï¿½ï¿½ï¿½Îªï¿½ï¿½Ç°ï¿½ï¿½ï¿½ï¿½ï¿½ï¿½Ù¶ÈºÍ½ï¿½ï¿½Ù¶ï¿½
        //ÎªÊ²Ã´ï¿½ï¿½Îª0.4fï¿½ï¿½
        foreach (Rigidbody subRb in breakedObject.GetComponentsInChildren<Rigidbody>())
        {
            subRb.linearVelocity = rb.linearVelocity * 0.4f;
            subRb.angularVelocity = rb.angularVelocity * 0.4f;
        }
        gameObject.SetActive(false);
    }

    //ï¿½ï¿½ï¿½ï¿½ï¿½ï¿½ï¿½ï¿½ï¿½ï¿½Ä¦ï¿½ï¿½ï¿½ï¿½ï¿½ï¿½Ê±ï¿½ï¿½ï¿½ï¿½ï¿½Ó³ï¿½ï¿½ï¿½ï¿½ï¿½ï¿½Öµ
    public void OnTriggerEnter(Collider other)
    {
        if (other.CompareTag("HighFriction"))
        {
            impulseThreshold = maxImpulseThreshold;
            
        }
    }
   
    public void OnTriggerExit(Collider other)
    {
        if (other.CompareTag("HighFriction"))
        {
            impulseThreshold = minImpulseThreshold;

        }
    }

    private float GetSqueezeInput()
    {

        return 1; // Ö»ï¿½ï¿½Ê¾ï¿½ï¿½ï¿½ï¿½ï¿½æ»»Îªï¿½ï¿½Êµï¿½ï¿½ï¿½ï¿½ï¿½È»ï¿½È¡ï¿½ß¼ï¿½
    }
}
>>>>>>> 0c14a5c8d787bef23f3133ad2b2203f5035105bb
