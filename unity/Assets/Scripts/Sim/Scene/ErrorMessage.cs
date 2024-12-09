using System.Collections.Generic;
using UnityEngine;

public class ErrorMessage : MonoBehaviour
{
    public Dictionary<string, string[]> errorMessage;

    private void Start()
    {
        errorMessage = new Dictionary<string, string[]>
        {
            { "Break", new string[] {
                "Unable to grasp", //�޷�ץס
                "Successfully grasped but slipped afterwards", //�ɹ�ץס����������
                "Object is too heavy", //����̫��
                "Grasping failed due to mechanical error", //ץȡʧ�ܣ���е����
                "Object is out of reach" //���峬����ץȡ��Χ
            } },
            { "Open", new string[] {
                "Object is already open", //�����Ѵ�
                "Object cannot be opened", //�����޷���
                "Successfully grasped but slipped afterwards"//�ɹ�ץס����������
            } },
            { "Toggle", new string[] {
                "Action not permitted", //��������д˲���
                "Device is already in the desired state", //�豸�Ѵ�������״̬
                "Operation failed due to mechanical issue" //���ڻ�е���ϣ�����ʧ��
            } },
            { "CookObject", new string[] {
                "Cooker malfunction", //����豸����
                "Cooking process interrupted" //��⿹��̱��ж�
            } },
            { "Fill", new string[] {
                "Reached maximum capacity", //�Ѵﵽ�������
                "Filling process failed due to blockage" //��������������ʧ��
            } },
            { "SliceObject", new string[] {
                "Slicing failed due to resistance", //�и�ʧ�ܣ���������
                "Invalid slicing position", //�и�λ�ô���
                "Object cannot be sliced" //���岻�ɱ��и�
            } },
            { "UsedUp", new string[] {
                "Object has been depleted", //�����ѱ�����
            } },
            { "TP", new string[] {
                "Target object does not have a valid TransferPoint", //Ŀ������û����Ч�Ĵ��͵�
                "Teleportation interrupted due to collision", //��������ײ�ж�
                "Target object is not accessible" //Ŀ�����岻�ɵ���
            } },
            { "MoveAhead", new string[] {
                "Movement blocked by obstacle",
                "Insufficient energy for forward movement",
                "Movement path ahead is not navigable"
            } },
            { "MoveRight", new string[] {
                "Movement blocked by obstacle",
                "Insufficient energy for rightward movement",
                "Movement path to the right is not navigable"
            } },
            { "MoveBack", new string[] {
                "Movement blocked by obstacle",
                "Insufficient energy for backward movement",
                "Movement path backward is not navigable"
            } },
            { "MoveLeft", new string[] {
                "Movement blocked by obstacle",
                "Insufficient energy for leftward movement",
                "Movement path to the left is not navigable"
            } },
            { "RotateRight", new string[] {
                "Rotation blocked by structural constraint",
                "Insufficient torque for rightward rotation",
                "Rotation interrupted by external force"
            } },
            { "RotateLeft", new string[] {
                "Rotation blocked by structural constraint",
                "Insufficient torque for leftward rotation",
                "Rotation interrupted by external force"
            } },
        };
    }

    // ���ݴ�������ȡ������Ϣ
    public string[] GetErrorMessage(string errorCode)
    {
        if (errorMessage.TryGetValue(errorCode, out string[] messages))
        {
            return messages;
        }
        else
        {
            return new string[] { "Unknown error code." };
        }
    }

}