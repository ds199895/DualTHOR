using UnityEngine;


using System;
public class CollisionHandler : MonoBehaviour
{
    public event Action<Collision> OnCollisionEnterEvent;

    private void OnCollisionEnter(Collision collision)
    {
        Debug.Log("Trigger Collider");
        OnCollisionEnterEvent?.Invoke(collision);
    }
}