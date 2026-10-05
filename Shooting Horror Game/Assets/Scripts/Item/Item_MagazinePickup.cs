using System.Collections;
using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// 필드에 배치되는 탄창 아이템. 플레이어가 닿으면 탄창을 획득하고 사라짐.
/// 콜라이더는 Trigger로 설정할 것.
/// </summary>
[RequireComponent(typeof(Collider))]
public class Item_MagazinePickup : MonoBehaviour
{
    [SerializeField] private int magCount = 1;

    private void Reset()
    {
        GetComponent<Collider>().isTrigger = true;
    }

    private void OnTriggerEnter(Collider other)
    {
        if (!other.CompareTag("Player")) return;

        Player_Shot playerShot = other.GetComponent<Player_Shot>();
        if (playerShot == null) return;

        playerShot.AddMagazine(magCount);

        // TODO : 획득 사운드 / UI 연출 연결
        Destroy(gameObject);
    }
}
