using System.Collections.Generic;
using UnityEngine;

public class Break : MonoBehaviour, IUniqueStateManager, IStateComponent
{
    [SerializeField]
    private GameObject PrefabToSwapTo = null;


    [SerializeField]
    private GameObject DirtyPrefabToSwapTo = null;

    [SerializeField]
    private float mass;
    [SerializeField]
    protected float ImpulseThreshold; // The threshold for breaking
    [SerializeField]
    private float squeezeThreshold; // The threshold for squeezing
    [SerializeField]
    protected float HighFrictionImpulseOffset = 2.0f;// The offset for the impulse threshold in the high friction area
    private float minImpulseThreshold; // The minimum impulse threshold
    private float maxImpulseThreshold; // The maximum impulse threshold
    [SerializeField]
    private float fallImpactForce ; // The impact force when falling
    [SerializeField]
    private float currentSqueezeForce; // The current squeeze force
    protected float CurrentImpulseThreshold; // modify this with ImpulseThreshold and HighFrictionImpulseOffset based on trigger callback functions

    private SceneStateManager sceneManager;

    [SerializeField]
    protected bool broken; // Whether the object has been broken
    [SerializeField]
    public bool Unbreakable=false; // Whether the object is unbreakable
    [SerializeField]
    protected bool readyToBreak = true; // Whether the object is ready to break

        // The list of object types that will not cause other objects to break
    //todo: need to simplify the list of object types later
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
    protected enum BreakType {
        PrefabSwap,
        MaterialSwap,
        Decal
    };
     [SerializeField]
    protected BreakType breakType; // please select how this object should be broken here

    [SerializeField]
    protected SwapObjList[] MaterialSwapObjects; // swap screen/surface with cracked version

    public void SaveState(ObjectState objectState)
    {
        objectState.breakState = new BreakState
        {
            isReadyToBreak = readyToBreak,
            broken = broken,
            isUnbreakable = Unbreakable,
        };
    }

    public void LoadState(ObjectState objectState)
    {
        readyToBreak = objectState.breakState.isReadyToBreak;
        broken = objectState.breakState.broken;
        Unbreakable = objectState.breakState.isUnbreakable;
    }


    public bool isBroken => broken;

    private void Start()
    {
#if UNITY_EDITOR
        PropertyValidator.ValidateProperty(gameObject, SimObjSecondaryProperty.CanBreak);
#endif

        minImpulseThreshold = ImpulseThreshold;
        maxImpulseThreshold = ImpulseThreshold + HighFrictionImpulseOffset;

        if (!TryGetComponent<Rigidbody>(out var rb)) return;
        
        // 计算冲击力
        fallImpactForce = CalculateFallImpactForce(rb);
        mass= rb.mass;
        sceneManager = GameObject.Find("SceneManager").GetComponent<SceneStateManager>();
    }

  

    public float CalculateFallImpactForce(Rigidbody rb)
    {
        float gravity = Physics.gravity.y; // get the gravity acceleration (usually -9.81)
        float height = transform.position.y / transform.parent.parent.localScale.y; // restore the height
        // calculate the final velocity based on the height and gravity
        float finalVelocity = Mathf.Sqrt(-2 * gravity * height); // note that gravity is negative, so we need to take the negative sign
        // calculate the impact force
        return (rb.mass * finalVelocity) / 0.1f; // keep the original adjustment factor
    }
    public float CalculateFallImpactForce(Rigidbody rb, Collider collider)
    {
        float gravity = Physics.gravity.y; // get the gravity acceleration (usually -9.81)
        float heightDifference = (transform.position.y - collider.bounds.max.y)/ transform.parent.parent.localScale.y; // get the y value of the bottom boundary of the collider
        // calculate the final velocity based on the height and gravity
        float finalVelocity = Mathf.Sqrt(-2 * gravity * heightDifference);
        // calculate the impact force
        return (rb.mass * finalVelocity) / 0.1f; // keep the original adjustment factor
    }
    public float CalculateCollisionImpactForce(Collision col)
    {
        // get the relative velocity of the collision
        Vector3 collisionVelocity = col.relativeVelocity;

        // calculate the impact force, using the mass and the velocity of the collision
        float impactForce =mass * collisionVelocity.magnitude; // use the magnitude of the velocity

        return impactForce; // return the calculated impact force
    }

    private void OnCollisionEnter(Collision col)
    {
        //col.impulse.magnitude is the impulse in N/s, which is the change of momentum per second
        float impactForce = CalculateCollisionImpactForce(col);
        //print("impactForce: " + impactForce);
        //float impactForce = CalculateFallImpactForce(GetComponent<Rigidbody>(), col.collider);
        //print("impactForce: " + impactForce);
        if (Unbreakable || impactForce <= ImpulseThreshold) return;
        //if (isUnbreakable || fallImpactForce  <= impulseThreshold) return;

        //if the collided object is in the TooSmallOrSoftToBreakOtherObjects list, return
        SimObjPhysics collidedObject = col.transform.GetComponentInParent<SimObjPhysics>();
        if (collidedObject != null && TooSmallOrSoftToBreakOtherObjects.Contains(collidedObject.Type))
        {
            return;
        }

        if (readyToBreak)
        {
            readyToBreak = false;
            BreakObject();
        }
    }

    private void OnCollisionStay(Collision collision)
    {
        if (collision.gameObject.CompareTag("Robot")&& currentSqueezeForce > squeezeThreshold)
        {
            print("OnCollisionStay");

            if (readyToBreak)
                {
                    readyToBreak = false;
                    BreakObject();
                }
        }
    }
    public void BreakObject()
    {
        Debug.Log("break it !");
        Rigidbody rb = gameObject.GetComponent<Rigidbody>();
        Debug.Log("prefabToSwapto: "+PrefabToSwapTo);

        GameObject breakedObject = Instantiate(PrefabToSwapTo, transform.position, transform.rotation);
        breakedObject.transform.SetPositionAndRotation(transform.position, transform.rotation);
        breakedObject.SetActive(true);
        Breakdown breakdown = breakedObject.GetComponent<Breakdown>();
        // breakdown.StartBreak();
        broken = true;

        foreach (Rigidbody subRb in breakedObject.GetComponentsInChildren<Rigidbody>())
        {
            subRb.linearVelocity = rb.linearVelocity * 0.4f;
            subRb.angularVelocity = rb.angularVelocity * 0.4f;
        }
        gameObject.SetActive(false);
    }

    public void Execute()
    {
        if (readyToBreak)
        {
            readyToBreak = false;
            BreakObject();
        }
    }

    //when the object enters the high friction area, increase the impulse threshold
    public void OnTriggerEnter(Collider other)
    {
        if (other.CompareTag("HighFriction"))
        {
            ImpulseThreshold = maxImpulseThreshold;
            
        }
    }
   
    public void OnTriggerExit(Collider other)
    {
        if (other.CompareTag("HighFriction"))
        {
            ImpulseThreshold = minImpulseThreshold;

        }
    }

    private float GetSqueezeInput()
    {

        return 1; // just for example, replace with the real squeeze input logic
    }
}
