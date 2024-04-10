using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Sound : MonoBehaviour {
	
	public AudioSource shell;

	void Start () {
		
		shell = GetComponent<AudioSource>();
		shell.pitch = Random.Range(0.75f, 1f);
		shell.Play();
	}
}
