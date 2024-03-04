using Cinemachine;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.VFX;

public class Player_Shot : MonoBehaviour
{
    [Header("Keybinds")]
    internal static KeyCode reloadKey = KeyCode.R;
    internal static KeyCode shotKey = KeyCode.Mouse0;
    internal static KeyCode aimKey = KeyCode.Mouse1;

    [Header("Bullet")]
    [SerializeField] private GameObject shootEffectPrefab;
    [SerializeField] private GameObject casingPrefab;
    [SerializeField] private int ammo = 8;
    [SerializeField] private int mags = 8;

    [Header("MuzzleFlash")]
    [SerializeField] private VisualEffect muzzleFlash;
    [SerializeField] private GameObject muzzleLight;

    [Header("AmmoUI")]
    [SerializeField] private CanvasGroup ammoCanvas;
    [SerializeField] private Image fillImage;

    [Header("Reload")]
    [SerializeField] private float checkTime;

    [Header("Point")]
    [SerializeField] private Transform firePoint;
    [SerializeField] private Transform casingPoint;

    [Header("Bools")]
    [SerializeField] private bool outOfAmmo = false;
    [SerializeField] private bool ammoCheck = false;
    internal static bool isReload = false;
    internal static bool isAim = false;

    [Header("Animation")]
    private Animator anim;

    [Header("ETC")]
    [SerializeField] private CinemachineImpulseSource impulseSource;

    // Start is called before the first frame update
    void Start()
    {
        Cursor.lockState = CursorLockMode.Locked;
        Cursor.visible = false;

        anim = GetComponentInChildren<Animator>();
        impulseSource = GetComponent<CinemachineImpulseSource>();
        shootEffectPrefab = Resources.Load<GameObject>("BulletHole");
        casingPrefab = Resources.Load<GameObject>("Casing");

        ammoCanvas.alpha = 0f;
    }

    // Update is called once per frame
    void Update()
    {
        if(Input.GetKeyDown(shotKey) && !isReload && ammo > 0 && !ammoCheck) Shot();
        if(!isReload) ReloadTimeCheck();

        if (Input.GetKey(aimKey) && !isReload) isAim = true;
        else isAim = false;

        AnimControl();
        AmmoCheckSlider();
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

    private void Shot()
    {
        if (outOfAmmo) return;

        anim.SetTrigger(PlayerAnimParameter.Shot);

        ammo--;
        Ray ray = new Ray(firePoint.position, firePoint.forward);
        RaycastHit hit;

        GameObject casingEffect = Instantiate(casingPrefab, casingPoint);
        casingEffect.transform.SetParent(null);

        if (Physics.Raycast(ray, out hit))
        {
            GameObject shootEffect = Instantiate(shootEffectPrefab, hit.point + (hit.normal * 0.01f), Quaternion.LookRotation(hit.normal));
            shootEffect.transform.SetParent(hit.transform);
        }

        impulseSource.GenerateImpulse();
        MuzzleFlash();
    }

    private void ReloadTimeCheck()
    {
        if (Input.GetKey(reloadKey))
        {
            if(checkTime <= 3f)
            {
                checkTime += Time.deltaTime;
            }
            if(checkTime > 0.5f)
            {
                ammoCheck = true;
            }
        }
        else if(Input.GetKeyUp(reloadKey))
        {
            if (checkTime <= 0.5f && mags > 0)
            {
                checkTime = 0;
                StopAllCoroutines();
                StartCoroutine(Reload());
            }
            else if (checkTime > 0.5f || mags == 0)
            {
                checkTime = 0;
                ammoCheck = false;
            }
        }
    }

    private void AmmoCheckSlider()
    {
        if (ammoCheck) CanvasFadeIn(ammoCanvas);
        else if(!ammoCheck) CanvasFadeOut(ammoCanvas);
        if(ammo != 0) fillImage.material.SetFloat("_Spread", ammo * 0.1f + 0.1f);
        else fillImage.material.SetFloat("_Spread", 0.1f);
    }

    private void CanvasFadeIn(CanvasGroup canvas)
    {
        canvas.alpha = Mathf.Lerp(canvas.alpha, 1f, Time.deltaTime * 5f);
    }

    private void CanvasFadeOut(CanvasGroup canvas)
    {
        canvas.alpha = Mathf.Lerp(canvas.alpha, 0f, Time.deltaTime * 5f);
    }

    private void MuzzleFlash()
    {
        muzzleFlash.Play();
        muzzleLight.SetActive(true);
    }

    private void MagChange()
    {
        ammo = Mathf.Clamp(ammo, 0, 8);
        
        while(ammo != 8)
        {
            if (mags <= 0) return;
            ammo++;
            mags--;
        }
    }

    private IEnumerator Reload()
    {
        if (!isReload)
        {
            int lastAmmo = ammo;

            isReload = true;
            
            MagChange();
            anim.SetTrigger(PlayerAnimParameter.Reload);
            
            if (lastAmmo != 0) ammo += 1;
            
            yield return new WaitForSeconds(2.8f);

            outOfAmmo = false;
            isReload = false;
        }
    }
}
