using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using static UnityEngine.UI.Image;
using UnityEngine.UIElements;

public class Player_Controller : MonoBehaviour
{
    [Header("Keybinds")]
    internal static KeyCode sprintKey = KeyCode.LeftShift;
    internal static KeyCode crouchKey = KeyCode.LeftControl;

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
    [SerializeField] private float h_input;
    [SerializeField] private float v_input;
    private Vector3 moveDirection;

    [Header("Ground Check")]
    [SerializeField] private float playerHeight;
    [SerializeField] private LayerMask Ground;

    [Header("Animator")]
    [SerializeField] private Animator anim;

    CapsuleCollider capsuleCollider;

    private bool isSprint { get; set; }
    private bool isMoving { get; set; }
    private bool isRestoreStamina { get; set; }
    private bool grounded { get; set; }
    private bool isCrouch { get; set; }

    // Start is called before the first frame update
    void Start()
    {
        anim = GetComponentInChildren<Animator>();
        rb = GetComponent<Rigidbody>();
        capsuleCollider = GetComponent<CapsuleCollider>();

        rb.freezeRotation = true;

        maxStamina = stamina;
    }

    // Update is called once per frame
    void Update()
    {
        //¶¥ Ã¼Å©
        grounded = Physics.Raycast(transform.position, Vector3.down, playerHeight/2 + 0.1f, Ground);

        if (!grounded) rb.AddForce(Vector3.down * gravityForce);

        MovementControl();
        SpeedControl();
        AnimateControl();

        if (grounded) Crouch();

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
        float multiplier = 1;

        anim.SetBool(PlayerAnimParameter.Move, isMoving);
        anim.SetBool(PlayerAnimParameter.Sprint, isSprint);
        anim.SetBool(PlayerAnimParameter.Crouch, isCrouch);
        if (isCrouch)
            anim.SetFloat(PlayerAnimParameter.CrouchSpeed, multiplier / 2);
        else
            anim.SetFloat(PlayerAnimParameter.CrouchSpeed, multiplier);
    }

    private void MovePlayer()
    {
        h_input = Input.GetAxisRaw("Horizontal");
        v_input = Input.GetAxisRaw("Vertical");

        if (h_input != 0 || v_input != 0) isMoving = true;
        else isMoving = false;

        moveDirection = orientation.forward * v_input + orientation.right * h_input;
        moveDirection.y = 0;

        rb.AddForce(moveDirection.normalized * nowSpeed * 10f, ForceMode.Force);
    }

    private void MovementControl()
    {
        Vector3 flatVel = new Vector3(rb.velocity.x, 0f, rb.velocity.z);

        if (flatVel.magnitude > nowSpeed)
        {
            Vector3 limitedVel = flatVel.normalized * nowSpeed;
            rb.velocity = new Vector3(limitedVel.x, rb.velocity.y, limitedVel.z);
        }
    }

    private void Crouch()
    {
        if (Input.GetKeyDown(crouchKey) && !isCrouch)
        {
            capsuleCollider.height = playerHeight / 2;
            rb.AddForce(Vector3.down * 10f, ForceMode.Impulse);
            isSprint = false;
            isCrouch = true;
        }
        if (Input.GetKeyUp(crouchKey))
        {
            capsuleCollider.height = playerHeight;
            isCrouch = false;
        }
    }

    private void SpeedControl()
    {
        if (Player_Shot.isAim)
        {
            isSprint = false;
            nowSpeed = moveSpeed / 2f;
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

        if (!isRestoreStamina && Input.GetKey(sprintKey) && isMoving && !isCrouch)
        {
            isSprint = true;
            nowSpeed = sprintSpeed;
        }
        else if(isRestoreStamina)
        {
            isSprint = false;
            nowSpeed = moveSpeed;
        }
        else if (isCrouch)
        {
            nowSpeed = moveSpeed / 2f;
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
    }
}
