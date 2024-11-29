using System;
using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using System.Linq;
[Serializable]
public class HighlightConfig
{
    public Color TextStrongColor; //��ʾ��Ҫ�ı�����ɫ
    public Color TextFaintColor;//�����ı��ĵ�ɫ
    public Color SoftOutlineColor;
    public float SoftOutlineThickness;
    public Color WithinReachOutlineColor;
    public float WithinReachOutlineThickness;
}
public class ObjectHightLightController : MonoBehaviour
{
    [SerializeField]
    private Transform player; // ��ɫ Transform
    [SerializeField]
    private Vector3 offset; // ��������ɫ֮���ƫ����
    [SerializeField]
    private float MinHighlightDistance = 1f; // ��С��������
    [SerializeField]
    private bool DisplayTargetText = true;//��ʾ�Ƿ���ʾĿ���ı��Ĳ���������
    [SerializeField]
    private TextMeshProUGUI TargetText;//Ŀ���ı�����������ʾ��ǰ������������ơ�
    [SerializeField]
    private TextMeshProUGUI CrosshairText;//ʮ��׼���ı�����
    [SerializeField]
    private HighlightConfig HighlightParams = new()
    {
        TextStrongColor = new Color(1.0f, 1.0f, 1.0f, 1.0f),
        TextFaintColor = new Color(197.0f / 255, 197.0f / 255, 197.0f / 255, 228.0f / 255),//�ϵ��Ļ�ɫ��ͬʱ������͸���ȣ������ʾЧ���ǰ�͸���ĵ���ɫ��
        //����ɫ�����нϵ͵�͸���ȣ��ʺ��ڴ�����͵�����Ч��
        SoftOutlineColor = new Color(0.66f, 0.66f, 0.66f, 0.1f),
        //������������������ĺ�ȡ���ȷ�����Ӿ��ϲ��Եù���ͻأ��������͵ذ������ı���Χ
        SoftOutlineThickness = 0.001f,
        //
        WithinReachOutlineColor = new Color(1, 1, 1, 0.3f),
        //
        WithinReachOutlineThickness = 0.005f,
    };//�����������ã�������ɫ�ͺ�ȡ�
    [SerializeField]
    private Camera m_Camera; // ��������
    [SerializeField]
    private GameObject hand; // �ֳ��������
    //[SerializeField]
    private SimObjPhysics highlightedObject; // ��������
    private void Start()
    {
        //m_Camera = Camera.main;
        //hand = GameObject.Find("Hand");
        TargetText.text="";

    }
    void Update()
    {
        UpdateHighlightedObject();
        //MouseControls();
        HandleMouseControls();
    }


    void LateUpdate()
    {
        // ���������λ��
        transform.position = player.position + offset;
    }


    private void UpdateHighlightedObject()
    {
        // ��ȡ���������㷢��������
        Ray ray = m_Camera.ViewportPointToRay(new Vector3(0.5f, 0.5f));

        // layerMask���������߼��Ĳ㼶���롣
        int layerMask = LayerMask.GetMask(
            "SimObjVisible"
        );

        // ����������Ƿ�����ײ���ཻ
        if (Physics.Raycast(ray, out RaycastHit hit, MinHighlightDistance, layerMask))
        {
            HandleHitObject(hit);
        }
        // �����⵽�����嵫��������С��������
        else if (Physics.Raycast(ray, out RaycastHit outerHit, float.MaxValue, layerMask))
        {
            SetTargetText(outerHit.transform.CompareTag("Interactable") ? outerHit.transform.GetComponent<SimObjPhysics>().ObjectID : "", false);
           
        }
        // ��û����������ʱ�����Ŀ���ı�
        else
        {
            //ClearTargetText();
            ClearTargetText(ray);
         
        }
    }

    private void HandleHitObject(RaycastHit hit)
    {
        Debug.DrawLine(hit.point, m_Camera.transform.position, Color.red);
        //print(hit.transform.name);//����������ȵ�����
        if (hit.transform.CompareTag("Interactable"))
        {
            if (hit.transform.TryGetComponent<SimObjPhysics>(out SimObjPhysics simObj))
            {
                bool withinReach = simObj.PrimaryProperty == SimObjPrimaryProperty.CanPickup ||
                                   simObj.SecondaryProperties.Any(prop => prop == SimObjSecondaryProperty.CanToggleOnOff || prop == SimObjSecondaryProperty.CanOpen);

                SetTargetText(simObj.ObjectID, withinReach);
                highlightedObject = withinReach ? simObj : null;
            }
        }
        else
        {
            ClearTargetText();
        }
    }

    private void ClearTargetText(Ray ray)
    {
        SetTargetText("", false);
        Debug.DrawLine(ray.origin, ray.origin + ray.direction * MinHighlightDistance, Color.blue);
        highlightedObject = null;
    }

    private void ClearTargetText() => SetTargetText("", false);

    public void HandleMouseControls()
    {
        if (Input.GetKeyDown(KeyCode.Mouse0))
        {
            if (!isHoldingObject)
            {
                TryPickOrInteractObject();
            }
            else
            {
                ReleaseHeldObject();
            }
        }
    }

    private bool isHoldingObject = false; // ���ڸ����Ƿ�ץ������
    private SimObjPhysics heldObject = null; // ���ڱ��浱ǰץס������
    private Rigidbody heldObjectRigidbody = null; // ���ڱ���ץס����ĸ���
    private void TryPickOrInteractObject()
    {
        if (highlightedObject == null) return;

        if (highlightedObject.SecondaryProperties.Contains(SimObjSecondaryProperty.CanToggleOnOff))
        {
            highlightedObject.GetComponent<CanToggleOnOff>()?.Toggle();
            return;
        }
        else if (highlightedObject.SecondaryProperties.Contains(SimObjSecondaryProperty.CanOpen))
        {
            highlightedObject.GetComponent<CanOpen_Object>()?.Interact();
            return;
        }

        heldObject = highlightedObject;
        if (heldObject.TryGetComponent<Rigidbody>(out heldObjectRigidbody))
        {
            heldObjectRigidbody.isKinematic = true;
            heldObject.transform.SetParent(hand.transform);
            //heldObject.transform.localPosition = Vector3.zero;
            heldObject.transform.localPosition = new Vector3(0, 5f, 0);

        }
        isHoldingObject = true;
    }

    private void ReleaseHeldObject()
    {
        if (heldObjectRigidbody != null)
        {
            heldObjectRigidbody.isKinematic = false;
        }

        heldObject.transform.SetParent(GameObject.Find("Objects").transform);
        heldObject = null;
        heldObjectRigidbody = null;
        isHoldingObject = false;
    }


    //����Ŀ���ı������ݶ����Ƿ��ڿɽ�����Χ���л��ı���ɫ��
    //text��Ŀ���ı������ݡ�
    //withinReach����ʾ�����Ƿ��ڿɽ�����Χ�ڵĲ���������Ĭ��Ϊ false��
    
    private void SetTargetText(string text, bool withinReach = false)
    {
        if (withinReach)
        {
            TargetText.color = HighlightParams.TextStrongColor;
            CrosshairText.text = "( + )";
        }
        else
        {
            TargetText.color = (Math.Abs(TargetText.color.a - HighlightParams.TextStrongColor.a) < 1e-5) ? HighlightParams.TextFaintColor : TargetText.color;
            CrosshairText.text = "+";
        }

        //�Ա�ֻ��ʾ�������͵����ƣ������������Ķ��� ID
        if (DisplayTargetText)
        {
            TargetText.text = text.Split('|')[0];
        }
    }



    

}
