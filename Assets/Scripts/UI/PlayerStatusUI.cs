using UnityEngine;
using TMPro;
using System.Collections;

public class PlayerStatusUI : MonoBehaviour
{
    [Header("UI Text References")]
    [SerializeField] private TextMeshProUGUI levelExpText;  // 레벨과 경험치를 한 줄에 표시
    
    [Header("Display Settings")]
    [SerializeField] private string displayFormat = "[Lv.{0}] EXP: {1}/{2}";  // 표시 형식
    [SerializeField] private Color normalColor = Color.white;
    [SerializeField] private Color levelUpColor = Color.yellow;
    [SerializeField] private float levelUpEffectDuration = 2f;
    
    // 현재 값 저장 (애니메이션용)
    private int currentLevel;
    private int currentExp;
    private int requiredExp;
    
    void Start()
    {
        ValidateReferences();
    }
    
    // ===== 레벨 표시 업데이트 =====
    public void UpdateLevelDisplay(int level)
    {
        currentLevel = level;
        UpdateDisplay();
    }
    
    // ===== 경험치 표시 업데이트 =====
    public void UpdateExpDisplay(int current, int required)
    {
        currentExp = current;
        requiredExp = required;
        UpdateDisplay();
    }
    
    // ===== 통합 표시 업데이트 =====
    private void UpdateDisplay()
    {
        if (levelExpText == null) return;
        
        // [Lv.5] EXP: 250/500 형식으로 표시
        levelExpText.text = string.Format(displayFormat, currentLevel, currentExp, requiredExp);
    }
    
    // ===== 모든 표시 초기화 =====
    public void UpdateAllDisplays()
    {
        if (PlayerDataManager.instance != null)
        {
            UpdateLevelDisplay(PlayerDataManager.instance.GetLevel());
            UpdateExpDisplay(
                PlayerDataManager.instance.GetCurrentExp(), 
                PlayerDataManager.instance.GetRequiredExp()
            );
        }
    }
    
    // ===== 레벨업 효과 =====
    public void ShowLevelUpEffect()
    {
        if (levelExpText != null)
        {
            StartCoroutine(LevelUpEffectCoroutine());
        }
    }
    
    private IEnumerator LevelUpEffectCoroutine()
    {
        // 색상 변경
        levelExpText.color = levelUpColor;
        
        // 크기 애니메이션
        Vector3 originalScale = levelExpText.transform.localScale;
        float elapsed = 0f;
        
        // 확대했다가 축소
        while (elapsed < 0.3f)
        {
            elapsed += Time.deltaTime;
            float scale = 1f + Mathf.Sin((elapsed / 0.3f) * Mathf.PI) * 0.2f;
            levelExpText.transform.localScale = originalScale * scale;
            yield return null;
        }
        
        levelExpText.transform.localScale = originalScale;
        
        // 색상 서서히 원래대로
        elapsed = 0f;
        while (elapsed < levelUpEffectDuration)
        {
            elapsed += Time.deltaTime;
            levelExpText.color = Color.Lerp(levelUpColor, normalColor, elapsed / levelUpEffectDuration);
            yield return null;
        }
        
        levelExpText.color = normalColor;
    }
    
    // ===== 유틸리티 =====
    private void ValidateReferences()
    {
        if (levelExpText == null)
        {
            Debug.LogWarning("PlayerStatusUI: Level/Exp Text가 연결되지 않았습니다!");
        }
    }
    
    // 표시 형식 변경 (런타임에 변경 가능)
    public void SetDisplayFormat(string format)
    {
        displayFormat = format;
        UpdateDisplay();
    }
    
    // 경험치 퍼센트 표시 옵션 (추가 기능)
    public void ShowWithPercentage(bool showPercentage)
    {
        if (showPercentage && requiredExp > 0)
        {
            float percentage = (currentExp / (float)requiredExp) * 100f;
            displayFormat = "[Lv.{0}] EXP: {1}/{2} ({3:F1}%)";
            levelExpText.text = string.Format(displayFormat, 
                currentLevel, currentExp, requiredExp, percentage);
        }
        else
        {
            displayFormat = "[Lv.{0}] EXP: {1}/{2}";
            UpdateDisplay();
        }
    }
}