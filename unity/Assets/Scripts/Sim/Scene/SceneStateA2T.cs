using System;
using System.Collections.Generic;
using UnityEngine;

//���ڷ�����python�˵��ض�����״̬��Ϣ
[System.Serializable]
public class SceneStateA2T
{
    public int id;
    public ObjectStateA2T[] objects;
    public AgentStateA2T agent;
    public Vector3[] reachablePositons;//���͵㣿
}

[System.Serializable] 
//�����״̬
public class ObjectStateA2T
{
    public string objectId;//����id
    public string name;//��������
    public string objectType;//��������
    public Vector3 position;//����λ��
    public Quaternion rotation;//���峯��
    public bool visible;
    public float distance;//�������ĵ��agent���ĵ��ŷʽ����
    public bool receptacle;//�Ƿ�������
    public bool toggleable;//�Ƿ���л�
    public bool isToggled;//�Ƿ��ڿ�״̬
    public bool breakable;//�Ƿ���ƻ�
    public bool isBroken;//�Ƿ����ƻ�
    public bool canFillWithLiquid;//�Ƿ�����Һ��
    public bool isFilledWithLiquid;//�Ƿ������Һ��
    public bool canBeUsedUp;//�Ƿ�ɱ�����
    public bool isUsedUp;//�Ƿ�������
    public bool cookable;//�Ƿ�����
    public bool isCooked;//�Ƿ������
    public bool sliceable;//�Ƿ����Ƭ
    public bool isSliced;//�Ƿ�����Ƭ
    public bool openable;//�Ƿ�ɴ�
    public bool isOpen;//�Ƿ��Ѵ�
    public bool pickupable;//�Ƿ�ɼ���
    public bool isPickedUp;//�Ƿ��Ѽ���
    public List<string> receptacleObjectIds;//�����������id
    public List<string> parentReceptacles;//�����������������id

}

[System.Serializable]
public class AgentStateA2T
{
    public string name;
    public Vector3 position;
    public Quaternion rotation;
    public string lastAction;//��һ��ִ�еĶ���
    public bool lastActionSuccess;//��һ��ִ�ж����Ƿ�ɹ�,���ɹ���״̬���ᷢ���ı�
    public string errorMessage;//������Ϣ
}

