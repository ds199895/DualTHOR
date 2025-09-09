using System;
using System.Collections.Generic;
using UnityEngine;

//State of the scene for A2T
[System.Serializable]
public class SceneStateA2T
{
    public int id;
    public ObjectStateA2T[] objects;
    public AgentStateA2T agent;
    public Vector3[] reachablePositons;//传送点？
}

[System.Serializable] 
//Object state
public class ObjectStateA2T
{
    public string objectId;//object id
    public string name;//object name
    public string objectType;//object type
    public Vector3 position;//object position
    public Quaternion rotation;//object rotation
    public bool visible;
    public float distance;//distance between object center and agent center
    public bool receptacle;//container or not
    public bool toggleable;//toggleable
    public bool isToggled;//toggled
    public bool breakable;//breakable
    public bool isBroken;//broken
    public bool canFillWithLiquid;//can fill with liquid
    public bool isFilledWithLiquid;//filled with liquid
    public bool canBeUsedUp;//can be used up
    public bool isUsedUp;//used up
    public bool cookable;//cookable
    public bool isCooked;//cooked
    public bool sliceable;//sliceable
    public bool isSliced;//sliced
    public bool openable;//openable
    public bool isOpen;//opened
    public bool pickupable;//pickupable
    public bool isPickedUp;//picked up
    public bool isMoveable;//moveable
    public List<string> receptacleObjectIds;//ids of objects in the container
    public List<string> parentReceptacles;//ids of containers containing this object
}

[System.Serializable]
public class AgentStateA2T
{
    public string name;
    public Vector3 position;
    public Quaternion rotation;
    public string lastAction;//last action
    public bool lastActionSuccess;//last action success, if not, the state will not change
    public string errorMessage;//error message
    public List<float> jointAngles; // joint angles of the robot
    public List<float> gripperAngles; // gripper angles of the robot
}

