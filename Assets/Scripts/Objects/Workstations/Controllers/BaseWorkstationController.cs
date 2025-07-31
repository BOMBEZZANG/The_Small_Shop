using UnityEngine;
using UnityEngine.Events;
using System.Collections;
using System.Collections.Generic;

[RequireComponent(typeof(Collider2D))]
public abstract class BaseWorkstationController : InteractableObject
{
    [Header("Workstation Data")]
    [SerializeField] protected WorkstationData workstationData;
    [SerializeField] protected WorkstationInteractionData workstationUIData;
    
    [Header("State")]
    [SerializeField] protected WorkstationState state = new WorkstationState();
    
    [Header("Repair")]
    [SerializeField] protected RepairRequirement repairRequirement;
    
    [Header("Effects")]
    [SerializeField] protected Transform effectSpawnPoint;
    [SerializeField] protected GameObject currentEffect;
    
    [Header("Events")]
    public UnityEvent<int> OnDurabilityChanged;
    public UnityEvent OnProcessingStarted;
    public UnityEvent OnProcessingCompleted;
    public UnityEvent OnRepaired;
    
    // 고유 ID (저장/로드용)
    protected string workstationID;
    
    // 컴포넌트
    protected AudioSource audioSource;
    protected Animator animator;
    
    protected override void Awake()
    {
        base.Awake();
        
        // 컴포넌트 가져오기
        audioSource = GetComponent<AudioSource>();
        animator = GetComponent<Animator>();
        
        // 고유 ID 생성 - null 체크 추가
        if (workstationData != null)
        {
            workstationID = $"{workstationData.workstationName}_{transform.position.x}_{transform.position.y}";
        }
        else
        {
            workstationID = $"{gameObject.name}_{transform.position.x}_{transform.position.y}";
        }
        
        // 기본 설정
        interactionType = InteractionType.Use;
        
        // UpdateInteractionPrompt는 Start에서 호출하도록 이동
    }
    
    protected virtual void Start()
    {
        // 매니저에 등록
        if (WorkstationManager.instance != null)
        {
            WorkstationManager.instance.RegisterWorkstation(this);
        }
        
        // 초기 내구도 설정
        if (workstationData != null && workstationData.hasDurability)
        {
            state.currentDurability = workstationData.maxDurability;
        }
        
        // 데이터가 준비된 후 UI 업데이트
        UpdateInteractionPrompt();
    }
    
    protected virtual void Update()
    {
        // 처리 중인 경우 진행 상황 업데이트
        if (state.currentStatus == WorkstationStatus.Processing)
        {
            // 처리 완료 체크
            if (state.IsProcessingComplete())
            {
                CompleteProcessing();
            }
            else
            {
                // 진행률 이벤트 발생
                float progress = state.GetProgress();
                TriggerProgressEvent(progress);
            }
        }
    }
    
    // ===== 상호작용 오버라이드 =====
    public override void StartInteraction(PlayerController player)
    {
        // 고장난 경우 수리 시도
        if (state.isBroken)
        {
            TryRepair(player);
            return;
        }
        
        // 상태에 따른 처리
        switch (state.currentStatus)
        {
            case WorkstationStatus.Idle:
                TryStartProcessing(player);
                break;
                
            case WorkstationStatus.Complete:
                CollectOutput(player);
                break;
                
            case WorkstationStatus.Processing:
                // 처리 중에는 상호작용 불가
                ShowMessage("아직 처리 중입니다...");
                break;
        }
    }
    
    // ===== 처리 시작 (추상 메서드 - 하위 클래스에서 구현) =====
    protected abstract bool TryStartProcessing(PlayerController player);
    protected abstract void SetupProcessingOutput();
    
    // ===== 공통 처리 로직 =====
    protected virtual void StartProcessing(MaterialData inputItem, int amount, float duration)
    {
        // 상태 설정
        state.currentStatus = WorkstationStatus.Processing;
        state.processingItem = inputItem;
        state.processingAmount = amount;
        state.processingStartTime = Time.time;
        state.processingDuration = duration;
        
        // 내구도 감소
        if (workstationData.hasDurability)
        {
            ModifyDurability(-workstationData.durabilityLossPerUse);
        }
        
        // 효과 시작
        StartProcessingEffects();
        
        // 이벤트 발생
        OnProcessingStarted?.Invoke();
        UpdateInteractionPrompt();
        
        // 매니저에 알림
        WorkstationManager.instance?.StartProcessing(this);
        
        Debug.Log($"{workstationData.workstationName} 처리 시작: {inputItem.materialName} x{amount}");
    }
    
    // ===== 처리 완료 =====
    protected virtual void CompleteProcessing()
    {
        // 상태 변경
        state.currentStatus = WorkstationStatus.Complete;
        
        // 결과물 생성
        SetupProcessingOutput();
        
        // 효과 정지
        StopProcessingEffects();
        
        // 완료 사운드
        PlaySound(workstationData.completeSound);
        
        // 이벤트 발생
        OnProcessingCompleted?.Invoke();
        UpdateInteractionPrompt();
        
        // 매니저에 알림
        WorkstationManager.instance?.CompleteProcessing(this);
        
        Debug.Log($"{workstationData.workstationName} 처리 완료!");
    }
    
    // ===== 결과물 수거 =====
    protected virtual void CollectOutput(PlayerController player)
    {
        int collectedCount = 0;
        
        // 모든 결과물을 인벤토리에 추가
        foreach (var item in state.completedItems)
        {
            for (int i = 0; i < item.amount; i++)
            {
                InventoryManager.instance.AddItem(item.item);
                collectedCount++;
            }
            
            // 매니저에 알림
            WorkstationManager.instance?.NotifyItemProcessed(this, item.item, item.amount);
        }
        
        // 경험치 지급
        if (state.currentRecipe != null && state.currentRecipe.expReward > 0)
        {
            PlayerDataManager.instance?.AddExp(state.currentRecipe.expReward);
        }
        
        // 상태 초기화
        state.Clear();
        UpdateInteractionPrompt();
        
        ShowMessage($"{collectedCount}개의 아이템을 획득했습니다!");
    }
    
    // ===== 내구도 관리 =====
    protected void ModifyDurability(int amount)
    {
        if (!workstationData.hasDurability) return;
        
        int previousDurability = state.currentDurability;
        state.currentDurability = Mathf.Clamp(state.currentDurability + amount, 0, workstationData.maxDurability);
        
        if (state.currentDurability != previousDurability)
        {
            OnDurabilityChanged?.Invoke(state.currentDurability);
            WorkstationManager.instance?.NotifyDurabilityChanged(this, state.currentDurability);
            
            // 내구도가 0이 되면 고장
            if (state.currentDurability <= 0)
            {
                BreakWorkstation();
            }
        }
    }
    
    // ===== 고장 처리 =====
    protected virtual void BreakWorkstation()
    {
        state.currentStatus = WorkstationStatus.Broken;
        
        // 처리 중이었다면 중단
        if (state.processingItem != null)
        {
            StopProcessingEffects();
            state.Clear();
            state.currentStatus = WorkstationStatus.Broken;
        }
        
        UpdateInteractionPrompt();
        ShowMessage($"{workstationData.workstationName}이(가) 고장났습니다!");
    }
    
    // ===== 수리 =====
    protected virtual void TryRepair(PlayerController player)
    {
        if (repairRequirement == null)
        {
            ShowMessage("수리할 수 없습니다.");
            return;
        }
        
        if (repairRequirement.CanRepair())
        {
            // 수리 실행
            if (repairRequirement.ExecuteRepair())
            {
                // 내구도 회복
                int restoreAmount = repairRequirement.fullRepair 
                    ? workstationData.maxDurability 
                    : repairRequirement.durabilityRestore;
                    
                ModifyDurability(restoreAmount);
                
                // 상태 복구
                state.currentStatus = WorkstationStatus.Idle;
                UpdateInteractionPrompt();
                
                // 이벤트 발생
                OnRepaired?.Invoke();
                WorkstationManager.instance?.NotifyRepaired(this);
                
                ShowMessage($"{workstationData.workstationName}을(를) 수리했습니다!");
                
                // 첫 수리인 경우 특별 처리
                if (repairRequirement.isInitialRepair)
                {
                    ShowMessage(repairRequirement.initialRepairMessage);
                    OnFirstRepairComplete();
                }
            }
        }
        else
        {
            ShowMessage(repairRequirement.GetMissingItemsText());
        }
    }
    
    // ===== 첫 수리 완료 (하위 클래스에서 오버라이드 가능) =====
    protected virtual void OnFirstRepairComplete()
    {
        // 튜토리얼 처리 등
    }
    
    // ===== 효과 관리 =====
    protected virtual void StartProcessingEffects()
    {
        // 파티클 효과
        if (workstationData.processingEffect != null && effectSpawnPoint != null)
        {
            currentEffect = Instantiate(workstationData.processingEffect, effectSpawnPoint);
        }
        
        // 사운드
        PlaySound(workstationData.processingSound, true);
        
        // 애니메이션
        if (animator != null)
        {
            animator.SetBool("IsProcessing", true);
        }
    }
    
    protected virtual void StopProcessingEffects()
    {
        // 파티클 효과 제거
        if (currentEffect != null)
        {
            Destroy(currentEffect);
            currentEffect = null;
        }
        
        // 사운드 정지
        if (audioSource != null && audioSource.isPlaying)
        {
            audioSource.Stop();
        }
        
        // 애니메이션
        if (animator != null)
        {
            animator.SetBool("IsProcessing", false);
        }
    }
    
    // ===== UI 업데이트 =====
    protected virtual void UpdateInteractionPrompt()
    {
        // 데이터가 없으면 기본값 사용
        if (workstationUIData == null || state == null) 
        {
            interactionName = "상호작용";
            return;
        }
        
        // 상태에 따른 프롬프트 설정
        interactionName = workstationUIData.GetStatusMessage(state.currentStatus);
        
        // 내구도가 낮은 경우 경고
        if (workstationData != null && workstationData.hasDurability && state.durabilityPercentage < 0.2f && state.currentStatus != WorkstationStatus.Broken)
        {
            interactionName += " (수리 필요!)";
        }
    }
    
    // ===== 유틸리티 =====
    protected void PlaySound(AudioClip clip, bool loop = false)
    {
        if (audioSource != null && clip != null)
        {
            audioSource.clip = clip;
            audioSource.loop = loop;
            audioSource.Play();
        }
    }
    
    protected void ShowMessage(string message)
    {
        // UI 매니저를 통해 메시지 표시
        UIManager.instance?.ShowNotification(message, 2f);
    }
    
    // ===== Getter =====
    public string GetWorkstationID() => workstationID;
    public WorkstationType GetWorkstationType() => workstationData.workstationType;
    public WorkstationStatus GetStatus() => state.currentStatus;
    public float GetDurabilityPercentage() => state.durabilityPercentage;
    public WorkstationState GetState() => state;
    
    // ===== 저장/로드 =====
    public virtual WorkstationSaveData GetSaveData()
    {
        WorkstationSaveData saveData = new WorkstationSaveData();
        saveData.workstationID = workstationID;
        saveData.status = state.currentStatus;
        saveData.durability = state.currentDurability;
        
        // 처리 중인 아이템
        if (state.processingItem != null)
        {
            saveData.processingItemID = state.processingItem.materialID;
            saveData.processingAmount = state.processingAmount;
            saveData.remainingTime = state.processingDuration - (Time.time - state.processingStartTime);
        }
        
        // 완료된 아이템
        foreach (var item in state.completedItems)
        {
            saveData.completedItems.Add(new WorkstationSaveData.SavedItem 
            { 
                itemID = item.item.materialID, 
                amount = item.amount 
            });
        }
        
        return saveData;
    }
    
    public virtual void LoadSaveData(WorkstationSaveData saveData)
    {
        if (saveData == null) return;
        
        state.currentStatus = saveData.status;
        state.currentDurability = saveData.durability;
        
        // TODO: 저장된 아이템 ID로부터 MaterialData 복구
        // 이를 위해서는 MaterialDatabase 같은 시스템이 필요
        
        UpdateInteractionPrompt();
    }
    
    protected virtual void OnDestroy()
    {
        // 매니저에서 등록 해제
        if (WorkstationManager.instance != null)
        {
            WorkstationManager.instance.UnregisterWorkstation(this);
        }
    }
}