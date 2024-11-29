using System;
using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using System.Linq;
[Serializable]
public class HighlightConfig
{
    public Color TextStrongColor; //表示主要文本的颜色
    public Color TextFaintColor;//高亮文本的淡色
    public Color SoftOutlineColor;
    public float SoftOutlineThickness;
    public Color WithinReachOutlineColor;
    public float WithinReachOutlineThickness;
}
public class ObjectHightLightController : MonoBehaviour
{
    [SerializeField]
    private Transform player; // 角色 Transform
    [SerializeField]
    private Vector3 offset; // 摄像机与角色之间的偏移量
    [SerializeField]
    private float MinHighlightDistance = 1f; // 最小高亮距离
    [SerializeField]
    private bool DisplayTargetText = true;//表示是否显示目标文本的布尔变量。
    [SerializeField]
    private TextMeshProUGUI TargetText;//目标文本对象，用于显示当前高亮对象的名称。
    [SerializeField]
    private TextMeshProUGUI CrosshairText;//十字准星文本对象。
    [SerializeField]
    private HighlightConfig HighlightParams = new()
    {
        TextStrongColor = new Color(1.0f, 1.0f, 1.0f, 1.0f),
        TextFaintColor = new Color(197.0f / 255, 197.0f / 255, 197.0f / 255, 228.0f / 255),//较淡的灰色，同时设置了透明度，因此显示效果是半透明的淡灰色。
        //淡灰色，且有较低的透明度，适合于创建柔和的轮廓效果
        SoftOutlineColor = new Color(0.66f, 0.66f, 0.66f, 0.1f),
        //参数定义了柔和轮廓的厚度。以确保在视觉上不显得过于突兀，而是柔和地包裹在文本周围
        SoftOutlineThickness = 0.001f,
        //
        WithinReachOutlineColor = new Color(1, 1, 1, 0.3f),
        //
        WithinReachOutlineThickness = 0.005f,
    };//高亮参数配置，包括颜色和厚度。
    [SerializeField]
    private Camera m_Camera; // 摄像机组件
    [SerializeField]
    private GameObject hand; // 手持物体对象
    //[SerializeField]
    private SimObjPhysics highlightedObject; // 高亮对象
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
        // 设置摄像机位置
        transform.position = player.position + offset;
    }


    private void UpdateHighlightedObject()
    {
        // 获取相机的中央点发出的射线
        Ray ray = m_Camera.ViewportPointToRay(new Vector3(0.5f, 0.5f));

        // layerMask：用于射线检测的层级掩码。
        int layerMask = LayerMask.GetMask(
            "SimObjVisible"
        );

        // 检测与射线是否与碰撞体相交
        if (Physics.Raycast(ray, out RaycastHit hit, MinHighlightDistance, layerMask))
        {
            HandleHitObject(hit);
        }
        // 如果检测到了物体但超出了最小高亮距离
        else if (Physics.Raycast(ray, out RaycastHit outerHit, float.MaxValue, layerMask))
        {
            SetTargetText(outerHit.transform.CompareTag("Interactable") ? outerHit.transform.GetComponent<SimObjPhysics>().ObjectID : "", false);
           
        }
        // 当没有命中物体时，清空目标文本
        else
        {
            //ClearTargetText();
            ClearTargetText(ray);
         
        }
    }

    private void HandleHitObject(RaycastHit hit)
    {
        Debug.DrawLine(hit.point, m_Camera.transform.position, Color.red);
        //print(hit.transform.name);//输出物体祖先的名称
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

    private bool isHoldingObject = false; // 用于跟踪是否抓着物体
    private SimObjPhysics heldObject = null; // 用于保存当前抓住的物体
    private Rigidbody heldObjectRigidbody = null; // 用于保存抓住物体的刚体
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


    //设置目标文本，根据对象是否在可交互范围内切换文本颜色。
    //text：目标文本的内容。
    //withinReach：表示对象是否在可交互范围内的布尔变量，默认为 false。
    
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

        //以便只显示对象类型的名称，而不是完整的对象 ID
        if (DisplayTargetText)
        {
            TargetText.text = text.Split('|')[0];
        }
    }



    

}
