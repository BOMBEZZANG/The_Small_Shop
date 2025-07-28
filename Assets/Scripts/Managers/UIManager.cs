using UnityEngine;
using System.Collections.Generic;

public class UIManager : MonoBehaviour
{
    public static UIManager instance = null;
    
    [Header("UI References")]
    public InventoryUI inventoryUI;
    public GameStatusUI gameStatusUI;  // 새로 추가
    
    private bool isInventoryOpen = false;

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
    
    void OnEnable()
    {
        // 기존 인벤토리 이벤트
        InventoryManager.OnInventoryChanged += OnInventoryUpdated;
        
        // 새로운 게임 상태 이벤트들 구독
        GoldManager.OnGoldChanged += OnGoldUpdated;
        TimeManager.OnTimeChanged += OnTimeUpdated;
        StaminaManager.OnStaminaChanged += OnStaminaUpdated;
    }
    
    void OnDisable()
    {
        // 이벤트 구독 해제
        InventoryManager.OnInventoryChanged -= OnInventoryUpdated;
        GoldManager.OnGoldChanged -= OnGoldUpdated;
        TimeManager.OnTimeChanged -= OnTimeUpdated;
        StaminaManager.OnStaminaChanged -= OnStaminaUpdated;
    }
    
    // ===== 인벤토리 관련 (기존) =====
    private void OnInventoryUpdated()
    {
        if (isInventoryOpen)
        {
            Dictionary<MaterialData, int> items = InventoryManager.instance.GetInventoryItems();
            inventoryUI.UpdateDisplay(items);
            Debug.Log("인벤토리 UI가 자동으로 업데이트되었습니다!");
        }
    }

    public void ToggleInventoryUI()
    {
        isInventoryOpen = !isInventoryOpen;

        if (isInventoryOpen)
        {
            inventoryUI.Open();
            Dictionary<MaterialData, int> items = InventoryManager.instance.GetInventoryItems();
            Debug.Log("인벤토리 아이템 " + items.Count + "종류를 받아왔습니다.");
            inventoryUI.UpdateDisplay(items);
        }
        else
        {
            inventoryUI.Close();
        }
    }
    
    // ===== 게임 상태 UI 업데이트 (새로 추가) =====
    
    // 골드 업데이트
    private void OnGoldUpdated(int newGoldAmount)
    {
        if (gameStatusUI != null)
        {
            gameStatusUI.UpdateGoldDisplay(newGoldAmount);
        }
    }
    
    // 시간 업데이트
    private void OnTimeUpdated(int hour, int minute, float totalMinutes)
    {
        if (gameStatusUI != null)
        {
            gameStatusUI.UpdateTimeDisplay(hour, minute);
        }
    }
    
    // 스태미나 업데이트
    private void OnStaminaUpdated(int currentStamina, int maxStamina)
    {
        if (gameStatusUI != null)
        {
            gameStatusUI.UpdateStaminaDisplay(currentStamina, maxStamina);
        }
    }
    
    // ===== 추가 UI 기능 =====
    
    // 모든 UI 숨기기/보이기
    public void ToggleAllUI(bool show)
    {
        if (inventoryUI != null)
            inventoryUI.gameObject.SetActive(show);
            
        if (gameStatusUI != null)
            gameStatusUI.gameObject.SetActive(show);
    }
    
    // 알림 메시지 표시 (추후 구현 가능)
    public void ShowNotification(string message, float duration = 2f)
    {
        Debug.Log($"알림: {message}");
        // TODO: 실제 알림 UI 구현
    }
    
    // 디버그용 - 현재 상태 확인
    public void LogCurrentStatus()
    {
        Debug.Log($"=== 현재 게임 상태 ===");
        Debug.Log($"골드: {GoldManager.instance.GetGold()}");
        Debug.Log($"시간: {TimeManager.instance.GetHour()}:{TimeManager.instance.GetMinute()}");
        Debug.Log($"스태미나: {StaminaManager.instance.GetStamina()}/{StaminaManager.instance.GetMaxStamina()}");
    }
}