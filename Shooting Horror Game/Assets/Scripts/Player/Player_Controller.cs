using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.InputSystem;
using static UnityEngine.InputSystem.InputAction;

public class Player_Controller : MonoBehaviour
{
    [Header("Keybinds")]
    internal static PlayerInput playerInput;
    internal static InputActionMap playerActionMap;
    internal static InputAction moveAction;
    internal static InputAction sprintAction;
    internal static InputAction crouchAction;
    internal static InputAction fireAction;
    internal static InputAction aimAction;
    internal static InputAction reloadAction;
    internal static InputAction funcAction;
    internal static InputAction flashAction;

    [Header("Movement")]
    [SerializeField] private Rigidbody rb;
    [SerializeField] private Transform orientation;
    [SerializeField] private float gravityForce = 5f;
    [SerializeField] private float nowSpeed;
    [SerializeField] private float moveSpeed = 2f;
    [SerializeField] private float sprintSpeed = 3.5f;
    [SerializeField] private float groundDrag;
    [SerializeField] private float dValue = 5f;
    [SerializeField] private float stamina;
    [SerializeField] private float maxStamina;
    private Vector2 moveInput;
    private Vector3 moveDirection;

    [Header("Ground Check")]
    [SerializeField] private float playerHeight;
    [SerializeField] private LayerMask Ground;

    [Header("Animator")]
    [SerializeField] private Animator anim;

    [Header("FootStep")]
    private AudioSource audioSource;

    [Header("ETC")]
    private CapsuleCollider capsuleCollider;
    private Light flashLight;

    private bool isSprint { get; set; }
    private bool isMoving { get; set; }
    private bool isRestoreStamina { get; set; }
    private bool grounded { get; set; }
    private bool isCrouch { get; set; }
    private Player_Shot playerShot { get; set; }
    internal static bool isFire { get; set; }
    internal static bool isAim { get; set; }
    internal static bool isReload { get; set; }

    // Start is called before the first frame update
    void Awake()
    {
        anim = GetComponentInChildren<Animator>();
        rb = GetComponent<Rigidbody>();
        capsuleCollider = GetComponent<CapsuleCollider>();
        flashLight = GetComponentInChildren<Light>();
        audioSource = GetComponent<AudioSource>();
        playerShot = GetComponent<Player_Shot>();

        rb.freezeRotation = true;
        flashLight.enabled = false;

        maxStamina = stamina;

        KeyBind(); // 키 바인딩 설정
    }

    // Update is called once per frame
    void Update()
    {
        //땅 체크
        grounded = Physics.Raycast(transform.position, Vector3.down, playerHeight / 2 + 0.1f, Ground);

        if (!grounded) rb.AddForce(Vector3.down * gravityForce);

        SpeedControl();
        AnimateControl();
        FootStep("Ground");

        if (flashAction.triggered) flashLight.enabled = !flashLight.enabled;

        if (isSprint) DecreaseStamina();
        if (!isSprint && stamina != maxStamina) IncreaseStamina();

        if (grounded) rb.drag = groundDrag;
        else rb.drag = 0;
    }

    private void FixedUpdate()
    {
        MovePlayer();
    }

    private void KeyBind()
    {
        playerInput = GetComponent<PlayerInput>();

        playerActionMap = playerInput.actions.FindActionMap("Player");
        moveAction = playerActionMap.FindAction("Move");
        sprintAction = playerActionMap.FindAction("Sprint");
        crouchAction = playerActionMap.FindAction("Crouch");
        fireAction = playerActionMap.FindAction("Fire");
        aimAction = playerActionMap.FindAction("Aim");
        reloadAction = playerActionMap.FindAction("Reload");
        funcAction = playerActionMap.FindAction("Function");
        flashAction = playerActionMap.FindAction("Flash");

        moveAction.performed += ctx => {
            moveInput = ctx.ReadValue<Vector2>();
            nowSpeed = moveSpeed;
            isMoving = true;
        };
        moveAction.canceled += ctx => {
            moveInput = Vector2.zero;
            isMoving = false;
        };

        sprintAction.performed += ctx => {
            if (grounded && isMoving && !isCrouch && !isAim)
            {
                isSprint = true;
                nowSpeed = sprintSpeed;
            }
        };
        sprintAction.canceled += ctx => {
            isSprint = false;
            nowSpeed = moveSpeed;
        };

        crouchAction.performed += ctx => {
            if (!isCrouch && grounded)
            {
                capsuleCollider.height = playerHeight / 2;
                rb.AddForce(Vector3.down * 5f, ForceMode.Impulse);
                isSprint = false;
                isCrouch = true;
            }
        };
        crouchAction.canceled += ctx => {
            if (isCrouch)
            {
                capsuleCollider.height = playerHeight;
                isCrouch = false;
            }
        };

        fireAction.started += ctx =>
        {
            if (playerShot.nowReload) return;
            if (Player_Shot.ammo == 0) GunSound.EmptySound();
            isFire = true;
            playerShot.Shot();
        };
        fireAction.canceled += ctx =>
        {
            isFire = false;
        };

        aimAction.started += ctx =>
        {
            if (playerShot.nowReload) return;
            if (isSprint) isSprint = false;
            isAim = true;
        };
        aimAction.canceled += ctx =>
        {
            isAim = false;
        };

        reloadAction.started += ctx =>
        {
            isReload = true;
        };
        reloadAction.canceled += ctx =>
        {
            isReload = false;
            playerShot.ReloadFunc();
        };
    }

    private void AnimateControl()
    {
        float multiplier = 1;

        anim.SetBool(PlayerAnimParameter.Move, isMoving);
        anim.SetBool(PlayerAnimParameter.Sprint, isSprint);
        anim.SetBool(PlayerAnimParameter.Crouch, isCrouch);
        anim.SetBool(PlayerAnimParameter.Aim, isAim);

        if (isCrouch)
            anim.SetFloat(PlayerAnimParameter.CrouchSpeed, multiplier / 2);
        else
            anim.SetFloat(PlayerAnimParameter.CrouchSpeed, multiplier);
    }

    private void MovePlayer()
    {
        if (isMoving)
        {
            moveDirection = orientation.forward * moveInput.y + orientation.right * moveInput.x;
            moveDirection.y = 0;

            rb.AddForce(moveDirection.normalized * nowSpeed * 10f, ForceMode.Force);
        }
    }

    private void SpeedControl()
    {
        if (isAim || isCrouch)
        {
            isSprint = false;
            nowSpeed = moveSpeed / 2f;
            return;
        }
        else
        {
            nowSpeed = moveSpeed;
        }

        if (stamina <= 0)
        {
            isRestoreStamina = true;
            isSprint = false;
            return;
        }
        else if (stamina >= 25f)
        {
            isRestoreStamina = false;
        }

        if (!isRestoreStamina && isSprint && isMoving && !isCrouch)
        {
            nowSpeed = sprintSpeed;
        }
        else if (isRestoreStamina)
        {
            isSprint = false;
            nowSpeed = moveSpeed;
        }
    }

    private void DecreaseStamina()
    {
        if (stamina > 0)
        {
            stamina -= dValue * Time.deltaTime;
        }
    }

    private void IncreaseStamina()
    {
        if (stamina < maxStamina)
        {
            stamina += dValue * Time.deltaTime / 2;
        }
    }

    private void FootStep(string tag)
    {
        switch (tag)
        {
            case "Ground":
                audioSource.clip = Managers.Resource.LoadAudioClip("FootStep/Walk");
                break;
        }

        if (isSprint)
        {
            audioSource.pitch = 1.5f;
            audioSource.volume = 1;
        }
        else if (isCrouch || isAim)
        {
            audioSource.pitch = 0.4f;
            audioSource.volume = 0.5f;
        }
        else
        {
            audioSource.pitch = 0.775f;
            audioSource.volume = 0.75f;
        }

        if (isMoving)
            audioSource.enabled = true;
        else
            audioSource.enabled = false;
    }

    private void OnCollisionEnter(Collision collision)
    {
    }
}
