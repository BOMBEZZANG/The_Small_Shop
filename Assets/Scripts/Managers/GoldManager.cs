using UnityEngine;
using System;

public class GoldManager : MonoBehaviour
{
    public static GoldManager instance = null;
    
    // 골드 변경 이벤트 (변경된 골드량 전달)
    public static event Action<int> OnGoldChanged;
    
    [SerializeField] private int currentGold = 0;
    
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
        // 시작 시 UI 업데이트를 위해 이벤트 발생
        OnGoldChanged?.Invoke(currentGold);
    }
    
    public void AddGold(int amount)
    {
        if (amount <= 0) return;
        
        currentGold += amount;
        Debug.Log($"골드 획득: +{amount} (총 {currentGold}G)");
        OnGoldChanged?.Invoke(currentGold);
    }
    
    public bool SpendGold(int amount)
    {
        if (amount <= 0) return false;
        
        if (currentGold >= amount)
        {
            currentGold -= amount;
            Debug.Log($"골드 사용: -{amount} (남은 골드: {currentGold}G)");
            OnGoldChanged?.Invoke(currentGold);
            return true;
        }
        else
        {
            Debug.Log("골드가 부족합니다!");
            return false;
        }
    }
    
    public int GetGold()
    {
        return currentGold;
    }
    
    // 치트 or 테스트용
    public void SetGold(int amount)
    {
        currentGold = Mathf.Max(0, amount);
        OnGoldChanged?.Invoke(currentGold);
    }
}