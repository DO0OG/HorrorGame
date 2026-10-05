using Cinemachine;
using System.Collections;
using UnityEngine;
using UnityEngine.AI;
using UnityEngine.UI;
using UnityEngine.VFX;

public class Player_Shot : MonoBehaviour
{
    [Header("Bullet")]
    private const int MAG_CAPACITY = 8;
    [SerializeField] private GameObject shootEffectPrefab;
    [SerializeField] private GameObject casingPrefab;
    [SerializeField] internal static int ammo = 8;
    [SerializeField] private int mags = 1;   // 보유 탄창 개수 (필드에서 주워서 획득)

    [Header("Muzzle")]
    [SerializeField] private VisualEffect muzzleFlash;
    [SerializeField] private GameObject muzzleLight;
    [SerializeField] private ParticleSystem muzzleSmoke;

    [Header("Ammo Inspection UI")]
    [SerializeField] private CanvasGroup ammoCanvas;
    [SerializeField] private Image fillImage;
    [SerializeField, Min(1f)] private float inspectionFadeSpeed = 7f;
    private Material ammoGaugeMaterial;

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
    internal bool ammoCheck { get; set; }
    internal static bool isAim { get; set; }
    internal bool nowReload { get; set; }
    public Material AmmoInspectionMaterial =>
        fillImage != null ? fillImage.material : null;

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

        if (ammoCanvas != null)
        {
            ammoCanvas.alpha = 0f;
            ammoGaugeMaterial = DiegeticGaugeUtility.Configure(
                ammoCanvas,
                fillImage,
                DiegeticGaugeUtility.AmmoColor,
                true);
        }
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
        if (Player_Controller.currentEquip != Define.EquipType.Gun) return;
        if (outOfAmmo || (!Player_Controller.isReload && ammo < 0 && !ammoCheck)) return;

        anim.SetTrigger(PlayerAnimParameter.Shot);

        ammo--;
        MonsterNoiseSystem.Emit(MonsterNoiseKind.Gunshot, transform.position, 1f, 40f);
        Ray ray = new Ray(firePoint.position, firePoint.forward);
        RaycastHit hit;

        GameObject casingEffect = Instantiate(casingPrefab, casingPoint);
        casingEffect.transform.SetParent(null);

        if (Physics.Raycast(ray, out hit, Mathf.Infinity))
        {
            if (hit.transform.CompareTag("Monster"))
            {
                Monster_Health monsterHealth = hit.transform.GetComponent<Monster_Health>();
                if (monsterHealth != null)
                {
                    monsterHealth.TakeDamage(20);
                }
            }
            if (!hit.transform.CompareTag("Monster"))
            {
                GameObject shootEffect = Instantiate(shootEffectPrefab, hit.point + (hit.normal * 0.01f), Quaternion.LookRotation(hit.normal));
                shootEffect.transform.SetParent(hit.transform);
            }
            // Debug.Log(hit.transform.name);
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

    /// <summary>
    /// 탄창 습득 (Item_MagazinePickup에서 호출)
    /// </summary>
    public void AddMagazine(int count)
    {
        mags += count;
        Debug.Log($"Magazine +{count} (보유 탄창 : {mags})");
    }

    public void ReloadFunc()
    {
        if (checkTime <= 0.5f && ammo == MAG_CAPACITY + 1)
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

    public void CancelAmmoInspection()
    {
        checkTime = 0f;
        ammoCheck = false;
    }

    private void AmmoCheckSlider()
    {
        if (ammoCanvas == null || fillImage == null)
            return;

        DiegeticGaugeUtility.Fade(
            ammoCanvas,
            ammoCheck,
            inspectionFadeSpeed,
            Time.deltaTime);

        float normalizedAmmo = Mathf.Clamp01(ammo / (float)(MAG_CAPACITY + 1));
        DiegeticGaugeUtility.SetLevel(
            ammoGaugeMaterial,
            fillImage,
            normalizedAmmo,
            DiegeticGaugeUtility.AmmoColor,
            ammoCheck);
    }

    private void OnDestroy()
    {
        if (ammoGaugeMaterial != null)
            Destroy(ammoGaugeMaterial);
    }

    private void MuzzleFlash()
    {
        muzzleFlash.Play();
        muzzleLight.SetActive(true);
    }

    private void MagChange()
    {
        // 주워 놓은 탄창 하나를 통째로 교체
        if (mags <= 0) return;

        mags--;
        ammo = MAG_CAPACITY;
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
