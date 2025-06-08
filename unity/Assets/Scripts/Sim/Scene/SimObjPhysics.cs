using System;
using System.Collections.Generic;
using UnityEngine;

public class SimObjPhysics : MonoBehaviour
{
    public class DroneFPSAgentController{

    }

    [System.Serializable]
    public class ReceptacleSpawnPoint {
        public BoxCollider ReceptacleBox; // the box the point is in
        public Vector3 Point; // Vector3 coordinate in world space, possible spawn location

        public Contains Script;
        public SimObjPhysics ParentSimObjPhys;

        public ReceptacleSpawnPoint(Vector3 p, BoxCollider box, Contains c, SimObjPhysics parentsop) {
            ReceptacleBox = box;
            Point = p;
            Script = c;
            ParentSimObjPhys = parentsop;
        }
    }

    public enum Temperature {
        RoomTemp,
        Hot,
        Cold
    };

    // Salient materials are only for pickupable and moveable objects,
    // for now static only objects do not report material back since we have to assign them manually
    // They are the materials that make up an object (ie: cell phone - metal, glass).
    public enum SalientObjectMaterial {
        Metal,
        Wood,
        Plastic,
        Glass,
        Ceramic,
        Stone,
        Fabric,
        Rubber,
        Food,
        Paper,
        Wax,
        Soap,
        Sponge,
        Organic,
        Leather
    };

    [Header("Unique String ID of this Object In Scene")]
    [SerializeField]
    private string objectID = string.Empty;

    [Header("Name of Prefab this Object comes from")]
    [SerializeField]
    public string assetID = string.Empty;

    [Header("Object Type")]
    [SerializeField]
    public SimObjType Type = SimObjType.Undefined;
    // public SimObjType objType=SimObjType.Mug;

    [Header("Primary Property (Must Have only 1)")]
    [SerializeField]
    public SimObjPrimaryProperty PrimaryProperty;

    [Header("Additional Properties (Can Have Multiple)")]
    [SerializeField]
    public SimObjSecondaryProperty[] SecondaryProperties;

    [Header("non Axis-Aligned Box enclosing all colliders of this object")]
    // This can be used to get the "bounds" of the object, but needs to be created manually
    // we should look into a programatic way to figure this out so we don't have to set it up for EVERY object
    // for now, only CanPickup objects have a BoundingBox, although maybe every sim object needs one for
    // spawning eventually? For now make sure the Box Collider component is disabled on this, because it's literally
    // just holding values for the center and size of the box.
    public GameObject BoundingBox = null;

    //[Header("Transfer Point")]
    [SerializeField]
    private Transform transferPoint; // the transfer point of the object

    //[Header("Interactable Points")]
    [SerializeField]
    private Transform[] interactablePoints; // the interactable points of the object
    [Header("Raycast to these points to determine Visible/Interactable")]


    [SerializeField]
    private Transform[] liftPoints; // the lift points of the object
    [SerializeField]
    public Transform[] VisibilityPoints = null;


    [Header("If this object is a Receptacle, put all trigger boxes here")]
    [SerializeField]
    public GameObject[] ReceptacleTriggerBoxes = null;

    public List<GameObject> parentReceptacleObjects;


    [Header("State information Bools here")]
#if UNITY_EDITOR
    public bool debugIsVisible = false;
    public bool debugIsInteractable = false;
#endif
    public bool isInAgentHand = false;

    public DroneFPSAgentController droneFPSAgent;

    // these collider references are used for switching physics materials for all colliders on this object
    [Header("Non - Trigger Colliders of this object")]
    public Collider[] MyColliders = null;

    [Header("High Friction physics values")]
    public float HFdynamicfriction;
    public float HFstaticfriction;
    public float HFbounciness;
    public float HFrbdrag;
    public float HFrbangulardrag;

    private float RBoriginalDrag;
    private float RBoriginalAngularDrag;

    [Header("Salient Materials")] // if this object is moveable or pickupable, set these up
    public SalientObjectMaterial[] salientMaterials;

    public Dictionary<Collider, ContactPoint[]> contactPointsDictionary =
        new Dictionary<Collider, ContactPoint[]>();

    // if this object is a receptacle, get all valid spawn points from any child ReceptacleTriggerBoxes and sort them by distance to Agent
    public List<ReceptacleSpawnPoint> MySpawnPoints = new List<ReceptacleSpawnPoint>();

    // keep track of this object's current temperature (abstracted to three states, RoomTemp/Hot/Cold)
    public Temperature CurrentTemperature = Temperature.RoomTemp;

    // value for how long it should take this object to get back to room temperature from hot/cold
    public float HowManySecondsUntilRoomTemp = 10f;
    private float TimerResetValue;


    public bool inMotion = false;

    // count of number of other sim objects this object has hit, if agent is drone
    public int numSimObjHit = 0;
    public int numFloorHit = 0;
    public int numStructureHit = 0;

    // the velocity of this object from the last frame
    public float lastVelocity = 0; // start at zero assuming at rest

    // reference to this gameobject's rigidbody
    private Rigidbody myRigidbody;



    // properties initialized during Start()
    public bool IsReceptacle;
    public bool IsPickupable;
    public bool IsMoveable;
    public bool isStatic;
    public bool IsToggleable;
    public bool IsOpenable;
    public bool IsBreakable;
    public bool IsFillable;
    public bool IsDirtyable;
    public bool IsCookable;
    public bool IsSliceable;
    public bool isHeatSource;
    public bool isColdSource;

    public bool CanBeUsedUp;



    public string ObjectID => objectID;
    // public SimObjType Type => type;
    // public SimObjPrimaryProperty PrimaryProperty => primaryProperty;
    // public SimObjSecondaryProperty[] SecondaryProperties => secondaryProperties;
    public Transform TransferPoint => transferPoint;
    public Transform[] InteractablePoints => interactablePoints;
    public Transform[] VisiblePoints => VisibilityPoints;

    public Transform[] LiftPoints => liftPoints;

    void Start()
    {
        objectID = gameObject.name;

        InitializeInteractablePoints();
        InitializeTransferPoint();

        InitializeVisiblePoints();
        InitializeProperties();
    }

    //initialize the transfer point
    private void InitializeTransferPoint()
    {
        // create a new GameObject as transferPoint
        GameObject newTransferPoint = new GameObject("TransferPoint");
        
        newTransferPoint.transform.SetParent(transform);


        // set the position and rotation
        newTransferPoint.transform.position = interactablePoints[0].position + interactablePoints[0].forward * -0.5f;
        // newTransferPoint.transform.position = transform.position + transform.up * 0.5f;
        // newTransferPoint.transform.position = transform.position+transform.right*0.5f;
        newTransferPoint.transform.rotation = transform.rotation;

        // assign the new Transform to transferPoint
        transferPoint = newTransferPoint.transform;
    }

    //initialize the interactable points
    private void InitializeInteractablePoints()
    {
        // get all the Renderer components of the object
        Renderer[] renderers = GetComponentsInChildren<Renderer>();
        if (renderers.Length > 0)
        {
            // initialize an empty Bounds
            Bounds bounds = renderers[0].bounds;
            
            // merge all the Renderer's Bounds
            foreach (Renderer renderer in renderers)
            {
                bounds.Encapsulate(renderer.bounds);
            }

            // create a new GameObject as interactablePoint
            GameObject newInteractablePoint = new GameObject("InteractablePoint");
            newInteractablePoint.transform.SetParent(transform);

            // set the position to the geometric center
            newInteractablePoint.transform.position = bounds.center;
            newInteractablePoint.transform.rotation=transform.rotation;
            // assign the new Transform to interactablePoints
            interactablePoints = new Transform[] { newInteractablePoint.transform };

            
            // create liftPoints - the left and right points
            GameObject leftLiftPoint = new GameObject("LeftLiftPoint");
            GameObject rightLiftPoint = new GameObject("RightLiftPoint");
            
            // set the parent object
            leftLiftPoint.transform.SetParent(transform);
            rightLiftPoint.transform.SetParent(transform);


            Debug.Log(gameObject.name+" PrimaryProperty: "+PrimaryProperty);
            if(PrimaryProperty==SimObjPrimaryProperty.Moveable){

                Debug.Log(gameObject.name+" Moveable, set lift points!");
                // calculate the position of the left and right points (using the exact size of the bounding box)
                // the center point of the left side = bounds center point - half of the bounds width to the right
                Vector3 leftPosition = new Vector3(
                    bounds.center.x - bounds.extents.x,  // X coordinate is the center point minus half of the width
                    bounds.center.y,                    // Y coordinate is the same as the center point
                    bounds.center.z                     // Z coordinate is the same as the center point
                );
                
                // the center point of the right side = bounds center point + half of the bounds width to the right
                Vector3 rightPosition = new Vector3(
                    bounds.center.x + bounds.extents.x,  // X coordinate is the center point plus half of the width
                    bounds.center.y,                    // Y coordinate is the same as the center point
                    bounds.center.z                     // Z coordinate is the same as the center point
                );

                Debug.Log("leftPosition: "+leftPosition);
                Debug.Log("rightPosition: "+rightPosition);
                
                // set the position
                leftLiftPoint.transform.position = leftPosition;
                rightLiftPoint.transform.position = rightPosition;
                
                // set the rotation to be the same as the parent object
                leftLiftPoint.transform.rotation = transform.rotation;
                rightLiftPoint.transform.rotation = transform.rotation;
                
                // add the two liftPoints to the array
                liftPoints = new Transform[] { leftLiftPoint.transform, rightLiftPoint.transform };
                Debug.Log("liftPoints: "+liftPoints.Length);
            }
        }
        else
        {
            Debug.LogWarning($"{gameObject.name} has no Renderer components.");
        }
    }
    
    //initialize the visible points
    private void InitializeVisiblePoints()
    {
        GameObject visiblePointsObject;
        if(Type==SimObjType.Cabinet){
            visiblePointsObject=transform.Find("StaticVisPoints").gameObject;
        }else{
            visiblePointsObject = transform.Find("VisibilityPoints").gameObject;
        }
        
        if (visiblePointsObject != null)
        {
            VisibilityPoints = new Transform[visiblePointsObject.transform.childCount];
            for (int i = 0; i < visiblePointsObject.transform.childCount; i++)
            {
                VisibilityPoints[i] = visiblePointsObject.transform.GetChild(i);
            }
        }
        else
        {
            Debug.LogWarning($"{gameObject.name} has no 'VisibilityPoints' child object.");
        }
    }

    public SimObjType GetObjectType(){
        return Type;
    }

    private void InitializeProperties()
    {
        IsReceptacle = Array.IndexOf(SecondaryProperties, SimObjSecondaryProperty.Receptacle) > -1 && ReceptacleTriggerBoxes != null;
        IsPickupable = PrimaryProperty == SimObjPrimaryProperty.CanPickup;
        IsMoveable = PrimaryProperty == SimObjPrimaryProperty.Moveable;
        isStatic = PrimaryProperty == SimObjPrimaryProperty.Static;
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
        return Array.Exists(SecondaryProperties, element => element == prop);
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


    public bool DoesThisObjectHaveThisSecondaryProperty(SimObjSecondaryProperty prop) {
        bool result = false;
        List<SimObjSecondaryProperty> temp = new List<SimObjSecondaryProperty>(SecondaryProperties);

        if (temp.Contains(prop)) {
            result = true;
        }

        return result;
    }

    //void OnDrawGizmos()
    //{
    //    // check if the visible points exist
    //    if (visiblePoints != null)
    //    {
    //        Gizmos.color = Color.yellow; // set the Gizmos color to yellow
    //        foreach (Transform point in visiblePoints)
    //        {
    //            // draw a yellow cube at the visible point's position
    //            Gizmos.DrawCube(point.position, Vector3.one * 0.03f); // 0.1f is the size of the cube, can be adjusted as needed
    //        }
    //    }
    //}
}
