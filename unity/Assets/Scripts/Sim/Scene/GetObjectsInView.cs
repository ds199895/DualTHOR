using System.Collections.Generic;
using System.Linq;
using UnityEngine;

public class GetObjectsInView : MonoBehaviour
{
    [SerializeField]
    private Camera targetCamera; // Camera
    [SerializeField]
    public float viewDistance = 30f; // View distance
    [SerializeField]
    private List<GameObject> canInteractableObjects; // Record GameObject array
    private SceneStateManager sceneManager;

    
    private void Start()
    {
        sceneManager =GameObject.Find("SceneManager").GetComponent<SceneStateManager>();
    }
#if UNITY_EDITOR
    private void Update()
    {
        DrawVisibleRays();
    }
#endif

    // Draw rays for debugging
    private void DrawVisibleRays()
    {
        foreach (GameObject parent in canInteractableObjects)
        {
            SimObjPhysics simObjPhysics = parent.GetComponent<SimObjPhysics>();
            //射线射到可见点
            if (simObjPhysics != null && simObjPhysics.VisiblePoints != null)
            {
                foreach (Transform visiblePoint in simObjPhysics.VisiblePoints)
                {
                    Vector3 direction = visiblePoint.position - targetCamera.transform.position;
                    Ray ray = new Ray(targetCamera.transform.position, direction);

                    // Draw rays for debugging
                    Debug.DrawLine(ray.origin, ray.origin + ray.direction * viewDistance, Color.red);
                }
            }
        }
    }
    public void GetObjects()
    {
        canInteractableObjects.Clear();
        Plane[] planes = GeometryUtility.CalculateFrustumPlanes(targetCamera);//Calculate the camera's frustum planes (six)
        int layerMask = LayerMask.GetMask("SimObjVisible");

        Collider[] collidersInLayer = Physics.OverlapSphere(targetCamera.transform.position, viewDistance, layerMask);
        print("GetObjectsInView colliders count: " + collidersInLayer.Length);
       
        foreach (Collider collider in collidersInLayer)
        {
            
            // print("Get parent: " + collider.GetComponentInParent<SimObjPhysics>().gameObject);
            if(collider.GetComponentInParent<SimObjPhysics>()){
                if(collider.GetComponentInParent<SimObjPhysics>().gameObject.CompareTag("Interactable"))
                {
                
                    GameObject interactableParent = collider.GetComponentInParent<SimObjPhysics>().gameObject;
                
                    if (interactableParent != null && GeometryUtility.TestPlanesAABB(planes, collider.bounds))
                    {
                        // print("object name: "+interactableParent.name);
                        if (!canInteractableObjects.Contains(interactableParent))
                        {
                            if (IsVisible(interactableParent))
                            {
                                // print("is visible "+interactableParent.name);
                                canInteractableObjects.Add(interactableParent); // Add to the collection
                            }
                            //canInteractableObjects.Add(interactableParent); // Add to the collection

                        }

                    }
                }
            }
           
        }
        // Convert the collection to a GameObject array
        sceneManager.canInteractableObjects = canInteractableObjects;
        sceneManager.canTransferPoints= sceneManager.TransferPoints.Where(t => t.transform != null && canInteractableObjects.Contains(t)).ToList();
    }

    private bool IsVisible(GameObject parent)
    {
        // Get the VisiblePoints of the current Interactable object
        SimObjPhysics simObjPhysics = parent.GetComponent<SimObjPhysics>();
        if (simObjPhysics != null && simObjPhysics.VisiblePoints != null)
        {
            // print("check visible ： "+parent.name);
            foreach (Transform visiblePoint in simObjPhysics.VisiblePoints)
            {
                Vector3 direction = visiblePoint.position - targetCamera.transform.position;
                // print("ray dir: "+direction);
                Ray ray = new(targetCamera.transform.position, direction);

                // Check if the ray intersects with the object
                if (Physics.Raycast(ray, out RaycastHit hit, viewDistance))
                {
                    // print("find hit: " + hit.transform.parent.name);
                    if (hit.transform.IsChildOf(parent.transform) || hit.transform == parent)
                    {
                        // print("GetObjectsInView IsVisible: " + parent);
                        return true; // Find the ray that intersects with the VisiblePoint
                    }
                }
            }
        }
        return false; // No visible point found
    }

}
