using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Player_Shot : MonoBehaviour
{
    [Header("Keybinds")]
    public KeyCode reloadKey = KeyCode.R;

    [Header("Bullet")]
    public GameObject shootEffectPrefab;
    public GameObject casingPrefab;
    public GameObject muzzleFlashPrefab;
    public float destroyTime = 3f;
    public int ammo = 7;

    [Header("Point")]
    public Transform firePoint;
    public Transform casingPoint;

    [Header("Bools")]
    public bool outOfAmmo = false;
    public static bool isReload = false;

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
        if(Input.GetKeyDown(KeyCode.Mouse0) && !isReload) Shot();
        AnimControl();
        if(Input.GetKeyDown(reloadKey)) StartCoroutine(Reload());
    }

    private void AnimControl()
    {
        anim.SetInteger(PlayerAnimParameter.Ammo, ammo);

        if (ammo == 0)
        {
            outOfAmmo = true;
            anim.SetBool(PlayerAnimParameter.OutOfAmmo, outOfAmmo);
        }
    }

    private void Shot()
    {
        if (outOfAmmo) return;

        ammo--;
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

    IEnumerator Reload()
    {
        if (!isReload)
        {
            isReload = true;
            anim.SetTrigger(PlayerAnimParameter.Reload);
            ammo = 7;
            yield return new WaitForSeconds(2.8f);
            outOfAmmo = false;
            anim.SetBool(PlayerAnimParameter.OutOfAmmo, outOfAmmo);
            isReload = false;
        }
    }
}
