using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class Player_Move : MonoBehaviour
{
    [Header("Keybinds")]
    public KeyCode sprintKey = KeyCode.LeftShift;

    [Header("Movement")]
    private float h_input;
    private float v_input;
    private Vector3 moveDirection;
    public Rigidbody rb;
    public Transform orientation;
    public float forceGravity = 5f;    //중력 레벨
    public float nowSpeed;
    public float moveSpeed = 5f;
    public float sprintSpeed = 8f;
    public float groundDrag;
    public float dValue = 5f;
    public float stamina;
    public float maxStamina;

    [Header("Check")]
    public bool isSlope;
    public bool isSprint;
    public bool isMoving = false;

    [Header("Ground Check")]
    public float playerHeight;
    public LayerMask Ground;
    public bool grounded;

    [Header("Slope Handling")]
    public float maxSlopeAngle;
    private RaycastHit slopeHit;
    private bool exitingSlope;

    // Start is called before the first frame update
    void Start()
    {
        rb = GetComponent<Rigidbody>();
        rb.freezeRotation = true;

        maxStamina = stamina;
    }

    // Update is called once per frame
    void Update()
    {
        //땅 체크
        grounded = Physics.Raycast(transform.position, Vector3.down, playerHeight * 0.64f + 0.2f, Ground);

        K_Input();
        SpeedControl();
        Sprint();

        isSlope = OnSlope();

        if (grounded) rb.drag = groundDrag;
        else rb.drag = 0;

        if (isSprint) DecreaseStamina();
        if (!isSprint && stamina != maxStamina) IncreaseStamina();
    }

    private void FixedUpdate()
    {
        MovePlayer();
    }

    private void K_Input()
    {
        h_input = Input.GetAxisRaw("Horizontal");
        v_input = Input.GetAxisRaw("Vertical");

        if ((h_input == -1f || h_input == 1f) || (v_input == -1f || v_input == 1f)) isMoving = true;
        else isMoving = false;
    }

    private void MovePlayer()
    {
        //이동 방향 계산
        moveDirection = orientation.forward * v_input + orientation.right * h_input;

        //경사
        if (OnSlope() && !exitingSlope)
        {
            rb.AddForce(GetSlopeMoveDirection() * nowSpeed * 18f, ForceMode.Force);

            if (rb.velocity.y > 0)
                rb.AddForce(Vector3.down * 2f, ForceMode.Force);
        }

        //땅
        else if (grounded)
            rb.AddForce(moveDirection.normalized * nowSpeed * 10f, ForceMode.Force);
    }

    private void SpeedControl()
    {
        if (OnSlope() && !exitingSlope)
        {
            if(rb.velocity.magnitude > nowSpeed)
            {
                rb.velocity = rb.velocity.normalized * nowSpeed;
            }    
        }
        else
        {
            Vector3 flatVel = new Vector3(rb.velocity.x, 0f, rb.velocity.z);

            if (flatVel.magnitude > nowSpeed)
            {
                Vector3 limitedVel = flatVel.normalized * nowSpeed;
                rb.velocity = new Vector3(limitedVel.x, rb.velocity.y, limitedVel.z);
            }
        }
    }

    private bool OnSlope()
    {
        if (Physics.Raycast(transform.position, Vector3.down, out slopeHit, playerHeight * 0.5f + 0.3f))
        {
            float angle = Vector3.Angle(Vector3.up, slopeHit.normal);
            return angle < maxSlopeAngle && angle != 0;
        }

        return false;
    }

    private void Sprint()
    {

        if (stamina <= 0)
        {
            isSprint = false;
            return;
        }
        
        if (Input.GetKey(sprintKey) && ((Input.GetKey(KeyCode.W) || Input.GetKey(KeyCode.A) || Input.GetKey(KeyCode.D))) & !Input.GetKey(KeyCode.S))
        {
            isSprint = true;
            nowSpeed = sprintSpeed;
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

    private Vector3 GetSlopeMoveDirection()
    {
        return Vector3.ProjectOnPlane(moveDirection, slopeHit.normal).normalized;
    }

    private void OnCollisionEnter(Collision collision)
    {
    }

    private void OnCollisionExit(Collision collision)
    {
    }

    private void OnTriggerEnter(Collider other)
    {
    }
}
