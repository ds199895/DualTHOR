using UnityEngine;

public class PlayerMovement : MonoBehaviour
{
    public float moveSpeed = 5.0f; // 移动速度
    public float mouseSensitivity = 2.0f; // 鼠标灵敏度
    public float verticalRotationLimit = 80.0f; // 垂直旋转限制

    private CharacterController characterController;
    private float rotationY = 0f; // 垂直旋转角度
    private float rotationX = 0f; // 水平旋转角度
    private bool isCursorLocked = true; // 光标锁定状态

    void Start()
    {
        characterController = GetComponent<CharacterController>();
        Cursor.lockState = CursorLockMode.Locked; // 初始状态锁定光标
    }

    void Update()
    {
        // 检测是否按下“~”键进行光标锁定状态的切换
        if (Input.GetKeyDown(KeyCode.BackQuote)) // KeyCode.BackQuote 是“~”键的对应键值
        {
            if (isCursorLocked)
            {
                Cursor.lockState = CursorLockMode.None; // 解锁光标
                isCursorLocked = false; // 更新状态
            }
            else
            {
                Cursor.lockState = CursorLockMode.Locked; // 锁定光标
                isCursorLocked = true; // 更新状态
            }
        }

        // 如果光标被锁定，则允许移动
        if (isCursorLocked)
        {
            // 获取输入
            float moveX = Input.GetAxis("Horizontal");
            float moveZ = Input.GetAxis("Vertical");

            // 创建移动向量
            Vector3 move = transform.right * moveX + transform.forward * moveZ;

            // 移动角色
            characterController.Move(moveSpeed * Time.deltaTime * move);
            if (Input.GetKey(KeyCode.LeftShift))
            {
                // 奔跑
                characterController.Move(1.5f * moveSpeed * Time.deltaTime * move);
            }

            // 获取鼠标输入
            float mouseX = Input.GetAxis("Mouse X") * mouseSensitivity;
            float mouseY = Input.GetAxis("Mouse Y") * mouseSensitivity;

            // 移动角色的水平旋转
            rotationX += mouseX;
            transform.eulerAngles = new Vector3(0, rotationX, 0);

            // 垂直旋转，限制旋转角度
            rotationY -= mouseY;
            rotationY = Mathf.Clamp(rotationY, -verticalRotationLimit, verticalRotationLimit);

            // 应用到摄像机的旋转
            Camera.main.transform.localEulerAngles = new Vector3(rotationY, 0, 0);
        }
    }
}
