using Cinemachine;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Rendering;

public class Camera_Controller : MonoBehaviour
{
    [Header("Transform")]
    [SerializeField] private Transform character;

    [Header("Force")]
    [SerializeField] private float sensitivity = 2;
    [SerializeField] private float smoothing = 1.5f;

    [Header("PostProcess")]
    [SerializeField] private VolumeProfile primaryProfile;

    [Header("Velocity")]
    private Vector2 velocity;
    private Vector2 frameVelocity;

    void Update()
    {
        MouseControl();
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
}