using UnityEngine;
using UnityEngine.InputSystem;

public class cameraMovement : MonoBehaviour
{
    // Start is called once before the first execution of Update after the MonoBehaviour is created
    public float speed = 5f;
    public float sensitivity = 2f;
    public InputActionReference moveAction;
    public InputActionReference lookAction;
    public InputActionReference toggleCam;
    private Vector3 direction;
    private Vector2 lookInput;

    void Start()
    {
    }

    // Update is called once per frame
    void Update()
    {
        if (toggleCam.action.IsPressed())
        {
            Cursor.lockState = CursorLockMode.Locked;
            Cursor.visible = false;

            lookInput = lookAction.action.ReadValue<Vector2>();
            lookInput.y = Mathf.Clamp(lookInput.y, -90f, 90f);
            transform.RotateAround(transform.position, Vector3.up, lookInput.x * sensitivity);
            transform.RotateAround(transform.position, transform.right, -lookInput.y * sensitivity);
        }
        else
        {
            Cursor.lockState = CursorLockMode.None;
            Cursor.visible = true;
        }

        direction = moveAction.action.ReadValue<Vector3>();
        Vector3 WASD_direction = transform.right * direction.x + transform.forward * direction.z;
        WASD_direction.y = 0;
        WASD_direction.Normalize();
        transform.Translate((WASD_direction) * speed * Time.deltaTime, Space.World);
        transform.Translate(Vector3.up * direction.y * speed * Time.deltaTime, Space.World);         
        
    }
}
