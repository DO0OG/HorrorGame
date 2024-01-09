using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Camera_Follow : MonoBehaviour
{
    [Header("Transform")]
    [SerializeField] private Transform character;

    [Header("Force")]
    public float sensitivity = 2;
    public float smoothing = 1.5f;

    [Header("Velocity")]
    Vector2 velocity;
    Vector2 frameVelocity;

    void Start()
    {
        // Lock the mouse cursor to the game screen.
        Cursor.lockState = CursorLockMode.Locked;
    }

    void Update()
    {
        Vector2 mouseDelta = new Vector2(Input.GetAxisRaw("Mouse X"), Input.GetAxisRaw("Mouse Y"));
        Vector2 rawFrameVelocity = Vector2.Scale(mouseDelta, Vector2.one * sensitivity);
        frameVelocity = Vector2.Lerp(frameVelocity, rawFrameVelocity, 1 / smoothing);
        velocity += frameVelocity;
        velocity.y = Mathf.Clamp(velocity.y, -35, 60);

        transform.localRotation = Quaternion.AngleAxis(-velocity.y, Vector3.forward);
        character.localRotation = Quaternion.AngleAxis(velocity.x, Vector3.up);
    }
}