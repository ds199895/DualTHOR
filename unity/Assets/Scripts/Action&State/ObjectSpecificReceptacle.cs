using System.Collections;
using System.Collections.Generic;
using UnityEngine;
//1.This script manages whether specific types of objects can be placed in a container and provides a placement point.
//2.Represent whether the container is full by the full boolean variable.
//3.Provide a method to check if the object belongs to the specific type that is allowed to be placed.
public class ObjectSpecificReceptacle : MonoBehaviour
{
    [Header("Only objects of these Types can be placed on this Receptacle")]
    [SerializeField]
    private SimObjType[] SpecificTypes;//The array of object types that can be placed on this receptacle.

    [Header("Point where specified object(s) attach to this Receptacle")]

    public Transform attachPoint;//The position where the object is placed in the container.

    [Header("Is this Receptacle already holding a valid object?")]
    public bool full = false;//Boolean variable representing whether the container is full.

    //Check if the incoming object type is in the list of allowed placement types.
    public bool HasSpecificType(SimObjType check)
    {
        bool result = false;

        foreach (SimObjType sot in SpecificTypes)
        {
            if (sot == check)
            {
                result = true;
            }
        }

        return result;
    }

    // Use this for initialization
    //Check if the object has the ObjectSpecificReceptacle property.
    void Start()
    {
#if UNITY_EDITOR
        if (
            !gameObject
                .GetComponent<SimObjPhysics>()
                .HasSecondaryProperty(
                    SimObjSecondaryProperty.ObjectSpecificReceptacle
                )
        )
        {
            Debug.LogError(
                this.name + " is missing the Secondary Property ObjectSpecificReceptacle!"
            );
        }
#endif
    }
}
