using System.Collections;
using System.Collections.Generic;
using UnityEngine;
//1.该脚本管理特定类型的对象是否可以放置在容器中，并提供放置点的位置
//2.通过 full 布尔变量来表示容器是否已满。
//3.提供方法来检查对象是否属于允许放置的特定类型。
public class ObjectSpecificReceptacle : MonoBehaviour
{
    [Header("Only objects of these Types can be placed on this Receptacle")]
    [SerializeField]
    private SimObjType[] SpecificTypes;//允许放置在容器中的对象类型数组。

    [Header("Point where specified object(s) attach to this Receptacle")]

    public Transform attachPoint;//对象放置在容器中的位置。

    [Header("Is this Receptacle already holding a valid object?")]
    public bool full = false;//表示容器是否已满的布尔变量。

    //检查传入的对象类型是否在允许放置的类型列表中。
    public bool HasSpecificType(SimObjType check)
    {
        bool result = false;

        foreach (SimObjType sot in SpecificTypes)
        {
            if (sot == check)
            {
                result = true;
            }
        }

        return result;
    }

    // Use this for initialization
    //检查对象是否具有 ObjectSpecificReceptacle 属性。
    void Start()
    {
#if UNITY_EDITOR
        if (
            !gameObject
                .GetComponent<SimObjPhysics>()
                .HasSecondaryProperty(
                    SimObjSecondaryProperty.ObjectSpecificReceptacle
                )
        )
        {
            Debug.LogError(
                this.name + " is missing the Secondary Property ObjectSpecificReceptacle!"
            );
        }
#endif
    }
}
