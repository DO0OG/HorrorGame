using UnityEngine;
using System.Collections;

public class Camera_Movement : MonoBehaviour
{
    [Header("Peeking")]
    public Transform peekLeft;
    public Transform peekRight;
    public Transform peekIdle;
    public float peekAngle = 10f; //작은 각도로 변경
    public float peekSpeed = 6f; //느린 속도로 변경
    public float peekDistance = 0.5f; //피킹 시 카메라가 이동할 거리

    [Header("Bobbing")]

    //서 있을 때의 Head bobbing
    public float idleBobbingSpeed = 3f;
    public float idleBobbingAmount = 0.0125f;
    public float idleBobbingMidpoint = 0.0f;

    //이동할 때의 Head bobbing
    public float movingBobbingSpeed = 15f;
    public float movingBobbingAmount = 0.035f;
    public float movingBobbingMidpoint = 0.0f;
    public Vector3 initialCameraPosition;
    public float shakeIntensity = 0.05f;
    public float shakeDuration = 0.1f;

    [Header("Bools")]
    [SerializeField] private bool check = false;
    [SerializeField] private bool isMoving = false;
    [SerializeField] private bool isMovingTransitioning = false;
    [SerializeField] private bool isJumping = false;
    [SerializeField] private bool isSprint;
    [SerializeField] private bool isMove;
    [SerializeField] private bool isPause;
    [SerializeField] private bool isCrouch;

    private float timer = 0.0f;

    void Start()
    {
        Cursor.visible = false;
        Cursor.lockState = CursorLockMode.Locked;
        initialCameraPosition = transform.localPosition;
    }

    void Update()
    {
        //이동 여부를 감지하여 isMoving 변수 업데이트
        if (!check && !isSprint)
        {
            if (isMove && !isMoving)
            {
                //이동 상태로 변경
                isMovingTransitioning = true;
            }
            else if (!isMove && isMoving)
            {
                //정지 상태로 변경
                isMovingTransitioning = true;
            }
        }

        // HandlePeek();
        //IdleHeadBob();
        MoveHeadBob();
    }

    private void FixedUpdate()
    {
        bool onLand = false;

        if (!isJumping && onLand)
        {
            isJumping = true;
            StartCoroutine(ShakeCamera(shakeDuration, shakeIntensity));
        }
        else if (isJumping && !onLand)
        {
            isJumping = false;
        }
    }

    public IEnumerator HitShakeCamera(float duration, float intensity, float speed = 1f)
    {
        float elapsed = 0f;
        Vector3 originalPosition = transform.localPosition;
        Quaternion originalRotation = transform.localRotation;
        float xSeed = Random.Range(-300f, 300f); //좌우 흔들림을 위한 시드
        float rotationSeed = Random.Range(-1000f, 1000f); //회전을 위한 시드

        check = true;

        while (elapsed < duration)
        {
            float progress = elapsed / duration;

            float xShake = Mathf.Lerp(0f, Mathf.PerlinNoise(xSeed, progress * speed) * 2 - 1, Mathf.SmoothStep(0f, 1f, progress));
            float rotationOffset = Mathf.Lerp(0f, Mathf.PerlinNoise(rotationSeed, progress * speed) * 2 - 1, Mathf.SmoothStep(0f, 1f, progress));

            float xOffset = xShake * intensity;
            float yOffset = Mathf.Sin(progress * Mathf.PI) * intensity + 0.25f;
            yOffset *= yOffset * 1.5f; //중력처럼 아래로만 흔들림을 강화

            transform.localPosition = originalPosition + new Vector3(xOffset, -yOffset, 0f);

            //약간의 회전
            transform.localRotation = originalRotation * Quaternion.Euler(0f, 0f, rotationOffset * intensity * 5f);

            elapsed += Time.deltaTime;
            yield return null;
        }
        check = false;
    }

    IEnumerator ShakeCamera(float duration, float intensity, float speed = 1f)
    {
        float elapsed = 0f;
        Vector3 originalPosition = transform.localPosition;
        Quaternion originalRotation = transform.localRotation;
        float xSeed = Random.Range(-300f, 300f); //좌우 흔들림을 위한 시드
        float rotationSeed = Random.Range(-1000f, 1000f); //회전을 위한 시드

        check = true;

        while (elapsed < duration)
        {
            float progress = elapsed / duration;

            float xShake = Mathf.Lerp(0f, Mathf.PerlinNoise(xSeed, progress * speed) * 2 - 1, Mathf.SmoothStep(0f, 1f, progress));
            float rotationOffset = Mathf.Lerp(0f, Mathf.PerlinNoise(rotationSeed, progress * speed) * 2 - 1, Mathf.SmoothStep(0f, 1f, progress));

            float xOffset = xShake * intensity;
            float yOffset = Mathf.Sin(progress * Mathf.PI) * intensity + 0.25f;
            yOffset *= yOffset * 1.5f; //중력처럼 아래로만 흔들림을 강화

            transform.localPosition = originalPosition + new Vector3(xOffset, -yOffset, 0f);

            //약간의 회전
            transform.localRotation = originalRotation * Quaternion.Euler(0f, 0f, rotationOffset * intensity * 5f);

            elapsed += Time.deltaTime;
            yield return null;
        }
        check = false;
    }

    void HandlePeek()
    {
        //왼쪽 벽 확인
        bool leftBlocked = Physics.Raycast(transform.position, -transform.right, peekDistance);

        //오른쪽 벽 확인
        bool rightBlocked = Physics.Raycast(transform.position, transform.right, peekDistance);

        bool isJump = false;
        bool isSprint = false;
        bool isMove = false;

        if (Input.GetKey(KeyCode.Q))
        {
            //왼쪽
            ApplyPeekTransform(peekLeft);
        }
        else if (Input.GetKey(KeyCode.E))
        {
            //오른쪽
            ApplyPeekTransform(peekRight);
        }
        else
        {
            //Idle
            ApplyPeekTransform(peekIdle);
        }
    }

    void ApplyPeekTransform(Transform peekTransform)
    {
        Vector3 peekPosition = peekTransform.position;
        Quaternion peekRotation = peekTransform.rotation;

        transform.position = Vector3.Lerp(transform.position, peekPosition, Time.deltaTime * peekSpeed);
        transform.rotation = Quaternion.Slerp(transform.rotation, peekRotation, Time.deltaTime * peekSpeed);
    }

    public void MoveHeadBob()
    {
        float isSprinting = 0f;

        float xNoise = Mathf.PerlinNoise(0f, Time.time * movingBobbingSpeed) * 2f - 1f;
        float yNoise = Mathf.PerlinNoise(1f, Time.time * movingBobbingSpeed) * 2f - 1f;

        if (isSprint && isMove && !isCrouch)
        {
            idleBobbingSpeed = 1f;
            idleBobbingAmount = 0.3f;
            movingBobbingSpeed = 3.5f;
            movingBobbingAmount = 0.45f;
        }
        else if (!isSprint && isMove && !isCrouch)
        {
            idleBobbingSpeed = 1.5f;
            idleBobbingAmount = 0.4f;
            movingBobbingSpeed = 2f;
            movingBobbingAmount = 0.45f;
        }
        else if (!isSprint && isMove && isCrouch)
        {
            idleBobbingSpeed = 1f;
            idleBobbingAmount = 0.4f;
            movingBobbingSpeed = 1.5f;
            movingBobbingAmount = 0.45f;
        }
        else if (!isSprint && !isMove && !isCrouch)
        {
            idleBobbingSpeed = 0.5f;
            idleBobbingAmount = 0.2f;
            movingBobbingSpeed = 0.5f;
            movingBobbingAmount = 0.25f;
        }

        float xBobbingAmount = (xNoise * movingBobbingAmount * isSprinting) + (xNoise * idleBobbingAmount * (1f - isSprinting)) + (xNoise * idleBobbingAmount);
        float yBobbingAmount = (yNoise * movingBobbingAmount * isSprinting) + (yNoise * idleBobbingAmount * (1f - isSprinting)) + (yNoise * idleBobbingAmount);

        Vector3 localPosition = transform.localPosition;
        localPosition.z = movingBobbingMidpoint + xBobbingAmount;
        localPosition.y = movingBobbingMidpoint + yBobbingAmount;

        transform.localPosition = Vector3.Lerp(transform.localPosition, localPosition, Time.deltaTime * movingBobbingSpeed);

        //상태 변경 시 부드럽게 전환
        if (isMovingTransitioning)
        {
            isMovingTransitioning = false;
            isMoving = !isMoving;
            if (!isMoving)
            {
                IdleHeadBob();
            }
        }
    }

    void IdleHeadBob()
    {
        float waveslice = Mathf.Sin(timer);
        bool isCrouch = false;

        Vector3 localPosition = transform.localPosition;

        //앉아있을 경우 더 차분하게(?) 흔듦
        if (isCrouch)
        {
            idleBobbingSpeed = 2f;
            idleBobbingAmount = 0.025f;
        }
        //평소처럼(?) 흔듦
        if (!isCrouch)
        {
            idleBobbingSpeed = 2f;
            idleBobbingAmount = 0.03f;
        }

        //움직임 여부에 따라 설정 변경
        float bobbingSpeed = idleBobbingSpeed;
        float bobbingAmount = idleBobbingAmount;
        float midpoint = idleBobbingMidpoint;

        timer += bobbingSpeed * Time.deltaTime;

        if (timer > Mathf.PI * 2)
        {
            timer -= (Mathf.PI * 2);
        }

        float translateChange = waveslice * bobbingAmount;
        localPosition.y = midpoint + translateChange;

        transform.localPosition = Vector3.Lerp(transform.localPosition, localPosition, Time.deltaTime * bobbingSpeed);

        //상태 변경 시 부드럽게 전환
        if (isMovingTransitioning)
        {
            isMovingTransitioning = false;
            isMoving = !isMoving;
            if (isMoving)
            {
                MoveHeadBob();
            }
        }
    }
}
