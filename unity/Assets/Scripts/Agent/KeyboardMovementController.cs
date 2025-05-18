using UnityEngine;
using System.Collections;

namespace Agent
{
    public class KeyboardMovementController : MonoBehaviour
    {
        [Header("Movement settings")]
        public float moveSpeed = 5.0f;        // translation speed
        public float rotateSpeed = 90.0f;     // rotation speed (degrees/second)
        public float moveDuration = 0.5f;     // smooth move duration

        [Header("Control keys")]
        [Tooltip("Press this key to accelerate movement")]
        public KeyCode sprintKey = KeyCode.LeftShift;
        [Tooltip("Acceleration multiplier")]
        public float sprintMultiplier = 2.0f;

        // joint control
        public ArticulationBody rootArt;      // robot root joint body

        // private variables
        private Transform cameraTransform;    // camera transform component reference
        private bool isMoving = false;        // whether moving
        private Coroutine currentMoveCoroutine; // current move coroutine

        private void Start()
        {
            // if rootArt is not specified, try to find it on the current object or its children
            if (rootArt == null)
            {
                rootArt = GetComponent<ArticulationBody>();
                if (rootArt == null)
                {
                    rootArt = GetComponentInChildren<ArticulationBody>();
                }
            }
            
            // get the main camera transform component
            cameraTransform = Camera.main.transform;
        }

        private void Update()
        {
            // handle rotation control (WASD keys)
            HandleRotation();
            
            // handle translation control (direction keys)
            HandleTranslation();
        }

        private void HandleRotation()
        {
            // get WASD input
            float rotateH = 0f;
            float rotateV = 0f;

            // check A and D keys, control Y axis rotation (left and right)
            if (Input.GetKey(KeyCode.A))
                rotateH = -1f;
            else if (Input.GetKey(KeyCode.D))
                rotateH = 1f;

            // check W and S keys, control X axis rotation (up and down)
            if (Input.GetKey(KeyCode.W))
                rotateV = 1f;
            else if (Input.GetKey(KeyCode.S))
                rotateV = -1f;

            // if there is rotation input
            if (rotateH != 0f || rotateV != 0f)
            {
                // adjust rotation speed according to time
                float rotationAmount = rotateSpeed * Time.deltaTime;
                
                // save current rotation
                Quaternion currentRotation = transform.rotation;
                
                // apply rotation
                if (rotateH != 0f)
                {
                    transform.Rotate(Vector3.up, rotateH * rotationAmount);
                }
                
                if (rotateV != 0f)
                {
                    transform.Rotate(Vector3.right, rotateV * rotationAmount);
                }
                
                // if there is root joint body, synchronize rotation
                if (rootArt != null)
                {
                    rootArt.TeleportRoot(transform.position, transform.rotation);
                }
            }
        }

        private void HandleTranslation()
        {
            // if moving, do not accept new movement input
            if (isMoving)
                return;
                
            // get direction key input
            float moveH = 0f;
            float moveV = 0f;

            // check direction key input
            if (Input.GetKey(KeyCode.RightArrow))
                moveH = 1f;
            else if (Input.GetKey(KeyCode.LeftArrow))
                moveH = -1f;

            if (Input.GetKey(KeyCode.UpArrow))
                moveV = 1f;
            else if (Input.GetKey(KeyCode.DownArrow))
                moveV = -1f;

            // if there is translation input
            if (moveH != 0f || moveV != 0f)
            {
                // calculate current speed
                float currentSpeed = moveSpeed;
                
                // if sprint key is pressed, apply sprint multiplier
                if (Input.GetKey(sprintKey))
                {
                    currentSpeed *= sprintMultiplier;
                }
                
                // determine the moving direction (local coordinate system)
                Vector3 localDirection = Vector3.zero;
                
                if (moveH != 0f)
                    localDirection += Vector3.right * moveH;
                    
                if (moveV != 0f)
                    localDirection += Vector3.forward * moveV;
                
                // normalize the direction vector
                if (localDirection.magnitude > 0)
                    localDirection.Normalize();
                
                // calculate the moving magnitude
                float magnitude = currentSpeed * Time.deltaTime * 10f; // multiply by 10 to make the movement more obvious
                
                // start smooth move coroutine
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

            // ensure reaching the target position
            if (rootArt != null)
            {
                rootArt.TeleportRoot(targetPosition, originRot);
            }
            
            transform.position = targetPosition;
            isMoving = false;
        }
    }
}