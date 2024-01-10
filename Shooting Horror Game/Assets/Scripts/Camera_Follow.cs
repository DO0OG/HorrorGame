using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Camera_Follow : MonoBehaviour
{
    [Header("Object")]
    public GameObject goFollow;
    public GameObject camPos;

    [Header("Force")]
    public float speed = 9.0f;
    public float rotationThreshold = 7.0f; // 오차 허용 범위
    public float angle;

    void Start()
    {
    }

    void Update()
    {
        MouseCheck();
        Follow();
    }

    private void MouseCheck()
    {
        if (Input.GetMouseButtonDown(1))
        {
            rotationThreshold /= 2f;
        }
        else if(Input.GetMouseButtonUp(1))
        {
            rotationThreshold *= 2f;
        }
    }

    private void Follow()
    {
        transform.position = camPos.transform.position;

        angle = Quaternion.Angle(transform.rotation, goFollow.transform.rotation);

        // 오차 허용 범위 내에 있을 때만 회전
        if (angle >= rotationThreshold)
        {
            transform.rotation = Quaternion.Slerp(transform.rotation, goFollow.transform.rotation, speed * Time.deltaTime);
        }
    }
}
