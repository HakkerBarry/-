using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public enum GroundType
{
    None,
    Plane
}
public enum PlayerState
{
    Alive,
    Dead
}

public class PlayerController : MonoBehaviour
{
    readonly Vector3 flippedScale = new Vector3(-1, 1, 1);

    [Header("����״̬")]
    [SerializeField] float NormalSpeed = 0.0f;
    [SerializeField] float NormalJumpSpeed = 0.0f;
    [SerializeField] float NormalMinFlipSpeed = 0.1f;
    [SerializeField] float NormalJumpGravityScale = 1.0f;
    [SerializeField] float NormalFallGravityScale = 1.0f;
    [SerializeField] Collider2D[] realEnableCollider;

    [Header("����״̬")]
    [SerializeField] float SleepSpeed = 0.0f;
    [SerializeField] float SleepJumpSpeed = 0.0f;
    [SerializeField] float SleepMinFlipSpeed = 0.1f;
    [SerializeField] float SleepJumpGravityScale = 1.0f;
    [SerializeField] float SleepFallGravityScale = 1.0f;
    [SerializeField] float DashDuration = 0.2f;
    [SerializeField] float DashSpeed = 100.0f;
    [SerializeField] Collider2D[] dreamEnableCollider;

    [Header("Other")]
    [SerializeField] bool resetSpeedOnLand = false;
    [SerializeField] Transform footPoint;
    [SerializeField] GameObject dashTrail;
    [SerializeField] Collider2D forwardCollider;
    [SerializeField] Material sceneMaterial;
    [SerializeField, Range(0.01f, 0.2f)] float sceneSwitchSpeed = 0.05f;

    [Header("Input")]


    private float Speed = 0.0f;
    private float JumpSpeed = 0.0f;
    private float MinFlipSpeed = 0.1f;
    private float JumpGravityScale = 1.0f;
    private float FallGravityScale = 1.0f;


    // Input
    private Vector2 movementInput;
    [SerializeField] private bool jumpInput;
    private bool transformInput;
    private bool dashInput;

    // Player Component
    private Animator animator;
    private Rigidbody2D rigidbody;
    private Collider2D collider;
    private LayerMask middleGroundMask;

    // Player State
    private Vector2 prevVelocity;
    private GroundType groundType;
    private Vector2 preDashVelocity;
    private Vector3 startPostion;
    private float ConstantDashDuration;
    [Header("Debug")]
    [SerializeField] bool isFlipped;
    [SerializeField] bool isJumping;
    [SerializeField] bool isFalling;
    [SerializeField] bool isSleeping = false;
    [SerializeField] bool canJumpAgain = false;
    [SerializeField] bool inAir;
    [SerializeField] bool isDashing = false;

    // Animator paramater
    private int animatorGroundedBool;
    private int animatorRunningSpeed;
    private int animatorJumpTrigger;
    private int animatorTransformTrigger;
    private int animatorFlipTrigger;
    // Start is called before the first frame update
    void Start()
    {
        animator = GetComponent<Animator>();
        rigidbody = GetComponent<Rigidbody2D>();
        collider = GetComponent<Collider2D>();
        middleGroundMask = LayerMask.GetMask("MiddleBackground");
        startPostion = this.transform.position;

        // Animator paramater Hash id
        animatorGroundedBool = Animator.StringToHash("Grounded");
        animatorRunningSpeed = Animator.StringToHash("RunningSpeed");
        animatorJumpTrigger = Animator.StringToHash("Jump");
        animatorTransformTrigger = Animator.StringToHash("Transform");
        animatorFlipTrigger = Animator.StringToHash("Flip");

        // init player property
        SetNormalProperty();
        dashTrail.SetActive(false);
        ConstantDashDuration = DashDuration;
    }
    // Update is called once per frame
    void Update()
    {
        // Horizontal movement
        float moveHorizontal = 0.0f;

        if (Input.GetKey(KeyCode.A)||Input.GetKey(KeyCode.LeftArrow))
            moveHorizontal = -1.0f;
        else if (Input.GetKey(KeyCode.D) || Input.GetKey(KeyCode.RightArrow))
            moveHorizontal = 1.0f;
        movementInput = new Vector2(moveHorizontal, 0);

        // Jumping input
        if (Input.GetKeyDown(KeyCode.W) || Input.GetKeyDown(KeyCode.UpArrow) || Input.GetKeyDown(KeyCode.Space))
        {
            if(!isSleeping)
            {
                if (!isJumping&&!inAir)
                    jumpInput = true;
            }
            else
            {
                if(inAir&&canJumpAgain)
                    jumpInput = true;
                else if(!isJumping)
                    jumpInput = true;
            }
        }
        if(Input.GetKeyDown(KeyCode.LeftShift))
        {
            dashInput = true;
        }
            

        if (Input.GetKeyDown(KeyCode.E))
            transformInput = true;
        
        if(Input.GetKeyDown(KeyCode.KeypadEnter))
        {
            // TODO ����
        }
    }

    private void FixedUpdate()
    {
        UpdateTranform();
        UpdateGrounding();
        UpdateVelocity();
        UpdateDirection();
        UpdateDash();
        UpdateJump();
        UpdateGravityScale();
        UpdatePlayerState();
        UpdateLerpScene();
    }

    #region ���º��� 
    private void UpdateDash()
    {
        if (!isSleeping)
        {
            dashInput = false;
            return;
        }
        if(dashInput)
        {
            preDashVelocity = rigidbody.velocity;
            if(isFlipped)
                rigidbody.velocity = new Vector2(-DashSpeed, rigidbody.velocity.y);
            else
                rigidbody.velocity = new Vector2(DashSpeed, rigidbody.velocity.y);
            isDashing = true;
            dashTrail.SetActive(true);
            dashInput = false;
        }
        if(isDashing)
        {
            ConstantDashDuration -= Time.fixedDeltaTime;
            if(ConstantDashDuration<0)
            {
                ConstantDashDuration = DashDuration;
                rigidbody.velocity = new Vector2(preDashVelocity.x, rigidbody.velocity.y);
                dashTrail.SetActive(false);
                isDashing = false;
            }
        }


    }
    private void UpdateTranform()
    {
        if (transformInput)
        {
            transformInput = false;
            // �ж��Ƿ�˯�ߣ��޸���Ҳ���
            if (isSleeping)
                SetNormalProperty();
            else
                SetSleepProperty();
            isSleeping = !isSleeping;
            animator.SetTrigger(animatorTransformTrigger);
        }
    }
    private void UpdateGrounding()
    {
        // Use character collider to check if touching ground layers
        if (collider.IsTouchingLayers(middleGroundMask))
            groundType = GroundType.Plane;
        else
            groundType = GroundType.None;

        // Update animator
        animator.SetBool(animatorGroundedBool, groundType != GroundType.None);
    }
    void UpdateVelocity()
    {
        if (isDashing)
            return;
        if (forwardCollider.IsTouchingLayers(middleGroundMask))
        {
            if(isFlipped)// ����
            {
                movementInput.x = Mathf.Max(0, movementInput.x);
            }
            else
            {
                movementInput.x = Mathf.Min(0, movementInput.x);
            }
            
        }
            

        // No acceleration
        Vector2 velocity = rigidbody.velocity;
        velocity.x = (movementInput * Speed).x;
        movementInput = Vector2.zero;
        rigidbody.velocity = velocity;

        // Update animator
        var horizontalSpeedNormalized = Mathf.Abs(velocity.x) / Speed;
        animator.SetFloat(animatorRunningSpeed, horizontalSpeedNormalized);

        // TODO Play audio
       
    }
    void UpdateDirection()
    {
        if (rigidbody.velocity.x > MinFlipSpeed && isFlipped)
        {
            isFlipped = false;
            transform.localScale = new Vector3(Mathf.Abs(transform.localScale.x), transform.localScale.y, transform.localScale.z);
        }
        else if (rigidbody.velocity.x < -MinFlipSpeed && !isFlipped)
        {
            isFlipped = true;
            transform.localScale = new Vector3(-Mathf.Abs(transform.localScale.x), transform.localScale.y, transform.localScale.z);
        }
    }
    private void UpdateGravityScale()
    {
        // Use grounded gravity scale by default.
        var gravityScale = JumpGravityScale;
        gravityScale = rigidbody.velocity.y > 0.0f ? JumpGravityScale : FallGravityScale;
        rigidbody.gravityScale = gravityScale;
    }
    void UpdateJump()
    {
        // ����ģʽ
        if(!isSleeping)
        {
            // Set falling flag
            if (isJumping && rigidbody.velocity.y < 0)
                isFalling = true;

            // Jump
            // Ray cast Ground
            RaycastHit2D hit = Physics2D.Raycast(footPoint.position, new Vector2(0, -1), 0.4f, middleGroundMask);
            if (jumpInput && groundType != GroundType.None && hit)
            {
                Vector2 velocity = rigidbody.velocity;
                velocity.y = JumpSpeed;
                rigidbody.velocity = velocity;
                animator.SetTrigger(animatorJumpTrigger);
                jumpInput = false;
                isJumping = true;
            }

            // Landed
            else if (isJumping && isFalling && groundType != GroundType.None)
            {
                if (resetSpeedOnLand)
                {
                    prevVelocity.y = rigidbody.velocity.y;
                    rigidbody.velocity = prevVelocity;
                }
                isJumping = false;
                isFalling = false;
                // TODO Play audio
            }
            else if (isJumping && !isFalling)
            {
                isJumping = false;
            }
        }
        // ����ģʽ
        else
        { 
            // Set falling flag
            if (isJumping && rigidbody.velocity.y < 0)
                isFalling = true;

            // Jump
            // Ray cast Ground
            if(jumpInput)
            {
                if(inAir)
                {
                    if(canJumpAgain)
                    {
                        Vector2 velocity = rigidbody.velocity;
                        velocity.y = JumpSpeed;
                        rigidbody.velocity = velocity;
                        animator.SetTrigger(animatorJumpTrigger);
                        jumpInput = false;
                        isJumping = true;
                        canJumpAgain = false;
                    }
                }
                else
                {
                    RaycastHit2D hit = Physics2D.Raycast(footPoint.position, new Vector2(0, -1), 0.4f, middleGroundMask);
                    if (groundType != GroundType.None && hit)
                    {
                        Vector2 velocity = rigidbody.velocity;
                        velocity.y = JumpSpeed;
                        rigidbody.velocity = velocity;
                        animator.SetTrigger(animatorJumpTrigger);
                        jumpInput = false;
                        isJumping = true;
                        canJumpAgain = true;
                    }
                }
                
            }
            // Landed
            else if (isJumping && isFalling && groundType != GroundType.None)
            {
                if (resetSpeedOnLand)
                {
                    prevVelocity.y = rigidbody.velocity.y;
                    rigidbody.velocity = prevVelocity;
                }
                isJumping = false;
                isFalling = false;
                canJumpAgain = false;
                // TODO Play audio
            }
            //else if (isJumping && !isFalling)
            //{
            //    isJumping = false;
            //}
        }
        
    }
    void UpdatePlayerState()
    {
        RaycastHit2D hit = Physics2D.Raycast(footPoint.position, new Vector2(0, -1), 0.4f, middleGroundMask);
        Debug.DrawLine
            (footPoint.position, hit.point,Color.red);
        if (!hit)
        {
            if (!inAir)
                canJumpAgain = true;
            inAir = true;
        }
        else
            inAir = false;
    }

    void UpdateLerpScene()
    {
        float lerpValue = sceneMaterial.GetFloat("Vector1_9d12b51f880a4daa83f84a0f9287934a");
        if (isSleeping) sceneMaterial.SetFloat("Vector1_9d12b51f880a4daa83f84a0f9287934a", lerpValue > 0 ? lerpValue - sceneSwitchSpeed : lerpValue);
        else sceneMaterial.SetFloat("Vector1_9d12b51f880a4daa83f84a0f9287934a",  lerpValue < 1 ? lerpValue + sceneSwitchSpeed : lerpValue);
    }
    #endregion

    void SetSleepProperty()
    {
        Speed = SleepSpeed;
        JumpSpeed = SleepJumpSpeed;
        MinFlipSpeed = SleepMinFlipSpeed;
        JumpGravityScale = SleepJumpGravityScale;
        FallGravityScale = SleepFallGravityScale;
        foreach(Collider2D c in realEnableCollider)
        {
            c.enabled = false;
        }
        foreach (Collider2D c in dreamEnableCollider)
        {
            c.enabled = true;
        }
    }

    void SetNormalProperty()
    {
        Speed = NormalSpeed;
        JumpSpeed = NormalJumpSpeed;
        MinFlipSpeed = NormalMinFlipSpeed;
        JumpGravityScale = NormalJumpGravityScale;
        FallGravityScale = NormalFallGravityScale;
        foreach (Collider2D c in realEnableCollider)
        {
            c.enabled = true;
        }
        foreach (Collider2D c in dreamEnableCollider)
        {
            c.enabled = false;
        }
    }

    private void OnTriggerEnter2D(Collider2D other)
    {
        if (other.CompareTag("Die"))
        {
            transform.position = startPostion;
        }
    }
}
