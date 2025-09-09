using UnityEngine;
using System.Collections.Generic;

/// <summary>
/// 集中管理碰撞检测器的工具类，提供检测器参数调整和调试功能
/// </summary>
public class CollisionDetectorManager : MonoBehaviour
{
    [Header("collision detector configuration")]
    public float defaultRayDistance = 0.3f;
    public bool showDebugRays = true;
    public bool disableCollisionDetection = false;
    
    [Header("debug settings")]
    public bool logDebugInfo = true;
    public KeyCode toggleCollisionKey = KeyCode.F9;
    public KeyCode increaseRayDistanceKey = KeyCode.F10;
    public KeyCode decreaseRayDistanceKey = KeyCode.F11;
    public KeyCode logDetectorsInfoKey = KeyCode.F12;
    
    // store all the active detector references
    private List<StructureCollisionDetector> activeDetectors = new List<StructureCollisionDetector>();
    
    // singleton instance
    private static CollisionDetectorManager _instance;
    public static CollisionDetectorManager Instance
    {
        get
        {
            if (_instance == null)
            {
                _instance = FindObjectOfType<CollisionDetectorManager>();
                if (_instance == null)
                {
                    GameObject manager = new GameObject("CollisionDetectorManager");
                    _instance = manager.AddComponent<CollisionDetectorManager>();
                    DontDestroyOnLoad(manager);
                }
            }
            return _instance;
        }
    }
    
    private void Awake()
    {
        // ensure only one instance exists
        if (_instance != null && _instance != this)
        {
            Destroy(gameObject);
            return;
        }
        
        _instance = this;
        DontDestroyOnLoad(gameObject);
        
        Debug.Log("collision detector manager initialized");
    }
    
    private void Update()
    {
        // check the keyboard input, for debugging
        if (Input.GetKeyDown(toggleCollisionKey))
        {
            ToggleCollisionDetection();
        }
        
        if (Input.GetKeyDown(increaseRayDistanceKey))
        {
            AdjustRayDistance(0.1f);
        }
        
        if (Input.GetKeyDown(decreaseRayDistanceKey))
        {
            AdjustRayDistance(-0.1f);
        }
        
        if (Input.GetKeyDown(logDetectorsInfoKey))
        {
            LogAllDetectorsInfo();
        }
    }
    
    /// <summary>
    /// register a new collision detector
    /// </summary>
    public void RegisterDetector(StructureCollisionDetector detector)
    {
        if (!activeDetectors.Contains(detector))
        {
            activeDetectors.Add(detector);
            
            // apply the default settings
            detector.raycastDistance = defaultRayDistance;
            detector.showDebugRays = showDebugRays;
            detector.disableCollisionDetection = disableCollisionDetection;
            detector.debugMode = logDebugInfo;
            
            Debug.Log($"registered a new collision detector, currently there are {activeDetectors.Count} detectors");
        }
    }
    
    /// <summary>
    /// unregister a collision detector
    /// </summary>
    public void UnregisterDetector(StructureCollisionDetector detector)
    {
        if (activeDetectors.Contains(detector))
        {
            activeDetectors.Remove(detector);
            Debug.Log($"unregistered a collision detector, currently there are {activeDetectors.Count} detectors");
        }
    }
    
    /// <summary>
    /// toggle the enable state of all collision detectors
    /// </summary>
    public void ToggleCollisionDetection()
    {
        disableCollisionDetection = !disableCollisionDetection;
        
        foreach (var detector in activeDetectors)
        {
            if (detector != null)
            {
                detector.disableCollisionDetection = disableCollisionDetection;
            }
        }
        
        Debug.Log($"collision detection is {(disableCollisionDetection ? "disabled" : "enabled")}");
    }
    
    /// <summary>
    /// adjust the ray distance of all collision detectors
    /// </summary>
    public void AdjustRayDistance(float delta)
    {
        defaultRayDistance = Mathf.Max(0.1f, defaultRayDistance + delta);
        
        foreach (var detector in activeDetectors)
        {
            if (detector != null)
            {
                detector.raycastDistance = defaultRayDistance;
            }
        }
        
        Debug.Log($"all collision detectors ray distance adjusted: {defaultRayDistance:F2}");
    }
    
    /// <summary>
    /// set the collision detection distance of all detectors
    /// </summary>
    public void SetRayDistance(float distance)
    {
        defaultRayDistance = Mathf.Max(0.1f, distance);
        
        foreach (var detector in activeDetectors)
        {
            if (detector != null)
            {
                detector.raycastDistance = defaultRayDistance;
            }
        }
        
        Debug.Log($"all collision detectors ray distance set to: {defaultRayDistance:F2}");
    }
    
    /// <summary>
    /// output the detailed information of all detectors
    /// </summary>
    public void LogAllDetectorsInfo()
    {
        Debug.Log($"===== collision detector manager status =====");
        Debug.Log($"active detectors count: {activeDetectors.Count}");
        Debug.Log($"default ray distance: {defaultRayDistance}");
        Debug.Log($"collision detection status: {(disableCollisionDetection ? "disabled" : "enabled")}");
        
        // clean the invalid references
        activeDetectors.RemoveAll(d => d == null);
        
        for (int i = 0; i < activeDetectors.Count; i++)
        {
            var detector = activeDetectors[i];
            Debug.Log($"detector #{i+1}: position={detector.transform.position}, collision state={detector.CollideStructure}");
            detector.LogDebugInfo("manager query");
        }
    }
    
    /// <summary>
    /// update the position of all collision detectors
    /// </summary>
    public void ResetAllDetectors()
    {
        foreach (var detector in activeDetectors)
        {
            if (detector != null && detector.robotBodyTransform != null)
            {
                detector.ResetPosition();
            }
        }
        
        Debug.Log("all collision detectors positions reset");
    }
} 