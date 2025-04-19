using System.Collections;
using System.Collections.Generic;
using Unity.Mathematics;
using UnityEngine;

public class ThirdPersonCharacter : MonoBehaviour
{
    private float Horizontal;//
    private float Vertical;//
    public bool Grounded = true;
    public LayerMask GroundLayer;
    public float GroundedRadius;
    public CharacterController Controller;//used in new controller
    public float MoveSpeed = 2.0f;
    public float SpeedChangeRate = 10f;
    public float VerticalSpeed;
    public float TerminalSpeed;
    public float Speed;//
    private float JumpMoveSpeed;
    public float RotateSpeed;//
    public float JumpTimeout;
    public float JumpHeight = 1.5f;
    public float Gravity = -10.0f;
    float JumpTimeoutDelta;
    bool IsJumping;

    public Transform Camera;//
    public float TurnSmoothTime=0.1f;//

    public bool CanMove = true;

    public Animator Animator;
    public PlayerAnimation playerAnimation;
    bool ifWalking;
    public bool scene3;
    void Awake()
    {
        JumpMoveSpeed = Speed;
    }
    void Update()
    {
        //if (!CanMove)
        //    return;
        Horizontal = Input.GetAxisRaw("Horizontal");
        Vertical = Input.GetAxisRaw("Vertical");
        JumpAndGravity();
        GroundedCheck();
        Vector3 dir = new Vector3(Horizontal, 0, Vertical).normalized;
        if (dir.magnitude >= 0.1f)
        {
            float targetAngle = Mathf.Atan2(dir.x, dir.z) * Mathf.Rad2Deg+ Camera.eulerAngles.y;
            //float angle = Mathf.SmoothDampAngle(transform.eulerAngles.y, targetAngle, ref RotateSpeed, TurnSmoothTime);
            transform.rotation = Quaternion.Euler(0, targetAngle, 0);
            Vector3 movedir = Quaternion.Euler(0, targetAngle, 0) * Vector3.forward;
            Controller.Move((movedir * Speed + new Vector3(0, VerticalSpeed, 0)) * Time.deltaTime);//
            ifWalking = true;
        }
        else
        {
            Controller.Move(new Vector3(0, VerticalSpeed, 0) * Time.deltaTime);
            ifWalking = false;
        }
        UpdateAnimation();
    }
    void UpdateAnimation()
    {
        Animator.SetBool("ifwalk", ifWalking);
        if (Grounded && playerAnimation.ifanimation )
        {
            Animator.SetBool("ifjump", false);
        }
    }

    //    Move();

    //}
    //void Move()
    //{
    //    float SpeedOffset = 0.1f;
    //    float CurrentHorizantalSpeed;
    //    float TargetRotation = 0.0f;

    //    CurrentHorizantalSpeed = new Vector3(Controller.velocity.x, 0, Controller.velocity.z).magnitude;
    //    if (CurrentHorizantalSpeed < MoveSpeed - SpeedOffset || CurrentHorizantalSpeed > MoveSpeed + SpeedOffset)
    //    {
    //        Speed = Mathf.Lerp(CurrentHorizantalSpeed, MoveSpeed, Time.deltaTime * SpeedChangeRate);
    //        Speed = Mathf.Round(Speed * 1000f) / 1000f;
    //    }
    //    else
    //        Speed = MoveSpeed;
    //    Vector3 MoveDir = new Vector3(Horizontal, 0, Vertical).normalized;
    //    Controller.Move(MoveDir * Speed * Time.deltaTime + new Vector3(0, VerticalSpeed, 0) * Time.deltaTime);
    //    if (Horizontal != 0 || Vertical != 0)
    //    {
    //        Quaternion targetRotation = Quaternion.LookRotation(MoveDir, Vector3.up);
    //        transform.rotation = Quaternion.Slerp(transform.rotation, targetRotation, Time.deltaTime * RotateSpeed);
    //    }

    //    //TargetRotation = Mathf.Atan2(Horizontal, Vertical) * Mathf.Rad2Deg;
    //    //transform.rotation = Quaternion.Euler(0, TargetRotation, 0);

    //}
    void GroundedCheck()
    {
        Vector3 SpherePosition = transform.position;
        Grounded = Physics.CheckSphere(SpherePosition, GroundedRadius, GroundLayer);
    }
    void JumpAndGravity()
    {
        if (Grounded)

        {
            if (VerticalSpeed < 0.0f)
                VerticalSpeed = -2f;
            if (Input.GetKeyDown(KeyCode.Space) && JumpTimeoutDelta <= 0 && !scene3)
            {
                VerticalSpeed = Mathf.Sqrt(JumpHeight * -2f * Gravity);
                IsJumping = true;
                Animator.SetBool("ifjump", true);
                playerAnimation.ifanimation = false;
            }
            if (JumpTimeoutDelta >= 0.0f)
                JumpTimeoutDelta -= Time.deltaTime;
            Speed = MoveSpeed;
        }
        else
        {
            JumpTimeoutDelta = JumpTimeout;
            Speed = JumpMoveSpeed;
        }
        if (VerticalSpeed < TerminalSpeed)
            VerticalSpeed += Gravity * Time.deltaTime;
        //Double Jump
        if (!Grounded && IsJumping && Input.GetKeyDown(KeyCode.Space) && !scene3)
        {
            VerticalSpeed = Mathf.Sqrt(JumpHeight * -2f * Gravity);
            IsJumping = false;
            Animator.SetBool("ifjump", true);
            //playerAnimation.ifanimation = false;
        }
    }

    //void OnCollisionEnter(Collision collision)
    //{
    //    if(collision.gameObject.CompareTag("Enemy"))
    //    Debug.Log("hi");
    //}
}
