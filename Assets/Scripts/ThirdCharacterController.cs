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

    [Header("Movement")]
    [SerializeField]
    private float moveSpeed;

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

    private Collider[] groundHitColliders;
    private bool isGrounded;

    private Vector3 velocity;

    private CharacterController characterController;
    private Transform trCharacter;
    
    void Start()
    {
        characterController = GetComponent<CharacterController>();
        trCharacter = transform;
        groundHitColliders = new Collider[5];
    }

    // depends on framerate
    void Update()
    {
        Jump();
        Move();
        characterController.Move(velocity * Time.deltaTime);
    }

    void Jump()
    {
        GroundCheck();
        bool desireJump = Input.GetButtonDown("Jump");
        if(desireJump && isGrounded)
        {
            // v^2 = -2gh
            velocity.y = Mathf.Sqrt(-2 * gravity * jumpHeight);
        }
        else
        {
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
                    velocity.y = -2f;
                }
            }
        }
    }

    void GroundCheck()
    {
        var groundCheckCenter = new Vector3(trCharacter.position.x, trCharacter.position.y + groundCheckPosOffset, trCharacter.position.z);
        isGrounded = Physics.OverlapSphereNonAlloc(groundCheckCenter, groundCheckRadius, groundHitColliders, groundLayer, QueryTriggerInteraction.Ignore) > 0;
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
    }

    void Move()
    {
        var xMove = Input.GetAxis("Horizontal");
        var zMove = Input.GetAxis("Vertical");
        
        float targetSpeed = 0f;
        float targetRotation = 0f;
        if(!Mathf.Approximately(xMove, 0f) || !Mathf.Approximately(zMove, 0f))
        {
            targetSpeed = moveSpeed;
            targetRotation = Mathf.Atan2(xMove, zMove) * Mathf.Rad2Deg + trCameraFollowTarget.eulerAngles.y;
            trCharacter.rotation = Quaternion.Euler(0, targetRotation, 0);
        }
        // Vector3.forward: (0,0,1)
        Vector3 faceDir = (Quaternion.Euler(0, targetRotation, 0) * Vector3.forward).normalized;
        velocity.x = faceDir.x * targetSpeed;
        velocity.z = faceDir.z * targetSpeed;
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
