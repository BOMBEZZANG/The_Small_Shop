using UnityEngine;
using System;

public class PlayerDataManager : MonoBehaviour
{
    public static PlayerDataManager instance = null;
    
    // 이벤트
    public static event Action<int> OnLevelChanged;           // 레벨 변경 (새 레벨)
    public static event Action<int, int> OnExpChanged;        // 경험치 변경 (현재 경험치, 필요 경험치)
    public static event Action<int> OnLevelUp;                // 레벨업 (새 레벨)
    
    [Header("Player Stats")]
    [SerializeField] private int currentLevel = 1;
    [SerializeField] private int currentExp = 0;
    
    [Header("Level Settings")]
    [SerializeField] private int baseExpRequired = 100;       // 레벨 2에 필요한 기본 경험치
    [SerializeField] private float expMultiplier = 1.5f;      // 레벨당 경험치 증가 배수
    [SerializeField] private int maxLevel = 50;               // 최대 레벨
    
    // 계산된 값
    private int expRequiredForNextLevel;
    
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
        // 초기 필요 경험치 계산
        CalculateRequiredExp();
        
        // 초기값 이벤트 발생
        OnLevelChanged?.Invoke(currentLevel);
        OnExpChanged?.Invoke(currentExp, expRequiredForNextLevel);
    }
    
    // ===== 경험치 관련 =====
    public void AddExp(int amount)
    {
        if (amount <= 0) return;
        if (currentLevel >= maxLevel) 
        {
            Debug.Log("최대 레벨에 도달했습니다!");
            return;
        }
        
        currentExp += amount;
        Debug.Log($"경험치 획득: +{amount} (총 {currentExp}/{expRequiredForNextLevel})");
        
        // 레벨업 체크
        CheckLevelUp();
        
        // 이벤트 발생
        OnExpChanged?.Invoke(currentExp, expRequiredForNextLevel);
    }
    
    // 레벨업 체크
    private void CheckLevelUp()
    {
        while (currentExp >= expRequiredForNextLevel && currentLevel < maxLevel)
        {
            // 레벨업!
            currentExp -= expRequiredForNextLevel;
            currentLevel++;
            
            Debug.Log($"레벨업! 현재 레벨: {currentLevel}");
            
            // 다음 레벨 필요 경험치 재계산
            CalculateRequiredExp();
            
            // 레벨업 이벤트
            OnLevelUp?.Invoke(currentLevel);
            OnLevelChanged?.Invoke(currentLevel);
            
            // 레벨업 보상 (추후 확장)
            ApplyLevelUpRewards();
        }
        
        // 최대 레벨 도달 시 경험치 초과분 제거
        if (currentLevel >= maxLevel)
        {
            currentExp = 0;
            expRequiredForNextLevel = 0;
        }
    }
    
    // 필요 경험치 계산
    private void CalculateRequiredExp()
    {
        if (currentLevel >= maxLevel)
        {
            expRequiredForNextLevel = 0;
            return;
        }
        
        // 공식: baseExp * (multiplier ^ (level - 1))
        expRequiredForNextLevel = Mathf.RoundToInt(baseExpRequired * Mathf.Pow(expMultiplier, currentLevel - 1));
    }
    
    // 레벨업 보상 적용
    private void ApplyLevelUpRewards()
    {
        // 레벨업 시 체력/스태미나 회복
        if (StaminaManager.instance != null)
        {
            StaminaManager.instance.RestoreFullStamina();
        }
        
        // 추후 확장 예시:
        // - 스킬 포인트 지급
        // - 최대 체력/스태미나 증가
        // - 새로운 능력 해금
    }
    
    // ===== Getter 메서드 =====
    public int GetLevel() => currentLevel;
    public int GetCurrentExp() => currentExp;
    public int GetRequiredExp() => expRequiredForNextLevel;
    public float GetExpPercentage() => (float)currentExp / expRequiredForNextLevel;
    public bool IsMaxLevel() => currentLevel >= maxLevel;
    
    // ===== 치트/테스트용 =====
    public void SetLevel(int level)
    {
        level = Mathf.Clamp(level, 1, maxLevel);
        currentLevel = level;
        currentExp = 0;
        CalculateRequiredExp();
        
        OnLevelChanged?.Invoke(currentLevel);
        OnExpChanged?.Invoke(currentExp, expRequiredForNextLevel);
    }
    
    public void AddLevels(int levels)
    {
        for (int i = 0; i < levels; i++)
        {
            if (currentLevel >= maxLevel) break;
            
            // 현재 레벨에서 필요한 경험치만큼 추가
            AddExp(expRequiredForNextLevel - currentExp);
        }
    }
    
    // ===== 저장/불러오기 준비 =====
    public PlayerSaveData GetSaveData()
    {
        return new PlayerSaveData
        {
            level = currentLevel,
            exp = currentExp
        };
    }
    
    public void LoadSaveData(PlayerSaveData data)
    {
        currentLevel = data.level;
        currentExp = data.exp;
        CalculateRequiredExp();
        
        OnLevelChanged?.Invoke(currentLevel);
        OnExpChanged?.Invoke(currentExp, expRequiredForNextLevel);
    }
}

// 저장용 데이터 클래스
[System.Serializable]
public class PlayerSaveData
{
    public int level;
    public int exp;
    // 추후 확장
    // public int hp;
    // public int mp;
    // public int skillPoints;
}