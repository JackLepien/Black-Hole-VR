using UnityEngine;

public class SimpleCamera : MonoBehaviour
{
    [Header("Movement Settings")]
    public float moveSpeed = 10f;

    [Header("Rotation Settings")]
    public float mouseSensitivity = 100f;
    public float clampAngle = 85f;

    private float rotX = 0f;
    private float rotY = 0f;

    void Start()
    {
        // Vector3 euler initial setup
        Vector3 rot = transform.localRotation.eulerAngles;
        rotY = rot.y;
        rotX = rot.x;

        // Locks and hides the cursor in the game view
        Cursor.lockState = CursorLockMode.Locked;
        Cursor.visible = false;
    }

    void Update()
    {
        HandleRotation();
        HandleMovement();
    }

    void HandleMovement()
    {
        // Get keyboard input (W/S for vertical, A/D for horizontal)
        float moveH = Input.GetAxis("Horizontal");
        float moveV = Input.GetAxis("Vertical");

        // Calculate direction based on where the camera is currently facing
        Vector3 moveDirection = (transform.forward * moveV) + (transform.right * moveH);
        
        // Prevent flying up/down globally if you only want ground movement 
        // (Remove this line if you want true 3D flying/spectator movement)
        moveDirection.y = 0f; 

        // Apply movement over time
        transform.position += moveDirection.normalized * moveSpeed * Time.deltaTime;
    }

    void HandleRotation()
    {
        // Get mouse movement input
        float mouseX = Input.GetAxis("Mouse X");
        float mouseY = Input.GetAxis("Mouse Y");

        // Accumulate rotation values
        rotY += mouseX * mouseSensitivity * Time.deltaTime;
        rotX -= mouseY * mouseSensitivity * Time.deltaTime; // Inverted so moving mouse up looks up

        // Clamp the vertical lookup to avoid flipping upside down
        rotX = Mathf.Clamp(rotX, -clampAngle, clampAngle);

        // Apply rotation to the camera
        transform.rotation = Quaternion.Euler(rotX, rotY, 0.0f);
    }
}
