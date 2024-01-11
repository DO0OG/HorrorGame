using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Player_Shot : MonoBehaviour
{
    [Header("Bullet")]
    public GameObject shootEffectPrefab;
    public GameObject casingPrefab;
    public GameObject muzzleFlashPrefab;
    public float destroyTime = 3f;

    [Header("Point")]
    public Transform firePoint;
    public Transform casingPoint;

    [Header("Animation")]
    Animator anim;

    // Start is called before the first frame update
    void Start()
    {
        anim = GetComponentInChildren<Animator>();
    }

    // Update is called once per frame
    void Update()
    {
        if(Input.GetKeyDown(KeyCode.Mouse0)) Shot();
    }

    private void Shot()
    {
        Ray ray = new Ray(firePoint.position, firePoint.forward);
        RaycastHit hit;

        anim.SetTrigger(PlayerAnimParameter.Shot);

        GameObject casingEffect = Instantiate(casingPrefab, casingPoint);

        if (Physics.Raycast(ray, out hit))
        {
            GameObject shootEffect = Instantiate(shootEffectPrefab, hit.point + (hit.normal * 0.01f), Quaternion.LookRotation(hit.normal));
            shootEffect.transform.SetParent(hit.transform);
            Destroy(shootEffect, destroyTime);
        }
    }
}
