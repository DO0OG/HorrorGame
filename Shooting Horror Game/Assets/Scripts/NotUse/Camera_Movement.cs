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

    public static IEnumerator ShakeCamera(Transform targetTransform, float duration, float intensity, float speed = 1f)
    {
        float elapsed = 0f;
        Vector3 originalPosition = targetTransform.localPosition;
        Quaternion originalRotation = targetTransform.localRotation;
        float zSeed = Random.Range(-300f, 300f);
        float rotationSeed = Random.Range(-1000f, 1000f);

        while (elapsed < duration)
        {
            float progress = elapsed / duration;

            float zShake = Mathf.Lerp(0f, Mathf.PerlinNoise(zSeed, progress * speed) * 2 - 1, Mathf.SmoothStep(0f, 1f, progress));
            float rotationOffset = Mathf.Lerp(0f, Mathf.PerlinNoise(rotationSeed, progress * speed) * 2 - 1, Mathf.SmoothStep(0f, 1f, progress));

            float zOffset = zShake * intensity;
            float yOffset = Mathf.Sin(progress * Mathf.PI) * intensity + 0.25f;
            yOffset *= yOffset * 1.5f;

            targetTransform.localPosition = originalPosition + new Vector3(0, -yOffset, zOffset);

            //약간의 회전
            targetTransform.localRotation = originalRotation * Quaternion.Euler(0f, 0f, rotationOffset * intensity * 5f);

            elapsed += Time.deltaTime;
            yield return null;
        }
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
