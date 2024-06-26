using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class DestroyAudioObject : MonoBehaviour
{
    private AudioSource audioSource;

    void Start()
    {
        audioSource = GetComponent<AudioSource>();

        if (audioSource == null)
        {
            Debug.Log("NULL");
        }
    }

    void Update()
    {
        if (audioSource != null && !audioSource.isPlaying)
        {
            Destroy(gameObject);
        }
    }
}
