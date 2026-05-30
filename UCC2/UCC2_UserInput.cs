using UnityEngine;

public class UCC2_UserInput : UCC2_ExposedInput
{
    [Header("Binds")]
    public KeyCode moveForward  = KeyCode.W;
    public KeyCode moveBackward = KeyCode.S;
    public KeyCode moveLeft     = KeyCode.A;
    public KeyCode moveRight    = KeyCode.D;

    public KeyCode runKey    = KeyCode.LeftShift;
    public KeyCode jumpKey   = KeyCode.Space;
    public KeyCode crouchKey = KeyCode.LeftControl;

    [Header("Camera")]
    public float sensitivity = 1f;

    float yRotation = 0f;
    float xRotation = 0f;

    void Update()
    {
        move   = new Vector2(Input.GetAxisRaw("Horizontal"), Input.GetAxisRaw("Vertical"));
        jump   = Input.GetKey(jumpKey);
        crouch = Input.GetKey(crouchKey);
        run    = Input.GetKey(runKey);

        float camX = Input.GetAxis("Mouse X");
        float camY = Input.GetAxis("Mouse Y");

        yRotation -= camY * sensitivity;
        yRotation  = Mathf.Clamp(yRotation, -90f, 90f);
        xRotation += camX * sensitivity;

        orientation = Quaternion.Euler(yRotation, xRotation, 0f);
    }
}
