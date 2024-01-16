using UnityEngine;
using System.Collections;

public class Camera_Movement : MonoBehaviour
{
    [Header("Bobbing")]
    [SerializeField] private float idleBobbingSpeed = 0.5f;
    [SerializeField] private float idleBobbingAmount = 0.1f;
    [SerializeField] private float idleBobbingMidpoint = 0.0f;

    [SerializeField] private float initialSpeed;
    [SerializeField] private float initialAmount;

    void Awake()
    {
        Cursor.visible = false;
        Cursor.lockState = CursorLockMode.Locked;
        
        initialAmount = idleBobbingAmount;
        initialSpeed = idleBobbingSpeed;
    }

    void Update()
    {
        HeadBobControl();
        HeadBob();
    }

    private void HeadBobControl()
    {
        if (Player_Shot.isAim && !Player_Move.isSprint)
        {
            idleBobbingAmount = initialAmount / 3f;
        }
        else if (Player_Move.isSprint && !Player_Shot.isAim)
        {
            idleBobbingSpeed = initialSpeed * 8f;
            idleBobbingAmount = initialAmount * 5f;
        }
        else
        {
            idleBobbingSpeed = initialSpeed;
            idleBobbingAmount = initialAmount;
        }
    }

    private void HeadBob()
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
