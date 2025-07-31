using UnityEngine;
using System.Collections.Generic;

[CreateAssetMenu(fileName = "WorkstationRecipe", menuName = "Game Data/Workstation/Recipe")]
public class WorkstationRecipe : ScriptableObject
{
    [Header("Recipe Info")]
    public string recipeName;
    public string description;
    public Sprite recipeIcon;
    
    [Header("Recipe Type")]
    public WorkstationType workstationType; // 어떤 작업대에서 사용 가능한지
    public RecipeCategory category; // 레시피 카테고리
    
    [Header("Input Items")]
    public List<RecipeIngredient> inputItems = new List<RecipeIngredient>();
    
    [Header("Output Items")]
    public List<RecipeOutput> outputItems = new List<RecipeOutput>();
    
    [Header("Processing")]
    public float baseProcessingTime = 5f; // 기본 처리 시간
    public int expReward = 5; // 완료 시 경험치
    
    [Header("Requirements")]
    public int requiredPlayerLevel = 0; // 필요 플레이어 레벨
    public bool isUnlocked = true; // 기본적으로 잠금 해제 상태
    
    [Header("Special Effects")]
    public bool consumeDurability = true; // 내구도 소모 여부
    public int durabilityConsumption = 5; // 소모되는 내구도량
    
    // 레시피 실행 가능 여부 확인
    public bool CanExecute()
    {
        // 잠금 상태 확인
        if (!isUnlocked)
        {
            Debug.Log($"{recipeName} 레시피가 잠겨있습니다.");
            return false;
        }
        
        // 플레이어 레벨 확인
        if (PlayerDataManager.instance != null && 
            PlayerDataManager.instance.GetLevel() < requiredPlayerLevel)
        {
            Debug.Log($"레벨 {requiredPlayerLevel} 이상이 필요합니다.");
            return false;
        }
        
        // 재료 확인
        foreach (var ingredient in inputItems)
        {
            int currentAmount = InventoryManager.instance.GetItemQuantity(ingredient.item);
            if (currentAmount < ingredient.amount)
            {
                Debug.Log($"{ingredient.item.materialName}이(가) 부족합니다. ({currentAmount}/{ingredient.amount})");
                return false;
            }
        }
        
        return true;
    }
    
    // 입력 아이템 소비
    public void ConsumeInputItems()
    {
        foreach (var ingredient in inputItems)
        {
            InventoryManager.instance.RemoveItem(ingredient.item, ingredient.amount);
        }
    }
    
    // 결과물 생성 (확률 적용)
    public List<ProcessedItem> GenerateOutputs(float qualityMultiplier = 1f)
    {
        List<ProcessedItem> results = new List<ProcessedItem>();
        
        foreach (var output in outputItems)
        {
            // 확률 체크 (품질 배수 적용)
            float adjustedChance = Mathf.Min(output.chance * qualityMultiplier, 100f);
            if (Random.Range(0f, 100f) <= adjustedChance)
            {
                // 수량 결정
                int amount = Random.Range(output.minAmount, output.maxAmount + 1);
                
                // 품질이 낮으면 수량도 감소
                if (qualityMultiplier < 1f)
                {
                    amount = Mathf.Max(1, Mathf.RoundToInt(amount * qualityMultiplier));
                }
                
                results.Add(new ProcessedItem(output.outputItem, amount));
            }
        }
        
        return results;
    }
    
    // UI용 정보 가져오기
    public string GetRequirementsText()
    {
        List<string> requirements = new List<string>();
        
        // 재료 요구사항
        foreach (var ingredient in inputItems)
        {
            int have = InventoryManager.instance.GetItemQuantity(ingredient.item);
            requirements.Add($"{ingredient.item.materialName} x{ingredient.amount} ({have})");
        }
        
        // 레벨 요구사항
        if (requiredPlayerLevel > 0)
        {
            requirements.Add($"레벨 {requiredPlayerLevel} 이상");
        }
        
        return string.Join("\n", requirements);
    }
    
    public string GetOutputsText()
    {
        List<string> outputs = new List<string>();
        
        foreach (var output in outputItems)
        {
            string amountText = output.minAmount == output.maxAmount 
                ? $"x{output.minAmount}" 
                : $"x{output.minAmount}-{output.maxAmount}";
                
            string chanceText = output.chance < 100 
                ? $" ({output.chance}%)" 
                : "";
                
            outputs.Add($"{output.outputItem.materialName} {amountText}{chanceText}");
        }
        
        return string.Join("\n", outputs);
    }
}

// 레시피 카테고리
public enum RecipeCategory
{
    BasicMaterial,    // 기본 재료
    AdvancedMaterial, // 고급 재료
    Component,        // 부품
    Tool,            // 도구
    Consumable,      // 소모품
    Special          // 특수
}

// 레시피 재료
[System.Serializable]
public class RecipeIngredient
{
    public MaterialData item;
    public int amount = 1;
}

// 이미 DecomposerData.cs에 정의되어 있지만, 
// 다른 곳에서도 사용할 수 있도록 여기에도 포함
[System.Serializable]
public class RecipeOutput
{
    public MaterialData outputItem;
    public int minAmount = 1;
    public int maxAmount = 1;
    [Range(0f, 100f)]
    public float chance = 100f; // 획득 확률
}