using System.Collections;
using System.Collections.Generic;
using System.Security.Claims;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;

public class Camera_Controller : MonoBehaviour
{
    [Header("Transform")]
    [SerializeField] private Transform character;

    [Header("Force")]
    public float sensitivity = 2;
    public float smoothing = 1.5f;

    [Header("PostProcess")]
    public VolumeProfile primaryProfile;
    ChromaticAberration chromatic;
    Vignette vignette;

    [Header("Velocity")]
    Vector2 velocity;
    Vector2 frameVelocity;

    [Header("ETC")]
    public static Camera mainCam;

    void Start()
    {
        mainCam = Camera.main;

        primaryProfile.TryGet(out vignette);
        primaryProfile.TryGet(out chromatic);
    }

    void Update()
    {
        Control();
        Aim();
        ChromaControl();
    }

    private void Control()
    {
        Vector2 mouseDelta = new Vector2(Input.GetAxisRaw("Mouse X"), Input.GetAxisRaw("Mouse Y"));
        Vector2 rawFrameVelocity = Vector2.Scale(mouseDelta, Vector2.one * sensitivity);
        frameVelocity = Vector2.Lerp(frameVelocity, rawFrameVelocity, 1 / smoothing);
        velocity += frameVelocity;
        velocity.y = Mathf.Clamp(velocity.y, -60, 60);

        transform.localRotation = Quaternion.AngleAxis(-velocity.y, Vector3.forward);
        character.localRotation = Quaternion.AngleAxis(velocity.x, Vector3.up);
    }

    private void Aim()
    {
        if (Input.GetKey(Player_Shot.aimKey) && !Player_Shot.isReload)
        {
            Player_Shot.isAim = true;
            mainCam.fieldOfView = Mathf.Lerp(mainCam.fieldOfView, 32.5f, 0.025f);
            vignette.smoothness.value = Mathf.Lerp(vignette.smoothness.value, 0.45f, 0.025f);
        }
        else
        {
            Player_Shot.isAim = false;
            mainCam.fieldOfView = Mathf.Lerp(mainCam.fieldOfView, 60f, 0.025f);
            vignette.smoothness.value = Mathf.Lerp(vignette.smoothness.value, 0.2f, 0.025f);
        }
    }

    private void ChromaControl()
    {
        if (Player_Move.isSprint)
        {
            chromatic.intensity.value = Mathf.Lerp(chromatic.intensity.value, 0.5f, 0.025f);
        }
        else
        {
            chromatic.intensity.value = Mathf.Lerp(chromatic.intensity.value, 0.25f, 0.025f);
        }
    }

    public static void ShotFoV()
    {
        mainCam.fieldOfView = Mathf.Lerp(mainCam.fieldOfView, mainCam.fieldOfView - 15f, 0.25f);
    }
}