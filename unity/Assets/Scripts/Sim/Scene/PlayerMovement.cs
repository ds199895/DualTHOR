using UnityEngine;

public class PlayerMovement : MonoBehaviour
{
    public float moveSpeed = 5.0f; // �ƶ��ٶ�
    public float mouseSensitivity = 2.0f; // ���������
    public float verticalRotationLimit = 80.0f; // ��ֱ��ת����

    private CharacterController characterController;
    private float rotationY = 0f; // ��ֱ��ת�Ƕ�
    private float rotationX = 0f; // ˮƽ��ת�Ƕ�
    private bool isCursorLocked = true; // �������״̬

    void Start()
    {
        characterController = GetComponent<CharacterController>();
        Cursor.lockState = CursorLockMode.Locked; // ��ʼ״̬�������
    }

    void Update()
    {
        // ����Ƿ��¡�~�������й������״̬���л�
        if (Input.GetKeyDown(KeyCode.BackQuote)) // KeyCode.BackQuote �ǡ�~�����Ķ�Ӧ��ֵ
        {
            if (isCursorLocked)
            {
                Cursor.lockState = CursorLockMode.None; // �������
                isCursorLocked = false; // ����״̬
            }
            else
            {
                Cursor.lockState = CursorLockMode.Locked; // �������
                isCursorLocked = true; // ����״̬
            }
        }

        // �����걻�������������ƶ�
        if (isCursorLocked)
        {
            // ��ȡ����
            float moveX = Input.GetAxis("Horizontal");
            float moveZ = Input.GetAxis("Vertical");

            // �����ƶ�����
            Vector3 move = transform.right * moveX + transform.forward * moveZ;

            // �ƶ���ɫ
            characterController.Move(moveSpeed * Time.deltaTime * move);
            if (Input.GetKey(KeyCode.LeftShift))
            {
                // ����
                characterController.Move(1.5f * moveSpeed * Time.deltaTime * move);
            }

            // ��ȡ�������
            float mouseX = Input.GetAxis("Mouse X") * mouseSensitivity;
            float mouseY = Input.GetAxis("Mouse Y") * mouseSensitivity;

            // �ƶ���ɫ��ˮƽ��ת
            rotationX += mouseX;
            transform.eulerAngles = new Vector3(0, rotationX, 0);

            // ��ֱ��ת��������ת�Ƕ�
            rotationY -= mouseY;
            rotationY = Mathf.Clamp(rotationY, -verticalRotationLimit, verticalRotationLimit);

            // Ӧ�õ����������ת
            Camera.main.transform.localEulerAngles = new Vector3(rotationY, 0, 0);
        }
    }
}
