using System.Collections;
using System.Collections.Generic;
using System.Globalization;
using UnityEngine;

[RequireComponent(typeof(AudioSource))]
public class GunSound : MonoBehaviour
{
    private static AudioSource audioSource;

    // Start is called before the first frame update
    void Start()
    {
        audioSource = GetComponent<AudioSource>();
        audioSource.loop = false;
    }

    // Update is called once per frame
    void Update()
    {
    }

    public static void EmptySound()
    {
        audioSource.volume = 0.3f;
        AudioClip clip = Managers.Resource.LoadAudioClip("AmmoEmpty");
        audioSource.PlayOneShot(clip);
    }

    void PlaySound(string name)
    {
        audioSource.volume = 0.5f;
        audioSource.clip = Managers.Resource.LoadAudioClip($"{name}");
        audioSource.Play();
    }
}
