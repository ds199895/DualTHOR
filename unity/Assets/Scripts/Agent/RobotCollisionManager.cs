using System.Collections.Generic;
using UnityEngine;

public class RobotCollisionManager : MonoBehaviour
{
    public static RobotCollisionManager Instance;
    private Dictionary<ArticulationBody, List<GameObject>> currentCollisions = new Dictionary<ArticulationBody, List<GameObject>>();

    private void Awake()
    {
        if (Instance == null)
        {
            Instance = this;
        }
        else
        {
            Destroy(gameObject);
        }
    }

    public void ReportCollision(ArticulationBody joint, GameObject other)
    {
        if (!currentCollisions.ContainsKey(joint))
        {
            currentCollisions[joint] = new List<GameObject>();
        }

        if (!currentCollisions[joint].Contains(other))
        {
            currentCollisions[joint].Add(other);
            Debug.Log($"关节 {joint.name} 开始碰撞 {other.name}");

        }
    }

    public void ReportOngoingCollision(ArticulationBody joint, GameObject other)
    {
        Debug.Log($"关节 {joint.name} 持续碰撞 {other.name}");
    }

    public void ReportCollisionExit(ArticulationBody joint, GameObject other)
    {
        if (currentCollisions.ContainsKey(joint))
        {
            currentCollisions[joint].Remove(other);
            Debug.Log($"关节 {joint.name} 结束碰撞 {other.name}");
        }
    }

    public void PrintAllCollisions()
    {
        foreach (var joint in currentCollisions)
        {
            Debug.Log($"关节 {joint.Key.name} 当前正在碰撞的物体: {string.Join(", ", joint.Value)}");
        }
    }
}
