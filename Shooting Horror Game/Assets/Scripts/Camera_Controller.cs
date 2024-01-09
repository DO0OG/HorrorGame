using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Camera_Controller : MonoBehaviour
{
    [Header("Object")]
    public GameObject goFollow;
    public GameObject camPos;

    [Header("Vector3")]
    public Vector3 vectOffset;

    [Header("Force")]
    public float speed = 1.0f;

    void Start()
    {
        vectOffset = transform.position - goFollow.transform.position;
    }

    void Update()
    {
        vectOffset = transform.position - goFollow.transform.position;
        transform.position = camPos.transform.position;
        transform.rotation = Quaternion.Slerp(transform.rotation, goFollow.transform.rotation, speed * Time.deltaTime);
    }
}
