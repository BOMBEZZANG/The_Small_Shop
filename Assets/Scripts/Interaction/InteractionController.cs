using UnityEngine;
using System.Collections.Generic;
using System.Linq;

[RequireComponent(typeof(PlayerController))]
public class InteractionController : MonoBehaviour
{
    [Header("Interaction Settings")]
    [SerializeField] private float interactionRange = 2f;
    [SerializeField] private LayerMask interactableLayer = -1; // 상호작용 가능한 레이어
    [SerializeField] private bool showDebugGizmos = true;
    
    // 상태
    private InteractableObject currentTarget;
    private InteractableObject previousTarget;
    private List<InteractableObject> objectsInRange = new List<InteractableObject>();
    
    // 컴포넌트
    private PlayerController playerController;
    
    // 이벤트
    public static event System.Action<InteractableObject> OnTargetChanged;
    public static event System.Action<InteractableObject> OnInteractionStarted;
    public static event System.Action<InteractableObject> OnInteractionCompleted;
    
    void Awake()
    {
        playerController = GetComponent<PlayerController>();
    }

    [System.Obsolete]
    void Update()
    {
        // 상호작용 가능한 객체 탐색
        FindInteractables();
        
        // 가장 가까운 객체 선택
        UpdateCurrentTarget();
        
        // 상호작용 입력 처리
        HandleInteractionInput();
    }
    
    // ===== 상호작용 가능한 객체 찾기 =====
    private void FindInteractables()
    {
        // 범위 내 모든 상호작용 가능한 객체 찾기
        Collider2D[] colliders = Physics2D.OverlapCircleAll(
            transform.position, 
            interactionRange, 
            interactableLayer
        );
        
        objectsInRange.Clear();
        
        foreach (var collider in colliders)
        {
            InteractableObject interactable = collider.GetComponent<InteractableObject>();
            if (interactable != null && interactable.gameObject.activeInHierarchy)
            {
                objectsInRange.Add(interactable);
            }
        }
    }
    
    // ===== 현재 타겟 업데이트 =====
    private void UpdateCurrentTarget()
    {
        previousTarget = currentTarget;
        
        if (objectsInRange.Count == 0)
        {
            currentTarget = null;
        }
        else
        {
            // 가장 가까운 객체 선택
            currentTarget = objectsInRange
                .OrderBy(obj => Vector2.Distance(transform.position, obj.transform.position))
                .FirstOrDefault();
        }
        
        // 타겟이 변경되었을 때
        if (currentTarget != previousTarget)
        {
            // 이전 타겟 하이라이트 해제
            if (previousTarget != null)
            {
                previousTarget.SetHighlight(false);
            }
            
            // 새 타겟 하이라이트
            if (currentTarget != null)
            {
                currentTarget.SetHighlight(true);
            }
            
            // 이벤트 발생
            OnTargetChanged?.Invoke(currentTarget);
        }
    }

    // ===== 상호작용 입력 처리 =====
    [System.Obsolete]
    private void HandleInteractionInput()
    {
        // E키 입력
        if (Input.GetKeyDown(KeyCode.E))
        {
            if (currentTarget != null && currentTarget.CanInteract(playerController))
            {
                StartInteraction();
            }
        }
        
        // 상호작용 중 취소 (ESC)
        if (Input.GetKeyDown(KeyCode.Escape))
        {
            if (currentTarget != null && currentTarget.IsInteracting())
            {
                currentTarget.CancelInteraction();
            }
        }
    }

    // ===== 상호작용 시작 =====
    [System.Obsolete]
    private void StartInteraction()
    {
        if (currentTarget == null) return;
        
        currentTarget.StartInteraction(playerController);
        OnInteractionStarted?.Invoke(currentTarget);
        
        // 완료 이벤트 구독
        currentTarget.OnInteractionComplete.AddListener(OnInteractionComplete);
    }
    
    // ===== 상호작용 완료 처리 =====
    private void OnInteractionComplete(PlayerController player)
    {
        if (currentTarget != null)
        {
            currentTarget.OnInteractionComplete.RemoveListener(OnInteractionComplete);
            OnInteractionCompleted?.Invoke(currentTarget);
        }
    }
    
    // ===== 외부 접근용 메서드 =====
    public InteractableObject GetCurrentTarget() => currentTarget;
    public bool HasTarget() => currentTarget != null;
    public float GetInteractionRange() => interactionRange;
    
    // ===== 디버그 =====
    void OnDrawGizmosSelected()
    {
        if (!showDebugGizmos) return;
        
        // 상호작용 범위 표시
        Gizmos.color = Color.yellow;
        Gizmos.DrawWireSphere(transform.position, interactionRange);
        
        // 현재 타겟 표시
        if (currentTarget != null)
        {
            Gizmos.color = Color.green;
            Gizmos.DrawLine(transform.position, currentTarget.transform.position);
        }
    }
}