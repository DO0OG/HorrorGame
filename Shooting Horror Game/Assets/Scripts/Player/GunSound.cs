using System.Collections;
using System.Collections.Generic;
using System.Globalization;
using UnityEngine;

[RequireComponent(typeof(AudioSource))]
public class GunSound : MonoBehaviour
{
    private AudioSource audioSource;

    // Start is called before the first frame update
    void Start()
    {
        audioSource = GetComponent<AudioSource>();
        audioSource.loop = false;
    }

    // Update is called once per frame
    void Update()
    {
        if (Input.GetKeyDown(Player_Shot.shotKey) && Player_Shot.ammo == 0)
        {
            audioSource.volume = 0.3f;
            AudioClip clip = Resources.Load<AudioClip>($"Player/AmmoEmpty");
            audioSource.PlayOneShot(clip);
        }

    }

    void PlaySound(string name)
    {
        audioSource.volume = 0.5f;
        audioSource.clip = Resources.Load<AudioClip>($"Player/{name}");
        audioSource.Play();
    }
}
