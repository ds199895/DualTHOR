using UnityEngine;
using System.Collections;

namespace Agent
{
    public class KeyboardMovementController : MonoBehaviour
    {
        [Header("移动设置")]
        public float moveSpeed = 5.0f;        // 平移速度
        public float rotateSpeed = 90.0f;     // 旋转速度（度/秒）
        public float moveDuration = 0.5f;     // 平滑移动的持续时间

        [Header("控制键")]
        [Tooltip("按住此键可以加速移动")]
        public KeyCode sprintKey = KeyCode.LeftShift;
        [Tooltip("加速倍率")]
        public float sprintMultiplier = 2.0f;

        // 关节控制
        public ArticulationBody rootArt;      // 机器人根部关节体

        // 私有变量
        private Transform cameraTransform;    // 相机变换组件引用
        private bool isMoving = false;        // 是否正在移动
        private Coroutine currentMoveCoroutine; // 当前移动协程

        private void Start()
        {
            // 如果没有指定rootArt，尝试在当前对象或子对象上查找
            if (rootArt == null)
            {
                rootArt = GetComponent<ArticulationBody>();
                if (rootArt == null)
                {
                    rootArt = GetComponentInChildren<ArticulationBody>();
                }
            }
            
            // 获取当前主相机的变换组件
            cameraTransform = Camera.main.transform;
        }

        private void Update()
        {
            // 处理旋转控制（WASD键）
            HandleRotation();
            
            // 处理平移控制（方向键）
            HandleTranslation();
        }

        private void HandleRotation()
        {
            // 获取WASD输入
            float rotateH = 0f;
            float rotateV = 0f;

            // 检查A和D键，控制Y轴旋转（左右）
            if (Input.GetKey(KeyCode.A))
                rotateH = -1f;
            else if (Input.GetKey(KeyCode.D))
                rotateH = 1f;

            // 检查W和S键，控制X轴旋转（上下）
            if (Input.GetKey(KeyCode.W))
                rotateV = 1f;
            else if (Input.GetKey(KeyCode.S))
                rotateV = -1f;

            // 如果有旋转输入
            if (rotateH != 0f || rotateV != 0f)
            {
                // 根据时间调整旋转速度
                float rotationAmount = rotateSpeed * Time.deltaTime;
                
                // 保存当前旋转
                Quaternion currentRotation = transform.rotation;
                
                // 应用旋转
                if (rotateH != 0f)
                {
                    transform.Rotate(Vector3.up, rotateH * rotationAmount);
                }
                
                if (rotateV != 0f)
                {
                    transform.Rotate(Vector3.right, rotateV * rotationAmount);
                }
                
                // 如果有根关节体，同步旋转
                if (rootArt != null)
                {
                    rootArt.TeleportRoot(transform.position, transform.rotation);
                }
            }
        }

        private void HandleTranslation()
        {
            // 如果正在移动，不接受新的移动输入
            if (isMoving)
                return;
                
            // 获取方向键输入
            float moveH = 0f;
            float moveV = 0f;

            // 检查方向键输入
            if (Input.GetKey(KeyCode.RightArrow))
                moveH = 1f;
            else if (Input.GetKey(KeyCode.LeftArrow))
                moveH = -1f;

            if (Input.GetKey(KeyCode.UpArrow))
                moveV = 1f;
            else if (Input.GetKey(KeyCode.DownArrow))
                moveV = -1f;

            // 如果有平移输入
            if (moveH != 0f || moveV != 0f)
            {
                // 计算当前速度
                float currentSpeed = moveSpeed;
                
                // 如果按下加速键，应用加速倍率
                if (Input.GetKey(sprintKey))
                {
                    currentSpeed *= sprintMultiplier;
                }
                
                // 确定移动方向（局部坐标系）
                Vector3 localDirection = Vector3.zero;
                
                if (moveH != 0f)
                    localDirection += Vector3.right * moveH;
                    
                if (moveV != 0f)
                    localDirection += Vector3.forward * moveV;
                
                // 归一化方向向量
                if (localDirection.magnitude > 0)
                    localDirection.Normalize();
                
                // 计算移动幅度
                float magnitude = currentSpeed * Time.deltaTime * 10f; // 乘以10使得移动更明显
                
                // 启动平滑移动协程
                if (currentMoveCoroutine != null)
                    StopCoroutine(currentMoveCoroutine);
                    
                currentMoveCoroutine = StartCoroutine(SmoothMove(localDirection, magnitude, moveDuration));
            }
        }

        private IEnumerator SmoothMove(Vector3 localDirection, float magnitude, float duration)
        {
            isMoving = true;

            Vector3 startPosition = transform.position;
            Vector3 targetPosition = startPosition + transform.TransformDirection(localDirection) * magnitude;
            
            Quaternion originRot = transform.rotation;
            float elapsedTime = 0f;

            while (elapsedTime < duration)
            {
                Vector3 pos_temp = Vector3.Lerp(startPosition, targetPosition, elapsedTime / duration);

                if (rootArt != null)
                {
                    rootArt.TeleportRoot(pos_temp, originRot);
                }
                
                transform.position = pos_temp;
                elapsedTime += Time.deltaTime;
                yield return null;
            }

            // 确保到达目标位置
            if (rootArt != null)
            {
                rootArt.TeleportRoot(targetPosition, originRot);
            }
            
            transform.position = targetPosition;
            isMoving = false;
        }
    }
}