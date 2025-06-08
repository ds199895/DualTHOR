

using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using Random = UnityEngine.Random;
using DG.Tweening;

//using for managing the overflow state of game objects
public class Spill : MonoBehaviour, IUniqueStateManager, IStateComponent
{
    [SerializeField]
    public bool isSpilled = false;

    public void SpillObject()
    {
        // get all child objects
        Transform[] children = GetComponentsInChildren<Transform>();
        
        // traverse all child objects
        foreach (Transform child in children)
        {
            // get the Rigidbody component of the child object
            // Rigidbody rb = child.GetComponent<Rigidbody>();
            
            // // if the child object has a Rigidbody component, then add an explosion force
            // if (rb != null)
            // {
            //     // add an explosion force
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
