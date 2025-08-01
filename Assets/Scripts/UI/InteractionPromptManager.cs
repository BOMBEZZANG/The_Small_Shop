using UnityEngine;
using TMPro;
using UnityEngine.UI;
using System.Collections;

public class InteractionPromptManager : MonoBehaviour
{
    public static InteractionPromptManager instance;
    
    [Header("UI References")]
    [SerializeField] private GameObject interactionPrompt;
    [SerializeField] private TextMeshProUGUI interactionPromptText;
    [SerializeField] private TextMeshProUGUI keyText;
    [SerializeField] private Image keyBackground;
    [SerializeField] private Image statusIcon;
    
    [Header("Animation Settings")]
    [SerializeField] private bool enableAnimations = true;
    [SerializeField] private float fadeInDuration = 0.3f;
    [SerializeField] private float fadeOutDuration = 0.2f;
    [SerializeField] private bool enablePulseEffect = true;
    [SerializeField] private float pulseInterval = 2f;
    
    [Header("Colors")]
    [SerializeField] private Color availableKeyColor = new Color(0.3f, 0.69f, 0.31f); // Green #4CAF50
    [SerializeField] private Color unavailableKeyColor = new Color(1f, 0.6f, 0f); // Orange #FF9800
    [SerializeField] private Color closedKeyColor = new Color(0.96f, 0.26f, 0.21f); // Red #F44336
    [SerializeField] private Color dialogueKeyColor = new Color(0.13f, 0.59f, 0.95f); // Blue #2196F3
    
    [Header("Status Icons")]
    [SerializeField] private Sprite shopOpenIcon;
    [SerializeField] private Sprite shopClosedIcon;
    [SerializeField] private Sprite shopUnavailableIcon;
    [SerializeField] private Sprite dialogueIcon;
    
    // State
    private bool isShowing = false;
    private Coroutine currentAnimation;
    private Coroutine pulseCoroutine;
    private CanvasGroup promptCanvasGroup;
    
    // Properties
    public bool IsShowing => isShowing;
    
    void Awake()
    {
        // Scene-specific singleton
        if (instance == null)
        {
            instance = this;
        }
        else if (instance != this)
        {
            Debug.LogWarning("Multiple InteractionPromptManagers found in scene. Destroying duplicate.");
            Destroy(gameObject);
            return;
        }
        
        // Get or add CanvasGroup for animations
        if (interactionPrompt != null)
        {
            promptCanvasGroup = interactionPrompt.GetComponent<CanvasGroup>();
            if (promptCanvasGroup == null)
            {
                promptCanvasGroup = interactionPrompt.AddComponent<CanvasGroup>();
            }
        }
        
        // Initially hide
        HideInteractionPrompt();
    }
    
    void Start()
    {
        // Validate references
        ValidateReferences();
        
        Debug.Log("InteractionPromptManager initialized for scene");
    }
    
    private void ValidateReferences()
    {
        if (interactionPrompt == null)
            Debug.LogWarning("InteractionPromptManager: interactionPrompt is not assigned!");
        if (interactionPromptText == null)
            Debug.LogWarning("InteractionPromptManager: interactionPromptText is not assigned!");
    }
    
    // ===== Public Interface =====
    
    public void ShowInteractionPrompt(string promptText)
    {
        ShowInteractionPrompt(promptText, PromptType.Available, true);
    }
    
    public void ShowInteractionPrompt(string promptText, PromptType type, bool showKey = true)
    {
        if (interactionPrompt == null) return;
        
        // Set text
        if (interactionPromptText != null)
        {
            interactionPromptText.text = promptText;
        }
        
        // Configure appearance based on type
        ConfigurePromptAppearance(type, showKey);
        
        // Show prompt
        if (!isShowing)
        {
            isShowing = true;
            
            if (enableAnimations)
            {
                ShowWithAnimation();
            }
            else
            {
                ShowImmediate();
            }
        }
        else
        {
            // Just update the text if already showing
            UpdatePromptContent(promptText, type, showKey);
        }
    }
    
    public void HideInteractionPrompt()
    {
        if (!isShowing) return;
        
        isShowing = false;
        
        // Stop pulse animation
        if (pulseCoroutine != null)
        {
            StopCoroutine(pulseCoroutine);
            pulseCoroutine = null;
        }
        
        if (enableAnimations)
        {
            HideWithAnimation();
        }
        else
        {
            HideImmediate();
        }
    }
    
    // ===== Specialized Show Methods =====
    
    public void ShowShopPrompt(string shopName, bool isOpen, bool hasShopkeeper = true)
    {
        PromptType type;
        string promptText;
        
        if (!hasShopkeeper)
        {
            type = PromptType.Unavailable;
            promptText = $"{shopName} (점원 부재)";
        }
        else if (isOpen)
        {
            type = PromptType.Available;
            promptText = $"[E] {shopName}에서 거래하기";
        }
        else
        {
            type = PromptType.Closed;
            promptText = $"{shopName} (영업 종료)";
        }
        
        ShowInteractionPrompt(promptText, type, isOpen && hasShopkeeper);
    }
    
    public void ShowDialoguePrompt(string npcName)
    {
        string promptText = $"[E] {npcName}와 대화하기";
        ShowInteractionPrompt(promptText, PromptType.Dialogue, true);
    }
    
    public void ShowCustomPrompt(string promptText, Color keyColor, Sprite icon = null)
    {
        if (interactionPromptText != null)
            interactionPromptText.text = promptText;
            
        if (keyBackground != null)
            keyBackground.color = keyColor;
            
        if (statusIcon != null && icon != null)
        {
            statusIcon.sprite = icon;
            statusIcon.gameObject.SetActive(true);
        }
        
        if (enableAnimations)
            ShowWithAnimation();
        else
            ShowImmediate();
    }
    
    // ===== Configuration =====
    
    private void ConfigurePromptAppearance(PromptType type, bool showKey)
    {
        // Configure key appearance
        if (keyBackground != null)
        {
            Color keyColor = type switch
            {
                PromptType.Available => availableKeyColor,
                PromptType.Closed => unavailableKeyColor,
                PromptType.Unavailable => closedKeyColor,
                PromptType.Dialogue => dialogueKeyColor,
                _ => availableKeyColor
            };
            
            keyBackground.color = keyColor;
            keyBackground.gameObject.SetActive(showKey);
        }
        
        // Configure key text
        if (keyText != null)
        {
            keyText.gameObject.SetActive(showKey);
        }
        
        // Configure status icon
        if (statusIcon != null)
        {
            Sprite iconToUse = type switch
            {
                PromptType.Available => shopOpenIcon,
                PromptType.Closed => shopClosedIcon,
                PromptType.Unavailable => shopUnavailableIcon,
                PromptType.Dialogue => dialogueIcon,
                _ => null
            };
            
            if (iconToUse != null)
            {
                statusIcon.sprite = iconToUse;
                statusIcon.gameObject.SetActive(true);
            }
            else
            {
                statusIcon.gameObject.SetActive(false);
            }
        }
    }
    
    private void UpdatePromptContent(string promptText, PromptType type, bool showKey)
    {
        if (interactionPromptText != null)
            interactionPromptText.text = promptText;
            
        ConfigurePromptAppearance(type, showKey);
    }
    
    // ===== Animation Methods =====
    
    private void ShowWithAnimation()
    {
        if (currentAnimation != null)
            StopCoroutine(currentAnimation);
            
        currentAnimation = StartCoroutine(ShowAnimationCoroutine());
    }
    
    private void HideWithAnimation()
    {
        if (currentAnimation != null)
            StopCoroutine(currentAnimation);
            
        currentAnimation = StartCoroutine(HideAnimationCoroutine());
    }
    
    private void ShowImmediate()
    {
        interactionPrompt.SetActive(true);
        
        if (promptCanvasGroup != null)
            promptCanvasGroup.alpha = 1f;
        
        interactionPrompt.transform.localScale = Vector3.one;
        
        StartPulseEffect();
    }
    
    private void HideImmediate()
    {
        interactionPrompt.SetActive(false);
    }
    
    private IEnumerator ShowAnimationCoroutine()
    {
        interactionPrompt.SetActive(true);
        
        // Start from invisible and small
        if (promptCanvasGroup != null)
            promptCanvasGroup.alpha = 0f;
        interactionPrompt.transform.localScale = Vector3.one * 0.8f;
        
        float elapsed = 0f;
        
        // Animate in
        while (elapsed < fadeInDuration)
        {
            elapsed += Time.deltaTime;
            float progress = elapsed / fadeInDuration;
            
            // Ease out animation
            float easedProgress = 1f - Mathf.Pow(1f - progress, 3f);
            
            if (promptCanvasGroup != null)
                promptCanvasGroup.alpha = easedProgress;
                
            // Slight bounce effect
            float scaleProgress = Mathf.Min(1f, progress * 1.2f);
            interactionPrompt.transform.localScale = Vector3.one * Mathf.Lerp(0.8f, 1.05f, scaleProgress);
            
            yield return null;
        }
        
        // Settle to final scale
        elapsed = 0f;
        while (elapsed < 0.1f)
        {
            elapsed += Time.deltaTime;
            float progress = elapsed / 0.1f;
            interactionPrompt.transform.localScale = Vector3.one * Mathf.Lerp(1.05f, 1f, progress);
            yield return null;
        }
        
        // Final state
        if (promptCanvasGroup != null)
            promptCanvasGroup.alpha = 1f;
        interactionPrompt.transform.localScale = Vector3.one;
        
        currentAnimation = null;
        StartPulseEffect();
    }
    
    private IEnumerator HideAnimationCoroutine()
    {
        float elapsed = 0f;
        
        while (elapsed < fadeOutDuration)
        {
            elapsed += Time.deltaTime;
            float progress = elapsed / fadeOutDuration;
            
            if (promptCanvasGroup != null)
                promptCanvasGroup.alpha = 1f - progress;
                
            interactionPrompt.transform.localScale = Vector3.one * (1f - progress * 0.1f);
            
            yield return null;
        }
        
        interactionPrompt.SetActive(false);
        currentAnimation = null;
    }
    
    // ===== Pulse Effect =====
    
    private void StartPulseEffect()
    {
        if (!enablePulseEffect) return;
        
        if (pulseCoroutine != null)
            StopCoroutine(pulseCoroutine);
            
        pulseCoroutine = StartCoroutine(PulseEffectCoroutine());
    }
    
    private IEnumerator PulseEffectCoroutine()
    {
        while (isShowing)
        {
            yield return new WaitForSeconds(pulseInterval);
            
            if (!isShowing) break;
            
            // Gentle pulse animation
            float elapsed = 0f;
            float pulseDuration = 0.8f;
            
            while (elapsed < pulseDuration && isShowing)
            {
                elapsed += Time.deltaTime;
                float progress = elapsed / pulseDuration;
                
                // Sine wave for smooth pulse
                float pulseAmount = Mathf.Sin(progress * Mathf.PI * 2f) * 0.05f;
                interactionPrompt.transform.localScale = Vector3.one * (1f + pulseAmount);
                
                // Optional: Pulse key background alpha
                if (keyBackground != null)
                {
                    Color keyColor = keyBackground.color;
                    keyColor.a = 1f - (Mathf.Sin(progress * Mathf.PI * 4f) * 0.15f);
                    keyBackground.color = keyColor;
                }
                
                yield return null;
            }
            
            // Reset to normal state
            if (isShowing)
            {
                interactionPrompt.transform.localScale = Vector3.one;
                if (keyBackground != null)
                {
                    Color keyColor = keyBackground.color;
                    keyColor.a = 1f;
                    keyBackground.color = keyColor;
                }
            }
        }
    }
    
    // ===== Settings =====
    
    public void SetAnimationsEnabled(bool enabled)
    {
        enableAnimations = enabled;
    }
    
    public void SetPulseEnabled(bool enabled)
    {
        enablePulseEffect = enabled;
        
        if (!enabled && pulseCoroutine != null)
        {
            StopCoroutine(pulseCoroutine);
            pulseCoroutine = null;
            interactionPrompt.transform.localScale = Vector3.one;
        }
    }
    
    // ===== Debug Methods =====
    
    [ContextMenu("Test - Show Available Prompt")]
    public void DebugShowAvailablePrompt()
    {
        ShowInteractionPrompt("[E] 테스트 상점에서 거래하기", PromptType.Available);
    }
    
    [ContextMenu("Test - Show Closed Prompt")]
    public void DebugShowClosedPrompt()
    {
        ShowInteractionPrompt("테스트 상점 (영업 종료)", PromptType.Closed, false);
    }
    
    [ContextMenu("Test - Show Dialogue Prompt")]
    public void DebugShowDialoguePrompt()
    {
        ShowDialoguePrompt("테스트 NPC");
    }
    
    [ContextMenu("Test - Hide Prompt")]
    public void DebugHidePrompt()
    {
        HideInteractionPrompt();
    }
    
    // ===== Cleanup =====
    
    void OnDestroy()
    {
        if (instance == this)
        {
            instance = null;
        }
        
        // Stop all coroutines
        if (currentAnimation != null)
            StopCoroutine(currentAnimation);
        if (pulseCoroutine != null)
            StopCoroutine(pulseCoroutine);
    }
}

// Prompt type enum (renamed to avoid conflict with existing InteractionType)
public enum PromptType
{
    Available,      // Green - can interact
    Closed,         // Orange - shop closed
    Unavailable,    // Red - can't interact (no shopkeeper, etc.)
    Dialogue        // Blue - dialogue interaction
}