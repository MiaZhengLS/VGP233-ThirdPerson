using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class ThirdCharacterController : MonoBehaviour
{
    [Header("Camera rotation")]
    [SerializeField]
    private Transform trCameraFollowTarget;
    [SerializeField]
    private float cameraSensitivity;
    [SerializeField]
    private float cameraPitchMin; //-90
    [SerializeField]
    private float cameraPitchMax; // 90

    private float cameraYaw;
    private float cameraPitch;

    [Header("Animation")]
    [SerializeField]
    private Animator animator;
    [SerializeField]
    private float fallAniStartHeight;

    private bool fallAniStarted;

    private int animParamMoveSpeed;
    private int animParamJump;
    private int animParamLand;

    [Header("Movement")]
    [SerializeField]
    private float moveSpeed;
    [SerializeField]
    private float runSpeed;
    [SerializeField]
    private float accer;
    [SerializeField]
    private float decel;
    [SerializeField]
    private float walkToRunTime;

    private float walkToRunTimer;

    [Header("Jump")]
    [SerializeField]
    private float gravity;
    [SerializeField]
    private float fastFallGravity;
    [SerializeField]
    private float jumpHeight;
    [SerializeField]
    private LayerMask groundLayer;
    [SerializeField]
    private float groundCheckPosOffset;
    [SerializeField]
    private float groundCheckRadius;
    [SerializeField]
    private float jumpAgainTime;


    private Collider[] groundHitColliders;
    private bool isGrounded;
    private float jumpAgainTimer;

    private Vector3 velocity;
    private Vector3 velocityAddedValue;
    private float externalJumpSpeed;

    private CharacterController characterController;
    private Transform trCharacter;
    
    void Start()
    {
        characterController = GetComponent<CharacterController>();
        trCharacter = transform;
        groundHitColliders = new Collider[5];

        animParamMoveSpeed = Animator.StringToHash("Speed");
        animParamJump = Animator.StringToHash("Jump");
        animParamLand = Animator.StringToHash("Land");
    }

    // depends on framerate
    void Update()
    {
        Jump();
        Move();
        characterController.Move(velocity * Time.deltaTime + velocityAddedValue);
    }

    void Jump()
    {
        animator.SetBool(animParamLand, false);
        GroundCheck();
        bool desireJump = Input.GetButtonDown("Jump");
        if(desireJump && isGrounded && jumpAgainTimer <= 0)
        {
            // v^2 = -2gh
            velocity.y = Mathf.Sqrt(-2 * gravity * jumpHeight);
            fallAniStarted = false;
            animator.SetTrigger(animParamJump);
            jumpAgainTimer = jumpAgainTime;
        }
        else
        {
            jumpAgainTimer -= Time.deltaTime;
            if(velocity.y > 0)
            {
                // v += gt
                velocity.y += gravity * Time.deltaTime;
            }
            else
            {
                velocity.y += fastFallGravity * Time.deltaTime;
                if (isGrounded)
                {
                    fallAniStarted = false;
                    velocity.y = -2f;
                    animator.SetBool(animParamLand, true);
                }
                else if(CanFallAniStart())
                {
                    fallAniStarted = true;
                    animator.SetBool(animParamLand, true);
                }
            }
        }
    }

    bool CanFallAniStart()
    {
        if(!fallAniStarted)
        {
            return Physics.Raycast(trCharacter.position, Vector3.down, fallAniStartHeight, groundLayer);
        }
        else
        {
            return false;
        }
    }

    void GroundCheck()
    {
        var groundCheckCenter = new Vector3(trCharacter.position.x, trCharacter.position.y + groundCheckPosOffset, trCharacter.position.z);
        isGrounded = Physics.OverlapSphereNonAlloc(groundCheckCenter, groundCheckRadius, groundHitColliders, groundLayer, QueryTriggerInteraction.Ignore) > 0;
        velocityAddedValue = Vector3.zero;
        if (isGrounded)
        {
            foreach(var col in groundHitColliders)
            {
                if(col != null)
                {
                    if(col.CompareTag("MovingPlatform"))
                    {
                        var movingPlatform = col.GetComponent<MovingPlatform>();
                        velocityAddedValue += movingPlatform.MoveSpeed;

                        var posDiffCausedByRotation = Vector3.zero;
                        if(movingPlatform.RotateSpeed != Vector3.zero)
                        {
                            var quaternion = Quaternion.Euler(movingPlatform.RotateSpeed);
                            trCharacter.rotation *= quaternion;
                            var characterOldPos = trCharacter.position;
                            var movingPlatformPos = movingPlatform.transform.position;
                            var diff = characterOldPos - movingPlatformPos;
                            diff = quaternion * diff;
                            posDiffCausedByRotation = movingPlatformPos + diff - characterOldPos;
                        }

                        velocityAddedValue += posDiffCausedByRotation;
                        break;
                    }
                    else if (col.CompareTag("BouncySurface"))
                    {
                        var pos = trCharacter.position;
                        externalJumpSpeed = col.GetComponent<BouncySurface>().GetSpeedIncrease(new Vector2(pos.x, pos.z));
                        velocityAddedValue += Vector3.up * (externalJumpSpeed * Time.deltaTime);
                        animator.SetTrigger(animParamJump);
                        fallAniStarted = false;
                        break;
                    }
                }
            }
        }
        else
        {
            externalJumpSpeed += gravity * Time.deltaTime;
            externalJumpSpeed = Mathf.Clamp(externalJumpSpeed, 0f, float.MaxValue);
            velocityAddedValue += Vector3.up * (externalJumpSpeed * Time.deltaTime);
        }
    }

    void OnDrawGizmosSelected()
    {
        if(isGrounded)
        {
            Gizmos.color = new Color(0, 0.5f, 0f, 0.5f);
        }
        else
        {
            Gizmos.color = new Color(0.5f, 0f, 0f, 0.5f);
        }

        Gizmos.DrawSphere(transform.position + Vector3.up * groundCheckPosOffset, groundCheckRadius);
        Gizmos.color = Color.green;
        Gizmos.DrawLine(transform.position, transform.position + Vector3.down * fallAniStartHeight);
    }

    void Move()
    {
        var xMove = Input.GetAxis("Horizontal");
        var zMove = Input.GetAxis("Vertical");
        var realMovementDir = Quaternion.Euler(Vector3.up * trCameraFollowTarget.eulerAngles.y) * new Vector3(xMove, 0, zMove);
        var reverse = Vector3.Angle(realMovementDir, velocity) > 90f;
        float targetSpeed = 0f;
        float targetRotation = 0f;

        bool run = false;
        if(!Mathf.Approximately(xMove, 0f) || !Mathf.Approximately(zMove, 0f))
        {
            targetSpeed = moveSpeed;
            targetRotation = Mathf.Atan2(xMove, zMove) * Mathf.Rad2Deg + trCameraFollowTarget.eulerAngles.y;
            trCharacter.rotation = Quaternion.Euler(0, targetRotation, 0);

            if(isGrounded)
            {
                if(reverse)
                {
                    walkToRunTimer = -1;
                }
                else if(walkToRunTimer < 0)
                {
                    walkToRunTimer = Time.deltaTime;
                }
                else
                {
                    walkToRunTimer += Time.deltaTime;
                }
            }
            else
            {
                walkToRunTimer = -1;
            }
            run = walkToRunTimer > walkToRunTime;
            targetSpeed = run ? runSpeed : moveSpeed;
        }
        else
        {
            walkToRunTimer = -1;
        }
        float acc = 0f;
        if(reverse)
        {
            acc = decel;
        }
        else if(run)
        {
            // y = 1-cos(x*pi*0.5)
            acc = Mathf.Lerp(0, accer, 1f - Mathf.Cos(Mathf.Clamp01(walkToRunTimer - walkToRunTime) * Mathf.PI * 0.5f));
        }
        else
        {
            acc = accer;
        }
        // Vector3.forward: (0,0,1)
        Vector3 faceDir = (Quaternion.Euler(0, targetRotation, 0) * Vector3.forward).normalized;
        velocity.x = Mathf.MoveTowards(velocity.x, faceDir.x * targetSpeed, acc * Time.deltaTime);
        velocity.z = Mathf.MoveTowards(velocity.z, faceDir.z * targetSpeed, acc * Time.deltaTime);
        animator.SetFloat(animParamMoveSpeed, Mathf.Sqrt(Mathf.Pow(velocity.x, 2) + Mathf.Pow(velocity.z, 2)));
    }
   
    void LateUpdate()
    {
        float mouseX = Input.GetAxis("Mouse X");
        float mouseY = Input.GetAxis("Mouse Y");
        if(Mathf.Abs(mouseX) >= cameraSensitivity || Mathf.Abs(mouseY) >= cameraSensitivity)
        {
            cameraYaw += mouseX;
            cameraPitch += mouseY;
        }

        cameraYaw = ClampAngle(cameraYaw, float.MinValue, float.MaxValue);
        cameraPitch = ClampAngle(cameraPitch, cameraPitchMin, cameraPitchMax);
        trCameraFollowTarget.rotation = Quaternion.Euler(cameraPitch, cameraYaw, 0);
    }
   
    float ClampAngle(float angle, float low, float high)
    {
        angle = angle > 360f ? (angle - 360f) : angle < -360f ? angle + 360f : angle;
        return Mathf.Clamp(angle, low, high);
    }
}
