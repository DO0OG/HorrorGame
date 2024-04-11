using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

public class UIManager
{
    const int ORIGINORDER = short.MinValue;
    int _order = short.MinValue;
    public static UIManager _instance { get; private set; }
    public UI_Scene SceneUI { get; private set; }

    internal Stack<UI_Popup> _popupStack = new Stack<UI_Popup>();

    private GameObject _root;

    public void Init()
    {
        Screen.fullScreen = false;
    }

    public GameObject Root
    {
        get
        {
            GameObject root = GameObject.Find("@UI_Root");
            if (root == null)
                root = new GameObject { name = "@UI_Root" };

            return root;
        }
    }

    public void SetCanvas(GameObject go, bool sort = true)
    {
        Canvas canvas = Utils.GetOrAddComponent<Canvas>(go);
        canvas.renderMode = RenderMode.ScreenSpaceOverlay;
        canvas.overrideSorting = true;

        if (sort)
        {
            _order++;
            canvas.sortingOrder = _order;
        }
        else if (go.CompareTag("AlwaysOnTop"))
        {
            canvas.sortingOrder = short.MaxValue;
        }
        else if (go.CompareTag("AlwaysOnBottom"))
        {
            canvas.sortingOrder = short.MinValue;
        }
        else
        {
            canvas.sortingOrder = 0;
        }
    }

    public T ShowSceneUI<T>(string name = null) where T : UI_Scene
    {
        if (string.IsNullOrEmpty(name))
            name = typeof(T).Name;

        GameObject go = Managers.Resource.Instantiate($"{name}");
        T sceneUI = Utils.GetOrAddComponent<T>(go);
        SceneUI = sceneUI;

        go.transform.SetParent(Root.transform);

        return sceneUI;
    }

    public T ShowPopupUI<T>(string name = null, Transform parent = null) where T : UI_Popup
    {
        if (string.IsNullOrEmpty(name))
            name = typeof(T).Name;

        GameObject prefab = Managers.Resource.Load<GameObject>(Path.PREFAB + name);

        GameObject go = Managers.Resource.Instantiate($"{name}");
        T popup = Utils.GetOrAddComponent<T>(go);

        _popupStack.Push(popup);

        if (parent != null)
            go.transform.SetParent(parent);
        else
            go.transform.SetParent(Root.transform);

        go.transform.localScale = Vector3.one;
        go.transform.localPosition = prefab.transform.position;

        return popup;
    }

    public void ClosePopupUI(UI_Popup popup)
    {
        if (_popupStack.Count == 0)
            return;

        if (_popupStack.Peek() != popup)
        {
            Debug.Log("Close Popup Failed!");
            return;
        }

        ClosePopupUI();
    }

    public void ClosePopupUI()
    {
        if (_popupStack.Count == 0)
            return;

        UI_Popup popup = _popupStack.Pop();

        if (popup != null)
        {
            Managers.Resource.Destroy(popup.gameObject);
            popup = null;
        }

        _order--;
    }

    public void CloseAllPopupUI()
    {
        while (_popupStack.Count > 0)
            ClosePopupUI();

        _order = ORIGINORDER;
    }
}
