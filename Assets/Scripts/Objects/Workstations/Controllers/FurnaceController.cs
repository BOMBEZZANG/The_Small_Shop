using UnityEngine;
using System.Collections.Generic;
using System.Linq;

// 용광로 컨트롤러 예시 - 확장성을 보여주기 위한 코드
public class FurnaceController : BaseWorkstationController
{
    [Header("Furnace Settings")]
    [SerializeField] private List<WorkstationRecipe> furnaceRecipes;
    [SerializeField] private float temperatureMultiplier = 1f; // 온도에 따른 처리 속도
    [SerializeField] private bool requiresFuel = true; // 연료 필요 여부
    [SerializeField] private MaterialData fuelItem; // 연료 아이템 (석탄 등)
    [SerializeField] private int fuelPerUse = 1;
    
    [Header("Visual")]
    [SerializeField] private ParticleSystem fireEffect;
    [SerializeField] private Light furnaceLight;
    
    private WorkstationRecipe selectedRecipe;
    
    protected override void Awake()
    {
        base.Awake();
        
        // 용광로는 보통 내구도가 더 높음
        if (workstationData != null && workstationData.hasDurability)
        {
            state.currentDurability = workstationData.maxDurability;
        }
    }
    
    // ===== 처리 시작 시도 =====
    protected override bool TryStartProcessing(PlayerController player)
    {
        // 연료 체크
        if (requiresFuel)
        {
            int fuelCount = InventoryManager.instance.GetItemQuantity(fuelItem);
            if (fuelCount < fuelPerUse)
            {
                ShowMessage($"{fuelItem.materialName}이(가) 필요합니다! ({fuelCount}/{fuelPerUse})");
                return false;
            }
        }
        
        // 사용 가능한 레시피 찾기
        var availableRecipes = GetAvailableRecipes();
        
        if (availableRecipes.Count > 0)
        {
            // 여러 레시피가 있으면 UI로 선택하게 할 수도 있음
            // 지금은 첫 번째 레시피 사용
            selectedRecipe = availableRecipes[0];
            
            // 재료와 연료 소비
            ConsumeIngredients(selectedRecipe);
            if (requiresFuel)
            {
                InventoryManager.instance.RemoveItem(fuelItem, fuelPerUse);
            }
            
            // 처리 시간 계산
            float processingTime = selectedRecipe.baseProcessingTime / temperatureMultiplier;
            
            // 처리 시작
            StartProcessing(selectedRecipe.inputItems[0].item, 
                          selectedRecipe.inputItems[0].amount, 
                          processingTime);
            
            return true;
        }
        else
        {
            ShowMessage("제련할 수 있는 재료가 없습니다.");
            return false;
        }
    }
    
    // ===== 사용 가능한 레시피 목록 =====
    private List<WorkstationRecipe> GetAvailableRecipes()
    {
        List<WorkstationRecipe> available = new List<WorkstationRecipe>();
        
        foreach (var recipe in furnaceRecipes)
        {
            if (recipe.workstationType == WorkstationType.Furnace && recipe.CanExecute())
            {
                available.Add(recipe);
            }
        }
        
        return available;
    }
    
    // ===== 재료 소비 =====
    private void ConsumeIngredients(WorkstationRecipe recipe)
    {
        recipe.ConsumeInputItems();
    }
    
    // ===== 처리 결과물 설정 =====
    protected override void SetupProcessingOutput()
    {
        if (selectedRecipe == null) return;
        
        state.completedItems.Clear();
        
        // 레시피의 결과물 생성
        var outputs = selectedRecipe.GenerateOutputs();
        foreach (var output in outputs)
        {
            state.completedItems.Add(output);
            Debug.Log($"제련 완료: {output.item.materialName} x{output.amount}");
        }
    }
    
    // ===== 효과 관리 오버라이드 =====
    protected override void StartProcessingEffects()
    {
        base.StartProcessingEffects();
        
        // 용광로 특유의 화염 효과
        if (fireEffect != null)
        {
            fireEffect.Play();
        }
        
        // 빛 효과
        if (furnaceLight != null)
        {
            furnaceLight.enabled = true;
            // 깜빡이는 효과를 위한 코루틴 시작
            StartCoroutine(FlickerLight());
        }
    }
    
    protected override void StopProcessingEffects()
    {
        base.StopProcessingEffects();
        
        if (fireEffect != null)
        {
            fireEffect.Stop();
        }
        
        if (furnaceLight != null)
        {
            furnaceLight.enabled = false;
            StopAllCoroutines();
        }
    }
    
    // ===== 빛 깜빡임 효과 =====
    private System.Collections.IEnumerator FlickerLight()
    {
        float baseIntensity = furnaceLight.intensity;
        
        while (furnaceLight.enabled)
        {
            furnaceLight.intensity = baseIntensity + Random.Range(-0.2f, 0.2f);
            yield return new WaitForSeconds(Random.Range(0.05f, 0.15f));
        }
    }
    
    // ===== UI 업데이트 =====
    protected override void UpdateInteractionPrompt()
    {
        base.UpdateInteractionPrompt();
        
        if (state.currentStatus == WorkstationStatus.Idle && !state.isBroken)
        {
            var recipes = GetAvailableRecipes();
            if (recipes.Count > 0)
            {
                // 첫 번째 가능한 레시피 표시
                var firstRecipe = recipes[0];
                interactionName = $"{firstRecipe.recipeName} 제련하기";
            }
            else if (requiresFuel && InventoryManager.instance.GetItemQuantity(fuelItem) < fuelPerUse)
            {
                interactionName = $"{fuelItem.materialName} 필요";
            }
        }
    }
}