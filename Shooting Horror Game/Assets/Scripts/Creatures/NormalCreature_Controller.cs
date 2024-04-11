using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class NormalCreature_Controller : MonoBehaviour
{
    private GameObject player;

    // Start is called before the first frame update
    void OnEnable()
    {
        player = GameObject.FindGameObjectWithTag("Player");
    }

    // Update is called once per frame
    void Update()
    {
        
    }
}
