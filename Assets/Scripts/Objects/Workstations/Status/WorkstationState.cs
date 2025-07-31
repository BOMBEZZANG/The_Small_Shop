using UnityEngine;
using System;
using System.Collections.Generic;

[System.Serializable]
public class WorkstationState
{
    // 기본 상태
    public WorkstationStatus currentStatus = WorkstationStatus.Idle;
    public int currentDurability = 100;
    
    // 처리 중인 아이템 정보
    public MaterialData processingItem;
    public int processingAmount;
    public float processingStartTime;
    public float processingDuration;
    public WorkstationRecipe currentRecipe;
    
    // 처리 완료된 아이템들
    public List<ProcessedItem> completedItems = new List<ProcessedItem>();
    
    // 내구도 관련
    public bool isBroken => currentDurability <= 0;
    public float durabilityPercentage => currentDurability / 100f;
    
    // 처리 진행률 계산
    public float GetProgress()
    {
        if (currentStatus != WorkstationStatus.Processing)
            return 0f;
            
        float elapsed = Time.time - processingStartTime;
        return Mathf.Clamp01(elapsed / processingDuration);
    }
    
    public bool IsProcessingComplete()
    {
        return currentStatus == WorkstationStatus.Processing && GetProgress() >= 1f;
    }
    
    public void Clear()
    {
        currentStatus = WorkstationStatus.Idle;
        processingItem = null;
        processingAmount = 0;
        processingStartTime = 0;
        processingDuration = 0;
        currentRecipe = null;
        completedItems.Clear();
    }
}

[System.Serializable]
public class ProcessedItem
{
    public MaterialData item;
    public int amount;
    
    public ProcessedItem(MaterialData item, int amount)
    {
        this.item = item;
        this.amount = amount;
    }
}

public enum WorkstationStatus
{
    Idle,       // 대기 중
    Processing, // 처리 중
    Complete,   // 완료 (수거 대기)
    Broken      // 고장 (수리 필요)
}

// Save/Load를 위한 직렬화 가능한 상태
[System.Serializable]
public class WorkstationSaveData
{
    public string workstationID;
    public WorkstationStatus status;
    public int durability;
    
    // 처리 중인 아이템 (ID로 저장)
    public int processingItemID;
    public int processingAmount;
    public float remainingTime;
    
    // 완료된 아이템들
    public List<SavedItem> completedItems = new List<SavedItem>();
    
    [System.Serializable]
    public class SavedItem
    {
        public int itemID;
        public int amount;
    }
}