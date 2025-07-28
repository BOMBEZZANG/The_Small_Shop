using UnityEngine;
using TMPro;

public class GameStatusUI : MonoBehaviour
{
    [Header("UI Text References")]
    [SerializeField] private TextMeshProUGUI goldText;
    [SerializeField] private TextMeshProUGUI timeText;
    [SerializeField] private TextMeshProUGUI staminaText;
    
    [Header("Display Settings")]
    [SerializeField] private bool useThousandSeparator = true;  // 1,000 형식 사용
    [SerializeField] private bool use24HourFormat = true;       // 24시간 형식 사용
    
    [Header("Text Formats")]
    [SerializeField] private string goldFormat = "{0} G";      // 골드 표시 형식
    [SerializeField] private string staminaFormat = "{0}/{1}"; // 현재/최대 스태미나
    
    void Start()
    {
        // null 체크 및 경고
        ValidateReferences();
    }
    
    // ===== 골드 표시 업데이트 =====
    public void UpdateGoldDisplay(int goldAmount)
    {
        if (goldText == null) return;
        
        string formattedGold = useThousandSeparator ? 
            goldAmount.ToString("N0") : goldAmount.ToString();
            
        goldText.text = string.Format(goldFormat, formattedGold);
    }
    
    // ===== 시간 표시 업데이트 =====
    public void UpdateTimeDisplay(int hour, int minute)
    {
        if (timeText == null) return;
        
        if (use24HourFormat)
        {
            // 24시간 형식 (14:30)
            timeText.text = $"{hour:D2}:{minute:D2}";
        }
        else
        {
            // 12시간 형식 (2:30 PM)
            string period = hour >= 12 ? "PM" : "AM";
            int displayHour = hour % 12;
            if (displayHour == 0) displayHour = 12;
            
            timeText.text = $"{displayHour}:{minute:D2} {period}";
        }
    }
    
    // ===== 스태미나 표시 업데이트 =====
    public void UpdateStaminaDisplay(int currentStamina, int maxStamina)
    {
        if (staminaText == null) return;
        
        staminaText.text = string.Format(staminaFormat, currentStamina, maxStamina);
        
        // 스태미나가 낮을 때 색상 변경 (선택적)
        UpdateStaminaColor(currentStamina, maxStamina);
    }
    
    // ===== 추가 기능 =====
    
    // 스태미나 색상 변경 (낮을 때 빨간색)
    private void UpdateStaminaColor(int current, int max)
    {
        if (staminaText == null) return;
        
        float percentage = (float)current / max;
        
        if (percentage <= 0.2f)
        {
            staminaText.color = Color.red;
        }
        else if (percentage <= 0.5f)
        {
            staminaText.color = Color.yellow;
        }
        else
        {
            staminaText.color = Color.white;
        }
    }
    
    // 모든 텍스트 한 번에 업데이트 (초기화용)
    public void UpdateAllDisplays()
    {
        if (GoldManager.instance != null)
            UpdateGoldDisplay(GoldManager.instance.GetGold());
            
        if (TimeManager.instance != null)
            UpdateTimeDisplay(TimeManager.instance.GetHour(), TimeManager.instance.GetMinute());
            
        if (StaminaManager.instance != null)
            UpdateStaminaDisplay(StaminaManager.instance.GetStamina(), StaminaManager.instance.GetMaxStamina());
    }
    
    // 참조 확인
    private void ValidateReferences()
    {
        if (goldText == null)
            Debug.LogWarning("GameStatusUI: Gold Text가 연결되지 않았습니다!");
            
        if (timeText == null)
            Debug.LogWarning("GameStatusUI: Time Text가 연결되지 않았습니다!");
            
        if (staminaText == null)
            Debug.LogWarning("GameStatusUI: Stamina Text가 연결되지 않았습니다!");
    }
    
    // 특정 텍스트 깜빡임 효과 (알림용)
    public void FlashText(StatusType statusType, float duration = 0.5f)
    {
        TextMeshProUGUI targetText = null;
        
        switch (statusType)
        {
            case StatusType.Gold:
                targetText = goldText;
                break;
            case StatusType.Time:
                targetText = timeText;
                break;
            case StatusType.Stamina:
                targetText = staminaText;
                break;
        }
        
        if (targetText != null)
        {
            StartCoroutine(FlashCoroutine(targetText, duration));
        }
    }
    
    private System.Collections.IEnumerator FlashCoroutine(TextMeshProUGUI text, float duration)
    {
        Color originalColor = text.color;
        float elapsed = 0f;
        
        while (elapsed < duration)
        {
            elapsed += Time.deltaTime;
            float alpha = Mathf.PingPong(elapsed * 4f, 1f);
            text.color = new Color(originalColor.r, originalColor.g, originalColor.b, alpha);
            yield return null;
        }
        
        text.color = originalColor;
    }
}

public enum StatusType
{
    Gold,
    Time,
    Stamina
}