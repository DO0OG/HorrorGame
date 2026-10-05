using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

/// <summary>
/// 인벤토리 한 칸의 데이터 (아이템 + 개수)
/// </summary>
[System.Serializable]
public class InventorySlot
{
    public Item item;
    public int count;

    public InventorySlot() { }

    public InventorySlot(Item item, int count)
    {
        this.item = item;
        this.count = count;
    }
}

/// <summary>
/// 인벤토리 데이터/로직 + TAB 열고 닫기 + UI 자동 생성.
/// DECAYED의 InventoryManager를 개량 : 좌측 아이템 그리드 + 우측 상세 정보 패널 레이아웃을
/// 코드로 생성하며(인스펙터 연결 불필요), 스택/커서/난이도별 일시정지를 지원한다.
/// </summary>
public class Inventory : MonoBehaviour
{
    public static Inventory Instance { get; private set; }

    [Header("Data")]
    [SerializeField] private List<InventorySlot> slots = new List<InventorySlot>();
    public IReadOnlyList<InventorySlot> Slots => slots;

    [Header("UI (비워두면 런타임에 자동 생성)")]
    [SerializeField] private GameObject inventoryUI;
    [SerializeField] private Transform slotContent;
    [SerializeField] private UI_InventorySlot slotPrefab;

    [Header("Input")]
    [SerializeField] private KeyCode toggleKey = KeyCode.Tab;

    [Header("Font (한글 폰트를 연결하면 자동 생성 UI에 적용됨)")]
    [SerializeField] private TMP_FontAsset font;

    // 자동 생성된 상세 정보 패널
    private Image infoIcon;
    private TextMeshProUGUI infoName;
    private TextMeshProUGUI infoDesc;
    private UI_InventorySlot selectedUI;

    private Camera_Controller cameraController;
    private bool pausedThisOpen;

    public bool IsOpen { get; private set; }

    // ── 색 팔레트 (호러/시인성) ──
    private static readonly Color ColOverlay = new Color(0f, 0f, 0f, 0.75f);
    private static readonly Color ColWindow  = new Color(0.09f, 0.09f, 0.11f, 0.98f);
    private static readonly Color ColHeader  = new Color(0.14f, 0.14f, 0.17f, 1f);
    private static readonly Color ColAccent  = new Color(0.65f, 0.18f, 0.16f, 1f);
    private static readonly Color ColListBg  = new Color(0.06f, 0.06f, 0.08f, 0.6f);
    private static readonly Color ColSlot    = new Color(0.18f, 0.18f, 0.22f, 1f);
    private static readonly Color ColText    = new Color(0.92f, 0.92f, 0.95f, 1f);

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }
        Instance = this;

        if (inventoryUI == null)
            BuildUI();

        if (inventoryUI != null)
            inventoryUI.SetActive(false);
    }

    private void Update()
    {
        if (Input.GetKeyDown(toggleKey))
            Toggle();
    }

    public void Toggle()
    {
        if (IsOpen) Close();
        else Open();
    }

    public void Open()
    {
        IsOpen = true;

        if (inventoryUI != null)
            inventoryUI.SetActive(true);

        Refresh();

        // 가장 어려운 난이도(Hard)에서는 멈추지 않음 — 인벤토리를 열어도 게임이 계속 진행됨
        pausedThisOpen = GameManager.Difficulty != Define.Difficulty.Hard;

        if (pausedThisOpen)
        {
            Time.timeScale = 0f;
            SetPlayerInput(false);
            Cursor.lockState = CursorLockMode.None;
            Cursor.visible = true;
        }
    }

    public void Close()
    {
        IsOpen = false;

        if (inventoryUI != null)
            inventoryUI.SetActive(false);

        if (pausedThisOpen)
        {
            Time.timeScale = 1f;
            SetPlayerInput(true);
            Cursor.lockState = CursorLockMode.Locked;
            Cursor.visible = false;
        }

        pausedThisOpen = false;
    }

    /// <summary>
    /// 플레이어의 모든 입력(이동/사격/조준/장전/장비전환)과 시선 이동을 켜고 끔.
    /// </summary>
    private void SetPlayerInput(bool enable)
    {
        if (Player_Controller.playerActionMap != null)
        {
            if (enable) Player_Controller.playerActionMap.Enable();
            else Player_Controller.playerActionMap.Disable();
        }

        if (cameraController == null)
            cameraController = FindObjectOfType<Camera_Controller>();
        if (cameraController != null)
            cameraController.enabled = enable;
    }

    // ============================================================
    //  데이터 조작
    // ============================================================

    public void Add(Item item, int count = 1)
    {
        if (item == null || count <= 0) return;

        if (item.maxStack > 1)
        {
            foreach (InventorySlot slot in slots)
            {
                if (slot.item != item || slot.count >= item.maxStack) continue;

                int add = Mathf.Min(item.maxStack - slot.count, count);
                slot.count += add;
                count -= add;
                if (count <= 0) break;
            }
        }

        while (count > 0)
        {
            int add = item.maxStack > 1 ? Mathf.Min(item.maxStack, count) : 1;
            slots.Add(new InventorySlot(item, add));
            count -= add;
        }

        if (IsOpen) Refresh();
    }

    public void Remove(Item item, int count = 1)
    {
        for (int i = slots.Count - 1; i >= 0 && count > 0; i--)
        {
            if (slots[i].item != item) continue;

            int take = Mathf.Min(slots[i].count, count);
            slots[i].count -= take;
            count -= take;

            if (slots[i].count <= 0)
                slots.RemoveAt(i);
        }

        if (IsOpen) Refresh();
    }

    public bool Has(string itemName)
        => slots.Exists(s => s.item != null && s.item.itemName == itemName);

    public int GetCount(Item item)
    {
        int total = 0;
        foreach (InventorySlot slot in slots)
            if (slot.item == item) total += slot.count;
        return total;
    }

    /// <summary>슬롯 클릭 시 선택 하이라이트 + 우측 상세 패널 갱신</summary>
    public void SelectSlot(UI_InventorySlot ui, InventorySlot data)
    {
        if (selectedUI != null && selectedUI != ui)
            selectedUI.SetSelected(false);

        selectedUI = ui;
        if (ui != null) ui.SetSelected(true);

        if (data != null && data.item != null)
        {
            if (infoIcon != null)
            {
                infoIcon.sprite = data.item.icon;
                infoIcon.enabled = data.item.icon != null;
            }
            if (infoName != null) infoName.text = data.item.itemName;
            if (infoDesc != null) infoDesc.text = data.item.itemInfo;
        }
    }

    public void Refresh()
    {
        if (slotContent == null) return;

        selectedUI = null;

        foreach (Transform child in slotContent)
            Destroy(child.gameObject);

        foreach (InventorySlot slot in slots)
        {
            UI_InventorySlot ui = slotPrefab != null
                ? Instantiate(slotPrefab, slotContent)
                : CreateSlotUI();
            ui.Set(slot);
        }

        // 상세 패널 초기화
        if (infoIcon != null) infoIcon.enabled = false;
        if (infoName != null) infoName.text = "";
        if (infoDesc != null) infoDesc.text = slots.Count > 0 ? "Select an item." : "Empty.";
    }

    // ============================================================
    //  UI 자동 생성 (좌: 그리드 스크롤 / 우: 상세 정보)
    // ============================================================

    private void BuildUI()
    {
        if (FindObjectOfType<EventSystem>() == null)
            new GameObject("EventSystem", typeof(EventSystem), typeof(StandaloneInputModule));

        GameObject canvasGO = new GameObject("InventoryCanvas",
            typeof(Canvas), typeof(CanvasScaler), typeof(GraphicRaycaster));
        canvasGO.transform.SetParent(transform, false);
        Canvas canvas = canvasGO.GetComponent<Canvas>();
        canvas.renderMode = RenderMode.ScreenSpaceOverlay;
        canvas.sortingOrder = 100;
        CanvasScaler scaler = canvasGO.GetComponent<CanvasScaler>();
        scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
        scaler.referenceResolution = new Vector2(1920f, 1080f);
        scaler.matchWidthOrHeight = 0.5f;

        // 전체 어두운 오버레이 = 열고 닫을 대상
        GameObject overlay = NewUI("Overlay", canvasGO.transform);
        overlay.AddComponent<Image>().color = ColOverlay;
        Stretch(overlay.GetComponent<RectTransform>());
        inventoryUI = overlay;

        // 중앙 창
        GameObject window = NewUI("Window", overlay.transform);
        window.AddComponent<Image>().color = ColWindow;
        RectTransform winRt = window.GetComponent<RectTransform>();
        Center(winRt);
        winRt.sizeDelta = new Vector2(1000f, 620f);

        BuildHeader(window.transform);
        BuildList(window.transform);
        BuildInfoPanel(window.transform);
    }

    private void BuildHeader(Transform window)
    {
        GameObject header = NewUI("Header", window);
        header.AddComponent<Image>().color = ColHeader;
        TopStretch(header.GetComponent<RectTransform>(), 64f);

        GameObject titleGO = NewUI("Title", header.transform);
        TextMeshProUGUI title = titleGO.AddComponent<TextMeshProUGUI>();
        title.text = "INVENTORY";
        title.fontSize = 34f;
        title.fontStyle = FontStyles.Bold;
        title.alignment = TextAlignmentOptions.Left;
        title.color = ColText;
        ApplyFont(title);
        RectTransform titleRt = titleGO.GetComponent<RectTransform>();
        Stretch(titleRt);
        titleRt.offsetMin = new Vector2(28f, 0f);
        titleRt.offsetMax = new Vector2(-28f, 0f);

        // 헤더 하단 강조선
        GameObject line = NewUI("AccentLine", header.transform);
        line.AddComponent<Image>().color = ColAccent;
        RectTransform lineRt = line.GetComponent<RectTransform>();
        lineRt.anchorMin = new Vector2(0f, 0f);
        lineRt.anchorMax = new Vector2(1f, 0f);
        lineRt.pivot = new Vector2(0.5f, 0f);
        lineRt.sizeDelta = new Vector2(0f, 3f);
        lineRt.anchoredPosition = Vector2.zero;
    }

    private void BuildList(Transform window)
    {
        // 좌측 스크롤 영역
        GameObject list = NewUI("List", window);
        list.AddComponent<Image>().color = ColListBg;
        ScrollRect scroll = list.AddComponent<ScrollRect>();
        scroll.horizontal = false;
        scroll.vertical = true;
        scroll.scrollSensitivity = 28f;
        scroll.movementType = ScrollRect.MovementType.Clamped;
        RectFill(list.GetComponent<RectTransform>(), 16f, 16f, 384f, 80f);

        GameObject viewport = NewUI("Viewport", list.transform);
        viewport.AddComponent<RectMask2D>();
        Stretch(viewport.GetComponent<RectTransform>());
        scroll.viewport = viewport.GetComponent<RectTransform>();

        GameObject content = NewUI("Content", viewport.transform);
        GridLayoutGroup grid = content.AddComponent<GridLayoutGroup>();
        grid.cellSize = new Vector2(110f, 130f);
        grid.spacing = new Vector2(12f, 12f);
        grid.padding = new RectOffset(14, 14, 14, 14);
        grid.childAlignment = TextAnchor.UpperLeft;
        ContentSizeFitter fitter = content.AddComponent<ContentSizeFitter>();
        fitter.verticalFit = ContentSizeFitter.FitMode.PreferredSize;
        RectTransform contentRt = content.GetComponent<RectTransform>();
        contentRt.anchorMin = new Vector2(0f, 1f);
        contentRt.anchorMax = new Vector2(1f, 1f);
        contentRt.pivot = new Vector2(0.5f, 1f);
        contentRt.anchoredPosition = Vector2.zero;
        contentRt.sizeDelta = Vector2.zero;
        scroll.content = contentRt;
        slotContent = content.transform;
    }

    private void BuildInfoPanel(Transform window)
    {
        GameObject info = NewUI("Info", window);
        info.AddComponent<Image>().color = ColListBg;
        RectFill(info.GetComponent<RectTransform>(), 632f, 16f, 16f, 80f);

        // 큰 아이콘
        GameObject iconGO = NewUI("InfoIcon", info.transform);
        infoIcon = iconGO.AddComponent<Image>();
        infoIcon.preserveAspect = true;
        infoIcon.enabled = false;
        RectTransform iconRt = iconGO.GetComponent<RectTransform>();
        iconRt.anchorMin = iconRt.anchorMax = new Vector2(0.5f, 1f);
        iconRt.pivot = new Vector2(0.5f, 1f);
        iconRt.sizeDelta = new Vector2(150f, 150f);
        iconRt.anchoredPosition = new Vector2(0f, -30f);

        // 이름
        GameObject nameGO = NewUI("InfoName", info.transform);
        infoName = nameGO.AddComponent<TextMeshProUGUI>();
        infoName.fontSize = 26f;
        infoName.fontStyle = FontStyles.Bold;
        infoName.alignment = TextAlignmentOptions.Center;
        infoName.color = new Color(0.95f, 0.9f, 0.7f);
        ApplyFont(infoName);
        RectTransform nameRt = nameGO.GetComponent<RectTransform>();
        nameRt.anchorMin = new Vector2(0f, 1f);
        nameRt.anchorMax = new Vector2(1f, 1f);
        nameRt.pivot = new Vector2(0.5f, 1f);
        nameRt.sizeDelta = new Vector2(-20f, 40f);
        nameRt.anchoredPosition = new Vector2(0f, -195f);

        // 설명
        GameObject descGO = NewUI("InfoDesc", info.transform);
        infoDesc = descGO.AddComponent<TextMeshProUGUI>();
        infoDesc.text = "Select an item.";
        infoDesc.fontSize = 18f;
        infoDesc.alignment = TextAlignmentOptions.TopLeft;
        infoDesc.color = new Color(0.8f, 0.8f, 0.82f);
        infoDesc.enableWordWrapping = true;
        ApplyFont(infoDesc);
        RectFill(descGO.GetComponent<RectTransform>(), 22f, 22f, 22f, 255f);
    }

    private UI_InventorySlot CreateSlotUI()
    {
        GameObject slotGO = NewUI("Slot", slotContent);
        Image bg = slotGO.AddComponent<Image>();
        bg.color = ColSlot;

        GameObject iconGO = NewUI("Icon", slotGO.transform);
        Image icon = iconGO.AddComponent<Image>();
        icon.raycastTarget = false;
        icon.preserveAspect = true;
        icon.enabled = false;
        RectTransform iconRt = iconGO.GetComponent<RectTransform>();
        iconRt.anchorMin = iconRt.anchorMax = new Vector2(0.5f, 1f);
        iconRt.pivot = new Vector2(0.5f, 1f);
        iconRt.sizeDelta = new Vector2(74f, 74f);
        iconRt.anchoredPosition = new Vector2(0f, -10f);

        GameObject nameGO = NewUI("Name", slotGO.transform);
        TextMeshProUGUI nameTmp = nameGO.AddComponent<TextMeshProUGUI>();
        nameTmp.fontSize = 15f;
        nameTmp.alignment = TextAlignmentOptions.Center;
        nameTmp.color = ColText;
        nameTmp.raycastTarget = false;
        nameTmp.enableWordWrapping = true;
        nameTmp.overflowMode = TextOverflowModes.Ellipsis;
        ApplyFont(nameTmp);
        RectTransform nameRt = nameGO.GetComponent<RectTransform>();
        nameRt.anchorMin = new Vector2(0f, 0f);
        nameRt.anchorMax = new Vector2(1f, 0f);
        nameRt.pivot = new Vector2(0.5f, 0f);
        nameRt.sizeDelta = new Vector2(-6f, 38f);
        nameRt.anchoredPosition = new Vector2(0f, 4f);

        GameObject countGO = NewUI("Count", slotGO.transform);
        TextMeshProUGUI countTmp = countGO.AddComponent<TextMeshProUGUI>();
        countTmp.fontSize = 18f;
        countTmp.fontStyle = FontStyles.Bold;
        countTmp.color = new Color(1f, 0.85f, 0.4f);
        countTmp.alignment = TextAlignmentOptions.BottomRight;
        countTmp.raycastTarget = false;
        ApplyFont(countTmp);
        RectTransform countRt = countGO.GetComponent<RectTransform>();
        Stretch(countRt);
        countRt.offsetMin = new Vector2(0f, 4f);
        countRt.offsetMax = new Vector2(-6f, 0f);

        UI_InventorySlot slot = slotGO.AddComponent<UI_InventorySlot>();
        slot.Configure(bg, icon, nameTmp, countTmp);
        return slot;
    }

    // ── RectTransform 헬퍼 ──

    private static GameObject NewUI(string name, Transform parent)
    {
        GameObject go = new GameObject(name, typeof(RectTransform));
        go.transform.SetParent(parent, false);
        return go;
    }

    private static void Stretch(RectTransform rt)
    {
        rt.anchorMin = Vector2.zero;
        rt.anchorMax = Vector2.one;
        rt.offsetMin = Vector2.zero;
        rt.offsetMax = Vector2.zero;
    }

    private static void Center(RectTransform rt)
    {
        rt.anchorMin = rt.anchorMax = rt.pivot = new Vector2(0.5f, 0.5f);
        rt.anchoredPosition = Vector2.zero;
    }

    private static void TopStretch(RectTransform rt, float height)
    {
        rt.anchorMin = new Vector2(0f, 1f);
        rt.anchorMax = new Vector2(1f, 1f);
        rt.pivot = new Vector2(0.5f, 1f);
        rt.sizeDelta = new Vector2(0f, height);
        rt.anchoredPosition = Vector2.zero;
    }

    /// <summary>부모 전체(stretch)에서 좌/하/우/상 여백(픽셀)으로 배치</summary>
    private static void RectFill(RectTransform rt, float left, float bottom, float right, float top)
    {
        rt.anchorMin = Vector2.zero;
        rt.anchorMax = Vector2.one;
        rt.offsetMin = new Vector2(left, bottom);
        rt.offsetMax = new Vector2(-right, -top);
    }

    private void ApplyFont(TextMeshProUGUI tmp)
    {
        if (font != null)
            tmp.font = font;
    }
}
