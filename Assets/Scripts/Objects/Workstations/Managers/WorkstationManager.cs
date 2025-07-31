using UnityEngine;
using System.Collections.Generic;
using System;

public class WorkstationManager : MonoBehaviour
{
    public static WorkstationManager instance = null;
    
    // 이벤트
    public static event Action<BaseWorkstationController, WorkstationStatus> OnWorkstationStatusChanged;
    public static event Action<BaseWorkstationController, MaterialData, int> OnItemProcessed;
    public static event Action<BaseWorkstationController, int> OnDurabilityChanged;
    public static event Action<BaseWorkstationController> OnWorkstationRepaired;
    
    // 등록된 모든 작업대
    private Dictionary<string, BaseWorkstationController> allWorkstations = new Dictionary<string, BaseWorkstationController>();
    
    // 활성 처리 중인 작업대들 (효율적인 업데이트를 위해)
    private List<BaseWorkstationController> activeWorkstations = new List<BaseWorkstationController>();
    
    void Awake()
    {
        if (instance == null)
        {
            instance = this;
            DontDestroyOnLoad(gameObject);
        }
        else if (instance != this)
        {
            Destroy(gameObject);
        }
    }
    
    // 작업대 등록
    public void RegisterWorkstation(BaseWorkstationController workstation)
    {
        string id = workstation.GetWorkstationID();
        if (!allWorkstations.ContainsKey(id))
        {
            allWorkstations.Add(id, workstation);
            Debug.Log($"작업대 등록: {id}");
        }
    }
    
    // 작업대 등록 해제
    public void UnregisterWorkstation(BaseWorkstationController workstation)
    {
        string id = workstation.GetWorkstationID();
        if (allWorkstations.ContainsKey(id))
        {
            allWorkstations.Remove(id);
            activeWorkstations.Remove(workstation);
            Debug.Log($"작업대 등록 해제: {id}");
        }
    }
    
    // 처리 시작
    public void StartProcessing(BaseWorkstationController workstation)
    {
        if (!activeWorkstations.Contains(workstation))
        {
            activeWorkstations.Add(workstation);
        }
    }
    
    // 처리 완료
    public void CompleteProcessing(BaseWorkstationController workstation)
    {
        activeWorkstations.Remove(workstation);
    }
    
    // 상태 변경 알림
    public void NotifyStatusChanged(BaseWorkstationController workstation, WorkstationStatus newStatus)
    {
        OnWorkstationStatusChanged?.Invoke(workstation, newStatus);
        
        // UI 업데이트를 위한 추가 처리
        if (UIManager.instance != null)
        {
            // 작업대 UI 업데이트 로직
        }
    }
    
    // 아이템 처리 완료 알림
    public void NotifyItemProcessed(BaseWorkstationController workstation, MaterialData item, int amount)
    {
        OnItemProcessed?.Invoke(workstation, item, amount);
        
        // 통계 추적
        TrackProcessingStats(item, amount);
    }
    
    // 내구도 변경 알림
    public void NotifyDurabilityChanged(BaseWorkstationController workstation, int newDurability)
    {
        OnDurabilityChanged?.Invoke(workstation, newDurability);
    }
    
    // 수리 완료 알림
    public void NotifyRepaired(BaseWorkstationController workstation)
    {
        OnWorkstationRepaired?.Invoke(workstation);
    }
    
    // 특정 작업대 찾기
    public BaseWorkstationController GetWorkstation(string id)
    {
        return allWorkstations.ContainsKey(id) ? allWorkstations[id] : null;
    }
    
    // 특정 타입의 모든 작업대 찾기
    public List<BaseWorkstationController> GetWorkstationsByType(WorkstationType type)
    {
        List<BaseWorkstationController> result = new List<BaseWorkstationController>();
        foreach (var workstation in allWorkstations.Values)
        {
            if (workstation.GetWorkstationType() == type)
            {
                result.Add(workstation);
            }
        }
        return result;
    }
    
    // 저장/로드
    public WorkstationSaveData[] GetSaveData()
    {
        List<WorkstationSaveData> saveDataList = new List<WorkstationSaveData>();
        
        foreach (var workstation in allWorkstations.Values)
        {
            WorkstationSaveData saveData = workstation.GetSaveData();
            if (saveData != null)
            {
                saveDataList.Add(saveData);
            }
        }
        
        return saveDataList.ToArray();
    }
    
    public void LoadSaveData(WorkstationSaveData[] saveDataArray)
    {
        if (saveDataArray == null) return;
        
        foreach (var saveData in saveDataArray)
        {
            if (allWorkstations.ContainsKey(saveData.workstationID))
            {
                allWorkstations[saveData.workstationID].LoadSaveData(saveData);
            }
        }
    }
    
    // 통계 추적
    private Dictionary<int, int> processedItemStats = new Dictionary<int, int>();
    
    private void TrackProcessingStats(MaterialData item, int amount)
    {
        if (item == null) return;
        
        if (processedItemStats.ContainsKey(item.materialID))
        {
            processedItemStats[item.materialID] += amount;
        }
        else
        {
            processedItemStats[item.materialID] = amount;
        }
    }
    
    public int GetProcessedItemCount(MaterialData item)
    {
        return item != null && processedItemStats.ContainsKey(item.materialID) 
            ? processedItemStats[item.materialID] : 0;
    }
}