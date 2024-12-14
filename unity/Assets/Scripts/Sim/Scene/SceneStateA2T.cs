using System;
using System.Collections.Generic;
using UnityEngine;

//用于反馈给python端的特定场景状态信息
[System.Serializable]
public class SceneStateA2T
{
    public int id;
    public ObjectStateA2T[] objects;
    public AgentStateA2T agent;
    public Vector3[] reachablePositons;//传送点？
}

[System.Serializable] 
//物体的状态
public class ObjectStateA2T
{
    public string objectId;//物体id
    public string name;//物体名称
    public string objectType;//物体类型
    public Vector3 position;//物体位置
    public Quaternion rotation;//物体朝向
    public bool visible;
    public float distance;//物体中心点距agent中心点的欧式距离
    public bool receptacle;//是否是容器
    public bool toggleable;//是否可切换
    public bool isToggled;//是否处于开状态
    public bool breakable;//是否可破坏
    public bool isBroken;//是否已破坏
    public bool canFillWithLiquid;//是否可填充液体
    public bool isFilledWithLiquid;//是否已填充液体
    public bool canBeUsedUp;//是否可被用完
    public bool isUsedUp;//是否已用完
    public bool cookable;//是否可烹饪
    public bool isCooked;//是否已烹饪
    public bool sliceable;//是否可切片
    public bool isSliced;//是否已切片
    public bool openable;//是否可打开
    public bool isOpen;//是否已打开
    public bool pickupable;//是否可捡起
    public bool isPickedUp;//是否已捡起
    public List<string> receptacleObjectIds;//容器内物体的id
    public List<string> parentReceptacles;//包含此物体的容器的id

}

[System.Serializable]
public class AgentStateA2T
{
    public string name;
    public Vector3 position;
    public Quaternion rotation;
    public string lastAction;//上一次执行的动作
    public bool lastActionSuccess;//上一次执行动作是否成功,不成功则状态不会发生改变
    public string errorMessage;//错误信息
}

