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

    [Header("正常状态")]
    [SerializeField] float NormalSpeed = 0.0f;
    [SerializeField] float NormalJumpForce = 0.0f;
    [SerializeField] float NormalMinFlipSpeed = 0.1f;
    [SerializeField] float NormalJumpGravityScale = 1.0f;
    [SerializeField] float NormalFallGravityScale = 1.0f;

    [Header("梦游状态")]
    [SerializeField] float SleepSpeed = 0.0f;
    [SerializeField] float SleepJumpForce = 0.0f;
    [SerializeField] float SleepMinFlipSpeed = 0.1f;
    [SerializeField] float SleepJumpGravityScale = 1.0f;
    [SerializeField] float SleepFallGravityScale = 1.0f;

    [Header("Other")]
    [SerializeField] bool resetSpeedOnLand = false;
    [SerializeField] Transform footPoint;

    private float Speed = 0.0f;
    private float JumpForce = 0.0f;
    private float MinFlipSpeed = 0.1f;
    private float JumpGravityScale = 1.0f;
    private float FallGravityScale = 1.0f;


    // Input
    private Vector2 movementInput;
    private bool jumpInput;
    private bool transformInput;

    // Player Component
    private Animator animator;
    private Rigidbody2D rigidbody;
    private Collider2D collider;
    private LayerMask middleGroundMask;

    // Player State
    private Vector2 prevVelocity;
    private GroundType groundType;
    private bool isFlipped;
    private bool isJumping;
    private bool isFalling;
    private bool isSleep = false;

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

        // Animator paramater Hash id
        animatorGroundedBool = Animator.StringToHash("Grounded");
        animatorRunningSpeed = Animator.StringToHash("RunningSpeed");
        animatorJumpTrigger = Animator.StringToHash("Jump");
        animatorTransformTrigger = Animator.StringToHash("Transform");
        animatorFlipTrigger = Animator.StringToHash("Flip");

        // init player property
        SetNormalProperty();
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
        if (!isJumping && Input.GetKeyDown(KeyCode.Space))
            jumpInput = true;

        if (Input.GetKeyDown(KeyCode.Tab))
            transformInput = true;
        
    }

    private void FixedUpdate()
    {
        UpdateTranform();
        UpdateGrounding();
        UpdateVelocity();
        UpdateDirection();
        UpdateJump();
        UpdateGravityScale();
        UpdatePlayerState();
    }

    #region 更新函数 
    private void UpdateTranform()
    {
        if (transformInput)
        {
            transformInput = false;
            // 判断是否睡眠，修改玩家参数
            if (isSleep)
                SetNormalProperty();
            else
                SetSleepProperty();
            isSleep = !isSleep;
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
            transform.localScale = Vector3.one;
        }
        else if (rigidbody.velocity.x < -MinFlipSpeed && !isFlipped)
        {
            isFlipped = true;
            transform.localScale = flippedScale;
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
        // Set falling flag
        if (isJumping && rigidbody.velocity.y < 0)
            isFalling = true;

        // Jump
        // Ray cast Ground
        RaycastHit2D hit = Physics2D.Raycast(footPoint.position,new Vector2(0,-1),0.2f, middleGroundMask);
        
        if (jumpInput && groundType != GroundType.None && hit)
        {
            // Jump using impulse force
            rigidbody.AddForce(new Vector2(0, JumpForce), ForceMode2D.Impulse);

            // Set animator
            animator.SetTrigger(animatorJumpTrigger);

            // We've consumed the jump, reset it.
            jumpInput = false;

            // Set jumping flag
            isJumping = true;

            // TODO Play audio
        }

        // Landed
        else if (isJumping && isFalling && groundType != GroundType.None)
        {
            // Since collision with ground stops rigidbody, reset velocity
            if (resetSpeedOnLand)
            {
                prevVelocity.y = rigidbody.velocity.y;
                rigidbody.velocity = prevVelocity;
            }

            // Reset jumping flags
            isJumping = false;
            isFalling = false;

            // TODO Play audio
        }
        else if(isJumping&&!isFalling)
        {
            isJumping = false;
        }
    }
    void UpdatePlayerState()
    {

    }
    #endregion

    void SetSleepProperty()
    {
        Speed = SleepSpeed;
        JumpForce = SleepJumpForce;
        MinFlipSpeed = SleepMinFlipSpeed;
        JumpGravityScale = SleepJumpGravityScale;
        FallGravityScale = SleepFallGravityScale;
    }

    void SetNormalProperty()
    {
        Speed = NormalSpeed;
        JumpForce = NormalJumpForce;
        MinFlipSpeed = NormalMinFlipSpeed;
        JumpGravityScale = NormalJumpGravityScale;
        FallGravityScale = NormalFallGravityScale;
    }
}
