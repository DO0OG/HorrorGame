using Cinemachine;
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
    [SerializeField] private float sensitivity = 2;
    [SerializeField] private float smoothing = 1.5f;

    [Header("PostProcess")]
    [SerializeField] private VolumeProfile primaryProfile;
    ChromaticAberration chromatic;
    Vignette vignette;

    [Header("Velocity")]
    private Vector2 velocity;
    private Vector2 frameVelocity;

    [Header("ETC")]
    private static CinemachineVirtualCamera virtualCamera;
    private CinemachineBasicMultiChannelPerlin noise;
    private CinemachineComposer composer;

    void Start()
    {
        virtualCamera = GameObject.Find("Virtual Camera").GetComponent<CinemachineVirtualCamera>();
        noise = virtualCamera.GetCinemachineComponent<CinemachineBasicMultiChannelPerlin>();
        composer = virtualCamera.GetCinemachineComponent<CinemachineComposer>();

        primaryProfile.TryGet(out vignette);
        primaryProfile.TryGet(out chromatic);
    }

    void Update()
    {
        MouseControl();
        Control();
        ChromaControl();
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

    private void Control()
    {
        if (Input.GetKey(Player_Shot.aimKey) && !Player_Shot.isReload)
        {
            Player_Shot.isAim = true;
            virtualCamera.m_Lens.FieldOfView = Mathf.Lerp(virtualCamera.m_Lens.FieldOfView, 32.5f, 0.1f);
            noise.m_FrequencyGain = Mathf.Lerp(noise.m_FrequencyGain, 0.55f, 0.1f);
            vignette.smoothness.value = Mathf.Lerp(vignette.smoothness.value, 0.45f, 0.025f);
        }
        else
        {
            Player_Shot.isAim = false;
            virtualCamera.m_Lens.FieldOfView = Mathf.Lerp(virtualCamera.m_Lens.FieldOfView, 60f, 0.1f);
            noise.m_AmplitudeGain = Mathf.Lerp(noise.m_AmplitudeGain, 2f, 0.1f);
            vignette.smoothness.value = Mathf.Lerp(vignette.smoothness.value, 0.2f, 0.025f);
        }

        if (Player_Move.isSprint)
        {
            noise.m_AmplitudeGain = Mathf.Lerp(noise.m_AmplitudeGain, 5f, 0.1f);
            noise.m_FrequencyGain = Mathf.Lerp(noise.m_FrequencyGain, 3f, 0.1f);
            composer.m_DeadZoneWidth = Mathf.Lerp(composer.m_DeadZoneWidth, 0, 0.1f);
            composer.m_DeadZoneHeight = Mathf.Lerp(composer.m_DeadZoneHeight, 0, 0.1f);
        }
        else if (!Player_Move.isSprint)
        {
            noise.m_FrequencyGain = Mathf.Lerp(noise.m_FrequencyGain, 1f, 0.1f);
            composer.m_DeadZoneWidth = Mathf.Lerp(composer.m_DeadZoneWidth, 0.25f, 0.1f);
            composer.m_DeadZoneHeight = Mathf.Lerp(composer.m_DeadZoneHeight, 0.25f, 0.1f);
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

    internal static void ShotFoV()
    {
        virtualCamera.m_Lens.FieldOfView = Mathf.Lerp(virtualCamera.m_Lens.FieldOfView, virtualCamera.m_Lens.FieldOfView - 15f, 0.25f);
    }
}