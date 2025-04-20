using UnityEngine;
using System.Collections.Generic;

/// <summary>
/// 集中管理碰撞检测器的工具类，提供检测器参数调整和调试功能
/// </summary>
public class CollisionDetectorManager : MonoBehaviour
{
    [Header("碰撞检测器配置")]
    public float defaultRayDistance = 0.3f;
    public bool showDebugRays = true;
    public bool disableCollisionDetection = false;
    
    [Header("调试设置")]
    public bool logDebugInfo = true;
    public KeyCode toggleCollisionKey = KeyCode.F9;
    public KeyCode increaseRayDistanceKey = KeyCode.F10;
    public KeyCode decreaseRayDistanceKey = KeyCode.F11;
    public KeyCode logDetectorsInfoKey = KeyCode.F12;
    
    // 存储所有活跃的检测器引用
    private List<StructureCollisionDetector> activeDetectors = new List<StructureCollisionDetector>();
    
    // 单例实例
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
        // 确保只有一个实例存在
        if (_instance != null && _instance != this)
        {
            Destroy(gameObject);
            return;
        }
        
        _instance = this;
        DontDestroyOnLoad(gameObject);
        
        Debug.Log("碰撞检测器管理器已初始化");
    }
    
    private void Update()
    {
        // 检测键盘输入，用于调试
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
    /// 注册一个新的碰撞检测器
    /// </summary>
    public void RegisterDetector(StructureCollisionDetector detector)
    {
        if (!activeDetectors.Contains(detector))
        {
            activeDetectors.Add(detector);
            
            // 应用默认设置
            detector.raycastDistance = defaultRayDistance;
            detector.showDebugRays = showDebugRays;
            detector.disableCollisionDetection = disableCollisionDetection;
            detector.debugMode = logDebugInfo;
            
            Debug.Log($"已注册新的碰撞检测器，当前共有 {activeDetectors.Count} 个检测器");
        }
    }
    
    /// <summary>
    /// 注销一个碰撞检测器
    /// </summary>
    public void UnregisterDetector(StructureCollisionDetector detector)
    {
        if (activeDetectors.Contains(detector))
        {
            activeDetectors.Remove(detector);
            Debug.Log($"已注销碰撞检测器，剩余 {activeDetectors.Count} 个检测器");
        }
    }
    
    /// <summary>
    /// 切换所有碰撞检测器的启用状态
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
        
        Debug.Log($"碰撞检测已{(disableCollisionDetection ? "禁用" : "启用")}");
    }
    
    /// <summary>
    /// 调整所有碰撞检测器的射线距离
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
        
        Debug.Log($"所有碰撞检测器射线距离已调整为: {defaultRayDistance:F2}");
    }
    
    /// <summary>
    /// 设置所有检测器的碰撞检测距离
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
        
        Debug.Log($"所有碰撞检测器射线距离已设置为: {defaultRayDistance:F2}");
    }
    
    /// <summary>
    /// 输出所有检测器的详细信息
    /// </summary>
    public void LogAllDetectorsInfo()
    {
        Debug.Log($"===== 碰撞检测器管理器状态 =====");
        Debug.Log($"活跃检测器数量: {activeDetectors.Count}");
        Debug.Log($"默认射线距离: {defaultRayDistance}");
        Debug.Log($"碰撞检测状态: {(disableCollisionDetection ? "禁用" : "启用")}");
        
        // 清理无效引用
        activeDetectors.RemoveAll(d => d == null);
        
        for (int i = 0; i < activeDetectors.Count; i++)
        {
            var detector = activeDetectors[i];
            Debug.Log($"检测器 #{i+1}: 位置={detector.transform.position}, 碰撞状态={detector.CollideStructure}");
            detector.LogDebugInfo("管理器查询");
        }
    }
    
    /// <summary>
    /// 更新所有碰撞检测器的位置
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
        
        Debug.Log("已重置所有碰撞检测器位置");
    }
} 