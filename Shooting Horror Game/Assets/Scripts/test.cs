using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class test : MonoBehaviour
{
    public Animator anim;

    public bool rightClick = false;

    // Start is called before the first frame update
    void Start()
    {
        anim = GetComponent<Animator>();
    }

    // Update is called once per frame
    void Update()
    {
        if (Input.GetKey(KeyCode.Mouse1))
        {
            rightClick = true;
        }
        else
        {
            rightClick = false;
        }

        anim.SetBool("Aim", rightClick);
    }
}
