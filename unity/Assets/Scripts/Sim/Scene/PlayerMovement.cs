using UnityEngine;

public class PlayerMovement : MonoBehaviour
{
    public float moveSpeed = 5.0f; // Movement speed
    public float mouseSensitivity = 2.0f; // Mouse sensitivity
    public float verticalRotationLimit = 80.0f; // Vertical rotation limit

    private CharacterController characterController;
    private float rotationY = 0f; // Vertical rotation angle
    private float rotationX = 0f; // Horizontal rotation angle
    private bool isCursorLocked = true; // Cursor lock state

    void Start()
    {
        characterController = GetComponent<CharacterController>();
        Cursor.lockState = CursorLockMode.Locked; // Initial state of cursor lock
    }

    void Update()
    {
            // Check if the "~" key is pressed to toggle the cursor lock state
        if (Input.GetKeyDown(KeyCode.BackQuote)) // KeyCode.BackQuote is the corresponding key value for the "~" key
        {
            if (isCursorLocked)
            {
                Cursor.lockState = CursorLockMode.None; // Unlock the cursor
                isCursorLocked = false; // Update the state
            }
            else
            {
                Cursor.lockState = CursorLockMode.Locked; // Lock the cursor
                isCursorLocked = true; // Update the state
            }
        }

        // If the cursor is locked, allow movement
        if (isCursorLocked)
        {
            // Get input
            float moveX = Input.GetAxis("Horizontal");
            float moveZ = Input.GetAxis("Vertical");

            // Create a movement vector
            Vector3 move = transform.right * moveX + transform.forward * moveZ;

            // Move the character
            characterController.Move(moveSpeed * Time.deltaTime * move);
            if (Input.GetKey(KeyCode.LeftShift))
            {
                // Run
                characterController.Move(1.5f * moveSpeed * Time.deltaTime * move);
            }

            // Get mouse input
            float mouseX = Input.GetAxis("Mouse X") * mouseSensitivity;
            float mouseY = Input.GetAxis("Mouse Y") * mouseSensitivity;

            // Rotate the character horizontally
            rotationX += mouseX;
            transform.eulerAngles = new Vector3(0, rotationX, 0);

            // Vertical rotation, limit the rotation angle
            rotationY -= mouseY;
            rotationY = Mathf.Clamp(rotationY, -verticalRotationLimit, verticalRotationLimit);

            // Apply to the camera rotation
            Camera.main.transform.localEulerAngles = new Vector3(rotationY, 0, 0);
        }
    }
}
