using System.Collections.Generic;
using System.Linq;
using UnityEngine;

public class GetObjectsInView : MonoBehaviour
{
    [SerializeField]
    private Camera targetCamera; // ���
    [SerializeField]
    private float viewDistance = 30f; // ��Ұ����
    [SerializeField]
    private List<GameObject> canInteractableObjects; // ��¼GameObject������
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

    // �������ߣ��������
    private void DrawVisibleRays()
    {
        foreach (GameObject parent in canInteractableObjects)
        {
            SimObjPhysics simObjPhysics = parent.GetComponent<SimObjPhysics>();
            //�����䵽�ɼ���
            if (simObjPhysics != null && simObjPhysics.VisiblePoints != null)
            {
                foreach (Transform visiblePoint in simObjPhysics.VisiblePoints)
                {
                    Vector3 direction = visiblePoint.position - targetCamera.transform.position;
                    Ray ray = new Ray(targetCamera.transform.position, direction);

                    // �������ߣ��������
                    Debug.DrawLine(ray.origin, ray.origin + ray.direction * viewDistance, Color.red);
                }
            }
            //�����䵽��������
            //if (simObjPhysics != null)
            //{
            //    // ��ȡ��������ĵ�
            //    Vector3 objectCenter = simObjPhysics.transform.position;
            //    Vector3 direction = objectCenter - targetCamera.transform.position;
            //    Ray ray = new Ray(targetCamera.transform.position, direction);

            //    // �������ߣ��������
            //    Debug.DrawLine(ray.origin, ray.origin + ray.direction * viewDistance, Color.red);
            //}
        }
    }
    public void GetObjects()
    {
        canInteractableObjects.Clear();
        Plane[] planes = GeometryUtility.CalculateFrustumPlanes(targetCamera);//�����������׶��ƽ�棨������
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
                            canInteractableObjects.Add(interactableParent); // ��ӵ�������
                        }
                        //canInteractableObjects.Add(interactableParent); // ��ӵ�������

                    }

                }
            }
        }
        // ������ת��ΪGameObject����
        sceneManager.canInteractableObjects = canInteractableObjects;
        sceneManager.canTransferPoints= sceneManager.TransferPoints.Where(t => t.transform != null && canInteractableObjects.Contains(t)).ToList();
    }

    //public void GetObjects()
    //{
    //    loggedInteractableParents.Clear();
    //    Plane[] planes = GeometryUtility.CalculateFrustumPlanes(targetCamera);
    //    //int layerMask = LayerMask.GetMask("SimObjVisible");

    //    // ��ȡ���д���� GameObject
    //    GameObject[] allObjects = GameObject.FindGameObjectsWithTag("Interactable");
    //    print(allObjects.Length);
    //    foreach (GameObject obj in allObjects)
    //    {
    //        // ��ȡ����������� MeshFilter
    //        MeshFilter[] meshFilters = obj.GetComponentsInChildren<MeshFilter>();
    //        foreach (MeshFilter meshFilter in meshFilters)
    //        {
    //            // ��ȡ����İ�Χ��
    //            Bounds bounds = meshFilter.mesh.bounds;
    //            bounds.center = meshFilter.transform.TransformPoint(bounds.center);
    //            bounds.Encapsulate(meshFilter.GetComponent<Renderer>().bounds);

    //            // ����Ƿ��������Ұ�ཻ
    //            if (GeometryUtility.TestPlanesAABB(planes, bounds))
    //            {
    //                Transform interactableParent = GetInteractableParent(obj.transform);
    //                if (interactableParent != null && !loggedInteractableParents.Contains(interactableParent))
    //                {
    //                    loggedInteractableParents.Add(interactableParent); // ��ӵ�������
    //                }
    //                break; // һ���ҵ�һ���ཻ�����壬�Ϳ�������ѭ��
    //            }
    //        }
    //    }

    //    // ������ת��ΪGameObject����
    //    canInteractableObjects = loggedInteractableParents.Select(t => t.gameObject).ToArray();
    //    sceneManager.canInteractableObjects = canInteractableObjects.ToList();
    //}

    private bool IsVisible(GameObject parent)
    {
        // ��ȡ��ǰInteractable�����VisiblePoints
        SimObjPhysics simObjPhysics = parent.GetComponent<SimObjPhysics>();
        if (simObjPhysics != null && simObjPhysics.VisiblePoints != null)
        {
            foreach (Transform visiblePoint in simObjPhysics.VisiblePoints)
            {
                Vector3 direction = visiblePoint.position - targetCamera.transform.position;
                Ray ray = new(targetCamera.transform.position, direction);

                // ��������Ƿ��������ཻ
                if (Physics.Raycast(ray, out RaycastHit hit, viewDistance))
                {
                    if (hit.transform.IsChildOf(parent.transform) || hit.transform == parent)
                    {
                        //print("GetObjectsInView IsVisible: " + parent);
                        return true; // �ҵ���VisiblePoint�ཻ������
                    }
                }
            }
        }
        return false; // û���ҵ��ɼ���
    }

}
