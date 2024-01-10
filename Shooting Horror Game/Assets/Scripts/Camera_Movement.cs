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
    public Vector3 initialCameraPosition;
    public float idleBobbingSpeed = 0.5f;
    public float idleBobbingAmount = 0.1f;
    public float idleBobbingMidpoint = 0.0f;

    void Awake()
    {
        Cursor.visible = false;
        Cursor.lockState = CursorLockMode.Locked;
        initialCameraPosition = transform.localPosition;
    }

    void Update()
    {
        HeadBob();
    }

    void HandlePeek()
    {
        //왼쪽 벽 확인
        bool leftBlocked = Physics.Raycast(transform.position, -transform.right, peekDistance);

        //오른쪽 벽 확인
        bool rightBlocked = Physics.Raycast(transform.position, transform.right, peekDistance);

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

    public void HeadBob()
    {
        float xNoise = Mathf.PerlinNoise(0f, Time.time * idleBobbingSpeed) * 2f - 1f;
        float yNoise = Mathf.PerlinNoise(1f, Time.time * idleBobbingSpeed) * 2f - 1f;

        float xBobbingAmount = (xNoise * idleBobbingAmount) + (xNoise * idleBobbingAmount);
        float yBobbingAmount = (yNoise * idleBobbingAmount) + (yNoise * idleBobbingAmount);

        Vector3 localPosition = transform.localPosition;
        localPosition.z = idleBobbingMidpoint + xBobbingAmount;
        localPosition.y = idleBobbingMidpoint + yBobbingAmount;

        transform.localPosition = Vector3.Lerp(transform.localPosition, localPosition, Time.deltaTime * idleBobbingSpeed);
    }
}
