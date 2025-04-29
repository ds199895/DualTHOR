

using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using Random = UnityEngine.Random;
using DG.Tweening;

//用于管理游戏对象的填充状态
public class Spill : MonoBehaviour, IUniqueStateManager, IStateComponent
{
    [SerializeField]
    public bool isSpilled = false;

    public void SpillObject()
    {
        // 获取所有子对象
        Transform[] children = GetComponentsInChildren<Transform>();
        
        // 遍历所有子对象
        foreach (Transform child in children)
        {
            // 获取子对象的 Rigidbody 组件
            // Rigidbody rb = child.GetComponent<Rigidbody>();
            
            // // 如果子对象有 Rigidbody 组件，则添加爆炸力
            // if (rb != null)
            // {
            //     // 添加爆炸力
            //     rb.AddExplosionForce(explosionForce, explosionPosition, explosionRadius, upwardsModifier, explosionMode);
            // }
        }   
    }   

    public void SaveState(ObjectState objectState)
    {
        objectState.spillState = new SpillState
        {
            isSpilled = isSpilled,

        };
    }

    public void LoadState(ObjectState objectState)
    {
        if (objectState.spillState != null)
        {
            isSpilled = objectState.spillState.isSpilled;
        }
    }


    public void Execute()
    {
        SpillObject();
    }
}
