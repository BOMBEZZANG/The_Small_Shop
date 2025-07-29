using UnityEngine;
using System;
using System.Collections;

// NPC 상태
public enum NPCState
{
    Idle,
    Moving,
    Talking,
    Busy,
    Disabled
}

[RequireComponent(typeof(Rigidbody2D))]
[RequireComponent(typeof(Collider2D))]
public abstract class NPCController : InteractableObject
{
    [Header("NPC Data")]
    [SerializeField] protected NPCData npcData;
    
    [Header("Components")]
    protected Rigidbody2D rb;
    protected Animator animator;
    protected SpriteRenderer spriteRenderer;
    
    [Header("State")]
    protected NPCState currentState = NPCState.Idle;
    protected NPCState previousState = NPCState.Idle;
    
    [Header("Detection")]
    [SerializeField] protected float playerDetectionRange = 5f;
    [SerializeField] protected LayerMask playerLayer = -1;
    protected Transform playerTransform;
    protected bool playerInRange = false;
    
    [Header("Movement")]
    protected Vector2 moveDirection;
    protected bool isMoving = false;
    protected float currentMoveSpeed;
    
    [Header("Visual")]
    [SerializeField] protected GameObject exclamationMark;      // ! 표시
    [SerializeField] protected GameObject questionMark;         // ? 표시
    [SerializeField] protected GameObject speechBubble;         // 말풍선
    [SerializeField] protected Transform indicatorPosition;     // 표시 위치
    
    // 이벤트
    public static event Action<NPCController, NPCState> OnNPCStateChanged;
    public static event Action<NPCController> OnPlayerDetected;
    public static event Action<NPCController> OnPlayerLost;
    
    // 대화 관련
    protected bool hasSpokenToPlayer = false;
    protected DialogueData currentAvailableDialogue;
    
    // ===== 초기화 =====
    protected override void Awake()
    {
        base.Awake();
        
        // 컴포넌트 가져오기
        rb = GetComponent<Rigidbody2D>();
        animator = GetComponent<Animator>();
        spriteRenderer = GetComponent<SpriteRenderer>();
        
        // Rigidbody2D 설정
        rb.gravityScale = 0f;
        rb.freezeRotation = true;
        
        // 표시 아이콘 초기화
        if (exclamationMark) exclamationMark.SetActive(false);
        if (questionMark) questionMark.SetActive(false);
        if (speechBubble) speechBubble.SetActive(false);
    }
    
    protected virtual void Start()
    {
        // NPC 데이터 검증
        if (npcData == null)
        {
            Debug.LogError($"{gameObject.name}: NPCData가 설정되지 않았습니다!");
            enabled = false;
            return;
        }
        
        // 데이터 유효성 검사
        if (!npcData.ValidateData())
        {
            enabled = false;
            return;
        }
        
        // InteractableObject 설정 (부모 클래스)
        interactionName = $"{npcData.npcName}와 대화";
        interactionTime = 0f; // 즉시 상호작용
        isReusable = true;
        
        // 플레이어 찾기
        GameObject player = GameObject.FindGameObjectWithTag("Player");
        if (player != null)
        {
            playerTransform = player.transform;
        }
        
        // 파생 클래스 초기화
        InitializeNPC();
    }
    
    // ===== 추상 메서드 (파생 클래스에서 구현) =====
    protected abstract void InitializeNPC();
    protected abstract void UpdateNPCBehavior();
    
    // ===== Update =====
    protected virtual void Update()
    {
        // 플레이어 감지
        DetectPlayer();
        
        // 상태별 업데이트
        UpdateStateBehavior();
        
        // 파생 클래스 업데이트
        UpdateNPCBehavior();
        
        // 애니메이션 업데이트
        UpdateAnimation();
    }
    
    protected virtual void FixedUpdate()
    {
        // 이동 처리
        if (isMoving && currentState == NPCState.Moving)
        {
            rb.linearVelocity = moveDirection * currentMoveSpeed;
        }
        else
        {
            rb.linearVelocity = Vector2.zero;
        }
    }
    
    // ===== 플레이어 감지 =====
    protected virtual void DetectPlayer()
    {
        if (playerTransform == null) return;
        
        float distance = Vector2.Distance(transform.position, playerTransform.position);
        bool wasInRange = playerInRange;
        playerInRange = distance <= playerDetectionRange;
        
        // 플레이어가 범위에 들어옴
        if (!wasInRange && playerInRange)
        {
            OnPlayerEnterRange();
            OnPlayerDetected?.Invoke(this);
        }
        // 플레이어가 범위를 벗어남
        else if (wasInRange && !playerInRange)
        {
            OnPlayerExitRange();
            OnPlayerLost?.Invoke(this);
        }
    }
    
    // ===== 플레이어 범위 진입/이탈 =====
    protected virtual void OnPlayerEnterRange()
    {
        Debug.Log($"{npcData.npcName}: 플레이어 감지!");
        
        // 가능한 대화 확인
        currentAvailableDialogue = npcData.GetAvailableDialogue();
        
        // 시각적 표시
        if (currentAvailableDialogue != null)
        {
            ShowIndicator(hasSpokenToPlayer ? questionMark : exclamationMark);
        }
    }
    
    protected virtual void OnPlayerExitRange()
    {
        Debug.Log($"{npcData.npcName}: 플레이어가 멀어졌습니다.");
        HideAllIndicators();
    }
    
    // ===== 상태 관리 =====
    public virtual void ChangeState(NPCState newState)
    {
        if (currentState == newState) return;
        
        previousState = currentState;
        currentState = newState;
        
        OnStateChanged();
        OnNPCStateChanged?.Invoke(this, newState);
    }
    
    protected virtual void OnStateChanged()
    {
        Debug.Log($"{npcData.npcName}: {previousState} → {currentState}");
        
        switch (currentState)
        {
            case NPCState.Idle:
                StopMoving();
                break;
                
            case NPCState.Moving:
                // 이동 시작
                break;
                
            case NPCState.Talking:
                StopMoving();
                FacePlayer();
                break;
                
            case NPCState.Busy:
                StopMoving();
                break;
        }
    }
    
    // ===== 상태별 행동 =====
    protected virtual void UpdateStateBehavior()
    {
        switch (currentState)
        {
            case NPCState.Idle:
                // 대기 중 행동
                break;
                
            case NPCState.Moving:
                // 이동 중 행동
                break;
                
            case NPCState.Talking:
                // 대화 중에는 플레이어를 바라봄
                if (playerTransform != null)
                {
                    FacePlayer();
                }
                break;
        }
    }
    
    // ===== 상호작용 (InteractableObject 오버라이드) =====
    public override bool CanInteract(PlayerController player)
    {
        // 기본 조건 체크
        if (!base.CanInteract(player)) return false;
        
        // NPC 특수 조건
        if (currentState == NPCState.Busy || currentState == NPCState.Disabled)
            return false;
            
        if (currentState == NPCState.Talking)
            return false;
            
        // 대화 가능한지 확인
        return currentAvailableDialogue != null;
    }

    [Obsolete]
    public override void StartInteraction(PlayerController player)
    {
        base.StartInteraction(player);
        
        // 대화 시작
        if (currentAvailableDialogue != null)
        {
            StartDialogue();
        }
    }

    // ===== 대화 시작 =====
    [Obsolete]
    protected virtual void StartDialogue()
    {
        if (DialogueManager.instance == null)
        {
            Debug.LogError("DialogueManager를 찾을 수 없습니다!");
            return;
        }
        
        // 상태 변경
        ChangeState(NPCState.Talking);
        
        // 표시 숨기기
        HideAllIndicators();
        
        // 대화 시작
        DialogueManager.instance.StartDialogue(currentAvailableDialogue, npcData);
        hasSpokenToPlayer = true;
        
        // 대화 종료 이벤트 구독
        DialogueManager.OnDialogueEnded += OnDialogueEnded;
    }
    
    // ===== 대화 종료 =====
    protected virtual void OnDialogueEnded()
    {
        // 이벤트 구독 해제
        DialogueManager.OnDialogueEnded -= OnDialogueEnded;
        
        // 상태 복구
        ChangeState(NPCState.Idle);
        
        // 다음 대화 확인
        currentAvailableDialogue = npcData.GetAvailableDialogue();
        
        // 플레이어가 아직 범위 내에 있으면 표시
        if (playerInRange && currentAvailableDialogue != null)
        {
            ShowIndicator(questionMark);
        }
    }
    
    // ===== 이동 관련 =====
    protected virtual void MoveTo(Vector2 targetPosition, float speed)
    {
        Vector2 direction = (targetPosition - (Vector2)transform.position).normalized;
        SetMoveDirection(direction, speed);
        ChangeState(NPCState.Moving);
    }
    
    protected virtual void SetMoveDirection(Vector2 direction, float speed)
    {
        moveDirection = direction.normalized;
        currentMoveSpeed = speed;
        isMoving = moveDirection.magnitude > 0.01f;
        
        // 스프라이트 방향 전환
        if (isMoving && spriteRenderer != null)
        {
            spriteRenderer.flipX = moveDirection.x < 0;
        }
    }
    
    protected virtual void StopMoving()
    {
        isMoving = false;
        moveDirection = Vector2.zero;
        currentMoveSpeed = 0f;
    }
    
    // ===== 플레이어 바라보기 =====
    protected virtual void FacePlayer()
    {
        if (playerTransform == null || spriteRenderer == null) return;
        
        Vector2 directionToPlayer = playerTransform.position - transform.position;
        spriteRenderer.flipX = directionToPlayer.x < 0;
    }
    
    // ===== 애니메이션 =====
    protected virtual void UpdateAnimation()
    {
        if (animator == null) return;
        
        // 기본 애니메이션 파라미터
        animator.SetBool("IsMoving", isMoving);
        animator.SetFloat("MoveX", moveDirection.x);
        animator.SetFloat("MoveY", moveDirection.y);
        animator.SetBool("IsTalking", currentState == NPCState.Talking);
    }
    
    // ===== 시각적 표시 =====
    protected virtual void ShowIndicator(GameObject indicator)
    {
        HideAllIndicators();
        
        if (indicator != null)
        {
            indicator.SetActive(true);
            
            // 위치 조정
            if (indicatorPosition != null)
            {
                indicator.transform.position = indicatorPosition.position;
            }
            else
            {
                indicator.transform.position = transform.position + npcData.indicatorOffset;
            }
        }
    }
    
    protected virtual void HideAllIndicators()
    {
        if (exclamationMark) exclamationMark.SetActive(false);
        if (questionMark) questionMark.SetActive(false);
        if (speechBubble) speechBubble.SetActive(false);
    }
    
    // ===== 디버그 =====
    protected virtual void OnDrawGizmosSelected()
    {
        // 플레이어 감지 범위
        Gizmos.color = Color.yellow;
        Gizmos.DrawWireSphere(transform.position, playerDetectionRange);
        
        // 상호작용 범위
        if (npcData != null)
        {
            Gizmos.color = Color.green;
            Gizmos.DrawWireSphere(transform.position, npcData.interactionRange);
        }
    }
    
    // ===== Getter/Setter =====
    public NPCData GetNPCData() => npcData;
    public NPCState GetCurrentState() => currentState;
    public bool IsPlayerInRange() => playerInRange;
    public bool HasSpokenToPlayer() => hasSpokenToPlayer;
}