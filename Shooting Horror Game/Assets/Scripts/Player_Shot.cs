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
    [SerializeField] private float destroyTime = 3f;
    [SerializeField] private int ammo = 7;

    [Header("Reload")]
    public float checkTime;

    [Header("Point")]
    public Transform firePoint;
    public Transform casingPoint;

    [Header("Bools")]
    [SerializeField] private bool outOfAmmo = false;
    [SerializeField] private bool ammoCheck = false;
    [SerializeField] internal static bool isReload = false;

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
        if(Input.GetKeyDown(KeyCode.Mouse0) && !isReload && ammo > 0 && !ammoCheck) Shot();
        if(!isReload) ReloadTimeCheck();
        AnimControl();
    }

    private void AnimControl()
    {
        anim.SetInteger(PlayerAnimParameter.Ammo, ammo);

        if(ammo == 0)
        {
            outOfAmmo = true;
        }
        anim.SetBool(PlayerAnimParameter.OutOfAmmo, outOfAmmo);

        anim.SetBool(PlayerAnimParameter.AmmoCheck, ammoCheck);
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

    private void ReloadTimeCheck()
    {
        if (Input.GetKey(reloadKey))
        {
            checkTime += Time.deltaTime;
            if(checkTime > 0.6f)
            {
                ammoCheck = true;
            }
        }
        else
        {
            if (Input.GetKeyUp(reloadKey) && checkTime <= 0.5f)
            {
                checkTime = 0;
                StartCoroutine(Reload());
            }
            else if (Input.GetKeyUp(reloadKey) && checkTime > 0.6f)
            {
                checkTime = 0;
                ammoCheck = false;
            }
        }
    }

    private IEnumerator Reload()
    {
        if (!isReload)
        {
            isReload = true;
            anim.SetTrigger(PlayerAnimParameter.Reload);
            ammo = 7;
            yield return new WaitForSeconds(2.8f);
            outOfAmmo = false;
            isReload = false;
        }
    }
}
