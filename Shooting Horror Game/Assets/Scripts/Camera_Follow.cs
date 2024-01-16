using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Camera_Follow : MonoBehaviour
{
    [Header("Object")]
    [SerializeField] private GameObject goFollow;
    [SerializeField] private GameObject camPos;

    [Header("Force")]
    [SerializeField] private float speed = 9.0f;
    [SerializeField] private float rotationThreshold = 7.0f; // ���� ��� ����
    [SerializeField] private float angle;

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

        // ���� ��� ���� ���� ���� ���� ȸ��
        if (angle >= rotationThreshold)
        {
            transform.rotation = Quaternion.Slerp(transform.rotation, goFollow.transform.rotation, speed * Time.deltaTime);
        }
    }
}
