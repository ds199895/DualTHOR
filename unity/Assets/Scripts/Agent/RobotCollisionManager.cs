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
    
    // 清理所有碰撞记录
    public void ClearAllCollisions()
    {
        int collisionCount = 0;
        foreach (var joint in currentCollisions)
        {
            collisionCount += joint.Value.Count;
        }
        
        // 记录清理的碰撞数量
        if (collisionCount > 0)
        {
            Debug.Log($"正在清理 {collisionCount} 个碰撞记录");
        }
        
        // 清空字典
        currentCollisions.Clear();
        Debug.Log("所有碰撞记录已清理");
    }
    
    // 检查指定关节是否有碰撞
    public bool HasCollision(ArticulationBody joint)
    {
        return currentCollisions.ContainsKey(joint) && currentCollisions[joint].Count > 0;
    }
    
    // 获取指定关节的所有碰撞对象
    public List<GameObject> GetCollisions(ArticulationBody joint)
    {
        if (currentCollisions.TryGetValue(joint, out List<GameObject> collisions))
        {
            return new List<GameObject>(collisions); // 返回副本，防止外部修改
        }
        return new List<GameObject>();
    }

    // 检查是否有任何碰撞
    public bool HasAnyCollision()
    {
        foreach (var pair in currentCollisions)
        {
            if (pair.Value.Count > 0)
            {
                return true;
            }
        }
        return false;
    }
}
