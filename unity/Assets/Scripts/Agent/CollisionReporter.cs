using UnityEngine;

public class CollisionReporter : MonoBehaviour
{
    private ArticulationBody parentArticulation;
    private bool hasCollision = false;
    private GameObject lastCollidedObject = null;

    void Start()
    {
        // 找到最近的 ArticulationBody 祖先
        parentArticulation = GetComponentInParent<ArticulationBody>();

        if (parentArticulation == null)
        {
            Debug.LogError($"{gameObject.name} 未找到 ArticulationBody 父级");
        }
    }

    void OnCollisionEnter(Collision collision)
    {
        hasCollision = true;
        lastCollidedObject = collision.gameObject;
        
        if (parentArticulation != null)
        {
            RobotCollisionManager.Instance.ReportCollision(parentArticulation, collision.gameObject);
        }else{
            Debug.Log("有碰撞");
        }
    }

    void OnCollisionStay(Collision collision)
    {
        hasCollision = true;
        lastCollidedObject = collision.gameObject;
        
        if (parentArticulation != null)
        {
            RobotCollisionManager.Instance.ReportOngoingCollision(parentArticulation, collision.gameObject);
        }else{
            Debug.Log("持续碰撞");
        }
    }

    void OnCollisionExit(Collision collision)
    {
        if (lastCollidedObject == collision.gameObject)
        {
            hasCollision = false;
            lastCollidedObject = null;
        }
        
        if (parentArticulation != null)
        {
            RobotCollisionManager.Instance.ReportCollisionExit(parentArticulation, collision.gameObject);
        }else{
            Debug.Log("结束碰撞");
        }
    }
    
    // 清理碰撞状态，此方法可以被Unity的SendMessage调用
    public void ClearCollision()
    {
        hasCollision = false;
        lastCollidedObject = null;
        
        // 确保始终移除在RobotCollisionManager中的记录
        if (parentArticulation != null && RobotCollisionManager.Instance != null)
        {
            // 检查是否存在该关节的碰撞信息
            var collisions = RobotCollisionManager.Instance.GetCollisions(parentArticulation);
            if (collisions.Count > 0)
            {
                Debug.Log($"清理关节 {parentArticulation.name} 的 {collisions.Count} 个碰撞");
                
                // 将所有碰撞报告为已结束
                foreach (var obj in collisions)
                {
                    RobotCollisionManager.Instance.ReportCollisionExit(parentArticulation, obj);
                }
            }
        }
        
        // 输出调试信息
        Debug.Log($"碰撞报告器 {gameObject.name} 已清理碰撞状态");
    }
    
    // 检查是否有碰撞
    public bool HasCollision()
    {
        return hasCollision;
    }
    
    // 获取最后碰撞的物体
    public GameObject GetLastCollidedObject()
    {
        return lastCollidedObject;
    }
}
