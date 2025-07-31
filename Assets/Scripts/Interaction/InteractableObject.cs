using UnityEngine;
using UnityEngine.Events;

public enum InteractionType
{
    Collect,    // 즉시 수집 (코인, 아이템)
    Harvest,    // 시간이 걸리는 수확 (나무, 광석)
    Talk,       // NPC 대화
    Examine,    // 조사하기
    Use         // 사용하기 (문, 스위치)
}

[RequireComponent(typeof(Collider2D))]
public class InteractableObject : MonoBehaviour
{
    [Header("Interaction Settings")]
    [SerializeField] protected InteractionType interactionType;
    [SerializeField] protected string interactionName = "상호작용";  // protected로 변경
    [SerializeField] protected float interactionTime = 0f;           // protected로 변경
    [SerializeField] protected bool isReusable = true;               // protected로 변경
    [SerializeField] private float cooldownTime = 0f;
    
    [Header("Requirements")]
    [SerializeField] private int requiredLevel = 0;
    [SerializeField] private MaterialData requiredTool;
    [SerializeField] private int staminaCost = 0;
    
    [Header("Visual Settings")]
    [SerializeField] private Color highlightColor = Color.yellow;
    [SerializeField] private bool showFloatingIcon = true;
    [SerializeField] private Sprite interactionIcon;
    
    [Header("Events")]
    public UnityEvent<PlayerController> OnInteractionStart;
    public UnityEvent<PlayerController> OnInteractionComplete;
    public UnityEvent OnInteractionFailed;

    [Header("UI Data")]
    [SerializeField] private InteractionData interactionData;

    // Getter 메서드
    public InteractionData GetInteractionData() => interactionData;
    
    // 상태
    private bool isHighlighted = false;
    private bool isInteracting = false;
    private bool isOnCooldown = false;
    private float cooldownTimer = 0f;
    private float interactionTimer = 0f;
    
    // 컴포넌트
    private InteractionVisualizer visualizer;
    
    // protected virtual로 변경하여 상속 가능하게
protected virtual void Awake()
{
    // Collider가 Trigger인지 확인
    Collider2D col = GetComponent<Collider2D>();
    col.isTrigger = true;
    
    // Visualizer 컴포넌트 확인
    visualizer = GetComponent<InteractionVisualizer>();
    if (visualizer == null)
    {
        visualizer = gameObject.AddComponent<InteractionVisualizer>();
    }
}

    [System.Obsolete]
    void Update()
    {
        // 쿨다운 처리
        if (isOnCooldown)
        {
            cooldownTimer -= Time.deltaTime;
            if (cooldownTimer <= 0)
            {
                isOnCooldown = false;
            }
        }
        
        // 상호작용 진행 처리
        if (isInteracting && interactionTime > 0)
        {
            interactionTimer += Time.deltaTime;
            float progress = interactionTimer / interactionTime;
            
            // 진행률 이벤트
            OnInteractionProgress?.Invoke(progress);
            
            if (interactionTimer >= interactionTime)
            {
                CompleteInteraction();
            }
        }
    }
    
    // ===== 상호작용 가능 여부 체크 (virtual로 변경) =====
    public virtual bool CanInteract(PlayerController player)
    {
        if (isOnCooldown || isInteracting) return false;
        
        // 레벨 체크
        if (requiredLevel > 0 && PlayerDataManager.instance.GetLevel() < requiredLevel)
        {
            Debug.Log($"레벨 {requiredLevel} 이상 필요합니다!");
            return false;
        }
        
        // 도구 체크
        if (requiredTool != null)
        {
            var inventory = InventoryManager.instance.GetInventoryItems();
            if (!inventory.ContainsKey(requiredTool))
            {
                Debug.Log($"{requiredTool.materialName}이(가) 필요합니다!");
                return false;
            }
        }
        
        // 스태미나 체크
        if (staminaCost > 0 && StaminaManager.instance.GetStamina() < staminaCost)
        {
            Debug.Log("스태미나가 부족합니다!");
            return false;
        }
        
        return true;
    }

    // ===== 상호작용 시작 (virtual로 변경) =====
    [System.Obsolete]
    public virtual void StartInteraction(PlayerController player)
    {
        if (!CanInteract(player)) 
        {
            OnInteractionFailed?.Invoke();
            return;
        }
        
        isInteracting = true;
        interactionTimer = 0f;
        
        // 스태미나 소모
        if (staminaCost > 0)
        {
            StaminaManager.instance.UseStamina(staminaCost);
        }
        
        // 즉시 완료 타입
        if (interactionTime <= 0)
        {
            CompleteInteraction();
        }
        else
        {
            // 플레이어 이동 정지
            player.SetMovementEnabled(false);
        }
        
        OnInteractionStart?.Invoke(player);
    }

    // ===== 상호작용 완료 =====
    private void CompleteInteraction()
    {
        isInteracting = false;
        
        var player = FindObjectOfType<PlayerController>();
        if (player != null)
        {
            player.SetMovementEnabled(true);
            OnInteractionComplete?.Invoke(player);
        }
        
        // 재사용 불가능한 경우 비활성화
        if (!isReusable)
        {
            Debug.Log($"비활성화: {gameObject.name} (isReusable = {isReusable})");
            gameObject.SetActive(false);
        }
        else if (cooldownTime > 0)
        {
            // 쿨다운 시작
            isOnCooldown = true;
            cooldownTimer = cooldownTime;
        }
    }

    // ===== 상호작용 취소 =====
    [System.Obsolete]
    public void CancelInteraction()
    {
        if (isInteracting)
        {
            isInteracting = false;
            interactionTimer = 0f;
            
            var player = FindObjectOfType<PlayerController>();
            if (player != null)
            {
                player.SetMovementEnabled(true);
            }
        }
    }
    
    // ===== 하이라이트 관리 =====
    public void SetHighlight(bool highlighted)
    {
        isHighlighted = highlighted;
        if (visualizer != null)
        {
            visualizer.SetHighlight(highlighted, highlightColor);
        }
    }
    
    // ===== Getter =====
    public InteractionType GetInteractionType() => interactionType;
    public string GetInteractionName() => interactionName;
    public float GetInteractionTime() => interactionTime;
    public bool IsInteracting() => isInteracting;
    public Color GetHighlightColor() => highlightColor;
    public bool ShouldShowIcon() => showFloatingIcon;
    public Sprite GetInteractionIcon() => interactionIcon;
    
    // 진행률 이벤트 (0~1)
    public event System.Action<float> OnInteractionProgress;
    
    // 하위 클래스에서 진행률 이벤트를 발생시킬 수 있도록 하는 메서드
    protected virtual void TriggerProgressEvent(float progress)
    {
        OnInteractionProgress?.Invoke(progress);
    }
}