using UnityEngine;
using System.Collections.Generic;

public class UIManager : MonoBehaviour
{
    public static UIManager instance = null;
    
    [Header("UI References")]
    public InventoryUI inventoryUI;
    public GameStatusUI gameStatusUI;
    public PlayerStatusUI playerStatusUI;  // 새로 추가
    
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
    
    void Start()
    {
        // Start에서 초기값 업데이트
        if (gameStatusUI != null)
        {
            gameStatusUI.UpdateAllDisplays();
        }
        
        if (playerStatusUI != null)
        {
            playerStatusUI.UpdateAllDisplays();
        }
    }
    
    void OnEnable()
    {
        // 기존 이벤트
        InventoryManager.OnInventoryChanged += OnInventoryUpdated;
        GoldManager.OnGoldChanged += OnGoldUpdated;
        TimeManager.OnTimeChanged += OnTimeUpdated;
        StaminaManager.OnStaminaChanged += OnStaminaUpdated;
        
        // 플레이어 데이터 이벤트 구독 (새로 추가)
        PlayerDataManager.OnLevelChanged += OnLevelUpdated;
        PlayerDataManager.OnExpChanged += OnExpUpdated;
        PlayerDataManager.OnLevelUp += OnPlayerLevelUp;
    }
    
    void OnDisable()
    {
        // 이벤트 구독 해제
        InventoryManager.OnInventoryChanged -= OnInventoryUpdated;
        GoldManager.OnGoldChanged -= OnGoldUpdated;
        TimeManager.OnTimeChanged -= OnTimeUpdated;
        StaminaManager.OnStaminaChanged -= OnStaminaUpdated;
        
        // 플레이어 데이터 이벤트 구독 해제 (새로 추가)
        PlayerDataManager.OnLevelChanged -= OnLevelUpdated;
        PlayerDataManager.OnExpChanged -= OnExpUpdated;
        PlayerDataManager.OnLevelUp -= OnPlayerLevelUp;
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
    
    // ===== 게임 상태 UI 업데이트 (기존) =====
    private void OnGoldUpdated(int newGoldAmount)
    {
        if (gameStatusUI != null)
        {
            gameStatusUI.UpdateGoldDisplay(newGoldAmount);
        }
    }
    
    private void OnTimeUpdated(int hour, int minute, float totalMinutes)
    {
        if (gameStatusUI != null)
        {
            gameStatusUI.UpdateTimeDisplay(hour, minute);
        }
    }
    
    private void OnStaminaUpdated(int currentStamina, int maxStamina)
    {
        if (gameStatusUI != null)
        {
            gameStatusUI.UpdateStaminaDisplay(currentStamina, maxStamina);
        }
    }
    
    // ===== 플레이어 상태 UI 업데이트 (새로 추가) =====
    private void OnLevelUpdated(int newLevel)
    {
        if (playerStatusUI != null)
        {
            playerStatusUI.UpdateLevelDisplay(newLevel);
        }
    }
    
    private void OnExpUpdated(int currentExp, int requiredExp)
    {
        if (playerStatusUI != null)
        {
            playerStatusUI.UpdateExpDisplay(currentExp, requiredExp);
        }
    }
    
    private void OnPlayerLevelUp(int newLevel)
    {
        if (playerStatusUI != null)
        {
            playerStatusUI.ShowLevelUpEffect();
        }
        
        // 레벨업 알림 (선택적)
        ShowNotification($"레벨업! Lv.{newLevel} 달성!", 3f);
    }
    
    // ===== 추가 UI 기능 =====
    public void ToggleAllUI(bool show)
    {
        if (inventoryUI != null)
            inventoryUI.gameObject.SetActive(show);
            
        if (gameStatusUI != null)
            gameStatusUI.gameObject.SetActive(show);
            
        if (playerStatusUI != null)
            playerStatusUI.gameObject.SetActive(show);
    }
    
    public void ShowNotification(string message, float duration = 2f)
    {
        Debug.Log($"알림: {message}");
        // TODO: 실제 알림 UI 구현
    }
    
    public void LogCurrentStatus()
    {
        Debug.Log($"=== 현재 게임 상태 ===");
        Debug.Log($"골드: {GoldManager.instance.GetGold()}");
        Debug.Log($"시간: {TimeManager.instance.GetHour()}:{TimeManager.instance.GetMinute()}");
        Debug.Log($"스태미나: {StaminaManager.instance.GetStamina()}/{StaminaManager.instance.GetMaxStamina()}");
        Debug.Log($"레벨: {PlayerDataManager.instance.GetLevel()}");
        Debug.Log($"경험치: {PlayerDataManager.instance.GetCurrentExp()}/{PlayerDataManager.instance.GetRequiredExp()}");
    }
}