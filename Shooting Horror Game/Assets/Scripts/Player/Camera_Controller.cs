using Cinemachine;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Rendering;

public class Camera_Controller : MonoBehaviour
{
    [Header("KeyBind")]
    internal static KeyCode leftPeekKey = KeyCode.Q;
    internal static KeyCode rightPeekKey = KeyCode.E;

    [Header("Transform")]
    [SerializeField] private Transform character;
    public Transform origin;
    public Transform leftPeek;
    public Transform rightPeek;

    [Header("Force")]
    [SerializeField] private float sensitivity = 2;
    [SerializeField] private float smoothing = 1.5f;

    [Header("PostProcess")]
    [SerializeField] private VolumeProfile primaryProfile;

    [Header("Velocity")]
    private Vector2 velocity;
    private Vector2 frameVelocity;

    [Header("Camera")]
    public CinemachineStateDrivenCamera cineCam;

    void Update()
    {
        MouseControl();
        Peek();
    }

    private void MouseControl()
    {
        Vector2 mouseDelta = new Vector2(Input.GetAxisRaw("Mouse X"), Input.GetAxisRaw("Mouse Y"));
        Vector2 rawFrameVelocity = Vector2.Scale(mouseDelta, Vector2.one * sensitivity);
        frameVelocity = Vector2.Lerp(frameVelocity, rawFrameVelocity, 1 / smoothing);
        velocity += frameVelocity;
        velocity.y = Mathf.Clamp(velocity.y, -60, 60);

        transform.localRotation = Quaternion.AngleAxis(-velocity.y, Vector3.forward);
        character.localRotation = Quaternion.AngleAxis(velocity.x, Vector3.up);
    }

    private void Peek()
    {
        if (Input.GetKey(leftPeekKey))
            cineCam.Follow = leftPeek;
        else if (Input.GetKey(rightPeekKey))
            cineCam.Follow = rightPeek;
        else
            cineCam.Follow = origin;
    }
}