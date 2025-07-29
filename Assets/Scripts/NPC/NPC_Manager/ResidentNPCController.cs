using UnityEngine;
using System.Collections;
using System.Collections.Generic;

public class ResidentNPCController : NPCController
{
    [Header("Resident Settings")]
    [SerializeField] private bool enablePatrol = true;
    [SerializeField] private bool enableSchedule = false;
    [SerializeField] private bool enableRandomChat = true;
    
    // 순찰 관련
    private ResidentNPCData residentData;
    private int currentPatrolIndex = 0;
    private bool isPatrolling = false;
    private Coroutine patrolCoroutine;
    
    // 일정 관련
    private NPCSchedule currentSchedule;
    private bool isFollowingSchedule = false;
    
    // 랜덤 대화
    private float lastChatterTime;
    private float chatterCooldown = 10f;
    
    // ===== 초기화 =====
    protected override void InitializeNPC()
    {
        // ResidentNPCData 캐스팅
        residentData = npcData as ResidentNPCData;
        if (residentData == null)
        {
            Debug.LogError($"{gameObject.name}: NPCData가 ResidentNPCData가 아닙니다!");
            enabled = false;
            return;
        }
        
        Debug.Log($"[ResidentNPC] {residentData.npcName} 초기화 완료!");
        
        // 순찰 시작
        if (enablePatrol && residentData.patrolPoints != null && residentData.patrolPoints.Length > 0)
        {
            StartPatrol();
        }
        
        // 일정 체크 시작
        if (enableSchedule && residentData.hasSchedule)
        {
            StartCoroutine(ScheduleCheckRoutine());
        }
    }
    
    // ===== NPC 행동 업데이트 =====
    protected override void UpdateNPCBehavior()
    {
        // 상태에 따른 행동
        switch (currentState)
        {
            case NPCState.Idle:
                HandleIdleBehavior();
                break;
                
            case NPCState.Moving:
                // 이동 중에는 특별한 행동 없음
                break;
                
            case NPCState.Talking:
                // 대화 중에는 다른 행동 중지
                break;
        }
        
        // 플레이어 회피 (설정된 경우)
        if (residentData.runsFromPlayer && playerInRange && currentState != NPCState.Talking)
        {
            RunFromPlayer();
        }
    }
    
    // ===== 대기 중 행동 =====
    private void HandleIdleBehavior()
    {
        // 랜덤 대화 (혼잣말)
        if (enableRandomChat && residentData.randomChatter != null && residentData.randomChatter.Length > 0)
        {
            if (Time.time - lastChatterTime > chatterCooldown)
            {
                if (Random.Range(0f, 1f) < 0.1f) // 10% 확률
                {
                    ShowRandomChatter();
                    lastChatterTime = Time.time;
                }
            }
        }
        
        // 가끔 주변 둘러보기
        if (Random.Range(0f, 1f) < 0.002f) // 0.2% 확률
        {
            LookAround();
        }
    }
    
    // ===== 순찰 시스템 =====
    private void StartPatrol()
    {
        if (patrolCoroutine != null)
        {
            StopCoroutine(patrolCoroutine);
        }
        
        isPatrolling = true;
        patrolCoroutine = StartCoroutine(PatrolRoutine());
    }
    
    private IEnumerator PatrolRoutine()
    {
        while (isPatrolling && residentData.patrolPoints.Length > 0)
        {
            // 현재 대화 중이면 대기
            while (currentState == NPCState.Talking)
            {
                yield return new WaitForSeconds(1f);
            }
            
            // 목표 지점으로 이동
            Vector2 targetPoint = residentData.patrolPoints[currentPatrolIndex];
            yield return StartCoroutine(MoveToPosition(targetPoint));
            
            // 도착 후 대기
            ChangeState(NPCState.Idle);
            
            // 대기 중 애니메이션 (선택적)
            if (animator != null)
            {
                animator.SetTrigger("LookAround");
            }
            
            yield return new WaitForSeconds(residentData.waitTimeAtPoint);
            
            // 다음 지점으로
            currentPatrolIndex = (currentPatrolIndex + 1) % residentData.patrolPoints.Length;
        }
    }
    
    // ===== 이동 코루틴 =====
    private IEnumerator MoveToPosition(Vector2 targetPosition)
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
            SetMoveDirection(direction, residentData.patrolSpeed);
            
            yield return null;
        }
        
        // 도착
        StopMoving();
    }
    
    // ===== 일정 시스템 =====
    private IEnumerator ScheduleCheckRoutine()
    {
        while (enableSchedule && residentData.hasSchedule)
        {
            // 현재 시간에 맞는 일정 찾기
            NPCSchedule newSchedule = GetCurrentSchedule();
            
            if (newSchedule != currentSchedule)
            {
                currentSchedule = newSchedule;
                
                if (currentSchedule != null)
                {
                    Debug.Log($"[ResidentNPC] {residentData.npcName}: {currentSchedule.scheduleName} 시작");
                    
                    // 해당 위치로 이동
                    yield return StartCoroutine(MoveToPosition(currentSchedule.locationPosition));
                    
                    // 특별 대화가 있으면 설정
                    if (currentSchedule.specialDialogue != null)
                    {
                        // 임시로 해당 대화를 우선순위로 설정
                        // TODO: 대화 우선순위 시스템 구현
                    }
                }
            }
            
            yield return new WaitForSeconds(60f); // 1분마다 체크
        }
    }
    
    private NPCSchedule GetCurrentSchedule()
    {
        if (!TimeManager.instance) return null;
        
        int currentHour = TimeManager.instance.GetHour();
        
        foreach (var schedule in residentData.dailySchedule)
        {
            if (currentHour >= schedule.startHour && currentHour < schedule.endHour)
            {
                return schedule;
            }
        }
        
        return null;
    }
    
    // ===== 플레이어 회피 =====
    private void RunFromPlayer()
    {
        if (playerTransform == null) return;
        
        Vector2 directionAwayFromPlayer = (transform.position - playerTransform.position).normalized;
        SetMoveDirection(directionAwayFromPlayer, residentData.patrolSpeed * 1.5f);
        ChangeState(NPCState.Moving);
        
        // 겁먹은 표정
        if (animator != null)
        {
            animator.SetBool("IsScared", true);
        }
    }
    
    // ===== 랜덤 대화 (혼잣말) =====
    private void ShowRandomChatter()
    {
        if (speechBubble == null) return;
        
        string chatter = residentData.randomChatter[Random.Range(0, residentData.randomChatter.Length)];
        
        // 말풍선 표시 (간단한 버전)
        speechBubble.SetActive(true);
        var textComponent = speechBubble.GetComponentInChildren<TMPro.TextMeshProUGUI>();
        if (textComponent != null)
        {
            textComponent.text = chatter;
        }
        
        // 3초 후 숨기기
        Invoke(nameof(HideChatter), 3f);
    }
    
    private void HideChatter()
    {
        if (speechBubble != null)
        {
            speechBubble.SetActive(false);
        }
    }
    
    // ===== 주변 둘러보기 =====
    private void LookAround()
    {
        // 랜덤 방향 보기
        bool lookLeft = Random.Range(0, 2) == 0;
        
        if (spriteRenderer != null)
        {
            spriteRenderer.flipX = lookLeft;
        }
        
        // 애니메이션
        if (animator != null)
        {
            animator.SetTrigger("LookAround");
        }
    }
    
    // ===== 플레이어 감지 오버라이드 =====
    protected override void OnPlayerEnterRange()
    {
        base.OnPlayerEnterRange();
        
        // 도망가는 NPC인 경우
        if (residentData.runsFromPlayer)
        {
            if (exclamationMark != null)
            {
                ShowIndicator(exclamationMark);
            }
        }
    }
    
    protected override void OnPlayerExitRange()
    {
        base.OnPlayerExitRange();
        
        // 겁먹은 상태 해제
        if (animator != null)
        {
            animator.SetBool("IsScared", false);
        }
    }
    
    // ===== 대화 관련 오버라이드 =====
    protected override void StartDialogue()
    {
        // 순찰 일시 중지
        if (patrolCoroutine != null)
        {
            StopCoroutine(patrolCoroutine);
        }
        
        base.StartDialogue();
    }
    
    protected override void OnDialogueEnded()
    {
        base.OnDialogueEnded();
        
        // 순찰 재개
        if (enablePatrol && isPatrolling)
        {
            StartCoroutine(ResumePatrolAfterDelay(2f));
        }
    }
    
    private IEnumerator ResumePatrolAfterDelay(float delay)
    {
        yield return new WaitForSeconds(delay);
        
        if (currentState == NPCState.Idle)
        {
            StartPatrol();
        }
    }
    
    // ===== 디버그 =====
    protected override void OnDrawGizmosSelected()
    {
        base.OnDrawGizmosSelected();
        
        // 순찰 경로 표시
        if (residentData != null && residentData.patrolPoints != null && residentData.patrolPoints.Length > 1)
        {
            Gizmos.color = Color.blue;
            
            for (int i = 0; i < residentData.patrolPoints.Length; i++)
            {
                Vector2 point = residentData.patrolPoints[i];
                Vector2 nextPoint = residentData.patrolPoints[(i + 1) % residentData.patrolPoints.Length];
                
                // 점 표시
                Gizmos.DrawWireSphere(point, 0.3f);
                
                // 선 연결
                Gizmos.DrawLine(point, nextPoint);
                
                // 방향 화살표
                Vector2 direction = (nextPoint - point).normalized;
                Vector2 arrowPoint = point + direction * 0.5f;
                DrawArrow(arrowPoint, direction);
            }
        }
    }
    
    private void DrawArrow(Vector2 position, Vector2 direction)
    {
        float arrowSize = 0.3f;
        Vector2 perpendicular = new Vector2(-direction.y, direction.x);
        
        Vector2 arrowTip = position + direction * arrowSize;
        Vector2 arrowLeft = position - direction * arrowSize * 0.5f + perpendicular * arrowSize * 0.5f;
        Vector2 arrowRight = position - direction * arrowSize * 0.5f - perpendicular * arrowSize * 0.5f;
        
        Gizmos.DrawLine(position, arrowTip);
        Gizmos.DrawLine(arrowTip, arrowLeft);
        Gizmos.DrawLine(arrowTip, arrowRight);
    }
    
    // ===== 정리 =====
    void OnDestroy()
    {
        if (patrolCoroutine != null)
        {
            StopCoroutine(patrolCoroutine);
        }
    }
}