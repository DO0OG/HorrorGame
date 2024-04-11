using System.Collections;
using System.Collections.Generic;
using System.Resources;
using UnityEngine;

public class Managers : MonoBehaviour
{
    private static Managers _instance;

    private static UIManager _uiManager = new UIManager();
    private static ResourceManager _resourceManager = new ResourceManager();

    public static UIManager UI { get { Init(); return _uiManager; } }
    public static ResourceManager Resource { get { Init(); return _resourceManager; } }

    private void Start()
    {
        Init();
    }

    private static void Init()
    {
        if (_instance == null)
        {
            GameObject go = GameObject.Find("@Managers");
            if (go == null)
                go = new GameObject { name = "@Managers" };

            _instance = Utils.GetOrAddComponent<Managers>(go);
            DontDestroyOnLoad(go);

            _uiManager.Init();
            _resourceManager.Init();
        }
    }
}
