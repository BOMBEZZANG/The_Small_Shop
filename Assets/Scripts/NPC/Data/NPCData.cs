using UnityEngine;
using System.Collections.Generic;

// NPC 타입 정의
public enum NPCType
{
    Resident,    // 상주형 (마을 주민)
    Visitor,     // 방문형 (퀘스트 제공)
    Merchant,    // 상인
    Guard,       // 경비
    Special      // 특수 NPC
}

// NPC 기본 데이터 (추상 클래스)
public abstract class NPCData : ScriptableObject
{
    [Header("Basic Information")]
    public int npcID;
    public string npcName;
    public NPCType npcType;
    
    [Header("Visual")]
    public GameObject npcPrefab;              // NPC 프리팹
    public Sprite npcPortrait;                // 대화창 초상화
    public RuntimeAnimatorController animator; // 애니메이터
    
    [Header("Dialogue")]
    public DialogueData[] availableDialogues; // 가능한 대화 목록
    public DialogueData defaultDialogue;      // 기본 대화
    
    [Header("Interaction")]
    public float interactionRange = 2f;       // 상호작용 가능 거리
    public Vector3 indicatorOffset = new Vector3(0, 2, 0); // 머리 위 표시 오프셋
    
    [Header("Audio")]
    public AudioClip greetingSound;           // 인사 소리
    public AudioClip[] footstepSounds;        // 발소리
    
    // 현재 가능한 대화 가져오기
    public virtual DialogueData GetAvailableDialogue()
    {
        // 우선순위가 높은 순서로 정렬
        List<DialogueData> validDialogues = new List<DialogueData>();
        
        foreach (var dialogue in availableDialogues)
        {
            if (dialogue != null && dialogue.CanStartDialogue())
            {
                validDialogues.Add(dialogue);
            }
        }
        
        if (validDialogues.Count > 0)
        {
            // 우선순위로 정렬
            validDialogues.Sort((a, b) => b.priority.CompareTo(a.priority));
            return validDialogues[0];
        }
        
        // 가능한 대화가 없으면 기본 대화 반환
        return defaultDialogue;
    }
    
    // 추상 메서드 - 각 NPC 타입별로 구현
    public abstract void InitializeNPCData();
    
    // 유효성 검사
    public virtual bool ValidateData()
    {
        if (npcPrefab == null)
        {
            Debug.LogError($"{npcName}: NPC Prefab이 설정되지 않았습니다!");
            return false;
        }
        
        if (defaultDialogue == null && (availableDialogues == null || availableDialogues.Length == 0))
        {
            Debug.LogWarning($"{npcName}: 대화 데이터가 하나도 설정되지 않았습니다!");
            return false;
        }
        
        return true;
    }
}

// 상주형 NPC 데이터
[CreateAssetMenu(fileName = "New Resident NPC", menuName = "Game Data/NPC/Resident NPC")]
public class ResidentNPCData : NPCData
{
    [Header("Resident Behavior")]
    public Vector2[] patrolPoints;            // 순찰 지점들
    public float patrolSpeed = 2f;            // 이동 속도
    public float waitTimeAtPoint = 3f;        // 각 지점 대기 시간
    
    [Header("Daily Schedule")]
    public bool hasSchedule = false;          // 일정 있는지
    public NPCSchedule[] dailySchedule;       // 시간대별 일정
    
    [Header("Special Behavior")]
    public bool runsFromPlayer = false;       // 플레이어 피하기
    public float runAwayDistance = 5f;        // 도망 거리
    public string[] randomChatter;            // 랜덤 대사
    
    public override void InitializeNPCData()
    {
        npcType = NPCType.Resident;
    }
}

// 방문형 NPC 데이터
[CreateAssetMenu(fileName = "New Visitor NPC", menuName = "Game Data/NPC/Visitor NPC")]
public class VisitorNPCData : NPCData
{
    [Header("Visit Conditions")]
    public int requiredPlayerLevel = 1;       // 필요 플레이어 레벨
    public string requiredQuestComplete = ""; // 필요 완료 퀘스트
    public bool visitOnce = true;             // 한 번만 방문
    
    [Header("Visit Behavior")]
    public float visitDelay = 0f;             // 조건 충족 후 대기 시간
    public float moveToPlayerSpeed = 4f;      // 플레이어에게 이동 속도
    public bool disappearAfterDialogue = true;// 대화 후 사라짐
    
    [Header("Quest")]
    public string questToGive;                // 부여할 퀘스트 ID
    public DialogueData questDialogue;        // 퀘스트 대화
    
    public override void InitializeNPCData()
    {
        npcType = NPCType.Visitor;
    }
    
    // 방문 조건 체크
    public bool CheckVisitConditions()
    {
        // 레벨 체크
        if (PlayerDataManager.instance.GetLevel() < requiredPlayerLevel)
            return false;
            
        // TODO: 퀘스트 완료 체크
        // if (!string.IsNullOrEmpty(requiredQuestComplete))
        // {
        //     if (!QuestManager.instance.IsQuestCompleted(requiredQuestComplete))
        //         return false;
        // }
        
        return true;
    }
}

// NPC 일정 (시간대별 행동)
[System.Serializable]
public class NPCSchedule
{
    public string scheduleName;               // "아침 일과"
    public int startHour;                     // 시작 시간
    public int endHour;                       // 종료 시간
    public Vector2 locationPosition;          // 해당 시간 위치
    public string locationName;               // "상점", "광장"
    public DialogueData specialDialogue;      // 해당 시간 전용 대화
}