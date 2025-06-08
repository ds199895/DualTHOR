using UnityEngine;

public class OpenDoor : MonoBehaviour
{
    [SerializeField] 
    private Transform door; // Associated door Transform (parent object)
    [SerializeField] 
    private float rotationAngle = 90f; // Angle of rotation when the door is opened
    [SerializeField]
    private bool isOpen = false; // Current state of the door

    private void OnTriggerEnter(Collider other)
    {
        if (other.CompareTag("Robot"))
        {
            print("Robot entered the door");
            OpenTheDoor();
        }
    }

    private void OnTriggerExit(Collider other)
    {
        if (other.CompareTag("Robot"))
        {
            print("Robot exited the door");
            CloseTheDoor();
        }
    }

    private void OpenTheDoor()
    {
        if (!isOpen)
        {
            // Rotate the door
            door.Rotate(Vector3.up, rotationAngle); // Rotate around the Y axis
            isOpen = true;
        }
    }

    private void CloseTheDoor()
    {
        if (isOpen)
        {
            // Rotate the door back
            door.Rotate(Vector3.up, -rotationAngle); // Rotate around the Y axis
            isOpen = false;
        }
    }
}
