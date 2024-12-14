<<<<<<< HEAD
using UnityEngine;

public class OpenDoor : MonoBehaviour
{
    [SerializeField] 
    private Transform door; // 关联门的 Transform（父物体）
    [SerializeField] 
    private float rotationAngle = 90f; // 开门时旋转的角度
    [SerializeField]
    private bool isOpen = false; // 门的当前状态

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
            // 旋转门
            door.Rotate(Vector3.up, rotationAngle); // 围绕 Y 轴旋转
            isOpen = true;
        }
    }

    private void CloseTheDoor()
    {
        if (isOpen)
        {
            // 旋转门回去
            door.Rotate(Vector3.up, -rotationAngle); // 围绕 Y 轴旋转
            isOpen = false;
        }
    }
}
=======
using UnityEngine;

public class OpenDoor : MonoBehaviour
{
    [SerializeField] 
    private Transform door; // 关联门的 Transform（父物体）
    [SerializeField] 
    private float rotationAngle = 90f; // 开门时旋转的角度
    [SerializeField]
    private bool isOpen = false; // 门的当前状态

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
            // 旋转门
            door.Rotate(Vector3.up, rotationAngle); // 围绕 Y 轴旋转
            isOpen = true;
        }
    }

    private void CloseTheDoor()
    {
        if (isOpen)
        {
            // 旋转门回去
            door.Rotate(Vector3.up, -rotationAngle); // 围绕 Y 轴旋转
            isOpen = false;
        }
    }
}
>>>>>>> 0c14a5c8d787bef23f3133ad2b2203f5035105bb
