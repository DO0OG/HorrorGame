using UnityEngine;

/// <summary>
/// 필드에 배치되는 줍는 아이템. 플레이어가 트리거에 닿으면 인벤토리에 담기고 사라진다.
/// DECAYED의 ItemPickup을 개량 : 개수 지정과 Trigger 자동 설정을 추가.
/// 콜라이더는 Trigger로 둘 것 (Reset 시 자동 설정됨).
/// </summary>
[RequireComponent(typeof(Collider))]
public class ItemPickup : MonoBehaviour
{
    [SerializeField] private Item item;
    [SerializeField] private int count = 1;

    private void Reset()
    {
        GetComponent<Collider>().isTrigger = true;
    }

    private void OnTriggerEnter(Collider other)
    {
        if (!other.CompareTag("Player")) return;
        if (Inventory.Instance == null || item == null) return;

        Inventory.Instance.Add(item, count);

        // TODO : 획득 사운드 / 팝업 연출 연결
        Destroy(gameObject);
    }
}
