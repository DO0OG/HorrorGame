using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;

public class test : MonoBehaviour
{
    [Header("PostProcess")]
    public VolumeProfile volumeProfile;
    Vignette vignette;

    public Animator anim;
    public Camera mainCam;

    public bool rightClick = false;

    // Start is called before the first frame update
    void Start()
    {
        anim = GetComponent<Animator>();
        mainCam = Camera.main;

        volumeProfile.TryGet(out vignette);
    }

    // Update is called once per frame
    void Update()
    {
        if (Input.GetKey(KeyCode.Mouse1))
        {
            rightClick = true;
            mainCam.fieldOfView = Mathf.Lerp(mainCam.fieldOfView, 32.5f, 0.025f);
            vignette.smoothness.value = Mathf.Lerp(vignette.smoothness.value, 0.45f, 0.025f);
        }
        else
        {
            rightClick = false;
            mainCam.fieldOfView = Mathf.Lerp(mainCam.fieldOfView, 60f, 0.025f);
            vignette.smoothness.value = Mathf.Lerp(vignette.smoothness.value, 0.2f, 0.025f);
        }

        anim.SetBool("Aim", rightClick);
    }
}
