using UnityEngine;
using Cinemachine;
using UnityEngine.InputSystem;

public class HeadTiltingExtension : CinemachineExtension
{
    public float tiltAmount = 2.5f;
    public float tiltSpeed = 5f;

    private float currentTilt = 0f;

    InputAction moveAction;

    protected override void PostPipelineStageCallback(CinemachineVirtualCameraBase vcam, CinemachineCore.Stage stage, ref CameraState state, float deltaTime)
    {
        moveAction = Player_Controller.moveAction;

        if (stage == CinemachineCore.Stage.Aim)
        {
            float inputX = Input.GetAxisRaw("Mouse X");
            float moveX = moveAction.ReadValue<Vector2>().x;

            float targetTilt = inputX * tiltAmount + -moveX * tiltAmount;

            currentTilt = Mathf.Lerp(currentTilt, targetTilt, tiltSpeed * deltaTime);

            state.RawOrientation = Quaternion.Euler(state.RawOrientation.eulerAngles.x, state.RawOrientation.eulerAngles.y, currentTilt);
        }
    }
}
