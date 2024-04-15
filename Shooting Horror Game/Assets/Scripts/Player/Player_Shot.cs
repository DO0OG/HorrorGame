using Cinemachine;
using System.Collections;
using UnityEngine;
using UnityEngine.AI;
using UnityEngine.UI;
using UnityEngine.VFX;

public class Player_Shot : MonoBehaviour
{
    [Header("Bullet")]
    [SerializeField] private GameObject shootEffectPrefab;
    [SerializeField] private GameObject casingPrefab;
    [SerializeField] internal static int ammo = 8;
    [SerializeField] private int mags = 8;

    [Header("Muzzle")]
    [SerializeField] private VisualEffect muzzleFlash;
    [SerializeField] private GameObject muzzleLight;
    [SerializeField] private ParticleSystem muzzleSmoke;

    [Header("AmmoUI")]
    [SerializeField] private CanvasGroup ammoCanvas;
    [SerializeField] private Image fillImage;

    [Header("Reload")]
    [SerializeField] private float checkTime;

    [Header("Point")]
    [SerializeField] private Transform firePoint;
    [SerializeField] private Transform casingPoint;

    [Header("Animation")]
    private Animator anim;

    [Header("ETC")]
    [SerializeField] private CinemachineImpulseSource impulseSource;

    [SerializeField] private bool outOfAmmo { get; set; }
    [SerializeField] private bool ammoCheck { get; set; }
    internal static bool isAim { get; set; }
    internal bool nowReload { get; set; }

    private AudioSource audioSource;

    // Start is called before the first frame update
    void Start()
    {
        Cursor.lockState = CursorLockMode.Locked;
        Cursor.visible = false;

        anim = GetComponentInChildren<Animator>();
        impulseSource = GetComponent<CinemachineImpulseSource>();
        shootEffectPrefab = Resources.Load<GameObject>("Player/BulletHole");
        casingPrefab = Resources.Load<GameObject>("Player/Casing");
        muzzleSmoke = muzzleFlash.gameObject.GetComponent<ParticleSystem>();
        audioSource = GetComponent<AudioSource>();

        ammoCanvas.alpha = 0f;
    }

    // Update is called once per frame
    void Update()
    {
        ReloadTimeCheck(Player_Controller.isReload);

        Debug.DrawLine(firePoint.position, firePoint.position + firePoint.forward * 10f);

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
    }

    public void Shot()
    {
        if (outOfAmmo || (!Player_Controller.isReload && ammo < 0 && !ammoCheck)) return;

        anim.SetTrigger(PlayerAnimParameter.Shot);

        ammo--;
        Ray ray = new Ray(firePoint.position, firePoint.forward);
        RaycastHit hit;

        GameObject casingEffect = Instantiate(casingPrefab, casingPoint);
        casingEffect.transform.SetParent(null);

        if (Physics.Raycast(ray, out hit, Mathf.Infinity))
        {
            GameObject shootEffect = Instantiate(shootEffectPrefab, hit.point + (hit.normal * 0.01f), Quaternion.LookRotation(hit.normal));
            shootEffect.transform.SetParent(hit.transform);

            if (hit.transform.CompareTag("Monster"))
            {
                Monster_Health monsterHealth = hit.transform.GetComponent<Monster_Health>();
                if (monsterHealth != null)
                {
                    monsterHealth.TakeDamage(20);
                }
            }

            Debug.Log(hit.transform.name);
        }

        impulseSource.GenerateImpulse();
        MuzzleFlash();
        muzzleSmoke.Play();
    }

    private void ReloadTimeCheck(bool isReload)
    {
        if (isReload && !nowReload)
        {
            if (checkTime <= 3f)
            {
                checkTime += Time.deltaTime;
            }
            if (checkTime > 0.5f)
            {
                ammoCheck = true;
            }
        }
    }

    public void ReloadFunc()
    {
        if (checkTime <= 0.5f && ammo == 9)
        {
            checkTime = 0;
            return;
        }

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
        if (!Player_Controller.isReload)
        {
            int lastAmmo = ammo;

            nowReload = true;
            
            Player_Controller.isAim = false;

            MagChange();
            anim.SetTrigger(PlayerAnimParameter.Reload);
            
            if (lastAmmo != 0) ammo += 1;
            
            yield return new WaitForSeconds(2.8f);

            outOfAmmo = false;
            nowReload = false;
        }
    }
}
