using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Contains : MonoBehaviour
{
    [SerializeField]
    private List<GameObject> currentlyContains = new(); //当前容器内包含的对象列表。

    [SerializeField]
    private GameObject myParent; //容器对象的父对象引用。

    public List<GameObject> CurrentlyContains { get { return currentlyContains; } }

    //获取容器对象的父对象引用。
    void OnEnable()
    {
        if (myParent == null)
        {
            if (gameObject.GetComponentInParent<SimObjPhysics>().transform.gameObject)
            {
                myParent = gameObject.GetComponentInParent<SimObjPhysics>().transform.gameObject;
            }
        }
    }

#if UNITY_EDITOR
    private void Start()
    {
        PropertyValidator.ValidateProperty(myParent, SimObjSecondaryProperty.Receptacle);
    }
#endif

    //第二个参数是不是第一个参数的父物体，从而排除掉父物体的碰撞体
    private bool HasAncestor(GameObject child, GameObject potentialAncestor)
    {
        if (child == potentialAncestor)
        {
            return true;
        }
        else if (child.transform.parent != null)
        {
            return HasAncestor(child.transform.parent.gameObject, potentialAncestor);
        }
        else
        {
            return false;
        }
    }

    //返回当前在碰撞体内的所有游戏物体的列表。通过 Physics.OverlapBox() 获取重叠的碰撞体，并且过滤掉触发器和可能的重复对象。
    public List<GameObject> CurrentlyContainedGameObjects()
    {
        List<GameObject> objs = new();
        BoxCollider b = this.GetComponent<BoxCollider>();

        //世界坐标
        Vector3 worldCenter = b.transform.TransformPoint(b.center);
        //世界缩放的一半尺寸
        Vector3 worldHalfExtents = new(
            b.size.x * b.transform.lossyScale.x / 2,
            b.size.y * b.transform.lossyScale.y / 2,
            b.size.z * b.transform.lossyScale.z / 2
        );

        foreach (Collider col in Physics.OverlapBox(worldCenter, worldHalfExtents, b.transform.rotation))
        {
            if (col.GetComponentInParent<SimObjPhysics>() && !col.isTrigger)
            {
                SimObjPhysics sop = col.GetComponentInParent<SimObjPhysics>();
                if (!HasAncestor(this.transform.gameObject, sop.transform.gameObject))
                {
                    if (!objs.Contains(sop.transform.gameObject))
                    {
                        objs.Add(sop.transform.gameObject);
                        if (!sop.parentReceptacleObjects.Contains(myParent)){
                            sop.parentReceptacleObjects.Add(myParent);
                        }
                        //将包含的对象添加到当前容器内。
                        if (sop.transform.gameObject.GetComponent<Contains>())
                        {
                            List<GameObject> nestedObjs = sop.transform.gameObject.GetComponent<Contains>().CurrentlyContains;
                            foreach (GameObject nestedObj in nestedObjs)
                            {
                                if (!objs.Contains(nestedObj))
                                {
                                    objs.Add(nestedObj);
                                    if (!sop.parentReceptacleObjects.Contains(myParent))
                                    {
                                        sop.parentReceptacleObjects.Add(myParent);
                                    }

                                }
                            }
                        }

                    }
                }
            }
        }
        return objs;
    }


    private void OnTriggerEnter(Collider other)
    {
        currentlyContains = CurrentlyContainedGameObjects();
    }

    private void OnTriggerExit(Collider other)
    {
        currentlyContains = CurrentlyContainedGameObjects();
    }

    public bool IsOccupied()
    {
        return CurrentlyContainedGameObjects().Count > 0;
    }

    //返回当前容器内包含的 SimObjPhysics 列表。
    public List<SimObjPhysics> CurrentlyContainedObjects()
    {
        List<SimObjPhysics> toSimObjPhysics = new();

        foreach (GameObject g in currentlyContains)
        {
            toSimObjPhysics.Add(g.GetComponent<SimObjPhysics>());
        }

        return toSimObjPhysics;
    }

    //返回当前容器内包含的对象 ID 列表。
    public List<string> CurrentlyContainedObjectIDs()
    {
        List<string> ids = new();

        foreach (GameObject g in currentlyContains)
        {
            ids.Add(g.GetComponent<SimObjPhysics>().ObjectID);
        }

        return ids;
    }
}
