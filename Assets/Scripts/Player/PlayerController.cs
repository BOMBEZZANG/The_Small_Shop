using UnityEngine;

[RequireComponent(typeof(Rigidbody2D))]
public class PlayerController : MonoBehaviour
{
    [Header("Movement Settings")]
    [SerializeField] private float walkSpeed = 5f;
    [SerializeField] private float runSpeed = 8f;
    
    [Header("Stamina Settings")]
    [SerializeField] private int walkStaminaCost = 0;      // 걷기 스태미나 소모 (0 = 소모 없음)
    [SerializeField] private int runStaminaCost = 1;       // 달리기 스태미나 소모 (초당)
    [SerializeField] private float staminaUseInterval = 1f; // 스태미나 소모 간격
    
    // 컴포넌트
    private Rigidbody2D rb;
    
    // 상태
    private Vector2 moveDirection;
    private bool isRunning = false;
    private bool isMoving = false;
    private float staminaUseTimer = 0f;
    
    // 이동 방향 (애니메이션용)
    private Vector2 lastMoveDirection = Vector2.down; // 마지막으로 바라본 방향
    
    // 이벤트
    public static event System.Action<bool> OnPlayerMovingChanged;  // 이동 상태 변경
    public static event System.Action<bool> OnPlayerRunningChanged; // 달리기 상태 변경
    public static event System.Action<Vector2> OnPlayerPositionChanged; // 위치 변경
    public static event System.Action<Vector2> OnPlayerDirectionChanged; // 방향 변경 (애니메이션용)
    
    void Awake()
    {
        rb = GetComponent<Rigidbody2D>();
        rb.gravityScale = 0f; // 탑다운 뷰이므로 중력 제거
    }
    
    void Update()
    {
        HandleInput();
        HandleStaminaUsage();
    }

    [System.Obsolete]
    void FixedUpdate()
    {
        HandleMovement();
    }
    
    // ===== 입력 처리 =====
    private void HandleInput()
    {
        // 방향키/WASD 입력
        float horizontal = Input.GetAxisRaw("Horizontal"); // A, D, ←, →
        float vertical = Input.GetAxisRaw("Vertical");     // W, S, ↑, ↓
        
        moveDirection = new Vector2(horizontal, vertical).normalized;
        
        // 이동 방향 저장 (idle 상태에서도 마지막 방향 유지)
        if (moveDirection != Vector2.zero)
        {
            lastMoveDirection = moveDirection;
            OnPlayerDirectionChanged?.Invoke(lastMoveDirection);
        }
        
        // 달리기 입력 (Shift)
        bool wasRunning = isRunning;
        isRunning = Input.GetKey(KeyCode.LeftShift) && moveDirection.magnitude > 0;
        
        // 달리기 상태 변경 시 이벤트 발생
        if (wasRunning != isRunning)
        {
            OnPlayerRunningChanged?.Invoke(isRunning);
        }
        
        // 이동 상태 확인
        bool wasMoving = isMoving;
        isMoving = moveDirection.magnitude > 0;
        
        // 이동 상태 변경 시 이벤트 발생
        if (wasMoving != isMoving)
        {
            OnPlayerMovingChanged?.Invoke(isMoving);
            Debug.Log($"플레이어 이동 상태: {(isMoving ? "이동 중" : "정지")}");
        }
    }

    // ===== 이동 처리 =====
    [System.Obsolete]
    private void HandleMovement()
    {
        if (moveDirection.magnitude <= 0)
        {
            rb.velocity = Vector2.zero;
            return;
        }
        
        // 달리기 시 스태미나 체크
        float currentSpeed = walkSpeed;
        if (isRunning)
        {
            // 스태미나가 있는지 확인
            if (StaminaManager.instance.GetStamina() > 0)
            {
                currentSpeed = runSpeed;
            }
            else
            {
                // 스태미나 없으면 달리기 취소
                isRunning = false;
                OnPlayerRunningChanged?.Invoke(false);
                Debug.Log("스태미나 부족으로 달리기 중단!");
            }
        }
        
        // 실제 이동 (2D)
        rb.velocity = moveDirection * currentSpeed;
        
        // 위치 변경 이벤트
        OnPlayerPositionChanged?.Invoke(transform.position);
    }
    
    // ===== 스태미나 사용 =====
    private void HandleStaminaUsage()
    {
        // 이동 중이 아니면 리턴
        if (!isMoving) 
        {
            staminaUseTimer = 0f;
            return;
        }
        
        // 스태미나 소모 타이머
        staminaUseTimer += Time.deltaTime;
        
        if (staminaUseTimer >= staminaUseInterval)
        {
            staminaUseTimer = 0f;
            
            // 달리기 중일 때만 스태미나 소모
            if (isRunning && runStaminaCost > 0)
            {
                bool success = StaminaManager.instance.UseStamina(runStaminaCost);
                
                if (!success)
                {
                    // 스태미나 부족 시 달리기 중단
                    isRunning = false;
                    OnPlayerRunningChanged?.Invoke(false);
                }
            }
        }
    }

    // ===== 외부에서 호출 가능한 메서드 =====

    // 이동 가능 여부 설정 (대화 중, 메뉴 열림 등)
    [System.Obsolete]
    public void SetMovementEnabled(bool enabled)
    {
        if (!enabled)
        {
            moveDirection = Vector2.zero;
            rb.velocity = Vector2.zero;
            isMoving = false;
            isRunning = false;
            OnPlayerMovingChanged?.Invoke(false);
            OnPlayerRunningChanged?.Invoke(false);
        }
    }
    
    // 특정 위치로 텔레포트
    public void TeleportTo(Vector2 position)
    {
        transform.position = position;
        OnPlayerPositionChanged?.Invoke(position);
    }
    
    // 현재 상태 확인
    public bool IsMoving() => isMoving;
    public bool IsRunning() => isRunning;
    public Vector2 GetMoveDirection() => moveDirection;
    public Vector2 GetLastDirection() => lastMoveDirection;
    
    // 방향 확인 (4방향)
    public Direction GetFacingDirection()
    {
        if (Mathf.Abs(lastMoveDirection.x) > Mathf.Abs(lastMoveDirection.y))
        {
            return lastMoveDirection.x > 0 ? Direction.Right : Direction.Left;
        }
        else
        {
            return lastMoveDirection.y > 0 ? Direction.Up : Direction.Down;
        }
    }
}

// 4방향 열거형
public enum Direction
{
    Up,
    Down,
    Left,
    Right
}