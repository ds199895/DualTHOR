using UnityEngine;

public class CollisionReporter : MonoBehaviour
{
    private ArticulationBody parentArticulation;

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
        Debug.Log("Has Collision");
        if (parentArticulation != null)
        {
            RobotCollisionManager.Instance.ReportCollision(parentArticulation, collision.gameObject);
        }else{
            Debug.Log("有碰撞");
        }
    }

    void OnCollisionStay(Collision collision)
    {
        if (parentArticulation != null)
        {
            RobotCollisionManager.Instance.ReportOngoingCollision(parentArticulation, collision.gameObject);
        }else{
            Debug.Log("持续碰撞");
        }
    }

    void OnCollisionExit(Collision collision)
    {
        if (parentArticulation != null)
        {
            RobotCollisionManager.Instance.ReportCollisionExit(parentArticulation, collision.gameObject);
        }else{
            Debug.Log("结束碰撞");
        }
    }
}
