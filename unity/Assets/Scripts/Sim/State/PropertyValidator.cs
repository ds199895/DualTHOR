using UnityEngine;

public static class PropertyValidator
{
    public static void ValidateProperty(GameObject obj, SimObjSecondaryProperty property)
    {
        if (!obj.GetComponent<SimObjPhysics>().HasSecondaryProperty(property))
        {
            Debug.LogError($"{obj.name} is missing the {property} secondary property!");
        }
    }
}
