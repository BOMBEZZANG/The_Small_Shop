using UnityEngine;
using System.Collections;
using System.Collections.Generic;

public class VisitorNPCController : NPCController
{
    [Header("Visitor Settings")]
    [SerializeField] private bool checkConditionsOnStart = true;
    [SerializeField] private float checkInterval = 5f;
    [SerializeField] private bool showArrivalEffect = true;
    [SerializeField] private bool debugForceSpawn = false; // Debug: Force spawn immediately
    
    [Header("Spawn Settings")]
    [SerializeField] private Transform spawnPoint;
    [SerializeField] private float spawnDistance = 10f;
    [SerializeField] private GameObject arrivalEffectPrefab;
    
    [Header("Path Debug")]
    [SerializeField] private bool showCurrentPathTarget = true;
    [SerializeField] private Color targetPointColor = Color.red;
    
    // 방문 관련
    private VisitorNPCData visitorData;
    private bool hasVisited = false;
    private bool isApproachingPlayer = false;
    private Coroutine approachCoroutine;
    private Coroutine conditionCheckCoroutine;
    
    // 경로 이동 관련
    private Coroutine pathMovementCoroutine;
    private int currentPathIndex = 0;
    private bool isFollowingPath = false;
    private Vector2 currentTargetPoint;
    
    // 숨김 처리용
    private bool isHidden = true;
    private Vector3 hiddenPosition = new Vector3(1000, 1000, 0); // 화면 밖
    
    // 상태
    private enum VisitorState
    {
        Waiting,        // 조건 대기 중
        FollowingPath,  // 경로 이동 중
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
        
        // Debug: Force spawn for testing
        if (debugForceSpawn)
        {
            Debug.Log($"[VisitorNPC] {visitorData.npcName} DEBUG: Force spawning!");
            StartCoroutine(ForceSpawnDebug());
        }
        // 조건 체크 시작
        else if (checkConditionsOnStart)
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
        Debug.Log($"[VisitorNPC] ShowNPC 실행! GameObject active: {gameObject.activeInHierarchy}");
        isHidden = false;
        
        // 렌더러 활성화
        if (spriteRenderer != null)
        {
            spriteRenderer.enabled = true;
            Debug.Log($"[VisitorNPC] SpriteRenderer 활성화: {spriteRenderer.enabled}, Sprite: {spriteRenderer.sprite?.name ?? "NULL"}");
            Debug.Log($"[VisitorNPC] Renderer bounds: {spriteRenderer.bounds}");
        }
        else
        {
            Debug.LogError("[VisitorNPC] SpriteRenderer가 null입니다!");
            spriteRenderer = GetComponent<SpriteRenderer>();
            if (spriteRenderer != null)
            {
                Debug.Log("[VisitorNPC] SpriteRenderer 재획득 성공!");
                spriteRenderer.enabled = true;
            }
        }
        
        // 콜라이더 활성화
        Collider2D col = GetComponent<Collider2D>();
        if (col != null)
        {
            col.enabled = true;
            Debug.Log("[VisitorNPC] Collider 활성화");
        }
        
        Debug.Log($"[VisitorNPC] 현재 위치: {transform.position}, Layer: {gameObject.layer}");
        Debug.Log($"[VisitorNPC] Parent: {transform.parent?.name ?? "No Parent"}");
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
        
        // 스폰 위치 결정
        Vector3 spawnPosition = GetSpawnPosition();
        
        // 경로가 있다면 첫 번째 지점에서 시작
        if (visitorData.IsPathValid())
        {
            spawnPosition = visitorData.movementPath[0];
            visitorState = VisitorState.FollowingPath;
        }
        else
        {
            visitorState = VisitorState.Approaching;
        }
        
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
        
        // 경로 이동 또는 플레이어 추적 시작
        if (visitorData.IsPathValid())
        {
            StartPathMovement();
        }
        else
        {
            Debug.Log($"[VisitorNPC] 플레이어 추적 시작 - Player: {playerTransform != null}");
            StartApproachingPlayer();
        }
    }
    
    // ===== 경로 이동 시작 =====
    private void StartPathMovement()
    {
        if (pathMovementCoroutine != null)
        {
            StopCoroutine(pathMovementCoroutine);
        }
        
        isFollowingPath = true;
        currentPathIndex = 0;
        pathMovementCoroutine = StartCoroutine(FollowPathRoutine());
    }
    
    // ===== 경로 따라 이동하는 코루틴 =====
    private IEnumerator FollowPathRoutine()
    {
        Debug.Log($"[VisitorNPC] 경로 이동 시작 - 총 {visitorData.movementPath.Count}개 지점");
        
        while (isFollowingPath && currentPathIndex < visitorData.movementPath.Count)
        {
            // 현재 목표 지점
            currentTargetPoint = visitorData.movementPath[currentPathIndex];
            Debug.Log($"[VisitorNPC] 목표 지점 {currentPathIndex + 1}/{visitorData.movementPath.Count}: {currentTargetPoint}");
            
            // 목표 지점으로 이동
            yield return StartCoroutine(MoveToPosition(currentTargetPoint, visitorData.pathMoveSpeed));
            
            // 도착 후 대기
            if (visitorData.waitTimeAtPoint > 0)
            {
                ChangeState(NPCState.Idle);
                yield return new WaitForSeconds(visitorData.waitTimeAtPoint);
            }
            
            // 다음 지점으로
            currentPathIndex++;
            
            // 경로 끝에 도달
            if (currentPathIndex >= visitorData.movementPath.Count)
            {
                if (visitorData.loopPath)
                {
                    // 경로 반복
                    currentPathIndex = 0;
                    Debug.Log("[VisitorNPC] 경로 반복");
                }
                else
                {
                    // 경로 완료
                    isFollowingPath = false;
                    Debug.Log("[VisitorNPC] 경로 이동 완료");
                    
                    // 플레이어에게 이동
                    if (visitorData.moveToPlayerAfterPath)
                    {
                        visitorState = VisitorState.Approaching;
                        StartApproachingPlayer();
                    }
                    else
                    {
                        visitorState = VisitorState.Ready;
                    }
                }
            }
        }
    }
    
    // ===== 특정 위치로 이동 =====
    private IEnumerator MoveToPosition(Vector2 targetPosition, float moveSpeed)
    {
        ChangeState(NPCState.Moving);
        
        float stoppingDistance = 0.1f;
        
        while (Vector2.Distance(transform.position, targetPosition) > stoppingDistance)
        {
            // 대화 시작되면 이동 중단
            if (currentState == NPCState.Talking)
            {
                yield break;
            }
            
            // 방향 계산
            Vector2 direction = (targetPosition - (Vector2)transform.position).normalized;
            SetMoveDirection(direction, moveSpeed);
            
            yield return null;
        }
        
        // 도착
        StopMoving();
        ChangeState(NPCState.Idle);
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
    
    protected override void UpdateNPCBehavior()
    {
        // 숨겨진 상태에서는 행동 안 함
        if (isHidden) return;
        
        switch (visitorState)
        {
            case VisitorState.FollowingPath:
                // 경로 이동 중일 때는 목표 지점을 바라봄
                if (isFollowingPath && moveDirection.magnitude > 0)
                {
                    // 이동 방향으로 스프라이트 플립
                    if (spriteRenderer != null)
                    {
                        spriteRenderer.flipX = moveDirection.x < 0;
                    }
                }
                break;
                
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
    
    private IEnumerator ForceSpawnDebug()
    {
        Debug.Log($"[VisitorNPC] DEBUG: Waiting 1 second before force spawn...");
        yield return new WaitForSeconds(1f);
        StartVisit();
    }
    
    private IEnumerator ConditionCheckRoutine()
    {
        Debug.Log($"[VisitorNPC] {visitorData.npcName}: 조건 체크 시작 - Required Level: {visitorData.requiredPlayerLevel}");
        
        while (visitorState == VisitorState.Waiting)
        {
            Debug.Log($"[VisitorNPC] {visitorData.npcName}: 조건 체크 중... Player Level: {PlayerDataManager.instance.GetLevel()}");
            
            if (visitorData.CheckVisitConditions())
            {
                Debug.Log($"[VisitorNPC] {visitorData.npcName}: 방문 조건 충족!");
                
                if (visitorData.visitDelay > 0)
                {
                    Debug.Log($"[VisitorNPC] {visitorData.npcName}: {visitorData.visitDelay}초 대기 중...");
                    yield return new WaitForSeconds(visitorData.visitDelay);
                }
                
                Debug.Log($"[VisitorNPC] {visitorData.npcName}: StartVisit() 호출!");
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
    
    // ===== 경로 이동 중단 =====
    public void StopPathMovement()
    {
        isFollowingPath = false;
        
        if (pathMovementCoroutine != null)
        {
            StopCoroutine(pathMovementCoroutine);
            pathMovementCoroutine = null;
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
        
        // 경로 표시
        if (visitorData != null && visitorData.IsPathValid() && visitorData.showPathInEditor)
        {
            Gizmos.color = visitorData.pathColor;
            
            // 경로 선 그리기
            for (int i = 0; i < visitorData.movementPath.Count - 1; i++)
            {
                Vector3 start = visitorData.movementPath[i];
                Vector3 end = visitorData.movementPath[i + 1];
                Gizmos.DrawLine(start, end);
                
                // 방향 화살표
                DrawArrow(start, (end - start).normalized);
            }
            
            // 경로 지점 표시
            for (int i = 0; i < visitorData.movementPath.Count; i++)
            {
                Vector3 point = visitorData.movementPath[i];
                Gizmos.DrawWireSphere(point, 0.3f);
                
                // 지점 번호 표시 (Scene 뷰에서만 보임)
                #if UNITY_EDITOR
                UnityEditor.Handles.Label(point + Vector3.up * 0.5f, $"{i + 1}");
                #endif
            }
            
            // 반복 경로면 마지막에서 첫 번째로 연결
            if (visitorData.loopPath && visitorData.movementPath.Count > 1)
            {
                Vector3 last = visitorData.movementPath[visitorData.movementPath.Count - 1];
                Vector3 first = visitorData.movementPath[0];
                Gizmos.DrawLine(last, first);
                DrawArrow(last, (first - last).normalized);
            }
            
            // 현재 목표 지점 강조
            if (isFollowingPath && showCurrentPathTarget)
            {
                Gizmos.color = targetPointColor;
                Gizmos.DrawWireSphere(currentTargetPoint, 0.5f);
                Gizmos.DrawLine(transform.position, currentTargetPoint);
            }
        }
    }
    
    // 화살표 그리기 헬퍼 메서드
    private void DrawArrow(Vector3 position, Vector3 direction, float size = 0.3f)
    {
        Vector3 perpendicular = new Vector3(-direction.y, direction.x, 0);
        
        Vector3 arrowTip = position + direction * size;
        Vector3 arrowLeft = position - direction * size * 0.5f + perpendicular * size * 0.5f;
        Vector3 arrowRight = position - direction * size * 0.5f - perpendicular * size * 0.5f;
        
        Gizmos.DrawLine(position, arrowTip);
        Gizmos.DrawLine(arrowTip, arrowLeft);
        Gizmos.DrawLine(arrowTip, arrowRight);
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
        
        if (pathMovementCoroutine != null)
        {
            StopCoroutine(pathMovementCoroutine);
        }
    }
}