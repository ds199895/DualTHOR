using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public abstract class IKBase : MonoBehaviour
{
    public abstract void ProcessTargetPosition(Vector3 newTargetPosition, bool isLeftArm);
}
