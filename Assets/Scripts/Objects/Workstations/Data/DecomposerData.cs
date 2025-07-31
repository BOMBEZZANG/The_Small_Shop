using UnityEngine;
using System.Collections.Generic;

[CreateAssetMenu(fileName = "DecomposerData", menuName = "Game Data/Workstation/Decomposer Data")]
public class DecomposerData : WorkstationData
{
    [Header("Decomposer Specific")]
    public List<DecomposerRecipe> recipes = new List<DecomposerRecipe>();
    
    [Header("Efficiency")]
    [Range(0.5f, 2f)]
    public float speedMultiplier = 1f; // 처리 속도 배수
    
    [Range(0f, 100f)]
    public float bonusOutputChance = 0f; // 추가 산출물 확률
    
    [Header("Durability Effects")]
    public float speedPenaltyAt50Percent = 0.8f; // 내구도 50% 이하 시 속도
    public float speedPenaltyAt20Percent = 0.5f; // 내구도 20% 이하 시 속도
    public float qualityPenaltyAt20Percent = 0.7f; // 내구도 20% 이하 시 품질
    
    [Header("Repair Items")]
    public MaterialData primaryRepairItem; // 고철
    public int primaryRepairAmount = 2;
    public MaterialData secondaryRepairItem; // 윤활유
    public int secondaryRepairAmount = 1;
    public int durabilityRestoreAmount = 50; // 한 번 수리 시 회복량
}

[System.Serializable]
public class DecomposerRecipe
{
    public string recipeName;
    public MaterialData inputItem;
    public int inputAmount = 1;
    
    [Header("Output")]
    public List<RecipeOutput> outputs = new List<RecipeOutput>();
    
    [Header("Processing")]
    public float processingTime = 5f; // 이 레시피의 처리 시간
    public int expReward = 5;
}

