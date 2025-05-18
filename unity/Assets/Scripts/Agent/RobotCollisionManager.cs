using System.Collections.Generic;
using UnityEngine;

public class RobotCollisionManager : MonoBehaviour
{
    public static RobotCollisionManager Instance;
    private Dictionary<ArticulationBody, List<GameObject>> currentCollisions = new Dictionary<ArticulationBody, List<GameObject>>();
    
    // Mark the collision object
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

    // Set the current interacting object ID
    public void SetCurrentInteractingObject(string objectID)
    {
        currentInteractingObjectID = objectID;
        Debug.Log($"RobotCollisionManager sets the current interacting object: {objectID}");
    }
    
    // Add the ignored collision object ID
    public void AddIgnoredCollisionObject(string objectID)
    {
        if (!string.IsNullOrEmpty(objectID) && !ignoredCollisionObjectIDs.Contains(objectID))
        {
            ignoredCollisionObjectIDs.Add(objectID);
            Debug.Log($"RobotCollisionManager adds the ignored collision object: {objectID}");
        }
    }
    
    // Clear the ignored list
    public void ClearIgnoredCollisionObjects()
    {
        ignoredCollisionObjectIDs.Clear();
        currentInteractingObjectID = string.Empty;
        Debug.Log("RobotCollisionManager has cleared the ignored list");
    }

    public void ReportCollision(ArticulationBody joint, GameObject other)
    {
        // Get the SimObjPhysics component of the collision object
        SimObjPhysics simObj = other.GetComponent<SimObjPhysics>();
        string objectID = simObj != null ? simObj.ObjectID : string.Empty;
        
        // Record the ID of the collision object
        if (simObj != null && !string.IsNullOrEmpty(objectID))
        {
            collisionObjectIDs[other] = objectID;
        }
        
        // Check if it is a collision with the current interacting object
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
                Debug.Log($"Joint {joint.name} starts colliding with the interacting object {other.name}, not considered as an error");
            }
            else
            {
                Debug.Log($"Joint {joint.name} starts colliding with {other.name}");
            }
        }
    }

    public void ReportOngoingCollision(ArticulationBody joint, GameObject other)
    {
        // Check if it is a collision with the current interacting object
        bool isInteractingObject = IsInteractingObject(other);
        
        if (isInteractingObject)
        {
            // The collision with the interacting object, do not record the log to reduce noise
            return;
        }
        
        Debug.Log($"Joint {joint.name} is continuously colliding with {other.name}");
    }

    public void ReportCollisionExit(ArticulationBody joint, GameObject other)
    {
        if (currentCollisions.ContainsKey(joint))
        {
            currentCollisions[joint].Remove(other);
            
            // Check if it is a collision with the current interacting object
            bool isInteractingObject = IsInteractingObject(other);
            
            if (isInteractingObject)
            {
                Debug.Log($"Joint {joint.name} ends colliding with the interacting object {other.name}");
            }
            else
            {
                Debug.Log($"Joint {joint.name} ends colliding with {other.name}");
            }
        }
    }
    
    // Check if the object is the current interacting object
    public bool IsInteractingObject(GameObject obj)
    {
        if (string.IsNullOrEmpty(currentInteractingObjectID) && ignoredCollisionObjectIDs.Count == 0)
            return false;
        
        // First check if we already know the ID of this object
        if (collisionObjectIDs.TryGetValue(obj, out string objectID))
        {
            if (objectID == currentInteractingObjectID || ignoredCollisionObjectIDs.Contains(objectID))
                return true;
        }
        
        // If we do not know the ID, try to get the SimObjPhysics component
        SimObjPhysics simObj = obj.GetComponent<SimObjPhysics>();
        if (simObj != null)
        {
            string id = simObj.ObjectID;
            if (id == currentInteractingObjectID || ignoredCollisionObjectIDs.Contains(id))
            {
                // Update the record
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
            Debug.Log($"Joint {joint.Key.name} is currently colliding with: {string.Join(", ", joint.Value)}");
        }
    }
    
    // Clear all collision records
    public void ClearAllCollisions()
    {
        int collisionCount = 0;
        foreach (var joint in currentCollisions)
        {
            collisionCount += joint.Value.Count;
        }
        
        // Record the number of collisions cleaned
        if (collisionCount > 0)
        {
            Debug.Log($"Cleaning {collisionCount} collision records");
        }
        
        // Clear the dictionary
        currentCollisions.Clear();
        collisionObjectIDs.Clear();
        Debug.Log("All collision records have been cleaned");
    }
    
    // Check if the specified joint has collisions, ignoring the interacting object
    public bool HasCollision(ArticulationBody joint)
    {
        if (!currentCollisions.ContainsKey(joint) || currentCollisions[joint].Count == 0)
            return false;
            
        // Check if there is a collision with a non-interacting object
        foreach (GameObject obj in currentCollisions[joint])
        {
            if (!IsInteractingObject(obj))
                return true;
        }
        
        return false;
    }
    
    // Get all collisions of the specified joint
    public List<GameObject> GetCollisions(ArticulationBody joint)
    {
        if (currentCollisions.TryGetValue(joint, out List<GameObject> collisions))
        {
            return new List<GameObject>(collisions); // Return a copy to prevent external modification
        }
        return new List<GameObject>();
    }

    // Check if there is any collision, ignoring the interacting object
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
                    Debug.LogWarning($"Detected non-interacting object collision: joint {pair.Key.name} collided with {obj.name}");
                }
            }
        }
        
        return hasCollision;
    }
    
    // Get all collisions of non-interacting objects
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
