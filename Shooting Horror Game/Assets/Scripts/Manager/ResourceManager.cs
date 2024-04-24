using System.Collections;
using System.Collections.Generic;
using System.IO;
using UnityEngine;

public class ResourceManager
{
    public Dictionary<string, GameObject> _prefabs = new Dictionary<string, GameObject>();
    public Dictionary<string, AudioClip> _sounds = new Dictionary<string, AudioClip>();

    public void Init()
    {
    }

    public GameObject LoadPrefab(string path) => Load<GameObject>(string.Concat(Path.PREFAB, path));

    public AudioClip LoadAudioClip(string path) => Load<AudioClip>(string.Concat(Path.SOUND, path));

    public T Load<T>(string path = null) where T : Object
    {
        if (typeof(T) == typeof(GameObject))
        {
            if (_prefabs.TryGetValue(path, out GameObject gameObject))
                return gameObject as T;

            GameObject go = Resources.Load<GameObject>(path);
            _prefabs.Add(path, go);
            return go as T;
        }
        else if (typeof(T) == typeof(AudioClip))
        {
            if (_sounds.TryGetValue(path, out AudioClip audioClip))
                return audioClip as T;

            AudioClip sd = Resources.Load<AudioClip>(path);
            _sounds.Add(path, sd);
            return sd as T;
        }

        return Resources.Load<T>(path);
    }

    public GameObject Instantiate(string path, Transform parent = null)
    {
        GameObject prefab = LoadPrefab(path);

        if (prefab == null)
        {
            Debug.Log($"Failed to load prefab : {path}");
            return null;
        }

        return Instantiate(prefab, parent);
    }

    public GameObject Instantiate(GameObject prefab, Transform parent = null)
    {
        GameObject go = Object.Instantiate(prefab, parent);
        go.name = prefab.name;
        return go;
    }

    public void Destroy(GameObject go)
    {
        if (go == null)
            return;

        Object.Destroy(go);
    }
}