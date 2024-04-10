using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class FOV : MonoBehaviour
{
    Camera mainCam;
    Camera thisCam;

    // Start is called before the first frame update
    void Start()
    {
        mainCam = Camera.main;
        thisCam = GetComponent<Camera>();
    }

    // Update is called once per frame
    void LateUpdate()
    {
        if(mainCam != null && mainCam.fieldOfView != thisCam.fieldOfView)
        {
            thisCam.fieldOfView = mainCam.fieldOfView;
        }
    }
}
