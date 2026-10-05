using UnityEngine;

/// <summary>
/// 인벤토리 아이템 정의 (ScriptableObject). DECAYED의 Item을 개량 :
/// 레거시 Text 대신 TMP를 쓰고, 스택 수량(maxStack)을 지원한다.
/// 에디터에서 Create > Inventory > Item 으로 생성.
/// </summary>
[CreateAssetMenu(fileName = "New Item", menuName = "Inventory/Item")]
public class Item : ScriptableObject
{
    public int id;
    public string itemName;
    [TextArea] public string itemInfo;
    public Sprite icon;

    [Tooltip("한 슬롯에 쌓을 수 있는 최대 개수. 1이면 스택 불가")]
    public int maxStack = 1;
}
