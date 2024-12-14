using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[System.Serializable]
public class SwapObjList
{
    // reference to game object that needs to have materials changed
    [Header("Object That Needs Mat Swaps")]
    [SerializeField]
    public GameObject MyObject;

    // copy the Materials array on MyObject's Renderer component here
    [Header("Materials for On state")]
    [SerializeField]
    public Material[] OnMaterials;

    // swap to this array of materials when off, usually just one or two materials will change
    [Header("Materials for Off state")]
    [SerializeField]
    public Material[] OffMaterials;
}
