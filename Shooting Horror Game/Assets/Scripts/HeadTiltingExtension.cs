using UnityEngine;
using Cinemachine;
using UnityEngine.InputSystem;

/// <summary>
/// 카메라를 현실적으로 만들어주는 확장 (모든 vcam에 부착됨).
/// - 좌우 이동/시선 회전에 따른 기울기(롤) : 관성으로 몸이 기우는 느낌
/// - 상하 시선 관성 : 빠르게 시선을 옮기면 카메라가 아주 살짝 늦게 따라옴
/// - 호흡 흔들림 : 정지 상태에서도 미세하게 살아있는 화면
/// 모든 값은 클램프되어 급격한 마우스 입력에도 과하게 기울지 않음
/// </summary>
public class HeadTiltingExtension : CinemachineExtension
{
    [Header("Tilt (기울기)")]
    public float tiltAmount = 3.5f;        // 좌우 이동 시 몸이 기우는 정도(도)
    public float tiltSpeed = 4f;           // 기울기 반응 속도 (낮을수록 부드럽게 기울고 돌아옴)
    public float mouseRollAmount = 1.8f;   // 시선 좌우 회전 시 롤(도)
    public float maxTilt = 7f;             // 롤 총합 한계(도)

    [Header("Look Inertia (시선 관성)")]
    public float pitchLagAmount = 0.8f;    // 상하 시선 이동 시 뒤따라오는 정도(도)
    public float pitchLagMax = 2f;
    public float pitchLagSpeed = 8f;

    [Header("Breathing (호흡)")]
    public float breathePitch = 0.35f;     // 상하 호흡 진폭(도)
    public float breatheRoll = 0.2f;       // 좌우 호흡 진폭(도)
    public float breatheSpeed = 1.6f;      // 호흡 속도(rad/s)

    private float currentTilt;
    private float currentPitchLag;
    private float breathePhase;

    protected override void PostPipelineStageCallback(CinemachineVirtualCameraBase vcam, CinemachineCore.Stage stage, ref CameraState state, float deltaTime)
    {
        if (stage != CinemachineCore.Stage.Aim) return;

        float dt = Mathf.Max(deltaTime, 0.0001f);

        float mouseX = Input.GetAxisRaw("Mouse X");
        float mouseY = Input.GetAxisRaw("Mouse Y");

        InputAction moveAction = Player_Controller.moveAction;
        Vector2 move = moveAction != null ? moveAction.ReadValue<Vector2>() : Vector2.zero;

        // 1. 롤 : 시선 회전 관성 + 좌우 이동 기울기 (각각 클램프 후 합산)
        float targetTilt = Mathf.Clamp(mouseX * mouseRollAmount, -maxTilt, maxTilt)
                         + Mathf.Clamp(-move.x * tiltAmount, -tiltAmount, tiltAmount);
        targetTilt = Mathf.Clamp(targetTilt, -maxTilt, maxTilt);
        currentTilt = Mathf.Lerp(currentTilt, targetTilt, tiltSpeed * dt);

        // 2. 상하 시선 관성 : 위를 빠르게 보면 카메라가 반 박자 늦게 따라오는 느낌.
        //    입력이 멈추면 목표가 0이 되어 자연스럽게 복귀
        float targetLag = Mathf.Clamp(mouseY * pitchLagAmount, -pitchLagMax, pitchLagMax);
        currentPitchLag = Mathf.Lerp(currentPitchLag, targetLag, pitchLagSpeed * dt);

        // 3. 호흡 : 주기가 다른 두 사인파로 규칙적이지 않게. 이동 중에는 걸음 흔들림에 자연스럽게 묻힘
        breathePhase += dt * breatheSpeed;
        float breathPitch = Mathf.Sin(breathePhase) * breathePitch;
        float breathRoll = Mathf.Sin(breathePhase * 0.7f + 1.3f) * breatheRoll;

        // 기존 회전(Composer 조준 결과)에 카메라 로컬 기준으로 얹어서 다른 스테이지 결과를 보존
        state.RawOrientation = state.RawOrientation
            * Quaternion.Euler(currentPitchLag + breathPitch, 0f, currentTilt + breathRoll);
    }
}
