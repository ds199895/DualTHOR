using UnityEngine;

public class StructureCollisionDetector : MonoBehaviour
{
    public bool CollideStructure { get; private set; }
    private int structureLayer;
    
    // Related parameters for raycasting
    public float raycastDistance = 0.3f;  // Default raycast length, temporarily reduced to 0.3
    public bool showDebugRays = true;     // Whether to show debug rays
    private Transform robotRoot;          // Robot root node
    
    // Debug options
    public bool debugMode = true;         // Whether to enable additional debug information
    public bool disableCollisionDetection = false; // Temporarily disable collision detection
    public bool useRaycastsOnly = true;  // Only use raycasting, ignore physical collisions
    
    // Reference to the robot body
    public Transform robotBodyTransform;
    private Vector3 offsetFromBody;
    private bool isPositionInitialized = false;
    
    // Current move direction, used for directional raycasting
    private Vector3 currentMoveDirection = Vector3.zero;
    
    // Define the raycasting direction array (front, back, left, right, front left, front right, back left, back right)
    private Vector3[] rayDirections = new Vector3[] {
        Vector3.forward,
        Vector3.back,
        Vector3.left,
        Vector3.right,
        new Vector3(1, 0, 1).normalized,   // front right
        new Vector3(-1, 0, 1).normalized,  // front left
        new Vector3(1, 0, -1).normalized,  // back right
        new Vector3(-1, 0, -1).normalized  // back left
    };
    
    // Store the last detected obstacle
    private GameObject lastDetectedObstacle = null;

    private void OnEnable()
    {
        // Register this detector with the manager
        if (CollisionDetectorManager.Instance != null)
        {
            CollisionDetectorManager.Instance.RegisterDetector(this);
        }
    }
    
    private void OnDisable()
    {
        // Unregister this detector from the manager
        if (CollisionDetectorManager.Instance != null)
        {
            CollisionDetectorManager.Instance.UnregisterDetector(this);
        }
    }

    private void Start()
    {
        // Get the index of the structure layer
        structureLayer = LayerMask.NameToLayer("Structure");
        CollideStructure = false;
        robotRoot = transform.root;
        
        // Find the robot body
        if (robotBodyTransform == null)
        {
            // Try to get the Transform of the AgentMovement component as the robot body
            AgentMovement agentMovement = FindObjectOfType<AgentMovement>();
            if (agentMovement != null)
            {
                robotBodyTransform = agentMovement.transform;
                Debug.Log("Automatically set the robot body reference for StructureCollisionDetector");
            }
        }
        
        // Initialize the offset
        InitializeOffset();
        
        // Output debug information
        LogDebugInfo("Detector initialized");
        
        // Register with the manager
        if (CollisionDetectorManager.Instance != null)
        {
            CollisionDetectorManager.Instance.RegisterDetector(this);
        }
    }
    
    // Set the current move direction, for external use
    public void SetMoveDirection(Vector3 direction)
    {
        if (direction.magnitude > 0.001f)
        {
            currentMoveDirection = direction.normalized;
            if (debugMode)
            {
                Debug.Log($"Set move direction: {currentMoveDirection}");
            }
        }
    }
    
    // Method for debugging, prints the current state
    public void LogDebugInfo(string message = "Debug information")
    {
        if (!debugMode) return;
        
        Debug.Log($"===== Collision detector debug information [{message}] =====");
        Debug.Log($"Detector position: {transform.position}");
        Debug.Log($"Robot position: {(robotBodyTransform != null ? robotBodyTransform.position.ToString() : "Not bound")}");
        Debug.Log($"Offset: {offsetFromBody}");
        Debug.Log($"Raycast distance: {raycastDistance}");
        Debug.Log($"Current collision state: {CollideStructure}");
        Debug.Log($"Initialized: {isPositionInitialized}");
        Debug.Log($"Structure layer index: {structureLayer}");
        Debug.Log($"Disable collision detection: {disableCollisionDetection}");
        Debug.Log($"Only use raycasting: {useRaycastsOnly}");
        Debug.Log($"Current move direction: {currentMoveDirection}");
        Debug.Log("===============================");
        
        // If there is a collision, try to identify the collision object
        if (CollideStructure)
        {
            Collider[] hitColliders = Physics.OverlapSphere(transform.position, raycastDistance, 1 << structureLayer);
            Debug.Log($"Detected {hitColliders.Length} Structure layer objects around:");
            for (int i = 0; i < hitColliders.Length; i++)
            {
                Debug.Log($"  {i+1}. {hitColliders[i].gameObject.name} - Distance: {Vector3.Distance(transform.position, hitColliders[i].transform.position):F3}");
                
                // Output more information about this object
                GameObject obj = hitColliders[i].gameObject;
                Debug.Log($"     Path: {GetFullPath(obj)}");
                Debug.Log($"     Position: {obj.transform.position}");
                Debug.Log($"     Size: {(hitColliders[i] is BoxCollider ? ((BoxCollider)hitColliders[i]).size.ToString() : "Non-BoxCollider")}");
            }
        }
    }
    
    // Get the full path of the GameObject
    private string GetFullPath(GameObject obj)
    {
        string path = obj.name;
        Transform parent = obj.transform.parent;
        
        while (parent != null)
        {
            path = parent.name + "/" + path;
            parent = parent.parent;
        }
        
        return path;
    }
    
    // Initialize the offset from the robot body
    private void InitializeOffset()
    {
        if (robotBodyTransform != null && !isPositionInitialized)
        {
            // Calculate the current offset from the robot body
            offsetFromBody = transform.position - robotBodyTransform.position;
            isPositionInitialized = true;
            Debug.Log($"Initialized collision detector offset: {offsetFromBody}");
        }
    }
    
    // Update the collision detector position at the end of each frame, ensuring it remains consistent with the robot
    private void LateUpdate()
    {
        if (robotBodyTransform != null)
        {
            if (!isPositionInitialized)
            {
                InitializeOffset();
            }
            
            // Update the position based on the offset, keeping the relative position to the robot body
            transform.position = robotBodyTransform.position + offsetFromBody;
            // Keep the rotation consistent with the robot body
            transform.rotation = robotBodyTransform.rotation;
        }
        
        // If collision detection is disabled in debug mode, return directly
        if (disableCollisionDetection)
        {
            CollideStructure = false;
            return;
        }
        
        // Only perform raycasting if there is a move direction
        if (currentMoveDirection.magnitude > 0.001f)
        {
            // Directional raycasting - only detect the move direction
            CheckDirectionalObstacles(currentMoveDirection);
        }
        else
        {
            // No move direction, do not perform detection
            CollideStructure = false;
        }
    }
    
    // No longer need the Update method, all updates are done in LateUpdate
    private void Update()
    {
        // This method is empty, as all updates are done in LateUpdate
    }

    // Directional raycasting method - only Check the obstacles in the specified direction
    private void CheckDirectionalObstacles(Vector3 direction)
    {
        if (direction.magnitude < 0.001f) return;
        
        int structureLayerMask = 1 << structureLayer;
        bool obstacleDetected = false;
        
        // Clear previous collision state, only set to true when necessary
        bool hadCollision = CollideStructure;
        CollideStructure = false;
        
        // Convert the direction to the world coordinate system
        Vector3 worldDirection = transform.TransformDirection(direction.normalized);
        
        // Calculate the ray width (fan-shaped detection)
        float rayWidth = 0.4f; // Ray width coefficient
        
        // Create the main ray and the two auxiliary rays
        Vector3[] checkDirections = new Vector3[]
        {
            worldDirection, // Center ray
            Quaternion.Euler(0, rayWidth * 15, 0) * worldDirection, // Right offset ray
            Quaternion.Euler(0, -rayWidth * 15, 0) * worldDirection  // Left offset ray
        };
        
        string collisionInfo = "";
        lastDetectedObstacle = null;
        
        // Shoot rays in each direction
        foreach (Vector3 checkDir in checkDirections)
        {
            RaycastHit hit;
            if (Physics.Raycast(transform.position, checkDir, out hit, raycastDistance, structureLayerMask))
            {
                obstacleDetected = true;
                CollideStructure = true;
                lastDetectedObstacle = hit.collider.gameObject;
                
                collisionInfo = $"Detected obstacle in the move direction: {hit.collider.gameObject.name}, Distance: {hit.distance:F3}";
                
                if (showDebugRays)
                {
                    // Show collision ray (red)
                    Debug.DrawRay(transform.position, checkDir * hit.distance, Color.red, 0.5f);
                    // Draw a small sphere at the collision point
                    DebugDrawSphere(hit.point, 0.05f, Color.red, 0.5f);
                }
                
                break; // Enough to find one collision
            }
            else if (showDebugRays)
            {
                // Show the non-collision ray (green)
                Debug.DrawRay(transform.position, checkDir * raycastDistance, Color.green, 0.1f);
            }
        }
        
        // Output logs only when the collision state changes
        if (obstacleDetected && !hadCollision)
        {
            Debug.LogWarning(collisionInfo);
            Debug.LogWarning($"Collision detector position: {transform.position}, Robot position: {(robotBodyTransform != null ? robotBodyTransform.position.ToString() : "Not bound")}");
        }
        else if (!obstacleDetected && hadCollision)
        {
            Debug.Log("Raycasting: No obstacle detected in the move direction");
        }
    }
    
    // Debug method, draw a sphere
    private void DebugDrawSphere(Vector3 position, float radius, Color color, float duration)
    {
        // Draw an approximate sphere
        for (int i = 0; i < 360; i += 45)
        {
            float rad = i * Mathf.Deg2Rad;
            Vector3 dir = new Vector3(Mathf.Cos(rad), 0, Mathf.Sin(rad));
            Debug.DrawRay(position, dir * radius, color, duration);
            
            Vector3 dirUp = new Vector3(Mathf.Cos(rad), Mathf.Sin(rad), 0);
            Debug.DrawRay(position, dirUp * radius, color, duration);
        }
    }
    
    
    // Check if it is safe to move in a specified direction
    public bool IsSafeToMoveInDirection(Vector3 direction, float distance)
    {
        // If collision detection is disabled, always return safe
        if (disableCollisionDetection)
            return true;
            
        // Temporarily save the current move direction
        Vector3 savedDirection = currentMoveDirection;
        
        // Set the current direction to the detection direction
        SetMoveDirection(direction);
        
        // Convert the direction to the world coordinate system
        Vector3 worldDirection = transform.TransformDirection(direction.normalized);
        RaycastHit hit;
        int structureLayerMask = 1 << structureLayer;
        
        // Calculate the ray width (fan-shaped detection)
        float rayWidth = 0.4f; // Ray width coefficient
        
        // Create the main ray and the two auxiliary rays
        Vector3[] checkDirections = new Vector3[]
        {
            worldDirection, // Center ray
            Quaternion.Euler(0, rayWidth * 15, 0) * worldDirection, // Right offset ray
            Quaternion.Euler(0, -rayWidth * 15, 0) * worldDirection  // Left offset ray
        };
        
        bool isSafe = true;
        
        // Shoot rays in each direction
        foreach (Vector3 checkDir in checkDirections)
        {
            if (Physics.Raycast(transform.position, checkDir, out hit, distance, structureLayerMask))
            {
                Debug.LogWarning($"Move direction: {direction}, Detected obstacle: {hit.collider.gameObject.name}, Distance: {hit.distance:F3}, Collision point: {hit.point}");
                
                if (showDebugRays)
                {
                    // Show collision ray (red)
                    Debug.DrawRay(transform.position, checkDir * hit.distance, Color.red, 3.0f);
                    // Draw a small sphere at the collision point
                    DebugDrawSphere(hit.point, 0.05f, Color.red, 3.0f);
                }
                
                isSafe = false;
                break; // Enough to find one collision
            }
        }
        
        if (isSafe && showDebugRays)
        {
            // Show safe ray (green)
            Debug.DrawRay(transform.position, worldDirection * distance, Color.green, 1.0f);
        }
        
        // Restore the original move direction
        currentMoveDirection = savedDirection;
        
        return isSafe; // Return the detection result
    }
    
    // Reset position and offset, called when the robot is reloaded or the position has changed significantly
    public void ResetPosition()
    {
        if (robotBodyTransform != null)
        {
            isPositionInitialized = false;
            InitializeOffset();
            Debug.Log("Collision detector position has been reset");
            
            // Output debug information
            LogDebugInfo("Position has been reset");
        }
    }

    // Public method to clear the collision state
    public void ClearCollisionState()
    {
        CollideStructure = false;
        currentMoveDirection = Vector3.zero; // Clear the current move direction
        lastDetectedObstacle = null;
        Debug.Log("Collision detector state has been reset");
    }
    
    // Get the last detected obstacle
    public GameObject GetLastDetectedObstacle()
    {
        return lastDetectedObstacle;
    }

    private void OnCollisionEnter(Collision collision)
    {
        // If collision detection is disabled, always return safe
        if (disableCollisionDetection)
            return;
        
        if (collision.gameObject.layer == structureLayer)
        {
            CollideStructure = true;
            lastDetectedObstacle = collision.gameObject;
            Debug.Log($"Collision with Structure layer object: {collision.gameObject.name}");
        }
    }

    private void OnCollisionStay(Collision collision)
    {
        // If collision detection is disabled, always return safe
        if (useRaycastsOnly) return;
        
        if (collision.gameObject.layer == structureLayer)
        {
            CollideStructure = true;
            lastDetectedObstacle = collision.gameObject;
        }
    }

    private void OnCollisionExit(Collision collision)
    {
        // If collision detection is disabled, always return safe
        if (useRaycastsOnly) return;
        
        if (collision.gameObject.layer == structureLayer)
        {
            // Only reset the state when there are no other collisions
            bool stillColliding = false;
            
            // Check if there are still other collisions
            Collider[] hitColliders = Physics.OverlapSphere(transform.position, 0.1f);
            foreach (var hitCollider in hitColliders)
            {
                if (hitCollider.gameObject.layer == structureLayer && hitCollider.gameObject != collision.gameObject)
                {
                    stillColliding = true;
                    break;
                }
            }
            
            if (!stillColliding)
            {
                CollideStructure = false;
                lastDetectedObstacle = null;
                Debug.Log($"Collision with Structure layer object ended: {collision.gameObject.name}");
            }
        }
    }
} 