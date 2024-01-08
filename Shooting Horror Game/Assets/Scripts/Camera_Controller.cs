using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Camera_Controller : MonoBehaviour
{
    public Vector3 vectOffset;
    public GameObject goFollow;
    public float speed = 1.0f;

    void Start()
    {
        vectOffset = transform.position - goFollow.transform.position;
    }

    void Update()
    {
        vectOffset = transform.position - goFollow.transform.position;
        transform.position = goFollow.transform.position + vectOffset;
        transform.rotation = Quaternion.Slerp(transform.rotation, goFollow.transform.rotation, speed * Time.deltaTime);
    }
}
