using System.Collections.Generic;
using UnityEngine;

public class Break : MonoBehaviour, IUniqueStateManager
{
    [SerializeField]
    private string prefabToSwapTo; // 破碎后替换的预制体
    [SerializeField]
    private float mass;
    [SerializeField]
    private float impulseThreshold; // 破碎需要的阈值
    [SerializeField]
    private float squeezeThreshold; // 捏的力度阈值
    [SerializeField]
    private float highFrictionImpulseOffset = 2.0f;// 高摩擦区域下的冲击力阈值偏移
    private float minImpulseThreshold; // 最小冲击力阈值
    private float maxImpulseThreshold; // 最大冲击力阈值
    [SerializeField]
    private float fallImpactForce ; // 下落时受到的冲击力
    [SerializeField]
    private float currentSqueezeForce; // 当前捏的力度

    private SceneManager sceneManager;

    [SerializeField]
    private bool broken; // 是否已经破碎
    [SerializeField]
    private bool isUnbreakable; // 是否不可破碎
    [SerializeField]
    private bool isReadyToBreak = true; // 是否准备好破碎

    // 不会导致其他物体破碎的物体类型列表
    //todo：后续还需要精简里面的物体类型
    private static readonly HashSet<SimObjType> TooSmallOrSoftToBreakOtherObjects = new()
    {
        SimObjType.TeddyBear,
        SimObjType.Pillow,
        SimObjType.Cloth,
        SimObjType.Bed,
        SimObjType.Bread,
        SimObjType.BreadSliced,
        SimObjType.Egg,
        SimObjType.EggShell,
        SimObjType.Omelette,
        SimObjType.EggCracked,
        SimObjType.LettuceSliced,
        SimObjType.TissueBox,
        SimObjType.Newspaper,
        SimObjType.TissueBoxEmpty,
        SimObjType.CreditCard,
        SimObjType.ToiletPaper,
        SimObjType.ToiletPaperRoll,
        SimObjType.SoapBar,
        SimObjType.Pen,
        SimObjType.Pencil,
        SimObjType.Towel,
        SimObjType.Watch,
        SimObjType.DishSponge,
        SimObjType.Tissue,
        SimObjType.CD,
        SimObjType.HandTowel
    };

    public void SaveState(ObjectState objectState)
    {
        objectState.breakState = new BreakState
        {
            isReadyToBreak = isReadyToBreak,
            broken = broken,
            isUnbreakable = isUnbreakable,
        };
    }

    public void LoadState(ObjectState objectState)
    {
        isReadyToBreak = objectState.breakState.isReadyToBreak;
        broken = objectState.breakState.broken;
        isUnbreakable = objectState.breakState.isUnbreakable;
    }


    public bool Broken => broken;

    private void Start()
    {
#if UNITY_EDITOR
        PropertyValidator.ValidateProperty(gameObject, SimObjSecondaryProperty.CanBreak);
#endif

        minImpulseThreshold = impulseThreshold;
        maxImpulseThreshold = impulseThreshold + highFrictionImpulseOffset;

        if (!TryGetComponent<Rigidbody>(out var rb)) return;
        
        // 计算冲击力
        fallImpactForce = CalculateFallImpactForce(rb);
        mass= rb.mass;
        sceneManager = GameObject.Find("SceneManager").GetComponent<SceneManager>();
    }

    //private void Update()
    //{
    //    // 处理捏的输入，假定使用Input.GetButton()方法来模拟捏的力度。
    //    // 这里需要根据实际输入系统进行调整
    //    if (Input.GetKeyDown(KeyCode.B))
    //    {
    //        // 假设通过某种方式获取捏的力度，这里使用0-1范围的值作为示例
    //        float squeezeValue = GetSqueezeInput(); // 你需要实现这个方法
    //        //print(squeezeValue);
    //        // 根据捏的力度设置当前捏力
    //        currentSqueezeForce = squeezeValue * 10.0f; // 可根据需要调整缩放因子
    //        if(currentSqueezeForce > squeezeThreshold)
    //        {
    //            if (isReadyToBreak)
    //            {
    //                isReadyToBreak = false;
    //                BreakObject();
    //            }
    //        }
    //    }
    //    //else
    //    //{
    //    //    currentSqueezeForce = 0;
    //    //}
    //}

    public float CalculateFallImpactForce(Rigidbody rb)
    {
        float gravity = Physics.gravity.y; // 获取重力加速度 (通常为 -9.81)
        float height = transform.position.y / transform.parent.parent.localScale.y; // 还原高度
        // 根据高度和重力计算最终速度
        float finalVelocity = Mathf.Sqrt(-2 * gravity * height); // 注意重力是负值，所以需要取负号
        // 计算冲击力
        return (rb.mass * finalVelocity) / 0.1f; // 保持原来的调整因子
    }
    public float CalculateFallImpactForce(Rigidbody rb, Collider collider)
    {
        float gravity = Physics.gravity.y; // 获取重力加速度 (通常为 -9.81)
        float heightDifference = (transform.position.y - collider.bounds.max.y)/ transform.parent.parent.localScale.y; // 获取碰撞体下边界的y值
        // 根据高度和重力计算最终速度
        float finalVelocity = Mathf.Sqrt(-2 * gravity * heightDifference);
        // 计算冲击力
        return (rb.mass * finalVelocity) / 0.1f; // 保持原来的调整因子
    }
    public float CalculateCollisionImpactForce(Collision col)
    {
        // 获取碰撞的相对速度
        Vector3 collisionVelocity = col.relativeVelocity;

        // 计算冲击力，使用物体的质量和碰撞时的速度
        float impactForce =mass * collisionVelocity.magnitude; // 使用速度的大小

        return impactForce; // 返回计算出的冲击力
    }

    private void OnCollisionEnter(Collision col)
    {
        //col.impulse.magnitude单位为牛顿/秒，N/s，表示一秒改变了多少动量
        float impactForce = CalculateCollisionImpactForce(col);
        //print("impactForce: " + impactForce);
        //float impactForce = CalculateFallImpactForce(GetComponent<Rigidbody>(), col.collider);
        //print("impactForce: " + impactForce);
        if (isUnbreakable || impactForce <= impulseThreshold) return;
        //if (isUnbreakable || fallImpactForce  <= impulseThreshold) return;

        //如果碰撞物体在 TooSmalOrSoftToBreakOtherObjects 列表中，直接返回
        SimObjPhysics collidedObject = col.transform.GetComponentInParent<SimObjPhysics>();
        if (collidedObject != null && TooSmallOrSoftToBreakOtherObjects.Contains(collidedObject.Type))
        {
            return;
        }

        if (isReadyToBreak)
        {
            isReadyToBreak = false;
            BreakObject();
        }
    }

    private void OnCollisionStay(Collision collision)
    {
        if (collision.gameObject.CompareTag("Robot")&& currentSqueezeForce > squeezeThreshold)
        {
            print("OnCollisionStay");

            if (isReadyToBreak)
                {
                    isReadyToBreak = false;
                    BreakObject();
                }
        }
    }
    public void BreakObject()
    {
        Rigidbody rb = gameObject.GetComponent<Rigidbody>();
        //实例化新的预制体，并将其设置为当前物体的位置和旋转
        if (!sceneManager.SimObjectsDict.TryGetValue(prefabToSwapTo, out GameObject breakedObject))
        {
            return; // 如果未找到对应的对象，直接返回
        }
        breakedObject.transform.SetPositionAndRotation(transform.position, transform.rotation);
        breakedObject.SetActive(true);
        Breakdown breakdown = breakedObject.GetComponent<Breakdown>();
        breakdown.StartBreak();
        broken = true;

        //将新物体的刚体速度和角速度设置为当前物体的速度和角速度
        //为什么设为0.4f？
        foreach (Rigidbody subRb in breakedObject.GetComponentsInChildren<Rigidbody>())
        {
            subRb.velocity = rb.velocity * 0.4f;
            subRb.angularVelocity = rb.angularVelocity * 0.4f;
        }
        gameObject.SetActive(false);
    }

    //当物体进入高摩擦区域时，增加冲击力阈值
    public void OnTriggerEnter(Collider other)
    {
        if (other.CompareTag("HighFriction"))
        {
            impulseThreshold = maxImpulseThreshold;
            
        }
    }
   
    public void OnTriggerExit(Collider other)
    {
        if (other.CompareTag("HighFriction"))
        {
            impulseThreshold = minImpulseThreshold;

        }
    }

    private float GetSqueezeInput()
    {

        return 1; // 只是示例，替换为真实捏力度获取逻辑
    }
}
