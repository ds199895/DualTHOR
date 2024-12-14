using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Contains : MonoBehaviour
{
    [SerializeField]
    private List<GameObject> currentlyContains = new(); //��ǰ�����ڰ����Ķ����б�

    [SerializeField]
    private GameObject myParent; //��������ĸ��������á�

    public List<GameObject> CurrentlyContains { get { return currentlyContains; } }

    //��ȡ��������ĸ��������á�
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

    //�ڶ��������ǲ��ǵ�һ�������ĸ����壬�Ӷ��ų������������ײ��
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

    //���ص�ǰ����ײ���ڵ�������Ϸ������б�ͨ�� Physics.OverlapBox() ��ȡ�ص�����ײ�壬���ҹ��˵��������Ϳ��ܵ��ظ�����
    public List<GameObject> CurrentlyContainedGameObjects()
    {
        List<GameObject> objs = new();
        BoxCollider b = this.GetComponent<BoxCollider>();

        //��������
        Vector3 worldCenter = b.transform.TransformPoint(b.center);
        //�������ŵ�һ��ߴ�
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
                        //�������Ķ�����ӵ���ǰ�����ڡ�
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

    //���ص�ǰ�����ڰ����� SimObjPhysics �б�
    public List<SimObjPhysics> CurrentlyContainedObjects()
    {
        List<SimObjPhysics> toSimObjPhysics = new();

        foreach (GameObject g in currentlyContains)
        {
            toSimObjPhysics.Add(g.GetComponent<SimObjPhysics>());
        }

        return toSimObjPhysics;
    }

    //���ص�ǰ�����ڰ����Ķ��� ID �б�
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
