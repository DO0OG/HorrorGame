using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.InputSystem;

public class Player_Move : MonoBehaviour
{
    [Header("Keybinds")]
    private PlayerInput playerInput;
    private InputActionMap playerActionMap;
    private InputAction moveAction;
    private InputAction sprintAction;
    private InputAction aimAction;

    [Header("Movement")]
    [SerializeField] private Rigidbody rb;
    [SerializeField] private Transform orientation;
    [SerializeField] private float gravityForce = 8f;
    [SerializeField] private float nowSpeed;
    [SerializeField] private float moveSpeed = 5f;
    [SerializeField] private float sprintSpeed = 8f;
    [SerializeField] private float groundDrag;
    [SerializeField] private float dValue = 5f;
    [SerializeField] private float stamina;
    [SerializeField] private float maxStamina;
    private Vector3 moveDirection;

    [Header("Check")]
    internal static bool isSprint = false;
    internal static bool isMoving = false;
    internal static bool isRestoreStamina = false;
    internal static bool grounded = false;

    [Header("Ground Check")]
    [SerializeField] private float playerHeight;
    [SerializeField] private LayerMask Ground;

    [Header("Animator")]
    [SerializeField] private Animator anim;

    // Start is called before the first frame update
    void Start()
    {
        anim = GetComponentInChildren<Animator>();
        playerInput = GetComponent<PlayerInput>();
        rb = GetComponent<Rigidbody>();

        rb.freezeRotation = true;

        maxStamina = stamina;

        playerActionMap = playerInput.actions.FindActionMap("Player");
        moveAction = playerActionMap.FindAction("Move");
        sprintAction = playerActionMap.FindAction("Sprint");
        aimAction = playerActionMap.FindAction("Aim");

        moveAction.performed += ctx => {
            Vector2 dir = ctx.ReadValue<Vector2>();
            moveDirection = new Vector3(dir.x, 0, dir.y);
            isMoving = true;
        };

        moveAction.canceled += ctx => {
            moveDirection = Vector3.zero;
            isMoving = false;
        };
    }

    // Update is called once per frame
    void Update()
    {
        //¶¥ Ã¼Å©
        grounded = Physics.Raycast(transform.position, Vector3.down, playerHeight/2 + 0.1f, Ground);

        if (!grounded) rb.AddForce(Vector3.down * gravityForce);

        SpeedControl();
        Sprint();
        AnimateControl();

        if (isSprint) DecreaseStamina();
        if (!isSprint && stamina != maxStamina) IncreaseStamina();

        if (grounded) rb.drag = groundDrag;
        else rb.drag = 0;
    }

    private void FixedUpdate()
    {
        MovePlayer();
    }

    private void AnimateControl()
    {
        anim.SetBool(PlayerAnimParameter.Move, isMoving);
        anim.SetBool(PlayerAnimParameter.Sprint, isSprint);
    }

    private void MovePlayer()
    {
        if(moveDirection != Vector3.zero)
        {
            Vector3 worldMoveDirection = orientation.TransformDirection(moveDirection);
            Vector3 flatMoveDirection = worldMoveDirection;
            flatMoveDirection.y = 0;

            rb.AddForce(flatMoveDirection * nowSpeed * 10f, ForceMode.Force);
        }
    }

    private void SpeedControl()
    {
        Vector3 flatVel = new Vector3(rb.velocity.x, 0f, rb.velocity.z);

        if (flatVel.magnitude > nowSpeed)
        {
            Vector3 limitedVel = flatVel.normalized * nowSpeed;
            rb.velocity = new Vector3(limitedVel.x, rb.velocity.y, limitedVel.z);
        }
    }

    private void Sprint()
    {
        if (aimAction.ReadValue<float>() != 0)
        {
            isSprint = false;
            nowSpeed = moveSpeed - 2f;
            return;
        }
        if (stamina <= 0)
        {
            isRestoreStamina = true;
            isSprint = false;
            return;
        }
        else if(stamina >= 25f)
        {
            isRestoreStamina = false;
        }
        
        if (!isRestoreStamina && sprintAction.ReadValue<float>() != 0 && moveDirection != Vector3.zero)
        {
            isSprint = true;
            nowSpeed = sprintSpeed;
        }
        else if(isRestoreStamina)
        {
            isSprint = false;
            nowSpeed = moveSpeed;
        }
        else
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

    private void OnCollisionEnter(Collision collision)
    {
        if (collision.gameObject.layer == LayerMask.NameToLayer("Ground"))
        {
            StartCoroutine(Camera_Movement.ShakeCamera(Camera.main.transform, 0.5f, 0.1f));
        }
    }
}
