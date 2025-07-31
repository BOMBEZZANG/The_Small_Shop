using UnityEngine;
using UnityEngine.Events;
using System.Collections.Generic;
using System.Linq;

public class DecomposerController : BaseWorkstationController
{
    [Header("Decomposer Settings")]
    [SerializeField] private DecomposerData decomposerData;
    [SerializeField] private bool isFirstDecomposer = false; // 튜토리얼용 첫 분해기
    
    [Header("Tutorial")]
    [SerializeField] private string tutorialCompleteMessage = "분해기가 작동합니다! 이제 고철을 분해할 수 있습니다.";
    
    // 첫 수리 완료 이벤트
    public UnityEvent OnFirstRepairCompleted;
    
    protected override void Awake()
    {
        base.Awake();
        
        // DecomposerData를 WorkstationData로도 설정
        if (decomposerData != null)
        {
            workstationData = decomposerData;
        }
    }
    
    protected override void Start()
    {
        base.Start();
        
        // 첫 분해기인 경우 고장 상태로 시작
        if (isFirstDecomposer)
        {
            state.currentDurability = 0;
            state.currentStatus = WorkstationStatus.Broken;
            UpdateInteractionPrompt();
            
            Debug.Log("튜토리얼 분해기: 고장 상태로 시작");
        }
    }
    
    // ===== 처리 시작 시도 =====
    protected override bool TryStartProcessing(PlayerController player)
    {
        // 사용 가능한 레시피 찾기
        var availableRecipe = FindAvailableRecipe();
        
        if (availableRecipe != null)
        {
            // 재료 소비
            InventoryManager.instance.RemoveItem(availableRecipe.inputItem, availableRecipe.inputAmount);
            
            // 처리 시간 계산 (내구도에 따른 속도 패널티 적용)
            float processingTime = CalculateProcessingTime(availableRecipe.processingTime);
            
            // 현재 레시피 저장
            state.currentRecipe = availableRecipe;
            
            // 처리 시작
            StartProcessing(availableRecipe.inputItem, availableRecipe.inputAmount, processingTime);
            
            return true;
        }
        else
        {
            // 분해 가능한 아이템이 없음
            ShowMessage("분해할 수 있는 아이템이 없습니다.");
            return false;
        }
    }
    
    // ===== 사용 가능한 레시피 찾기 =====
    private DecomposerRecipe FindAvailableRecipe()
    {
        // null 체크 추가
        if (decomposerData == null)
        {
            return null;
        }
        
        if (decomposerData.recipes == null || decomposerData.recipes.Count == 0)
        {
            Debug.LogWarning("분해기에 레시피가 설정되지 않았습니다!");
            return null;
        }
        
        // 인벤토리에서 분해 가능한 첫 번째 레시피 찾기
        foreach (var recipe in decomposerData.recipes)
        {
            if (recipe.inputItem != null)
            {
                int itemCount = InventoryManager.instance.GetItemQuantity(recipe.inputItem);
                if (itemCount >= recipe.inputAmount)
                {
                    return recipe;
                }
            }
        }
        
        return null;
    }
    
    // ===== 처리 시간 계산 =====
    private float CalculateProcessingTime(float baseTime)
    {
        float time = baseTime;
        
        // 속도 배수 적용
        time /= decomposerData.speedMultiplier;
        
        // 내구도에 따른 속도 패널티
        float durabilityPercent = state.durabilityPercentage;
        
        if (durabilityPercent <= 0.2f)
        {
            time /= decomposerData.speedPenaltyAt20Percent;
        }
        else if (durabilityPercent <= 0.5f)
        {
            time /= decomposerData.speedPenaltyAt50Percent;
        }
        
        return time;
    }
    
    // ===== 처리 결과물 설정 =====
    protected override void SetupProcessingOutput()
    {
        if (state.currentRecipe == null) return;
        
        state.completedItems.Clear();
        
        // 품질 배수 계산 (내구도가 낮으면 품질도 낮음)
        float qualityMultiplier = 1f;
        if (state.durabilityPercentage <= 0.2f)
        {
            qualityMultiplier = decomposerData.qualityPenaltyAt20Percent;
        }
        
        // 각 출력 아이템 처리
        foreach (var output in state.currentRecipe.outputs)
        {
            // 기본 확률에 품질 배수 적용
            float chance = output.chance * qualityMultiplier;
            
            // 보너스 확률 추가
            chance += decomposerData.bonusOutputChance;
            chance = Mathf.Min(chance, 100f);
            
            // 확률 체크
            if (Random.Range(0f, 100f) <= chance)
            {
                // 수량 결정
                int amount = Random.Range(output.minAmount, output.maxAmount + 1);
                
                // 품질이 낮으면 수량도 감소
                if (qualityMultiplier < 1f)
                {
                    amount = Mathf.Max(1, Mathf.RoundToInt(amount * qualityMultiplier));
                }
                
                state.completedItems.Add(new ProcessedItem(output.outputItem, amount));
                
                Debug.Log($"분해 결과: {output.outputItem.materialName} x{amount}");
            }
        }
        
        // 결과물이 하나도 없는 경우 최소 보장
        if (state.completedItems.Count == 0 && state.currentRecipe.outputs.Count > 0)
        {
            var firstOutput = state.currentRecipe.outputs[0];
            state.completedItems.Add(new ProcessedItem(firstOutput.outputItem, 1));
            
            Debug.Log("분해 실패... 최소 결과물만 획득");
        }
    }
    
    // ===== 첫 수리 완료 오버라이드 =====
    protected override void OnFirstRepairComplete()
    {
        base.OnFirstRepairComplete();
        
        if (isFirstDecomposer)
        {
            // 튜토리얼 메시지
            ShowMessage(tutorialCompleteMessage);
            
            // 이벤트 발생
            OnFirstRepairCompleted?.Invoke();
            
            // 더 이상 첫 분해기가 아님
            isFirstDecomposer = false;
            
            // 퀘스트 진행 등 추가 처리
            // QuestManager.instance?.CompleteObjective("repair_decomposer");
        }
    }
    
    // ===== UI 업데이트 오버라이드 =====
    protected override void UpdateInteractionPrompt()
    {
        base.UpdateInteractionPrompt();
        
        // 대기 상태에서 분해 가능한 아이템 표시
        if (state.currentStatus == WorkstationStatus.Idle && !state.isBroken)
        {
            var availableRecipe = FindAvailableRecipe();
            if (availableRecipe != null)
            {
                interactionName = $"{availableRecipe.inputItem.materialName} 분해하기";
            }
            else
            {
                interactionName = "분해할 아이템 없음";
            }
        }
    }
    
    // ===== 디버그 기능 =====
    [ContextMenu("Debug - Add Test Scrap")]
    private void DebugAddScrap()
    {
        if (decomposerData.recipes.Count > 0 && decomposerData.recipes[0].inputItem != null)
        {
            InventoryManager.instance.AddItem(decomposerData.recipes[0].inputItem, 5);
            Debug.Log("테스트용 고철 5개 추가");
        }
    }
    
    [ContextMenu("Debug - Break Decomposer")]
    private void DebugBreak()
    {
        state.currentDurability = 0;
        BreakWorkstation();
    }
    
    [ContextMenu("Debug - Full Repair")]
    private void DebugRepair()
    {
        state.currentDurability = workstationData.maxDurability;
        state.currentStatus = WorkstationStatus.Idle;
        UpdateInteractionPrompt();
    }
    
    // ===== Gizmos =====
    private void OnDrawGizmosSelected()
    {
        // 효과 스폰 위치 표시
        if (effectSpawnPoint != null)
        {
            Gizmos.color = Color.yellow;
            Gizmos.DrawWireSphere(effectSpawnPoint.position, 0.2f);
        }
        
        // 상태 정보 표시
        #if UNITY_EDITOR
        UnityEditor.Handles.Label(
            transform.position + Vector3.up * 2f,
            $"분해기\n상태: {state.currentStatus}\n내구도: {state.currentDurability}%"
        );
        #endif
    }
}