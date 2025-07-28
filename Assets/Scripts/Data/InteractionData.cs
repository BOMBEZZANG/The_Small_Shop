using UnityEngine;

[CreateAssetMenu(fileName = "New InteractionData", menuName = "Game Data/Interaction Data")]
public class InteractionData : ScriptableObject
{
    [Header("Basic Info")]
    [SerializeField] private string actionName = "상호작용";        // "나무 베기", "대화하기" 등
    [SerializeField] private Sprite iconSprite;                    // 상호작용 아이콘 (도끼, 말풍선 등)
    [SerializeField] private string actionKey = "E";                // 상호작용 키
    
    [Header("UI Texts")]
    [SerializeField] private string promptFormat = "[{0}] {1}";     // "[E] 나무 베기" 형식
    [SerializeField] private string progressFormat = "{0} 중... {1:0}%";  // "베는 중... 50%"
    [SerializeField] private string cooldownFormat = "{0:0}초 후 가능";   // "30초 후 가능"
    
    [Header("Requirement Messages")]
    [SerializeField] private string levelRequiredFormat = "레벨 {0} 필요";
    [SerializeField] private string toolRequiredFormat = "{0} 필요";
    [SerializeField] private string staminaRequiredFormat = "스태미나 {0} 필요";
    
    [Header("Visual Settings")]
    [SerializeField] private Color normalColor = Color.white;
    [SerializeField] private Color disabledColor = new Color(1f, 1f, 1f, 0.5f);
    [SerializeField] private Color warningColor = Color.red;
    
    [Header("Display Settings")]
    [SerializeField] private float iconOnlyDistance = 3f;       // 아이콘만 표시하는 거리
    [SerializeField] private float fullTextDistance = 1.5f;     // 전체 텍스트 표시하는 거리
    [SerializeField] private bool alwaysShowIcon = false;       // 항상 아이콘 표시 여부
    
    // ===== Getter 메서드 =====
    public string GetPromptText()
    {
        return string.Format(promptFormat, actionKey, actionName);
    }
    
    public string GetProgressText(float progress)
    {
        return string.Format(progressFormat, actionName, progress * 100f);
    }
    
    public string GetCooldownText(float secondsRemaining)
    {
        return string.Format(cooldownFormat, secondsRemaining);
    }
    
    public string GetLevelRequiredText(int level)
    {
        return string.Format(levelRequiredFormat, level);
    }
    
    public string GetToolRequiredText(string toolName)
    {
        return string.Format(toolRequiredFormat, toolName);
    }
    
    public string GetStaminaRequiredText(int stamina)
    {
        return string.Format(staminaRequiredFormat, stamina);
    }
    
    // Properties
    public Sprite IconSprite => iconSprite;
    public string ActionKey => actionKey;
    public Color NormalColor => normalColor;
    public Color DisabledColor => disabledColor;
    public Color WarningColor => warningColor;
    public float IconOnlyDistance => iconOnlyDistance;
    public float FullTextDistance => fullTextDistance;
    public bool AlwaysShowIcon => alwaysShowIcon;
}