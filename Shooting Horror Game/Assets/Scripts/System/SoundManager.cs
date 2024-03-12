using System;
using System.Collections;
using UnityEngine;
using UnityEngine.Audio;
using UnityEngine.UI;

public enum SoundType
{
    Bgm,
    SFX,
    Max
}

public class SoundManager
{
    [Header("Audio Mixers")]
    public AudioMixer audioMixer;
    public Slider masterSlider;
    public Slider bgmSlider;
    public Slider sfxSlider;

    [Header("Singleton")]
    private static SoundManager s_instance;
    public static SoundManager Instance => s_instance ??= s_instance = new SoundManager();

    [Header("Audio")]
    AudioSource[] _audioSources;
    GameObject _soundRoot;

    private SoundManager()
    {
        Init();
    }

    public void Init()
    {
        _soundRoot = GameObject.Find("@SoundRoot");
        if (_soundRoot == null)
        {
            _soundRoot = new GameObject { name = "@SoundRoot" };
            UnityEngine.Object.DontDestroyOnLoad(_soundRoot);

            string[] soundTypeNames = Enum.GetNames(typeof(SoundType));
            _audioSources = new AudioSource[(int)SoundType.Max];
            for (int i = 0; i < _audioSources.Length; ++i)
            {
                GameObject go = new GameObject(soundTypeNames[i]);
                _audioSources[i] = go.AddComponent<AudioSource>();
                go.transform.parent = _soundRoot.transform;
            }
        }

        _audioSources[(int)SoundType.Bgm].loop = true;
    }

    public void Play(SoundType soundType, string path)
    {
        // TODO : 사운드 경로 설정 
        AudioClip clip = Resources.Load<AudioClip>(path);
        AudioSource source = _audioSources[(int)soundType];

        switch (soundType)
        {
            case SoundType.Bgm:
                if (source.isPlaying)
                {
                    source.Stop();
                }
                source.clip = clip;
                source.Play();
                break;
            case SoundType.SFX:
                source.PlayOneShot(clip);
                break;
        }
    }

    /// <summary>
    /// 각 슬라이더에 audioMixer의 값(master, bgm, sfx 볼륨값)을 설정
    /// </summary>
    public void SetSliderValue()
    {
        float masterVolume;
        if (audioMixer.GetFloat("Master", out masterVolume))
        {
            masterSlider.value = Mathf.Pow(10f, masterVolume / 20f);
        }

        float BGMVolume;
        if (audioMixer.GetFloat("BGM", out BGMVolume))
        {
            bgmSlider.value = Mathf.Pow(10f, BGMVolume / 20f);
        }

        float SFXVolume;
        if (audioMixer.GetFloat("SFX", out SFXVolume))
        {
            sfxSlider.value = Mathf.Pow(10f, SFXVolume / 20f);
        }
    }
}