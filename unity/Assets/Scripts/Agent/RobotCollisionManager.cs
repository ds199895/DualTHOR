using System.Collections.Generic;
using UnityEngine;

public class RobotCollisionManager : MonoBehaviour
{
    public static RobotCollisionManager Instance;
    private Dictionary<ArticulationBody, List<GameObject>> currentCollisions = new Dictionary<ArticulationBody, List<GameObject>>();
    
    // 用于标记与交互物体相关的碰撞
    private Dictionary<GameObject, string> collisionObjectIDs = new Dictionary<GameObject, string>();
    private string currentInteractingObjectID = string.Empty;
    private List<string> ignoredCollisionObjectIDs = new List<string>();

    private void Awake()
    {
        if (Instance == null)
        {
            Instance = this;
            DontDestroyOnLoad(gameObject);
        }
        else
        {
            Destroy(gameObject);
        }
    }

    // 设置当前交互物体ID
    public void SetCurrentInteractingObject(string objectID)
    {
        currentInteractingObjectID = objectID;
        Debug.Log($"RobotCollisionManager设置当前交互物体: {objectID}");
    }
    
    // 添加忽略的碰撞物体ID
    public void AddIgnoredCollisionObject(string objectID)
    {
        if (!string.IsNullOrEmpty(objectID) && !ignoredCollisionObjectIDs.Contains(objectID))
        {
            ignoredCollisionObjectIDs.Add(objectID);
            Debug.Log($"RobotCollisionManager添加忽略碰撞物体: {objectID}");
        }
    }
    
    // 清空忽略列表
    public void ClearIgnoredCollisionObjects()
    {
        ignoredCollisionObjectIDs.Clear();
        currentInteractingObjectID = string.Empty;
        Debug.Log("RobotCollisionManager已清空忽略列表");
    }

    public void ReportCollision(ArticulationBody joint, GameObject other)
    {
        // 获取碰撞物体的SimObjPhysics组件
        SimObjPhysics simObj = other.GetComponent<SimObjPhysics>();
        string objectID = simObj != null ? simObj.ObjectID : string.Empty;
        
        // 记录碰撞物体的ID
        if (simObj != null && !string.IsNullOrEmpty(objectID))
        {
            collisionObjectIDs[other] = objectID;
        }
        
        // 检查是否是与当前交互物体的碰撞
        bool isInteractingObject = IsInteractingObject(other);
        
        if (!currentCollisions.ContainsKey(joint))
        {
            currentCollisions[joint] = new List<GameObject>();
        }

        if (!currentCollisions[joint].Contains(other))
        {
            currentCollisions[joint].Add(other);
            
            if (isInteractingObject)
            {
                Debug.Log($"关节 {joint.name} 开始与交互物体碰撞 {other.name}，不视为错误");
            }
            else
            {
                Debug.Log($"关节 {joint.name} 开始碰撞 {other.name}");
            }
        }
    }

    public void ReportOngoingCollision(ArticulationBody joint, GameObject other)
    {
        // 检查是否是与当前交互物体的碰撞
        bool isInteractingObject = IsInteractingObject(other);
        
        if (isInteractingObject)
        {
            // 交互物体的碰撞，不记录日志以减少噪音
            return;
        }
        
        Debug.Log($"关节 {joint.name} 持续碰撞 {other.name}");
    }

    public void ReportCollisionExit(ArticulationBody joint, GameObject other)
    {
        if (currentCollisions.ContainsKey(joint))
        {
            currentCollisions[joint].Remove(other);
            
            // 检查是否是与当前交互物体的碰撞
            bool isInteractingObject = IsInteractingObject(other);
            
            if (isInteractingObject)
            {
                Debug.Log($"关节 {joint.name} 结束与交互物体碰撞 {other.name}");
            }
            else
            {
                Debug.Log($"关节 {joint.name} 结束碰撞 {other.name}");
            }
        }
    }
    
    // 检查物体是否为当前交互物体
    public bool IsInteractingObject(GameObject obj)
    {
        if (string.IsNullOrEmpty(currentInteractingObjectID) && ignoredCollisionObjectIDs.Count == 0)
            return false;
        
        // 先检查我们是否已经知道这个物体的ID
        if (collisionObjectIDs.TryGetValue(obj, out string objectID))
        {
            if (objectID == currentInteractingObjectID || ignoredCollisionObjectIDs.Contains(objectID))
                return true;
        }
        
        // 如果没有记录，尝试获取SimObjPhysics组件
        SimObjPhysics simObj = obj.GetComponent<SimObjPhysics>();
        if (simObj != null)
        {
            string id = simObj.ObjectID;
            if (id == currentInteractingObjectID || ignoredCollisionObjectIDs.Contains(id))
            {
                // 更新记录
                collisionObjectIDs[obj] = id;
                return true;
            }
        }
        
        return false;
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
        collisionObjectIDs.Clear();
        Debug.Log("所有碰撞记录已清理");
    }
    
    // 检查指定关节是否有碰撞，忽略交互物体
    public bool HasCollision(ArticulationBody joint)
    {
        if (!currentCollisions.ContainsKey(joint) || currentCollisions[joint].Count == 0)
            return false;
            
        // 检查是否有非交互物体的碰撞
        foreach (GameObject obj in currentCollisions[joint])
        {
            if (!IsInteractingObject(obj))
                return true;
        }
        
        return false;
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

    // 检查是否有任何碰撞，忽略交互物体
    public bool HasAnyCollision()
    {
        bool hasCollision = false;
        
        foreach (var pair in currentCollisions)
        {
            foreach (GameObject obj in pair.Value)
            {
                if (!IsInteractingObject(obj))
                {
                    hasCollision = true;
                    Debug.LogWarning($"检测到非交互物体碰撞: 关节 {pair.Key.name} 碰撞对象 {obj.name}");
                }
            }
        }
        
        return hasCollision;
    }
    
    // 获取所有非交互物体的碰撞列表
    public List<KeyValuePair<ArticulationBody, GameObject>> GetAllNonInteractingCollisions()
    {
        List<KeyValuePair<ArticulationBody, GameObject>> result = new List<KeyValuePair<ArticulationBody, GameObject>>();
        
        foreach (var pair in currentCollisions)
        {
            foreach (GameObject obj in pair.Value)
            {
                if (!IsInteractingObject(obj))
                {
                    result.Add(new KeyValuePair<ArticulationBody, GameObject>(pair.Key, obj));
                }
            }
        }
        
        return result;
    }
}
