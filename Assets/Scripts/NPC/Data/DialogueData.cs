using UnityEngine;
using System;

// 대화 한 줄을 나타내는 클래스
[System.Serializable]
public class DialogueLine
{
    [TextArea(2, 5)]
    public string text;                    // 대화 내용
    public string speakerName = "NPC";     // 화자 이름
    public Sprite speakerPortrait;         // 화자 초상화
    public AudioClip voiceClip;            // 음성 (선택)
    
    [Header("Options")]
    public float displayTime = 0f;         // 0 = 수동 진행, >0 = 자동 진행
    public bool isPlayerLine = false;      // 플레이어 대사인지
}

// 선택지
[System.Serializable]
public class DialogueChoice
{
    public string choiceText;              // 선택지 텍스트
    public int nextDialogueID = -1;        // 다음 대화 ID (-1 = 종료)
    public DialogueCondition[] conditions; // 선택지 표시 조건
    public DialogueEffect[] effects;       // 선택 시 효과
}

// 대화 조건
[System.Serializable]
public class DialogueCondition
{
    public enum ConditionType
    {
        PlayerLevel,
        HasItem,
        QuestStatus,
        NPCRelationship,
        Gold,
        Custom
    }
    
    public ConditionType type;
    public string parameterKey;    // 아이템 ID, 퀘스트 ID 등
    public int requiredValue;      // 필요한 값
    public string customCondition; // 커스텀 조건용
    
    // 조건 체크 메서드
    public bool CheckCondition()
    {
        switch (type)
        {
            case ConditionType.PlayerLevel:
                return PlayerDataManager.instance.GetLevel() >= requiredValue;
                
            case ConditionType.Gold:
                return GoldManager.instance.GetGold() >= requiredValue;
                
            // TODO: 다른 조건들 구현
            default:
                return true;
        }
    }
}

// 대화 효과 (선택지 선택 시 결과)
[System.Serializable]
public class DialogueEffect
{
    public enum EffectType
    {
        GiveItem,
        GiveGold,
        GiveExp,
        StartQuest,
        CompleteQuest,
        ChangeNPCRelationship,
        Custom
    }
    
    public EffectType type;
    public string parameterKey;    // 아이템 ID, 퀘스트 ID 등
    public int value;              // 수량, 경험치 등
    public string customEffect;    // 커스텀 효과용
}

// 메인 대화 데이터
[CreateAssetMenu(fileName = "New Dialogue", menuName = "Game Data/Dialogue Data")]
public class DialogueData : ScriptableObject
{
    [Header("Basic Info")]
    public int dialogueID;
    public string dialogueName;        // 에디터용 이름
    
    [Header("Dialogue Content")]
    public DialogueLine[] dialogueLines;    // 대화 내용
    
    [Header("Player Choices")]
    public bool hasChoices = false;         // 선택지 유무
    public DialogueChoice[] choices;        // 선택지 목록
    
    [Header("Conditions")]
    public DialogueCondition[] startConditions; // 대화 시작 조건
    
    [Header("Settings")]
    public bool isRepeatable = true;        // 반복 가능 여부
    public int priority = 0;                // 우선순위 (높을수록 먼저)
    
    // 대화 시작 가능 여부 체크
    public bool CanStartDialogue()
    {
        if (startConditions == null || startConditions.Length == 0)
            return true;
            
        foreach (var condition in startConditions)
        {
            if (!condition.CheckCondition())
                return false;
        }
        
        return true;
    }
    
    // 디버그용
    public void LogDialogueInfo()
    {
        Debug.Log($"[Dialogue] {dialogueName} (ID: {dialogueID})");
        Debug.Log($"Lines: {dialogueLines.Length}, Choices: {choices?.Length ?? 0}");
    }
}