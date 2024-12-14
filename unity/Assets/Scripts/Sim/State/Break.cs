using System.Collections.Generic;
using UnityEngine;

public class Break : MonoBehaviour, IUniqueStateManager
{
    [SerializeField]
    private string prefabToSwapTo; // ������滻��Ԥ����
    [SerializeField]
    private float mass;
    [SerializeField]
    private float impulseThreshold; // ������Ҫ����ֵ
    [SerializeField]
    private float squeezeThreshold; // ���������ֵ
    [SerializeField]
    private float highFrictionImpulseOffset = 2.0f;// ��Ħ�������µĳ������ֵƫ��
    private float minImpulseThreshold; // ��С�������ֵ
    private float maxImpulseThreshold; // ���������ֵ
    [SerializeField]
    private float fallImpactForce ; // ����ʱ�ܵ��ĳ����
    [SerializeField]
    private float currentSqueezeForce; // ��ǰ�������

    private SceneManager sceneManager;

    [SerializeField]
    private bool broken; // �Ƿ��Ѿ�����
    [SerializeField]
    private bool isUnbreakable; // �Ƿ񲻿�����
    [SerializeField]
    private bool isReadyToBreak = true; // �Ƿ�׼��������

    // ���ᵼ������������������������б�
    //todo����������Ҫ�����������������
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
        
        // ��������
        fallImpactForce = CalculateFallImpactForce(rb);
        mass= rb.mass;
        sceneManager = GameObject.Find("SceneManager").GetComponent<SceneManager>();
    }

    //private void Update()
    //{
    //    // ����������룬�ٶ�ʹ��Input.GetButton()������ģ��������ȡ�
    //    // ������Ҫ����ʵ������ϵͳ���е���
    //    if (Input.GetKeyDown(KeyCode.B))
    //    {
    //        // ����ͨ��ĳ�ַ�ʽ��ȡ������ȣ�����ʹ��0-1��Χ��ֵ��Ϊʾ��
    //        float squeezeValue = GetSqueezeInput(); // ����Ҫʵ���������
    //        //print(squeezeValue);
    //        // ��������������õ�ǰ����
    //        currentSqueezeForce = squeezeValue * 10.0f; // �ɸ�����Ҫ������������
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
        float gravity = Physics.gravity.y; // ��ȡ�������ٶ� (ͨ��Ϊ -9.81)
        float height = transform.position.y / transform.parent.parent.localScale.y; // ��ԭ�߶�
        // ���ݸ߶Ⱥ��������������ٶ�
        float finalVelocity = Mathf.Sqrt(-2 * gravity * height); // ע�������Ǹ�ֵ��������Ҫȡ����
        // ��������
        return (rb.mass * finalVelocity) / 0.1f; // ����ԭ���ĵ�������
    }
    public float CalculateFallImpactForce(Rigidbody rb, Collider collider)
    {
        float gravity = Physics.gravity.y; // ��ȡ�������ٶ� (ͨ��Ϊ -9.81)
        float heightDifference = (transform.position.y - collider.bounds.max.y)/ transform.parent.parent.localScale.y; // ��ȡ��ײ���±߽��yֵ
        // ���ݸ߶Ⱥ��������������ٶ�
        float finalVelocity = Mathf.Sqrt(-2 * gravity * heightDifference);
        // ��������
        return (rb.mass * finalVelocity) / 0.1f; // ����ԭ���ĵ�������
    }
    public float CalculateCollisionImpactForce(Collision col)
    {
        // ��ȡ��ײ������ٶ�
        Vector3 collisionVelocity = col.relativeVelocity;

        // ����������ʹ���������������ײʱ���ٶ�
        float impactForce =mass * collisionVelocity.magnitude; // ʹ���ٶȵĴ�С

        return impactForce; // ���ؼ�����ĳ����
    }

    private void OnCollisionEnter(Collision col)
    {
        //col.impulse.magnitude��λΪţ��/�룬N/s����ʾһ��ı��˶��ٶ���
        float impactForce = CalculateCollisionImpactForce(col);
        //print("impactForce: " + impactForce);
        //float impactForce = CalculateFallImpactForce(GetComponent<Rigidbody>(), col.collider);
        //print("impactForce: " + impactForce);
        if (isUnbreakable || impactForce <= impulseThreshold) return;
        //if (isUnbreakable || fallImpactForce  <= impulseThreshold) return;

        //�����ײ������ TooSmalOrSoftToBreakOtherObjects �б��У�ֱ�ӷ���
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
        //ʵ�����µ�Ԥ���壬����������Ϊ��ǰ�����λ�ú���ת
        if (!sceneManager.SimObjectsDict.TryGetValue(prefabToSwapTo, out GameObject breakedObject))
        {
            return; // ���δ�ҵ���Ӧ�Ķ���ֱ�ӷ���
        }
        breakedObject.transform.SetPositionAndRotation(transform.position, transform.rotation);
        breakedObject.SetActive(true);
        Breakdown breakdown = breakedObject.GetComponent<Breakdown>();
        breakdown.StartBreak();
        broken = true;

        //��������ĸ����ٶȺͽ��ٶ�����Ϊ��ǰ������ٶȺͽ��ٶ�
        //Ϊʲô��Ϊ0.4f��
        foreach (Rigidbody subRb in breakedObject.GetComponentsInChildren<Rigidbody>())
        {
            subRb.linearVelocity = rb.linearVelocity * 0.4f;
            subRb.angularVelocity = rb.angularVelocity * 0.4f;
        }
        gameObject.SetActive(false);
    }

    //����������Ħ������ʱ�����ӳ������ֵ
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

        return 1; // ֻ��ʾ�����滻Ϊ��ʵ�����Ȼ�ȡ�߼�
    }
}
