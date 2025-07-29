using UnityEngine;

public class TestNPCController : NPCController
{
    [Header("Test Settings")]
    [SerializeField] private bool autoGreetPlayer = true;
    [SerializeField] private float greetingDelay = 1f;
    
    private bool hasGreeted = false;
    
    // NPCController 추상 메서드 구현
    protected override void InitializeNPC()
    {
        Debug.Log($"{npcData.npcName} NPC 초기화 완료!");
    }
    
    protected override void UpdateNPCBehavior()
    {
        // 간단한 테스트 동작
        if (currentState == NPCState.Idle && !isMoving)
        {
            // 가끔 랜덤하게 고개 돌리기
            if (Random.Range(0f, 1f) < 0.001f) // 0.1% 확률
            {
                spriteRenderer.flipX = !spriteRenderer.flipX;
            }
        }
    }
    
    // 플레이어가 범위에 들어왔을 때
    protected override void OnPlayerEnterRange()
    {
        base.OnPlayerEnterRange();
        
        // 자동 인사
        if (autoGreetPlayer && !hasGreeted && currentAvailableDialogue != null)
        {
            hasGreeted = true;
            Invoke(nameof(AutoStartDialogue), greetingDelay);
        }
    }
    
    private void AutoStartDialogue()
    {
        if (playerInRange && currentState == NPCState.Idle)
        {
            StartDialogue();
        }
    }
    
    // 대화 종료 시
    protected override void OnDialogueEnded()
    {
        base.OnDialogueEnded();
        
        // 테스트: 대화 후 플레이어에게 손 흔들기
        if (animator != null)
        {
            animator.SetTrigger("Wave");
        }
    }
}