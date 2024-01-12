using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;

public class Player_Shot : MonoBehaviour
{
    [Header("Keybinds")]
    public static KeyCode reloadKey = KeyCode.R;
    public static KeyCode shotKey = KeyCode.Mouse0;
    public static KeyCode aimKey = KeyCode.Mouse1;

    [Header("PostProcess")]
    public VolumeProfile volumeProfile;
    Vignette vignette;

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
    internal static bool isReload = false;
    internal static bool isAim = false;

    [Header("Animation")]
    Animator anim;

    [Header("ETC")]
    public Camera mainCam;

    // Start is called before the first frame update
    void Start()
    {
        anim = GetComponentInChildren<Animator>();
        mainCam = Camera.main;

        volumeProfile.TryGet(out vignette);
    }

    // Update is called once per frame
    void Update()
    {
        if(Input.GetKeyDown(shotKey) && !isReload && ammo > 0 && !ammoCheck) Shot();
        if(!isReload) ReloadTimeCheck();
        AnimControl();
        Aim();
    }

    private void AnimControl()
    {
        if (ammo == 0)
        {
            outOfAmmo = true;
        }
        anim.SetInteger(PlayerAnimParameter.Ammo, ammo);

        anim.SetBool(PlayerAnimParameter.OutOfAmmo, outOfAmmo);

        anim.SetBool(PlayerAnimParameter.AmmoCheck, ammoCheck);

        anim.SetBool(PlayerAnimParameter.Aim, isAim);
    }

    private void Aim()
    {
        if (Input.GetKey(aimKey) && !isReload)
        {
            isAim = true;
            mainCam.fieldOfView = Mathf.Lerp(mainCam.fieldOfView, 32.5f, 0.025f);
            vignette.smoothness.value = Mathf.Lerp(vignette.smoothness.value, 0.45f, 0.025f);
        }
        else
        {
            isAim = false;
            mainCam.fieldOfView = Mathf.Lerp(mainCam.fieldOfView, 60f, 0.025f);
            vignette.smoothness.value = Mathf.Lerp(vignette.smoothness.value, 0.2f, 0.025f);
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

    private void ReloadTimeCheck()
    {
        if (Input.GetKey(reloadKey))
        {
            if(checkTime <= 3f)
            {
                checkTime += Time.deltaTime;
            }
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
