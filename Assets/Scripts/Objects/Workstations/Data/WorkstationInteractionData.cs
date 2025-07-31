using UnityEngine;

[CreateAssetMenu(fileName = "WorkstationInteractionData", menuName = "Game Data/Workstation/Interaction Data")]
public class WorkstationInteractionData : InteractionData
{
    [Header("Workstation Specific")]
    public Sprite brokenIcon; // 고장 상태 아이콘
    public Sprite processingIcon; // 처리 중 아이콘
    public Sprite completeIcon; // 완료 아이콘
    
    [Header("Status Messages")]
    public string idleMessage = "사용하기";
    public string processingMessage = "처리 중...";
    public string completeMessage = "수거하기";
    public string brokenMessage = "수리가 필요합니다";
    public string noItemMessage = "넣을 아이템이 없습니다";
    public string inventoryFullMessage = "인벤토리가 가득 찼습니다";
    
    [Header("Repair Messages")]
    public string repairPrompt = "수리하기";
    public string repairProgressMessage = "수리 중...";
    public string repairCompleteMessage = "수리 완료!";
    
    [Header("Durability Display")]
    public bool showDurability = true;
    public string durabilityFormat = "내구도: {0}%";
    public Color highDurabilityColor = Color.green;
    public Color mediumDurabilityColor = Color.yellow;
    public Color lowDurabilityColor = Color.red;
    
    // 내구도에 따른 색상 반환
    public Color GetDurabilityColor(float durabilityPercent)
    {
        if (durabilityPercent > 0.5f)
            return highDurabilityColor;
        else if (durabilityPercent > 0.2f)
            return mediumDurabilityColor;
        else
            return lowDurabilityColor;
    }
    
    // 상태에 따른 메시지 반환
    public string GetStatusMessage(WorkstationStatus status)
    {
        switch (status)
        {
            case WorkstationStatus.Idle:
                return idleMessage;
            case WorkstationStatus.Processing:
                return processingMessage;
            case WorkstationStatus.Complete:
                return completeMessage;
            case WorkstationStatus.Broken:
                return brokenMessage;
            default:
                return "";
        }
    }
    
    // 상태에 따른 아이콘 반환
    public Sprite GetStatusIcon(WorkstationStatus status)
    {
        switch (status)
        {
            case WorkstationStatus.Processing:
                return processingIcon ?? IconSprite;
            case WorkstationStatus.Complete:
                return completeIcon ?? IconSprite;
            case WorkstationStatus.Broken:
                return brokenIcon ?? IconSprite;
            default:
                return IconSprite;
        }
    }
}