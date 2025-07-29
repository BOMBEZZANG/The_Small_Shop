using UnityEngine;
using System.Collections;

public class VisitorNPCController : NPCController
{
    [Header("Visitor Settings")]
    [SerializeField] private bool checkConditionsOnStart = true;
    [SerializeField] private float checkInterval = 5f;
    [SerializeField] private bool showArrivalEffect = true;
    
    [Header("Spawn Settings")]
    [SerializeField] private Transform spawnPoint;
    [SerializeField] private float spawnDistance = 10f;
    [SerializeField] private GameObject arrivalEffectPrefab;
    
    // 방문 관련
    private VisitorNPCData visitorData;
    private bool hasVisited = false;
    private bool isApproachingPlayer = false;
    private Coroutine approachCoroutine;
    private Coroutine conditionCheckCoroutine;
    
    // 숨김 처리용
    private bool isHidden = true;
    private Vector3 hiddenPosition = new Vector3(1000, 1000, 0); // 화면 밖
    
    // 상태
    private enum VisitorState
    {
        Waiting,        // 조건 대기 중
        Approaching,    // 플레이어 접근 중
        Ready,          // 대화 준비 완료
        Completed       // 용무 완료
    }
    private VisitorState visitorState = VisitorState.Waiting;
    
    // ===== 초기화 수정 =====
    protected override void Awake()
    {
        base.Awake();
        
        // 즉시 숨기기 (렌더러와 콜라이더만)
        HideNPC();
    }
    
    protected override void InitializeNPC()
    {
        // VisitorNPCData 캐스팅
        visitorData = npcData as VisitorNPCData;
        if (visitorData == null)
        {
            Debug.LogError($"{gameObject.name}: NPCData가 VisitorNPCData가 아닙니다!");
            enabled = false;
            return;
        }
        
        Debug.Log($"[VisitorNPC] {visitorData.npcName} 초기화!");
        
        // GameObject는 활성 상태 유지, 보이지만 않게
        HideNPC();
        
        // 조건 체크 시작
        if (checkConditionsOnStart)
        {
            conditionCheckCoroutine = StartCoroutine(ConditionCheckRoutine());
        }
    }
    
    // ===== NPC 숨기기/보이기 =====
    private void HideNPC()
    {
        isHidden = true;
        
        // 렌더러 비활성화
        if (spriteRenderer != null)
            spriteRenderer.enabled = false;
        
        // 콜라이더 비활성화
        Collider2D col = GetComponent<Collider2D>();
        if (col != null)
            col.enabled = false;
        
        // 인디케이터 숨기기
        HideAllIndicators();
        
        // 화면 밖으로 이동 (추가 안전장치)
        transform.position = hiddenPosition;
    }
    
    private void ShowNPC()
{
    Debug.Log("[VisitorNPC] ShowNPC 실행!");
    isHidden = false;
    
    // 렌더러 활성화
    if (spriteRenderer != null)
    {
        spriteRenderer.enabled = true;
        Debug.Log($"[VisitorNPC] SpriteRenderer 활성화: {spriteRenderer.enabled}");
    }
    else
    {
        Debug.LogError("[VisitorNPC] SpriteRenderer가 null입니다!");
    }
    
    // 콜라이더 활성화
    Collider2D col = GetComponent<Collider2D>();
    if (col != null)
    {
        col.enabled = true;
        Debug.Log("[VisitorNPC] Collider 활성화");
    }
    
    Debug.Log($"[VisitorNPC] 현재 위치: {transform.position}");
}

    // ===== 방문 시작 수정 =====
private void StartVisit()
{
    Debug.Log($"[VisitorNPC] StartVisit 호출됨!");
    
    // 이미 방문했고 한 번만 방문하는 설정이면 중단
    if (hasVisited && visitorData.visitOnce)
    {
        Debug.Log($"[VisitorNPC] {visitorData.npcName}: 이미 방문 완료");
        return;
    }
    
    hasVisited = true;
    visitorState = VisitorState.Approaching;
    
    // 스폰 위치 결정
    Vector3 spawnPosition = GetSpawnPosition();
    Debug.Log($"[VisitorNPC] 스폰 위치: {spawnPosition}");
    transform.position = spawnPosition;
    
    // NPC 보이기
    Debug.Log($"[VisitorNPC] ShowNPC 호출 전 - SpriteRenderer: {spriteRenderer != null}");
    ShowNPC();
    Debug.Log($"[VisitorNPC] ShowNPC 호출 후 - enabled: {spriteRenderer?.enabled}");
    
    // 등장 효과
    if (showArrivalEffect)
    {
        PlayArrivalEffect();
    }
    
    // 플레이어 추적 시작
    Debug.Log($"[VisitorNPC] 플레이어 추적 시작 - Player: {playerTransform != null}");
    StartApproachingPlayer();
}
    
    // ===== 사라지기 수정 =====
    private IEnumerator DisappearAfterDelay(float delay)
    {
        yield return new WaitForSeconds(delay);
        
        // 작별 인사
        ShowFarewellMessage();
        
        yield return new WaitForSeconds(2f);
        
        // 사라지는 효과
        if (showArrivalEffect && arrivalEffectPrefab != null)
        {
            GameObject effect = Instantiate(arrivalEffectPrefab, transform.position, Quaternion.identity);
            Destroy(effect, 3f);
        }
        
        // 페이드 아웃 (선택적)
        if (spriteRenderer != null)
        {
            yield return StartCoroutine(FadeOut(1f));
        }
        
        // 숨기기
        HideNPC();
        
        // 한 번만 방문하는 경우 컴포넌트 비활성화
        if (visitorData.visitOnce)
        {
            enabled = false;
        }
    }
    
    // ===== 나머지 코드는 동일 =====
    
    protected override void UpdateNPCBehavior()
    {
        // 숨겨진 상태에서는 행동 안 함
        if (isHidden) return;
        
        switch (visitorState)
        {
            case VisitorState.Approaching:
                if (playerTransform != null)
                {
                    FacePlayer();
                }
                break;
                
            case VisitorState.Ready:
                if (playerTransform != null && currentState != NPCState.Talking)
                {
                    FacePlayer();
                }
                break;
        }
    }
    
    private IEnumerator ConditionCheckRoutine()
    {
        while (visitorState == VisitorState.Waiting)
        {
            if (visitorData.CheckVisitConditions())
            {
                Debug.Log($"[VisitorNPC] {visitorData.npcName}: 방문 조건 충족!");
                
                if (visitorData.visitDelay > 0)
                {
                    yield return new WaitForSeconds(visitorData.visitDelay);
                }
                
                StartVisit();
                break;
            }
            
            yield return new WaitForSeconds(checkInterval);
        }
    }
    
    private Vector3 GetSpawnPosition()
    {
        if (spawnPoint != null)
        {
            return spawnPoint.position;
        }
        
        if (playerTransform != null)
        {
            Vector2 randomDirection = Random.insideUnitCircle.normalized;
            Vector3 spawnPos = playerTransform.position + (Vector3)(randomDirection * spawnDistance);
            return spawnPos;
        }
        
        return transform.position;
    }
    
    private void PlayArrivalEffect()
    {
        if (arrivalEffectPrefab != null)
        {
            GameObject effect = Instantiate(arrivalEffectPrefab, transform.position, Quaternion.identity);
            Destroy(effect, 3f);
        }
        
        if (animator != null)
        {
            animator.SetTrigger("Appear");
        }
        
        if (exclamationMark != null)
        {
            ShowIndicator(exclamationMark);
            Invoke(nameof(HideAllIndicators), 3f);
        }
    }
    
    private void StartApproachingPlayer()
    {
        if (approachCoroutine != null)
        {
            StopCoroutine(approachCoroutine);
        }
        
        isApproachingPlayer = true;
        approachCoroutine = StartCoroutine(ApproachPlayerRoutine());
    }
    
    private IEnumerator ApproachPlayerRoutine()
    {
        ChangeState(NPCState.Moving);
        
        while (isApproachingPlayer && playerTransform != null)
        {
            float distanceToPlayer = Vector2.Distance(transform.position, playerTransform.position);
            
            if (distanceToPlayer <= visitorData.interactionRange)
            {
                StopMoving();
                ChangeState(NPCState.Idle);
                visitorState = VisitorState.Ready;
                
                if (visitorData.questDialogue != null)
                {
                    yield return new WaitForSeconds(1f);
                    
                    if (playerInRange && !DialogueManager.instance.IsInDialogue())
                    {
                        StartDialogue();
                    }
                }
                
                break;
            }
            
            Vector2 direction = (playerTransform.position - transform.position).normalized;
            SetMoveDirection(direction, visitorData.moveToPlayerSpeed);
            
            yield return null;
        }
        
        isApproachingPlayer = false;
    }
    
    public override bool CanInteract(PlayerController player)
    {
        if (isHidden) return false;
        
        if (visitorState != VisitorState.Ready && visitorState != VisitorState.Completed)
        {
            return false;
        }
        
        return base.CanInteract(player);
    }
    
    protected override void StartDialogue()
    {
        if (visitorData.questDialogue != null && visitorState == VisitorState.Ready)
        {
            currentAvailableDialogue = visitorData.questDialogue;
        }
        
        base.StartDialogue();
    }
    
    protected override void OnDialogueEnded()
    {
        base.OnDialogueEnded();
        
        if (!string.IsNullOrEmpty(visitorData.questToGive) && visitorState == VisitorState.Ready)
        {
            GiveQuest();
        }
        
        visitorState = VisitorState.Completed;
        
        if (visitorData.disappearAfterDialogue)
        {
            StartCoroutine(DisappearAfterDelay(2f));
        }
    }
    
    private void GiveQuest()
    {
        Debug.Log($"[VisitorNPC] 퀘스트 부여: {visitorData.questToGive}");
        Debug.Log($"[VisitorNPC] {visitorData.npcName}가 '{visitorData.questToGive}' 퀘스트를 부여했습니다!");
        
        if (animator != null)
        {
            animator.SetTrigger("GiveItem");
        }
    }
    
    private void ShowFarewellMessage()
    {
        if (speechBubble != null)
        {
            speechBubble.SetActive(true);
            var textComponent = speechBubble.GetComponentInChildren<TMPro.TextMeshProUGUI>();
            if (textComponent != null)
            {
                textComponent.text = "다음에 또 만나요!";
            }
        }
    }
    
    private IEnumerator FadeOut(float duration)
    {
        Color startColor = spriteRenderer.color;
        Color endColor = new Color(startColor.r, startColor.g, startColor.b, 0f);
        
        float elapsed = 0f;
        while (elapsed < duration)
        {
            elapsed += Time.deltaTime;
            float t = elapsed / duration;
            spriteRenderer.color = Color.Lerp(startColor, endColor, t);
            yield return null;
        }
    }
    
    [ContextMenu("Force Visit")]
    public void ForceVisit()
    {
        if (visitorState == VisitorState.Waiting)
        {
            StartVisit();
        }
    }
    
    protected override void OnDrawGizmosSelected()
    {
        // 숨겨진 상태면 기즈모 안 그림
        if (isHidden) return;
        
        base.OnDrawGizmosSelected();
        
        if (spawnPoint != null)
        {
            Gizmos.color = Color.magenta;
            Gizmos.DrawWireSphere(spawnPoint.position, 1f);
            Gizmos.DrawLine(transform.position, spawnPoint.position);
        }
        
        if (playerTransform != null)
        {
            Gizmos.color = new Color(1f, 0f, 1f, 0.3f);
            Gizmos.DrawWireSphere(playerTransform.position, spawnDistance);
        }
    }
    
    void OnDestroy()
    {
        if (approachCoroutine != null)
        {
            StopCoroutine(approachCoroutine);
        }
        
        if (conditionCheckCoroutine != null)
        {
            StopCoroutine(conditionCheckCoroutine);
        }
    }
}