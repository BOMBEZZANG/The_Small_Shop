using UnityEngine;
using System.Collections.Generic;

[CreateAssetMenu(fileName = "RepairRequirement", menuName = "Game Data/Workstation/Repair Requirement")]
public class RepairRequirement : ScriptableObject
{
    [Header("Repair Info")]
    public string requirementName = "기본 수리";
    public string description = "작업대를 수리합니다";
    
    [Header("Required Items")]
    public List<RepairItem> requiredItems = new List<RepairItem>();
    
    [Header("Repair Effects")]
    public int durabilityRestore = 50; // 회복되는 내구도
    public bool fullRepair = false; // true면 내구도 100% 회복
    
    [Header("First Time Repair")]
    public bool isInitialRepair = false; // 첫 수리인지 (튜토리얼용)
    public string initialRepairMessage = "이제 분해기를 사용할 수 있습니다!";
    
    // 수리 가능 여부 확인
    public bool CanRepair()
    {
        foreach (var item in requiredItems)
        {
            if (InventoryManager.instance.GetItemQuantity(item.item) < item.amount)
            {
                return false;
            }
        }
        return true;
    }
    
    // 수리 실행 (아이템 소비)
    public bool ExecuteRepair()
    {
        if (!CanRepair()) return false;
        
        // 아이템 소비
        foreach (var item in requiredItems)
        {
            InventoryManager.instance.RemoveItem(item.item, item.amount);
        }
        
        return true;
    }
    
    // 부족한 아이템 목록 가져오기
    public string GetMissingItemsText()
    {
        List<string> missingItems = new List<string>();
        
        foreach (var item in requiredItems)
        {
            int have = InventoryManager.instance.GetItemQuantity(item.item);
            int need = item.amount;
            
            if (have < need)
            {
                missingItems.Add($"{item.item.materialName} ({have}/{need})");
            }
        }
        
        return missingItems.Count > 0 
            ? "필요한 아이템: " + string.Join(", ", missingItems)
            : "";
    }
}

[System.Serializable]
public class RepairItem
{
    public MaterialData item;
    public int amount = 1;
}