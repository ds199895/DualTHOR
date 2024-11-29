using System;
using System.Collections.Generic;
using UnityEngine;

//场景版本管理的场景状态信息
[System.Serializable]
public class SceneState
{
    public int id;
    public Vector3 agentPosition;
    public Quaternion agentRotation;
    public ObjectState[] objects; 

    //每个手臂七个关节角+2个爪子角度
    //public List<float> jointsStates; // 存储所有关节的状态
    //public List<float> gripperStates; // 存储爪子的状态
}

[System.Serializable] //加了这个才能被JsonUtility.ToJson正确序列化
//物体的状态
public class ObjectState
{
    public string name;//物体名称
    public Vector3 position;//物体位置
    public Quaternion rotation;//物体朝向
    public bool isActive;
    //break物体具有的状态
    public BreakState breakState;
    //cook物体具有的状态
    public CookState cookState;
    //toggle物体状态
    public ToggleState toggleState;
    //open物体状态
    public OpenState openState;
    //fill物体状态
    public FillState fillState;
    //usedup物体状态
    public UsedUpState usedUpState;
    //slice物体状态
    public SlicedState slicedState;
}

[System.Serializable]
//break物体具有的状态
public class BreakState
{

    public bool isReadyToBreak;
    public bool broken;
    public bool isUnbreakable;

}

[System.Serializable]
//cook物体具有的状态
public class CookState
{
    public bool isCooked;
    public float temperature;
    public List<Material> materials;
}

//toggle的状态
[System.Serializable]
public class ToggleState
{
    public bool isOn;
    public List<Material> materials;
    public GameObject[] movingParts;
    public Light[] lights;
    public GameObject[] effects;

}

//openable的状态
[System.Serializable]
public class OpenState
{
    public bool isOpen;
    public GameObject[] movingParts;

}

//fillable的状态
[System.Serializable]
public class FillState
{
    public bool isFilled = false;
    public bool[] filledObj;
    public float[] fillObjHeight;
}

//slice的状态
[System.Serializable]
public class SliceState
{
    public bool isSliced = false;
}

//usedup的状态
[System.Serializable]

public class UsedUpState
{
    public bool isUsedUp;
}

[System.Serializable]

public class SlicedState
{
    public bool isSliced;
}



public class jointsStates
{
    // 左手臂的关节角度
    public float arm_left_link01; // 左手臂第1个关节角度
    public float arm_left_link02; // 左手臂第2个关节角度
    public float arm_left_link03; // 左手臂第3个关节角度
    public float arm_left_link04; // 左手臂第4个关节角度
    public float arm_left_link05; // 左手臂第5个关节角度
    public float arm_left_link06; // 左手臂第6个关节角度
    public float arm_left_link07; // 左手臂第7个关节角度
    public float hand_left_link02;   // 左手臂左夹爪角度
    public float hand_left_link01;   // 左手臂右夹爪角度

    // 右手臂的关节角度
    public float arm_right_link01; // 右手臂第1个关节角度
    public float arm_right_link02; // 右手臂第2个关节角度
    public float arm_right_link03; // 右手臂第3个关节角度
    public float arm_right_link04; // 右手臂第4个关节角度
    public float arm_right_link05; // 右手臂第5个关节角度
    public float arm_right_link06; // 右手臂第6个关节角度
    public float arm_right_link07; // 右手臂第7个关节角度
    public float hand_right_link02;   // 右手臂左夹爪角度
    public float hand_right_link01;   // 右手臂右夹爪角度

}