using System.Collections.Generic;
using UnityEngine;
using TMPro;

public class InventoryUI : MonoBehaviour
{
    [Header("UI References")]
    public GameObject inventoryPanel;
    public Transform contentTransform;
    public GameObject slotPrefab;
    
    [Header("Pool Settings")]
    [SerializeField] private int initialPoolSize = 20;     // 초기 슬롯 생성 개수
    [SerializeField] private int poolExpandSize = 5;       // 부족할 때 추가 생성 개수
    [SerializeField] private bool trimExcessSlots = true;  // 과도한 슬롯 자동 정리 여부
    [SerializeField] private int maxPoolSize = 100;        // 최대 슬롯 개수 제한
    
    // 슬롯 풀 관리
    private List<InventorySlot> allSlots = new List<InventorySlot>();        // 생성된 모든 슬롯
    private Queue<InventorySlot> availableSlots = new Queue<InventorySlot>(); // 사용 가능한 슬롯
    private List<InventorySlot> activeSlots = new List<InventorySlot>();      // 현재 사용 중인 슬롯
    
    // 성능 모니터링 (디버그용)
    private int totalSlotsCreated = 0;
    private int reuseCount = 0;
    
    void Awake()
    {
        // 초기 슬롯 풀 생성
        InitializeSlotPool();
    }
    
    void Update()
    {
        // I키로 인벤토리 토글
        if (Input.GetKeyDown(KeyCode.I))
        {
            UIManager.instance.ToggleInventoryUI();
        }
    }
    
    // ===== 슬롯 풀 초기화 =====
    private void InitializeSlotPool()
    {
        for (int i = 0; i < initialPoolSize; i++)
        {
            CreateNewSlot();
        }
        
        Debug.Log($"[InventoryUI] 슬롯 풀 초기화 완료: {initialPoolSize}개 생성");
    }
    
    // ===== 새 슬롯 생성 =====
    private InventorySlot CreateNewSlot()
    {
        GameObject slotObj = Instantiate(slotPrefab, contentTransform);
        InventorySlot slot = slotObj.GetComponent<InventorySlot>();
        
        if (slot == null)
        {
            Debug.LogError("[InventoryUI] 슬롯 프리팹에 InventorySlot 컴포넌트가 없습니다!");
            Destroy(slotObj);
            return null;
        }
        
        // 초기 설정
        slotObj.SetActive(false);
        slotObj.name = $"InventorySlot_{totalSlotsCreated}";
        
        // 풀에 추가
        allSlots.Add(slot);
        availableSlots.Enqueue(slot);
        totalSlotsCreated++;
        
        return slot;
    }
    
    // ===== 풀에서 슬롯 가져오기 =====
    private InventorySlot GetSlotFromPool()
    {
        InventorySlot slot = null;
        
        // 사용 가능한 슬롯이 있는지 확인
        if (availableSlots.Count > 0)
        {
            slot = availableSlots.Dequeue();
            reuseCount++;
        }
        else
        {
            // 풀이 부족한 경우
            if (allSlots.Count < maxPoolSize)
            {
                // 추가 생성
                Debug.Log($"[InventoryUI] 슬롯 부족! {poolExpandSize}개 추가 생성");
                for (int i = 0; i < poolExpandSize && allSlots.Count < maxPoolSize; i++)
                {
                    CreateNewSlot();
                }
                
                if (availableSlots.Count > 0)
                {
                    slot = availableSlots.Dequeue();
                    reuseCount++;
                }
            }
            else
            {
                Debug.LogWarning($"[InventoryUI] 최대 슬롯 개수({maxPoolSize})에 도달했습니다!");
            }
        }
        
        if (slot != null)
        {
            slot.gameObject.SetActive(true);
            activeSlots.Add(slot);
        }
        
        return slot;
    }
    
    // ===== 슬롯을 풀로 반환 =====
    private void ReturnSlotToPool(InventorySlot slot)
    {
        if (slot == null) return;
        
        slot.gameObject.SetActive(false);
        slot.Clear();  // 슬롯 데이터 초기화
        
        activeSlots.Remove(slot);
        availableSlots.Enqueue(slot);
    }
    
    // ===== UI 열기 =====
    public void Open()
    {
        inventoryPanel.SetActive(true);
        LogPoolStats();  // 디버그용
    }
    
    // ===== UI 닫기 =====
    public void Close()
    {
        inventoryPanel.SetActive(false);
        
        // 과도한 슬롯 정리 (선택적)
        if (trimExcessSlots && allSlots.Count > initialPoolSize * 2)
        {
            TrimExcessSlots();
        }
    }
    
    // ===== 인벤토리 표시 업데이트 (최적화됨) =====
    public void UpdateDisplay(Dictionary<MaterialData, int> items)
    {
        // 성능 측정 시작 (디버그용)
        float startTime = Time.realtimeSinceStartup;
        
        // 현재 활성화된 슬롯들을 임시 저장
        List<InventorySlot> previousSlots = new List<InventorySlot>(activeSlots);
        activeSlots.Clear();
        
        int slotIndex = 0;
        
        // 각 아이템에 대해 슬롯 할당
        foreach (var item in items)
        {
            InventorySlot slot = null;
            
            // 기존 슬롯 재사용 시도
            if (slotIndex < previousSlots.Count)
            {
                slot = previousSlots[slotIndex];
                previousSlots[slotIndex] = null;  // 재사용 표시
                activeSlots.Add(slot);
            }
            else
            {
                // 새 슬롯 필요
                slot = GetSlotFromPool();
            }
            
            if (slot != null)
            {
                slot.SetItem(item.Key, item.Value);
                slot.transform.SetSiblingIndex(slotIndex);  // UI 순서 유지
            }
            else
            {
                Debug.LogError($"[InventoryUI] 슬롯을 할당할 수 없습니다! (아이템: {item.Key.materialName})");
            }
            
            slotIndex++;
        }
        
        // 사용하지 않은 이전 슬롯들을 풀로 반환
        foreach (var slot in previousSlots)
        {
            if (slot != null)
            {
                ReturnSlotToPool(slot);
            }
        }
        
        // 성능 측정 종료 (디버그용)
        float endTime = Time.realtimeSinceStartup;
        float deltaTime = (endTime - startTime) * 1000f;  // ms로 변환
        
        // 성능 로그 출력 (빌드에서는 자동 제외)
        if (Debug.isDebugBuild)
        {
            Debug.Log($"[InventoryUI] 업데이트 완료: {deltaTime:F2}ms, " +
                     $"아이템 {items.Count}개, " +
                     $"재사용률 {GetReuseRate():F1}%");
        }
    }
    
    // ===== 과도한 슬롯 정리 =====
    private void TrimExcessSlots()
    {
        int targetCount = initialPoolSize;
        int removeCount = allSlots.Count - targetCount;
        
        if (removeCount <= 0 || availableSlots.Count == 0) return;
        
        Debug.Log($"[InventoryUI] 과도한 슬롯 {removeCount}개 정리 시작");
        
        int removed = 0;
        while (removed < removeCount && availableSlots.Count > 0)
        {
            InventorySlot slot = availableSlots.Dequeue();
            allSlots.Remove(slot);
            
            if (slot != null && slot.gameObject != null)
            {
                Destroy(slot.gameObject);
            }
            
            removed++;
        }
        
        Debug.Log($"[InventoryUI] {removed}개 슬롯 정리 완료");
    }
    
    // ===== 디버그 및 통계 =====
    private float GetReuseRate()
    {
        if (totalSlotsCreated == 0) return 0f;
        return (float)reuseCount / (reuseCount + totalSlotsCreated) * 100f;
    }
    
    public void LogPoolStats()
    {
        Debug.Log($"=== 인벤토리 슬롯 풀 통계 ===");
        Debug.Log($"총 생성된 슬롯: {totalSlotsCreated}개");
        Debug.Log($"현재 풀 크기: {allSlots.Count}개");
        Debug.Log($"활성 슬롯: {activeSlots.Count}개");
        Debug.Log($"대기 슬롯: {availableSlots.Count}개");
        Debug.Log($"재사용 횟수: {reuseCount}회");
        Debug.Log($"재사용률: {GetReuseRate():F1}%");
        Debug.Log($"========================");
    }
    
    // ===== 풀 초기화 (씬 전환 등) =====
    public void ResetPool()
    {
        // 모든 슬롯을 풀로 반환
        List<InventorySlot> tempList = new List<InventorySlot>(activeSlots);
        foreach (var slot in tempList)
        {
            ReturnSlotToPool(slot);
        }
        
        activeSlots.Clear();
        
        // 통계 리셋
        reuseCount = 0;
    }
    
    // ===== 정리 =====
    void OnDestroy()
    {
        // 모든 슬롯 파괴
        foreach (var slot in allSlots)
        {
            if (slot != null && slot.gameObject != null)
            {
                Destroy(slot.gameObject);
            }
        }
        
        allSlots.Clear();
        availableSlots.Clear();
        activeSlots.Clear();
    }
}