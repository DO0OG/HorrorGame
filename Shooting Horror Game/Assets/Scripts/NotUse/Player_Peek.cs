using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Player_Peek : MonoBehaviour
{
    [Header("Keybinds")]
    public static KeyCode leftPeekKey = KeyCode.Q;
    public static KeyCode rightPeekKey = KeyCode.E;

    [Header("Peeking")]
    public Transform peekLeft;
    public Transform peekRight;
    public Transform peekIdle;
    public float peekAngle = 10f;
    public float peekSpeed = 6f;
    public float peekDistance = 0.5f;

    // Start is called before the first frame update
    void Start()
    {
        
    }

    // Update is called once per frame
    void Update()
    {
        HandlePeek();
    }

    void HandlePeek()
    {
        // 왼쪽 벽 확인
        bool leftBlocked = Physics.Raycast(transform.position, -transform.right, peekDistance);

        // 오른쪽 벽 확인
        bool rightBlocked = Physics.Raycast(transform.position, transform.right, peekDistance);

        if (Input.GetKey(leftPeekKey))
        {
            ApplyPeekTransform(peekLeft);
        }
        else if (Input.GetKey(rightPeekKey))
        {
            ApplyPeekTransform(peekRight);
        }
        else
        {
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
}
