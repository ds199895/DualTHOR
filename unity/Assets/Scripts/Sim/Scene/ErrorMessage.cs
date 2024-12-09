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
                "Unable to grasp", //无法抓住
                "Successfully grasped but slipped afterwards", //成功抓住但后续滑落
                "Object is too heavy", //物体太重
                "Grasping failed due to mechanical error", //抓取失败，机械故障
                "Object is out of reach" //物体超出可抓取范围
            } },
            { "Open", new string[] {
                "Object is already open", //物体已打开
                "Object cannot be opened", //物体无法打开
                "Successfully grasped but slipped afterwards"//成功抓住但后续滑落
            } },
            { "Toggle", new string[] {
                "Action not permitted", //不允许进行此操作
                "Device is already in the desired state", //设备已处于所需状态
                "Operation failed due to mechanical issue" //由于机械故障，操作失败
            } },
            { "CookObject", new string[] {
                "Cooker malfunction", //烹饪设备故障
                "Cooking process interrupted" //烹饪过程被中断
            } },
            { "Fill", new string[] {
                "Reached maximum capacity", //已达到最大容量
                "Filling process failed due to blockage" //填充过程因阻塞而失败
            } },
            { "SliceObject", new string[] {
                "Slicing failed due to resistance", //切割失败，由于阻力
                "Invalid slicing position", //切割位置错误
                "Object cannot be sliced" //物体不可被切割
            } },
            { "UsedUp", new string[] {
                "Object has been depleted", //物体已被消耗
            } },
            { "TP", new string[] {
                "Target object does not have a valid TransferPoint", //目标物体没有有效的传送点
                "Teleportation interrupted due to collision", //传送因碰撞中断
                "Target object is not accessible" //目标物体不可到达
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

    // 根据错误代码获取错误信息
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