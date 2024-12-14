<<<<<<< HEAD
using UnityEngine;

public class OpenDoor : MonoBehaviour
{
    [SerializeField] 
    private Transform door; // �����ŵ� Transform�������壩
    [SerializeField] 
    private float rotationAngle = 90f; // ����ʱ��ת�ĽǶ�
    [SerializeField]
    private bool isOpen = false; // �ŵĵ�ǰ״̬

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
            // ��ת��
            door.Rotate(Vector3.up, rotationAngle); // Χ�� Y ����ת
            isOpen = true;
        }
    }

    private void CloseTheDoor()
    {
        if (isOpen)
        {
            // ��ת�Ż�ȥ
            door.Rotate(Vector3.up, -rotationAngle); // Χ�� Y ����ת
            isOpen = false;
        }
    }
}
=======
using UnityEngine;

public class OpenDoor : MonoBehaviour
{
    [SerializeField] 
    private Transform door; // �����ŵ� Transform�������壩
    [SerializeField] 
    private float rotationAngle = 90f; // ����ʱ��ת�ĽǶ�
    [SerializeField]
    private bool isOpen = false; // �ŵĵ�ǰ״̬

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
            // ��ת��
            door.Rotate(Vector3.up, rotationAngle); // Χ�� Y ����ת
            isOpen = true;
        }
    }

    private void CloseTheDoor()
    {
        if (isOpen)
        {
            // ��ת�Ż�ȥ
            door.Rotate(Vector3.up, -rotationAngle); // Χ�� Y ����ת
            isOpen = false;
        }
    }
}
>>>>>>> 0c14a5c8d787bef23f3133ad2b2203f5035105bb
