using UnityEngine;

public class CollisionReporter : MonoBehaviour
{
    private ArticulationBody parentArticulation;
    private bool hasCollision = false;
    private GameObject lastCollidedObject = null;

    void Start()
    {
        // find the nearest ArticulationBody ancestor
        parentArticulation = GetComponentInParent<ArticulationBody>();

        if (parentArticulation == null)
        {
            Debug.LogError($"{gameObject.name} not found ArticulationBody ancestor");
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
            Debug.Log("collision detected");
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
            Debug.Log("ongoing collision");
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
            Debug.Log("collision exit");
        }
    }
    
    // clear the collision state, this method can be called by Unity's SendMessage
    public void ClearCollision()
    {
        hasCollision = false;
        lastCollidedObject = null;
        
        // ensure always remove the record in RobotCollisionManager
        if (parentArticulation != null && RobotCollisionManager.Instance != null)
        {
            // check if the collision information exists for the joint
            var collisions = RobotCollisionManager.Instance.GetCollisions(parentArticulation);
            if (collisions.Count > 0)
            {
                Debug.Log($"clear the collision information for the joint {parentArticulation.name}, there are {collisions.Count} collisions");
                
                // report all collisions as ended
                foreach (var obj in collisions)
                {
                    RobotCollisionManager.Instance.ReportCollisionExit(parentArticulation, obj);
                }
            }
        }
        
        // output the debug information
        Debug.Log($"collision reporter {gameObject.name} has cleared the collision state");
    }
    
    // check if there is a collision
    public bool HasCollision()
    {
        return hasCollision;
    }
    
    // get the last collided object
    public GameObject GetLastCollidedObject()
    {
        return lastCollidedObject;
    }
}
