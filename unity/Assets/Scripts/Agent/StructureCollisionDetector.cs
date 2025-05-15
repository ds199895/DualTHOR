using UnityEngine;

public class StructureCollisionDetector : MonoBehaviour
{
    public bool CollideStructure { get; private set; }
    private int structureLayer;
    
    // 射线检测相关参数
    public float raycastDistance = 0.3f;  // 默认射线长度，临时减小为0.3
    public bool showDebugRays = true;     // 是否显示调试射线
    private Transform robotRoot;          // 机器人根节点
    
    // 调试选项
    public bool debugMode = true;         // 是否启用额外调试信息
    public bool disableCollisionDetection = false; // 临时禁用碰撞检测
    public bool useRaycastsOnly = true;  // 仅使用射线检测，忽略物理碰撞
    
    // 用于跟踪机器人主体的引用
    public Transform robotBodyTransform;
    private Vector3 offsetFromBody;
    private bool isPositionInitialized = false;
    
    // 当前移动方向，用于定向射线检测
    private Vector3 currentMoveDirection = Vector3.zero;
    
    // 定义射线检测方向数组 (前、后、左、右、前左、前右、后左、后右)
    private Vector3[] rayDirections = new Vector3[] {
        Vector3.forward,
        Vector3.back,
        Vector3.left,
        Vector3.right,
        new Vector3(1, 0, 1).normalized,   // 前右
        new Vector3(-1, 0, 1).normalized,  // 前左
        new Vector3(1, 0, -1).normalized,  // 后右
        new Vector3(-1, 0, -1).normalized  // 后左
    };
    
    // 存储上次检测到碰撞的物体
    private GameObject lastDetectedObstacle = null;

    private void OnEnable()
    {
        // 向管理器注册此检测器
        if (CollisionDetectorManager.Instance != null)
        {
            CollisionDetectorManager.Instance.RegisterDetector(this);
        }
    }
    
    private void OnDisable()
    {
        // 从管理器注销此检测器
        if (CollisionDetectorManager.Instance != null)
        {
            CollisionDetectorManager.Instance.UnregisterDetector(this);
        }
    }

    private void Start()
    {
        // 获取structure层的索引
        structureLayer = LayerMask.NameToLayer("Structure");
        CollideStructure = false;
        robotRoot = transform.root;
        
        // 查找机器人主体
        if (robotBodyTransform == null)
        {
            // 尝试获取AgentMovement组件所在的Transform作为机器人主体
            AgentMovement agentMovement = FindObjectOfType<AgentMovement>();
            if (agentMovement != null)
            {
                robotBodyTransform = agentMovement.transform;
                Debug.Log("自动设置StructureCollisionDetector的机器人主体引用");
            }
        }
        
        // 初始化偏移量
        InitializeOffset();
        
        // 输出调试信息
        LogDebugInfo("检测器已初始化");
        
        // 向管理器注册
        if (CollisionDetectorManager.Instance != null)
        {
            CollisionDetectorManager.Instance.RegisterDetector(this);
        }
    }
    
    // 设置当前移动方向，供外部调用
    public void SetMoveDirection(Vector3 direction)
    {
        if (direction.magnitude > 0.001f)
        {
            currentMoveDirection = direction.normalized;
            if (debugMode)
            {
                Debug.Log($"设置移动方向: {currentMoveDirection}");
            }
        }
    }
    
    // 用于调试的方法，打印当前状态
    public void LogDebugInfo(string message = "调试信息")
    {
        if (!debugMode) return;
        
        Debug.Log($"===== 碰撞检测器调试信息 [{message}] =====");
        Debug.Log($"检测器位置: {transform.position}");
        Debug.Log($"机器人位置: {(robotBodyTransform != null ? robotBodyTransform.position.ToString() : "未绑定")}");
        Debug.Log($"位置偏移量: {offsetFromBody}");
        Debug.Log($"射线检测距离: {raycastDistance}");
        Debug.Log($"当前碰撞状态: {CollideStructure}");
        Debug.Log($"已初始化偏移: {isPositionInitialized}");
        Debug.Log($"Structure层索引: {structureLayer}");
        Debug.Log($"禁用碰撞检测: {disableCollisionDetection}");
        Debug.Log($"仅使用射线检测: {useRaycastsOnly}");
        Debug.Log($"当前移动方向: {currentMoveDirection}");
        Debug.Log("===============================");
        
        // 如果有碰撞，尝试识别碰撞物体
        if (CollideStructure)
        {
            Collider[] hitColliders = Physics.OverlapSphere(transform.position, raycastDistance, 1 << structureLayer);
            Debug.Log($"周围检测到 {hitColliders.Length} 个Structure层物体:");
            for (int i = 0; i < hitColliders.Length; i++)
            {
                Debug.Log($"  {i+1}. {hitColliders[i].gameObject.name} - 距离: {Vector3.Distance(transform.position, hitColliders[i].transform.position):F3}");
                
                // 输出关于这个对象的更多信息
                GameObject obj = hitColliders[i].gameObject;
                Debug.Log($"    路径: {GetFullPath(obj)}");
                Debug.Log($"    位置: {obj.transform.position}");
                Debug.Log($"    大小: {(hitColliders[i] is BoxCollider ? ((BoxCollider)hitColliders[i]).size.ToString() : "非BoxCollider")}");
            }
        }
    }
    
    // 获取GameObject的完整路径
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
    
    // 初始化与机器人主体的偏移量
    private void InitializeOffset()
    {
        if (robotBodyTransform != null && !isPositionInitialized)
        {
            // 计算当前与机器人主体的偏移量
            offsetFromBody = transform.position - robotBodyTransform.position;
            isPositionInitialized = true;
            Debug.Log($"初始化碰撞检测器偏移量: {offsetFromBody}");
        }
    }
    
    // 在每帧末尾更新碰撞检测器位置，确保与机器人保持一致
    private void LateUpdate()
    {
        if (robotBodyTransform != null)
        {
            if (!isPositionInitialized)
            {
                InitializeOffset();
            }
            
            // 根据偏移量更新位置，保持与机器人主体的相对位置
            transform.position = robotBodyTransform.position + offsetFromBody;
            // 保持旋转与机器人主体一致
            transform.rotation = robotBodyTransform.rotation;
        }
        
        // 如果调试模式下禁用了碰撞检测，则直接返回
        if (disableCollisionDetection)
        {
            CollideStructure = false;
            return;
        }
        
        // 只有在设置了移动方向时才进行射线检测
        if (currentMoveDirection.magnitude > 0.001f)
        {
            // 定向射线检测 - 只检测移动方向
            CheckDirectionalObstacles(currentMoveDirection);
        }
        else
        {
            // 未设置方向时不进行检测
            CollideStructure = false;
        }
    }
    
    // 不再需要Update方法，所有更新都在LateUpdate中完成
    private void Update()
    {
        // 此方法为空，因为我们在LateUpdate中进行所有更新
    }

    // 定向射线检测方法 - 只在指定方向发射射线
    private void CheckDirectionalObstacles(Vector3 direction)
    {
        if (direction.magnitude < 0.001f) return;
        
        int structureLayerMask = 1 << structureLayer;
        bool obstacleDetected = false;
        
        // 清除之前的碰撞状态，只在必要时设置为true
        bool hadCollision = CollideStructure;
        CollideStructure = false;
        
        // 将方向转换为世界坐标系
        Vector3 worldDirection = transform.TransformDirection(direction.normalized);
        
        // 计算射线宽度（扇形检测）
        float rayWidth = 0.4f; // 射线宽度系数 
        
        // 创建主射线和两侧辅助射线
        Vector3[] checkDirections = new Vector3[]
        {
            worldDirection, // 中心射线
            Quaternion.Euler(0, rayWidth * 15, 0) * worldDirection, // 右偏射线
            Quaternion.Euler(0, -rayWidth * 15, 0) * worldDirection  // 左偏射线
        };
        
        string collisionInfo = "";
        lastDetectedObstacle = null;
        
        // 对每个方向发射射线
        foreach (Vector3 checkDir in checkDirections)
        {
            RaycastHit hit;
            if (Physics.Raycast(transform.position, checkDir, out hit, raycastDistance, structureLayerMask))
            {
                obstacleDetected = true;
                CollideStructure = true;
                lastDetectedObstacle = hit.collider.gameObject;
                
                collisionInfo = $"移动方向上检测到障碍物: {hit.collider.gameObject.name}, 距离: {hit.distance:F3}";
                
                if (showDebugRays)
                {
                    // 显示碰撞射线(红色)
                    Debug.DrawRay(transform.position, checkDir * hit.distance, Color.red, 0.5f);
                    // 在碰撞点绘制小球体
                    DebugDrawSphere(hit.point, 0.05f, Color.red, 0.5f);
                }
                
                break; // 找到一个碰撞就足够了
            }
            else if (showDebugRays)
            {
                // 显示未碰撞射线(绿色)
                Debug.DrawRay(transform.position, checkDir * raycastDistance, Color.green, 0.1f);
            }
        }
        
        // 只在碰撞状态变化时输出日志
        if (obstacleDetected && !hadCollision)
        {
            Debug.LogWarning(collisionInfo);
            Debug.LogWarning($"碰撞检测器位置: {transform.position}, 机器人位置: {(robotBodyTransform != null ? robotBodyTransform.position.ToString() : "未绑定")}");
        }
        else if (!obstacleDetected && hadCollision)
        {
            Debug.Log("射线检测: 移动方向上不再检测到障碍物");
        }
    }
    
    // 调试用方法，绘制球体
    private void DebugDrawSphere(Vector3 position, float radius, Color color, float duration)
    {
        // 绘制一个近似球体
        for (int i = 0; i < 360; i += 45)
        {
            float rad = i * Mathf.Deg2Rad;
            Vector3 dir = new Vector3(Mathf.Cos(rad), 0, Mathf.Sin(rad));
            Debug.DrawRay(position, dir * radius, color, duration);
            
            Vector3 dirUp = new Vector3(Mathf.Cos(rad), Mathf.Sin(rad), 0);
            Debug.DrawRay(position, dirUp * radius, color, duration);
        }
    }
    
    // 射线检测方法，检查机器人周围是否有障碍物 - 不再使用此方法
    private void CheckForObstaclesWithRaycasts()
    {
        // 此方法不再使用，改为使用定向射线检测
    }
    
    // 根据指定方向检查是否安全可移动
    public bool IsSafeToMoveInDirection(Vector3 direction, float distance)
    {
        // 如果禁用了碰撞检测，总是返回安全
        if (disableCollisionDetection)
            return true;
            
        // 临时保存当前移动方向
        Vector3 savedDirection = currentMoveDirection;
        
        // 设置当前方向为检测方向
        SetMoveDirection(direction);
        
        // 将方向转换为世界坐标系
        Vector3 worldDirection = transform.TransformDirection(direction.normalized);
        RaycastHit hit;
        int structureLayerMask = 1 << structureLayer;
        
        // 计算射线宽度（扇形检测）
        float rayWidth = 0.4f; // 射线宽度系数
        
        // 创建主射线和两侧辅助射线
        Vector3[] checkDirections = new Vector3[]
        {
            worldDirection, // 中心射线
            Quaternion.Euler(0, rayWidth * 15, 0) * worldDirection, // 右偏射线
            Quaternion.Euler(0, -rayWidth * 15, 0) * worldDirection  // 左偏射线
        };
        
        bool isSafe = true;
        
        // 对每个方向发射射线
        foreach (Vector3 checkDir in checkDirections)
        {
            if (Physics.Raycast(transform.position, checkDir, out hit, distance, structureLayerMask))
            {
                Debug.LogWarning($"移动方向: {direction}, 检测到障碍物: {hit.collider.gameObject.name}, 距离: {hit.distance:F3}, 碰撞点: {hit.point}");
                
                if (showDebugRays)
                {
                    // 显示碰撞射线(红色)
                    Debug.DrawRay(transform.position, checkDir * hit.distance, Color.red, 3.0f);
                    // 在碰撞点绘制小球体
                    DebugDrawSphere(hit.point, 0.05f, Color.red, 3.0f);
                }
                
                isSafe = false;
                break; // 找到一个碰撞就足够了
            }
        }
        
        if (isSafe && showDebugRays)
        {
            // 显示安全射线(绿色)
            Debug.DrawRay(transform.position, worldDirection * distance, Color.green, 1.0f);
        }
        
        // 恢复原来的移动方向
        currentMoveDirection = savedDirection;
        
        return isSafe; // 返回检测结果
    }
    
    // 重置位置和偏移量，用于机器人重新加载或位置发生大变化时调用
    public void ResetPosition()
    {
        if (robotBodyTransform != null)
        {
            isPositionInitialized = false;
            InitializeOffset();
            Debug.Log("碰撞检测器位置已重置");
            
            // 输出调试信息
            LogDebugInfo("位置重置");
        }
    }

    // 清理碰撞状态的公共方法
    public void ClearCollisionState()
    {
        CollideStructure = false;
        currentMoveDirection = Vector3.zero; // 清空当前移动方向
        lastDetectedObstacle = null;
        Debug.Log("碰撞检测器状态已重置");
    }
    
    // 获取最后检测到的障碍物
    public GameObject GetLastDetectedObstacle()
    {
        return lastDetectedObstacle;
    }

    private void OnCollisionEnter(Collision collision)
    {
        // 如果启用了仅使用射线检测，则忽略物理碰撞
        if (useRaycastsOnly) return;
        
        if (collision.gameObject.layer == structureLayer)
        {
            CollideStructure = true;
            lastDetectedObstacle = collision.gameObject;
            Debug.Log($"与Structure层物体发生物理碰撞: {collision.gameObject.name}");
        }
    }

    private void OnCollisionStay(Collision collision)
    {
        // 如果启用了仅使用射线检测，则忽略物理碰撞
        if (useRaycastsOnly) return;
        
        if (collision.gameObject.layer == structureLayer)
        {
            CollideStructure = true;
            lastDetectedObstacle = collision.gameObject;
        }
    }

    private void OnCollisionExit(Collision collision)
    {
        // 如果启用了仅使用射线检测，则忽略物理碰撞
        if (useRaycastsOnly) return;
        
        if (collision.gameObject.layer == structureLayer)
        {
            // 只有当没有其他碰撞时才重置状态
            bool stillColliding = false;
            
            // 检查是否仍有其他碰撞
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
                Debug.Log($"与Structure层物体结束碰撞: {collision.gameObject.name}");
            }
        }
    }
} 