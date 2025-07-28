using UnityEngine;
using System;

public class StaminaManager : MonoBehaviour
{
    public static StaminaManager instance = null;
    
    // 스태미나 변경 이벤트 (현재 스태미나, 최대 스태미나)
    public static event Action<int, int> OnStaminaChanged;
    
    [Header("Stamina Settings")]
    [SerializeField] private int maxStamina = 100;
    [SerializeField] private int currentStamina = 100;
    [SerializeField] private float regenRate = 1f; // 초당 회복량
    [SerializeField] private bool enableAutoRegen = true;
    
    private float regenTimer = 0f;
    
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
        currentStamina = maxStamina;
        OnStaminaChanged?.Invoke(currentStamina, maxStamina);
    }
    
    void Update()
    {
        // 자동 회복
        if (enableAutoRegen && currentStamina < maxStamina)
        {
            regenTimer += Time.deltaTime;
            
            if (regenTimer >= 1f)
            {
                RestoreStamina(Mathf.FloorToInt(regenRate));
                regenTimer = 0f;
            }
        }
    }
    
    public bool UseStamina(int amount)
    {
        if (amount <= 0) return false;
        
        if (currentStamina >= amount)
        {
            currentStamina -= amount;
            Debug.Log($"스태미나 사용: -{amount} (남은 스태미나: {currentStamina}/{maxStamina})");
            OnStaminaChanged?.Invoke(currentStamina, maxStamina);
            return true;
        }
        else
        {
            Debug.Log("스태미나가 부족합니다!");
            return false;
        }
    }
    
    public void RestoreStamina(int amount)
    {
        if (amount <= 0) return;
        
        int previousStamina = currentStamina;
        currentStamina = Mathf.Min(currentStamina + amount, maxStamina);
        
        if (currentStamina != previousStamina)
        {
            Debug.Log($"스태미나 회복: +{currentStamina - previousStamina} ({currentStamina}/{maxStamina})");
            OnStaminaChanged?.Invoke(currentStamina, maxStamina);
        }
    }
    
    // 최대 스태미나까지 즉시 회복
    public void RestoreFullStamina()
    {
        currentStamina = maxStamina;
        OnStaminaChanged?.Invoke(currentStamina, maxStamina);
    }
    
    // 최대 스태미나 증가 (레벨업 등)
    public void IncreaseMaxStamina(int amount)
    {
        maxStamina += amount;
        currentStamina = Mathf.Min(currentStamina, maxStamina);
        OnStaminaChanged?.Invoke(currentStamina, maxStamina);
    }
    
    public int GetStamina() => currentStamina;
    public int GetMaxStamina() => maxStamina;
    public float GetStaminaPercentage() => (float)currentStamina / maxStamina;
    
    // 자동 회복 설정
    public void SetAutoRegen(bool enable)
    {
        enableAutoRegen = enable;
    }
}