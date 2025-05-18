using System;
using System.Collections.Generic;
using UnityEngine;

// Scene state information for version management
[System.Serializable]
public class SceneState
{
    public int id;
    public Vector3 agentPosition;
    public Quaternion agentRotation;
    public ObjectState[] objects; 

    // Each arm has seven joint angles + 2 gripper angles
    public List<float> jointAngles; // Store all joint angles
    public List<float> gripperAngles; // Store gripper angles
}

[System.Serializable] // Add this to be serialized by JsonUtility.ToJson
// Object state
public class ObjectState
{
    public string name;// Object name
    public Vector3 position;// Object position
    public Quaternion rotation;// Object orientation
    public bool isActive;
    public bool isPickedUp; // Whether it is picked up, that is, the parent object is Hand
    // Break object state
    public BreakState breakState;
    // Cook object state
    public CookState cookState;
    // Toggle object state
    public ToggleState toggleState;
    // Open object state
    public OpenState openState;
    // Fill object state
    public FillState fillState;
    // Used up object state
    public UsedUpState usedUpState;
    // Sliced object state
    public SlicedState slicedState;
    // Spill object state
    public SpillState spillState;
}

[System.Serializable]
// Break object state
public class BreakState
{

    public bool isReadyToBreak;
    public bool broken;
    public bool isUnbreakable;

}

[System.Serializable]
// Cook object state
public class CookState
{
    public bool isCooked;
    public float temperature;
    public List<Material> materials;
}

// Toggle object state
[System.Serializable]
public class ToggleState
{
    public bool isOn;
    public List<Material> materials;
    public GameObject[] movingParts;
    public Light[] lights;
    public GameObject[] effects;

}

// Open object state
[System.Serializable]
public class OpenState
{
    public bool isOpen;
    public GameObject[] movingParts;

}

// Fill object state
[System.Serializable]
public class FillState
{
    public bool isFilled = false;
    public bool[] filledObj;
    public float[] fillObjHeight;
}

// Sliced object state
[System.Serializable]
public class SliceState
{
    public bool isSliced = false;
}

// Used up object state
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

[System.Serializable]
public class SpillState
{
    public bool isSpilled;
}

public class jointsStates
{
    // Left arm joint angle
    public float arm_left_link01; // Left arm joint angle 1
    public float arm_left_link02; // Left arm joint angle 2
    public float arm_left_link03; // Left arm joint angle 3
    public float arm_left_link04; // Left arm joint angle 4
    public float arm_left_link05; // Left arm joint angle 5
    public float arm_left_link06; // Left arm joint angle 6
    public float arm_left_link07; // Left arm joint angle 7
    public float hand_left_link02;   // Left arm left gripper angle
    public float hand_left_link01;   // Left arm right gripper angle

    // Right arm joint angle
    public float arm_right_link01; // Right arm joint angle 1
    public float arm_right_link02; // Right arm joint angle 2
    public float arm_right_link03; // Right arm joint angle 3
    public float arm_right_link04; // Right arm joint angle 4
    public float arm_right_link05; // Right arm joint angle 5
    public float arm_right_link06; // Right arm joint angle 6
    public float arm_right_link07; // Right arm joint angle 7
    public float hand_right_link02;   // Right arm left gripper angle
    public float hand_right_link01;   // Right arm right gripper angle

}