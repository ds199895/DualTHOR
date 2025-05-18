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
    private Transform player; // Character Transform
    [SerializeField]
    private Vector3 offset; // Camera offset from the character
    [SerializeField]
    private float MinHighlightDistance = 1f; // Minimum highlight distance
    [SerializeField]
    private bool DisplayTargetText = true;//Whether to display the target text

    [SerializeField]
    private HighlightConfig HighlightParams = new()
    {
        TextStrongColor = new Color(1.0f, 1.0f, 1.0f, 1.0f),
        TextFaintColor = new Color(197.0f / 255, 197.0f / 255, 197.0f / 255, 228.0f / 255),//较淡的灰色，同时设置了透明度，因此显示效果是半透明的淡灰色。
        //light gray, with lower transparency, suitable for creating a soft outline effect
        SoftOutlineColor = new Color(0.66f, 0.66f, 0.66f, 0.1f),
        //The parameter defines the thickness of the soft outline. To ensure that it does not appear too obtrusive, but rather soft and wrapped around the text.
        SoftOutlineThickness = 0.001f,
        //
        WithinReachOutlineColor = new Color(1, 1, 1, 0.3f),
        //
        WithinReachOutlineThickness = 0.005f,
    };//Highlight parameter configuration, including color and thickness.
    [SerializeField]
    private Camera m_Camera; // Camera component
    [SerializeField]
    private GameObject hand; // Hand object
    //[SerializeField]
    private SimObjPhysics highlightedObject; // Highlighted object
    private void Start()
    {

    }
    void Update()
    {
        UpdateHighlightedObject();
        //MouseControls();
        HandleMouseControls();
    }


    void LateUpdate()
    {
        // Set camera position
        transform.position = player.position + offset;
    }


    private void UpdateHighlightedObject()
    {
        // Get the ray from the center of the camera
        Ray ray = m_Camera.ViewportPointToRay(new Vector3(0.5f, 0.5f));

        // layerMask：level mask for ray detection.
        int layerMask = LayerMask.GetMask(
            "SimObjVisible"
        );

    }

    private void HandleHitObject(RaycastHit hit)
    {
        Debug.DrawLine(hit.point, m_Camera.transform.position, Color.red);
        //print(hit.transform.name);//output the name of the object's ancestor
        if (hit.transform.CompareTag("Interactable"))
        {
            if (hit.transform.TryGetComponent<SimObjPhysics>(out SimObjPhysics simObj))
            {
                bool withinReach = simObj.PrimaryProperty == SimObjPrimaryProperty.CanPickup ||
                                   simObj.SecondaryProperties.Any(prop => prop == SimObjSecondaryProperty.CanToggleOnOff || prop == SimObjSecondaryProperty.CanOpen);

                // SetTargetText(simObj.ObjectID, withinReach);
                highlightedObject = withinReach ? simObj : null;
            }
        }
        // else
        // {
        //     ClearTargetText();
        // }
    }

    // private void ClearTargetText(Ray ray)
    // {
    //     SetTargetText("", false);
    //     Debug.DrawLine(ray.origin, ray.origin + ray.direction * MinHighlightDistance, Color.blue);
    //     highlightedObject = null;
    // }

    // private void ClearTargetText() => SetTargetText("", false);

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

    private bool isHoldingObject = false; // Used to track whether an object is being held
    private SimObjPhysics heldObject = null; // Used to save the current object being held
    private Rigidbody heldObjectRigidbody = null; // Used to save the rigidbody of the held object
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
            heldObject.transform.localPosition = new Vector3(0, 0f, 0);

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



    

}
