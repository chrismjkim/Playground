using System;
using System.Collections;
using UnityEngine;
using UnityEngine.InputSystem;

public class Player : MonoBehaviour
{
    [Header("이동")]
    [SerializeField] private float moveSpeed = 5f;
    [SerializeField] private float jumpForce = 5f;

    // [Header("카메라 제어")]
    // [SerializeField] private float sensitivityX = 0.08f; // 마우스 델타 스케일
    // [SerializeField] private bool invertX = false;

    [Header("지면 체크")]
    [SerializeField] private Transform groundCheck;
    [SerializeField] private float groundCheckRadius = 0.2f;
    [SerializeField] private LayerMask groundMask;
    [SerializeField] private bool onSlope;
    [SerializeField] private float slopeStickForce = 10f;
    [SerializeField] private bool exitingSlope;
    private bool jumpQueued;
    private bool jumpThisStep;
    
    public bool OnSlope()
    {
        Vector3 origin = groundCheck != null ? groundCheck.position : transform.position;
        float rayDistance = groundCheckRadius + 0.1f;
        if (Physics.Raycast(origin, Vector3.down, out slopeHit, rayDistance, groundMask, QueryTriggerInteraction.Ignore))
        {
            float slopeAngle = Vector3.Angle(slopeHit.normal, Vector3.up);
            if (slopeAngle > 0.01f)
            {
                onSlope = true;
                return true;
            }
            onSlope = false;
            return false;
        }
        onSlope = false;
        return false;
    }
    /*
    오브젝트 아웃라인 로직
    활성화/비활성화 방법: 스크립트 컴포넌트 활성화/비활성화..?

    isSelectingRecall이 True일때만 활성화
        RayCast가 처음 닿은 오브젝트의 Layer가 RecallableObject이면 아웃라인 활성화
        RayCast가 오브젝트에서 벗어나면 아웃라인 비활성화
            =>  처음 닿은 오브젝트 != 현재 닿아있는 첫 오브젝트
    */

    private Rigidbody rb;
    private RaycastHit slopeHit;
    private Vector2 moveInput = Vector2.zero;

    private float yaw; // 누적 y 회전
    private float lookX; //이번 틱에 적용할 yaw 델타(?)

    private void Awake()
    {
        rb = GetComponent<Rigidbody>();
    }

    private void Start()
    {
        Cursor.visible = false;                     // 커서가 안 보이도록
        Cursor.lockState = CursorLockMode.Locked;   // 커서를 화면 중앙에 고정

        yaw = transform.eulerAngles.y;
    }

    private void Update()
    {
        // 런타임 중 ESC로 커서 고정/해제
        if (Keyboard.current != null && Keyboard.current.escapeKey.wasPressedThisFrame)
        {
            Cursor.visible = true;
            Cursor.lockState = CursorLockMode.None;
        }
        if (Cursor.lockState != CursorLockMode.Locked &&
            Mouse.current != null &&
            Mouse.current.leftButton.wasPressedThisFrame)
        {
            Cursor.visible = false;
            Cursor.lockState = CursorLockMode.Locked;
        }
    }

    private void FixedUpdate()
    {
        // 1) 회전 처리
        /*
        yaw += lookX;
        rb.MoveRotation(Quaternion.Euler(0f, yaw, 0f));
        lookX = 0f;
        */
        Vector3 cameraForward = Camera.main.transform.forward;
        cameraForward.y = 0f; // 상하 기울기는 무시

        if (cameraForward.sqrMagnitude > 0.01f)
        {
            Quaternion targetRotation = Quaternion.LookRotation(cameraForward);
            rb.MoveRotation(targetRotation);
            // rb.rotation = targetRotation;
        }

        // 2) 이동 처리 (AddForce 방식)
        Vector3 fwd = transform.forward; fwd.y = 0f; fwd.Normalize();
        Vector3 right = transform.right; right.y = 0f; right.Normalize();

        Vector3 moveDir = right * moveInput.x + fwd * moveInput.y;
        if (moveDir.sqrMagnitude > 1f) moveDir.Normalize();
        bool isGrounded = IsGrounded();
        jumpThisStep = false;
        if (jumpQueued && isGrounded)
        {
            jumpQueued = false;
            jumpThisStep = true;
            exitingSlope = true;

            // 현재 y속도 리셋 후 임펄스 (벽 비빔/경사에서 점프감 안정)
            Vector3 v = rb.linearVelocity;
            v.y = 0f;
            rb.linearVelocity = v;

            rb.AddForce(Vector3.up * jumpForce, ForceMode.Impulse);
        }
        if (exitingSlope && isGrounded && rb.linearVelocity.y <= 0f && !jumpThisStep)
        {
            exitingSlope = false;
        }
        bool isOnSlope = isGrounded && OnSlope();
        Vector3 slopeMoveDir = isOnSlope ? Vector3.ProjectOnPlane(moveDir, slopeHit.normal) : moveDir;
        Vector3 targetVelocity;
        // 목표 속도 계산
        if (isOnSlope)
        {
            targetVelocity = slopeMoveDir * moveSpeed;
        }
        else
        {
            targetVelocity = moveDir * moveSpeed;
        }

        // 현재 속도와 목표 속도의 차이(가속도) 계산
        // Y축 속도는 보존하여 중력 영향을 유지합니다.
        Vector3 currentVelocity = rb.linearVelocity;
        Vector3 velocityChange = targetVelocity - currentVelocity;
        if (!isOnSlope || exitingSlope)
        {
            velocityChange.y = 0;
        }

        // ForceMode.VelocityChange를 사용하여 질량에 관계없이 즉각적으로 속도를 변경
        rb.AddForce(velocityChange, ForceMode.VelocityChange);

        // 경사면에 서 있다면 수직항력(부착력) 제공, 점프 시에는 작동하지 않음.
        if (isOnSlope && !exitingSlope/*moveInput.sqrMagnitude < 0.01f*/)
        {
            Vector3 v = rb.linearVelocity;
            if (moveInput.sqrMagnitude < 0.01f && Mathf.Abs(v.y) > 0.001f)
            {
                v.y = 0f;
                rb.linearVelocity = v;
            }
            rb.AddForce(-slopeHit.normal * slopeStickForce, ForceMode.Acceleration);
        }
        // 경사면 미끄러짐 방지(중력 영향을 받지 않기 위함)
        rb.useGravity = !isOnSlope || exitingSlope;
    }


    public void OnMove(InputAction.CallbackContext context)
    {
        if ( context.performed || context.canceled )
        // started: 입력 시작됨 (Hold 입력 체크, 애니메이션 트리거 등)
        // performed: 입력 동작이 수행됨 (공격, 점프와 같은 단발성 동작)
        // canceled: 입력 취소됨(이동 중지 등 키를 뗄 때)
        {
            moveInput = context.ReadValue<Vector2>();
        }
    }

    public void OnJump(InputAction.CallbackContext context)
    {
        if (!context.performed) return;
        if (!IsGrounded()) return;

        jumpQueued = true;
    }
    /*
    public void OnLook(InputAction.CallbackContext context)
    {
        Vector2 delta = context.ReadValue<Vector2>();

        float dx = delta.x * sensitivityX * (invertX ? -1f : 1f);
        yaw += dx;
    }
    */
    public void OnRecall(InputAction.CallbackContext context)
    {
        if (context.started) {
            GameManager.instance.recall.Recall();
        }
    }

    private bool IsGrounded()
    {
        if (groundCheck == null) return false;
        return Physics.CheckSphere(groundCheck.position, groundCheckRadius, groundMask, QueryTriggerInteraction.Ignore);
    }
    private void OnDrawGizmosSelected()
    {
        if (groundCheck == null) return;
        Gizmos.color = Color.yellow;
        Gizmos.DrawWireSphere(groundCheck.position, groundCheckRadius);
    }
}
