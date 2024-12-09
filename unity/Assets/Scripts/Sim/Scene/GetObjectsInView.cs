using System.Collections.Generic;
using System.Linq;
using UnityEngine;

public class GetObjectsInView : MonoBehaviour
{
    [SerializeField]
    private Camera targetCamera; // 相机
    [SerializeField]
    private float viewDistance = 30f; // 视野距离
    [SerializeField]
    private List<GameObject> canInteractableObjects; // 记录GameObject的数组
    private SceneManager sceneManager;

    
    private void Start()
    {
        sceneManager =GameObject.Find("SceneManager").GetComponent<SceneManager>();
    }
#if UNITY_EDITOR
    private void Update()
    {
        DrawVisibleRays();
    }
#endif

    // 绘制射线，方便调试
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

                    // 绘制射线，方便调试
                    Debug.DrawLine(ray.origin, ray.origin + ray.direction * viewDistance, Color.red);
                }
            }
            //射线射到物体中心
            //if (simObjPhysics != null)
            //{
            //    // 获取物体的中心点
            //    Vector3 objectCenter = simObjPhysics.transform.position;
            //    Vector3 direction = objectCenter - targetCamera.transform.position;
            //    Ray ray = new Ray(targetCamera.transform.position, direction);

            //    // 绘制射线，方便调试
            //    Debug.DrawLine(ray.origin, ray.origin + ray.direction * viewDistance, Color.red);
            //}
        }
    }
    public void GetObjects()
    {
        canInteractableObjects.Clear();
        Plane[] planes = GeometryUtility.CalculateFrustumPlanes(targetCamera);//计算相机的视锥体平面（六个）
        int layerMask = LayerMask.GetMask("SimObjVisible");

        Collider[] collidersInLayer = Physics.OverlapSphere(targetCamera.transform.position, viewDistance, layerMask);
        //print("GetObjectsInView colliders count: " + collidersInLayer.Length);
       
        foreach (Collider collider in collidersInLayer)
        {
            if(collider.GetComponentInParent<SimObjPhysics>().gameObject.CompareTag("Interactable"))
            {
                GameObject interactableParent = collider.GetComponentInParent<SimObjPhysics>().gameObject;
                
                if (interactableParent != null && GeometryUtility.TestPlanesAABB(planes, collider.bounds))
                {
                    if (!canInteractableObjects.Contains(interactableParent))
                    {
                        if (IsVisible(interactableParent))
                        {
                            canInteractableObjects.Add(interactableParent); // 添加到集合中
                        }
                        //canInteractableObjects.Add(interactableParent); // 添加到集合中

                    }

                }
            }
        }
        // 将集合转换为GameObject数组
        sceneManager.canInteractableObjects = canInteractableObjects;
        sceneManager.canTransferPoints= sceneManager.TransferPoints.Where(t => t.transform != null && canInteractableObjects.Contains(t)).ToList();
    }

    //public void GetObjects()
    //{
    //    loggedInteractableParents.Clear();
    //    Plane[] planes = GeometryUtility.CalculateFrustumPlanes(targetCamera);
    //    //int layerMask = LayerMask.GetMask("SimObjVisible");

    //    // 获取所有处理的 GameObject
    //    GameObject[] allObjects = GameObject.FindGameObjectsWithTag("Interactable");
    //    print(allObjects.Length);
    //    foreach (GameObject obj in allObjects)
    //    {
    //        // 获取所有子物体的 MeshFilter
    //        MeshFilter[] meshFilters = obj.GetComponentsInChildren<MeshFilter>();
    //        foreach (MeshFilter meshFilter in meshFilters)
    //        {
    //            // 获取网格的包围盒
    //            Bounds bounds = meshFilter.mesh.bounds;
    //            bounds.center = meshFilter.transform.TransformPoint(bounds.center);
    //            bounds.Encapsulate(meshFilter.GetComponent<Renderer>().bounds);

    //            // 检查是否与相机视野相交
    //            if (GeometryUtility.TestPlanesAABB(planes, bounds))
    //            {
    //                Transform interactableParent = GetInteractableParent(obj.transform);
    //                if (interactableParent != null && !loggedInteractableParents.Contains(interactableParent))
    //                {
    //                    loggedInteractableParents.Add(interactableParent); // 添加到集合中
    //                }
    //                break; // 一旦找到一个相交的物体，就可以跳出循环
    //            }
    //        }
    //    }

    //    // 将集合转换为GameObject数组
    //    canInteractableObjects = loggedInteractableParents.Select(t => t.gameObject).ToArray();
    //    sceneManager.canInteractableObjects = canInteractableObjects.ToList();
    //}

    private bool IsVisible(GameObject parent)
    {
        // 获取当前Interactable物体的VisiblePoints
        SimObjPhysics simObjPhysics = parent.GetComponent<SimObjPhysics>();
        if (simObjPhysics != null && simObjPhysics.VisiblePoints != null)
        {
            foreach (Transform visiblePoint in simObjPhysics.VisiblePoints)
            {
                Vector3 direction = visiblePoint.position - targetCamera.transform.position;
                Ray ray = new(targetCamera.transform.position, direction);

                // 检测射线是否与物体相交
                if (Physics.Raycast(ray, out RaycastHit hit, viewDistance))
                {
                    if (hit.transform.IsChildOf(parent.transform) || hit.transform == parent)
                    {
                        //print("GetObjectsInView IsVisible: " + parent);
                        return true; // 找到与VisiblePoint相交的射线
                    }
                }
            }
        }
        return false; // 没有找到可见点
    }

}
