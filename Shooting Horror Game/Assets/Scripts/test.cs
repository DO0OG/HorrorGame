using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class test : MonoBehaviour
{
    public Animator anim;
    public Camera mainCam;

    public bool rightClick = false;

    // Start is called before the first frame update
    void Start()
    {
        anim = GetComponent<Animator>();
        mainCam = Camera.main;
    }

    // Update is called once per frame
    void Update()
    {
        if (Input.GetKey(KeyCode.Mouse1))
        {
            rightClick = true;
            mainCam.fieldOfView = Mathf.Lerp(mainCam.fieldOfView, 32.5f, 0.025f);
        }
        else
        {
            rightClick = false;
            mainCam.fieldOfView = Mathf.Lerp(mainCam.fieldOfView, 60f, 0.025f);
        }

        anim.SetBool("Aim", rightClick);
    }
}
