using System;
using System.Collections.Generic;
using UnityEngine;

//�����汾����ĳ���״̬��Ϣ
[System.Serializable]
public class SceneState
{
    public int id;
    public Vector3 agentPosition;
    public Quaternion agentRotation;
    public ObjectState[] objects; 

    //ÿ���ֱ��߸��ؽڽ�+2��צ�ӽǶ�
    //public List<float> jointsStates; // �洢���йؽڵ�״̬
    //public List<float> gripperStates; // �洢צ�ӵ�״̬
}

[System.Serializable] //����������ܱ�JsonUtility.ToJson��ȷ���л�
//�����״̬
public class ObjectState
{
    public string name;//��������
    public Vector3 position;//����λ��
    public Quaternion rotation;//���峯��
    public bool isActive;
    //break������е�״̬
    public BreakState breakState;
    //cook������е�״̬
    public CookState cookState;
    //toggle����״̬
    public ToggleState toggleState;
    //open����״̬
    public OpenState openState;
    //fill����״̬
    public FillState fillState;
    //usedup����״̬
    public UsedUpState usedUpState;
    //slice����״̬
    public SlicedState slicedState;
}

[System.Serializable]
//break������е�״̬
public class BreakState
{

    public bool isReadyToBreak;
    public bool broken;
    public bool isUnbreakable;

}

[System.Serializable]
//cook������е�״̬
public class CookState
{
    public bool isCooked;
    public float temperature;
    public List<Material> materials;
}

//toggle��״̬
[System.Serializable]
public class ToggleState
{
    public bool isOn;
    public List<Material> materials;
    public GameObject[] movingParts;
    public Light[] lights;
    public GameObject[] effects;

}

//openable��״̬
[System.Serializable]
public class OpenState
{
    public bool isOpen;
    public GameObject[] movingParts;

}

//fillable��״̬
[System.Serializable]
public class FillState
{
    public bool isFilled = false;
    public bool[] filledObj;
    public float[] fillObjHeight;
}

//slice��״̬
[System.Serializable]
public class SliceState
{
    public bool isSliced = false;
}

//usedup��״̬
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
    // ���ֱ۵ĹؽڽǶ�
    public float arm_left_link01; // ���ֱ۵�1���ؽڽǶ�
    public float arm_left_link02; // ���ֱ۵�2���ؽڽǶ�
    public float arm_left_link03; // ���ֱ۵�3���ؽڽǶ�
    public float arm_left_link04; // ���ֱ۵�4���ؽڽǶ�
    public float arm_left_link05; // ���ֱ۵�5���ؽڽǶ�
    public float arm_left_link06; // ���ֱ۵�6���ؽڽǶ�
    public float arm_left_link07; // ���ֱ۵�7���ؽڽǶ�
    public float hand_left_link02;   // ���ֱ����צ�Ƕ�
    public float hand_left_link01;   // ���ֱ��Ҽ�צ�Ƕ�

    // ���ֱ۵ĹؽڽǶ�
    public float arm_right_link01; // ���ֱ۵�1���ؽڽǶ�
    public float arm_right_link02; // ���ֱ۵�2���ؽڽǶ�
    public float arm_right_link03; // ���ֱ۵�3���ؽڽǶ�
    public float arm_right_link04; // ���ֱ۵�4���ؽڽǶ�
    public float arm_right_link05; // ���ֱ۵�5���ؽڽǶ�
    public float arm_right_link06; // ���ֱ۵�6���ؽڽǶ�
    public float arm_right_link07; // ���ֱ۵�7���ؽڽǶ�
    public float hand_right_link02;   // ���ֱ����צ�Ƕ�
    public float hand_right_link01;   // ���ֱ��Ҽ�צ�Ƕ�

}