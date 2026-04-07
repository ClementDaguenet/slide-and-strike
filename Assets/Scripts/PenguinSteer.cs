using UnityEngine;
using UnityEngine.InputSystem;

[RequireComponent(typeof(Rigidbody))]
public class PenguinSteer : MonoBehaviour
{
    [SerializeField] float turnSpeedDegPerSec = 130f;
    [SerializeField] float lateralAcceleration = 34f;

    Rigidbody _rb;

    void Awake()
    {
        _rb = GetComponent<Rigidbody>();
    }

    static float ReadSteerInput()
    {
        var k = Keyboard.current;
        if (k != null)
        {
            float h = 0f;
            if (k.leftArrowKey.isPressed || k.qKey.isPressed || k.aKey.isPressed)
                h -= 1f;
            if (k.rightArrowKey.isPressed || k.dKey.isPressed)
                h += 1f;
            if (!Mathf.Approximately(h, 0f))
                return Mathf.Clamp(h, -1f, 1f);
        }

        var pad = Gamepad.current;
        if (pad != null)
        {
            float lx = pad.leftStick.x.ReadValue();
            if (Mathf.Abs(lx) > 0.15f)
                return Mathf.Clamp(lx, -1f, 1f);
        }

        return 0f;
    }

    void FixedUpdate()
    {
        float input = ReadSteerInput();
        if (Mathf.Abs(input) < 0.01f)
            return;

        float yaw = input * turnSpeedDegPerSec * Time.fixedDeltaTime;
        _rb.MoveRotation(_rb.rotation * Quaternion.Euler(0f, yaw, 0f));

        Vector3 flatRight = transform.right;
        flatRight.y = 0f;
        if (flatRight.sqrMagnitude < 1e-6f)
            return;
        flatRight.Normalize();
        _rb.AddForce(flatRight * (input * lateralAcceleration), ForceMode.Acceleration);
    }
}
