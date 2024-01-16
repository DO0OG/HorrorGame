using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class Player_Move : MonoBehaviour
{
    [Header("Keybinds")]
    internal static KeyCode sprintKey = KeyCode.LeftShift;
    internal static KeyCode jumpKey = KeyCode.Space;

    [Header("Movement")]
    private float h_input;
    private float v_input;
    private Vector3 moveDirection;
    [SerializeField] private Rigidbody rb;
    [SerializeField] private Transform orientation;
    [SerializeField] private float jumpForce = 8f;
    [SerializeField] private float gravityForce = 8f;
    [SerializeField] private float nowSpeed;
    [SerializeField] private float moveSpeed = 5f;
    [SerializeField] private float sprintSpeed = 8f;
    [SerializeField] private float groundDrag;
    [SerializeField] private float dValue = 5f;
    [SerializeField] private float stamina;
    [SerializeField] private float maxStamina;

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
        rb = GetComponent<Rigidbody>();
        rb.freezeRotation = true;

        anim = GetComponentInChildren<Animator>();

        maxStamina = stamina;
    }

    // Update is called once per frame
    void Update()
    {
        //땅 체크
        grounded = Physics.Raycast(transform.position, Vector3.down, playerHeight/2 + 0.1f, Ground);

        if (!grounded) rb.AddForce(Vector3.down * gravityForce);

        Key_Input();
        SpeedControl();
        Sprint();
        Jump();
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
        anim.SetBool(PlayerAnimParameter.Sprint, isSprint);
        anim.SetBool(PlayerAnimParameter.Move, isMoving);
    }

    private void Key_Input()
    {
        h_input = Input.GetAxisRaw("Horizontal");
        v_input = Input.GetAxisRaw("Vertical");

        if ((h_input != 0) || (v_input != 0)) isMoving = true;
        else isMoving = false;
    }

    private void MovePlayer()
    {
        //이동 방향 계산
        moveDirection = orientation.forward * v_input + orientation.right * h_input;
        moveDirection.y = 0;

        rb.AddForce(moveDirection.normalized * moveSpeed * 10f, ForceMode.Force);
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
        if (Input.GetMouseButton(1))
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
        
        if (!isRestoreStamina && Input.GetKey(sprintKey) && ((Input.GetKey(KeyCode.W) || Input.GetKey(KeyCode.A) || Input.GetKey(KeyCode.D))) & !Input.GetKey(KeyCode.S))
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

    private void Jump()
    {
        if(Input.GetKeyDown(jumpKey) && grounded) rb.AddForce(Vector3.up * jumpForce, ForceMode.Impulse);
    }

    private void OnCollisionEnter(Collision collision)
    {
        if (collision.gameObject.layer == LayerMask.NameToLayer("Ground"))
        {
            StartCoroutine(Camera_Movement.ShakeCamera(Camera.main.transform, 0.5f, 0.1f));
        }
    }
}
