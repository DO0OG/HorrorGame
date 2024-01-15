using UnityEngine;
using System.Collections;

public class Camera_Movement : MonoBehaviour
{
    [Header("Bobbing")]
    public float idleBobbingSpeed = 0.5f;
    public float idleBobbingAmount = 0.1f;
    public float idleBobbingMidpoint = 0.0f;

    void Awake()
    {
        Cursor.visible = false;
        Cursor.lockState = CursorLockMode.Locked;
    }

    void Update()
    {
        HeadBobControl();
        HeadBob();
    }

    private void HeadBobControl()
    {
        if (Input.GetKeyDown(Player_Shot.aimKey))
        {
            idleBobbingAmount /= 3f;
        }
        else if (Input.GetKeyUp(Player_Shot.aimKey))
        {
            idleBobbingAmount *= 3f;
        }

        if (Input.GetKeyDown(Player_Move.sprintKey))
        {
            idleBobbingSpeed *= 8f;
            idleBobbingAmount *= 5f;
        }
        else if (Input.GetKeyUp(Player_Move.sprintKey))
        {
            idleBobbingSpeed /= 8f;
            idleBobbingAmount /= 5f;
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
