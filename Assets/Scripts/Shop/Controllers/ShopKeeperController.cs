using UnityEngine;

[RequireComponent(typeof(ShopController))]
public class ShopKeeperController : MonoBehaviour
{
    [Header("Shop Keeper Configuration")]
    [SerializeField] private bool enableDialogue = true;
    [SerializeField] private bool requireDialogueBeforeShop = true; // Must talk first before shopping
    
    [Header("Dialogue Settings")]
    [SerializeField] private DialogueData greetingDialogue; // First meeting dialogue
    [SerializeField] private DialogueData shopOpenDialogue; // Regular shop opening dialogue
    [SerializeField] private DialogueData shopClosedDialogue; // When shop is closed
    [SerializeField] private DialogueData farewellDialogue; // After shopping dialogue
    
    [Header("NPC Behavior")]
    [SerializeField] private bool hasGreeted = false; // Has met player before
    [SerializeField] private float dialogueCooldown = 2f; // Seconds between dialogues
    [SerializeField] private bool allowRepeatedGreetings = false;
    
    // Components
    private ShopController shopController;
    private NPCController npcController;
    private DialogueManager dialogueManager;
    
    // State
    private bool isInDialogue = false;
    private bool hasDialoguedThisSession = false;
    private float lastDialogueTime = 0f;
    private PlayerController currentPlayer = null;
    
    // Events
    public System.Action<PlayerController> OnDialogueStarted;
    public System.Action<PlayerController> OnDialogueEnded;
    public System.Action<PlayerController> OnShopOpenRequested;
    
    void Awake()
    {
        // Get required components
        shopController = GetComponent<ShopController>();
        npcController = GetComponent<NPCController>();
        
        // Find DialogueManager
        dialogueManager = FindObjectOfType<DialogueManager>();
        
        if (shopController == null)
        {
            Debug.LogError($"ShopKeeperController on {gameObject.name} requires a ShopController component!");
        }
        
        if (enableDialogue && dialogueManager == null)
        {
            Debug.LogWarning($"ShopKeeperController on {gameObject.name} has dialogue enabled but no DialogueManager found in scene!");
        }
    }
    
    void Start()
    {
        // Subscribe to events
        SubscribeToEvents();
        
        Debug.Log($"ShopKeeper '{GetShopKeeperName()}' initialized");
    }
    
    // ===== Shop Interaction Entry Point =====
    
    public void TryStartShopInteraction(PlayerController player)
    {
        if (player == null) return;
        
        currentPlayer = player;
        
        // Check cooldown
        if (Time.time - lastDialogueTime < dialogueCooldown)
        {
            Debug.Log("Shop keeper is busy (cooldown active)");
            return;
        }
        
        // If dialogue is disabled or not required, open shop directly
        if (!enableDialogue || !requireDialogueBeforeShop)
        {
            OpenShopDirectly();
            return;
        }
        
        // If already dialogued this session and shop is open, skip to shop
        if (hasDialoguedThisSession && shopController.IsShopOpen && !allowRepeatedGreetings)
        {
            OpenShopDirectly();
            return;
        }
        
        // Start appropriate dialogue
        StartDialogueSequence(player);
    }
    
    // ===== Dialogue Management =====
    
    private void StartDialogueSequence(PlayerController player)
    {
        if (isInDialogue || dialogueManager == null) return;
        
        DialogueData dialogueToUse = GetAppropriateDialogue();
        
        if (dialogueToUse == null)
        {
            Debug.LogWarning($"No dialogue data found for shop keeper '{GetShopKeeperName()}'");
            OpenShopDirectly();
            return;
        }
        
        // Start dialogue
        isInDialogue = true;
        lastDialogueTime = Time.time;
        hasDialoguedThisSession = true;
        
        // Mark as greeted if using greeting dialogue
        if (dialogueToUse == greetingDialogue)
        {
            hasGreeted = true;
        }
        
        OnDialogueStarted?.Invoke(player);
        
        // Start dialogue through DialogueManager using the proper format
        if (npcController != null && npcController.GetNPCData() != null)
        {
            dialogueManager.StartDialogue(dialogueToUse, npcController.GetNPCData(), transform);
            
            // Subscribe to dialogue end event since we can't use callback
            DialogueManager.OnDialogueEnded += OnDialogueComplete;
        }
        else
        {
            Debug.LogWarning("Cannot start dialogue: Missing NPCController or NPCData");
            OpenShopDirectly();
            return;
        }
        
        Debug.Log($"Started dialogue with shop keeper '{GetShopKeeperName()}'");
    }
    
    private DialogueData GetAppropriateDialogue()
    {
        // Shop closed dialogue
        if (!shopController.IsShopOpen && shopClosedDialogue != null)
        {
            return shopClosedDialogue;
        }
        
        // First meeting dialogue
        if (!hasGreeted && greetingDialogue != null)
        {
            return greetingDialogue;
        }
        
        // Regular shop dialogue
        if (shopOpenDialogue != null)
        {
            return shopOpenDialogue;
        }
        
        // Fallback to greeting if available
        return greetingDialogue;
    }
    
    private void OnDialogueComplete()
    {
        // Unsubscribe from dialogue end event
        DialogueManager.OnDialogueEnded -= OnDialogueComplete;
        
        isInDialogue = false;
        
        OnDialogueEnded?.Invoke(currentPlayer);
        
        // If shop is open, offer to open shop
        if (shopController.IsShopOpen)
        {
            // Add small delay before opening shop
            Invoke(nameof(OpenShopAfterDialogue), 0.5f);
        }
        else
        {
            Debug.Log($"Shop '{shopController.ShopName}' is closed, cannot open after dialogue");
        }
    }
    
    private void OpenShopAfterDialogue()
    {
        if (currentPlayer != null && shopController.PlayerInRange)
        {
            OnShopOpenRequested?.Invoke(currentPlayer);
            OpenShopDirectly();
        }
    }
    
    private void OpenShopDirectly()
    {
        if (shopController != null)
        {
            shopController.OpenShopDirectly();
        }
    }
    
    // ===== NPC Integration =====
    // Note: Integration with NPCController is handled through ShopController events
    
    // ===== Event Management =====
    
    private void SubscribeToEvents()
    {
        // Subscribe to shop controller events
        if (shopController != null)
        {
            shopController.OnPlayerEnterRange += OnPlayerEnterShopRange;
            shopController.OnPlayerExitRange += OnPlayerExitShopRange;
        }
        
        // Subscribe to shop manager events for farewell dialogue
        ShopManager.OnShopClosed += OnShopClosed;
    }
    
    private void UnsubscribeFromEvents()
    {
        // Unsubscribe from shop controller events
        if (shopController != null)
        {
            shopController.OnPlayerEnterRange -= OnPlayerEnterShopRange;
            shopController.OnPlayerExitRange -= OnPlayerExitShopRange;
        }
        
        // Unsubscribe from shop manager events
        ShopManager.OnShopClosed -= OnShopClosed;
    }
    
    private void OnPlayerEnterShopRange(ShopController shop)
    {
        // Reset session dialogue flag when player approaches
        // (Optional: you might want to keep this persistent)
        // hasDialoguedThisSession = false;
    }
    
    private void OnPlayerExitShopRange(ShopController shop)
    {
        // Clean up current player reference
        currentPlayer = null;
        
        // Cancel any ongoing dialogue
        if (isInDialogue && dialogueManager != null)
        {
            dialogueManager.EndDialogue();
            isInDialogue = false;
        }
    }
    
    private void OnShopClosed(ShopData closedShop)
    {
        // Show farewell dialogue if this is our shop and we have farewell dialogue
        if (shopController.ShopData == closedShop && 
            farewellDialogue != null && 
            currentPlayer != null && 
            shopController.PlayerInRange &&
            enableDialogue)
        {
            // Small delay before farewell
            Invoke(nameof(ShowFarewellDialogue), 1f);
        }
    }
    
    private void ShowFarewellDialogue()
    {
        if (currentPlayer != null && farewellDialogue != null && !isInDialogue)
        {
            isInDialogue = true;
            
            if (npcController != null && npcController.GetNPCData() != null)
            {
                dialogueManager?.StartDialogue(farewellDialogue, npcController.GetNPCData(), transform);
                
                // Subscribe to dialogue end to reset state
                DialogueManager.OnDialogueEnded += OnFarewellDialogueComplete;
            }
            else
            {
                isInDialogue = false;
            }
        }
    }
    
    private void OnFarewellDialogueComplete()
    {
        DialogueManager.OnDialogueEnded -= OnFarewellDialogueComplete;
        isInDialogue = false;
    }
    
    // ===== Public Interface =====
    
    public void SetDialogueData(DialogueData greeting, DialogueData shopOpen, DialogueData shopClosed = null, DialogueData farewell = null)
    {
        greetingDialogue = greeting;
        shopOpenDialogue = shopOpen;
        shopClosedDialogue = shopClosed;
        farewellDialogue = farewell;
    }
    
    public void ResetGreeting()
    {
        hasGreeted = false;
        hasDialoguedThisSession = false;
    }
    
    public string GetShopKeeperName()
    {
        if (shopController?.ShopData != null)
        {
            return shopController.ShopData.shopKeeperName;
        }
        return gameObject.name;
    }
    
    public bool IsInDialogue => isInDialogue;
    public bool HasGreeted => hasGreeted;
    
    // ===== Configuration =====
    
    public void EnableDialogue(bool enable)
    {
        enableDialogue = enable;
    }
    
    public void SetRequireDialogue(bool require)
    {
        requireDialogueBeforeShop = require;
    }
    
    public void SetDialogueCooldown(float cooldown)
    {
        dialogueCooldown = cooldown;
    }
    
    // ===== Debug Methods =====
    
    [ContextMenu("Debug - Force Start Greeting")]
    public void DebugForceGreeting()
    {
        if (greetingDialogue != null)
        {
            hasGreeted = false;
            var player = FindObjectOfType<PlayerController>();
            if (player != null)
            {
                StartDialogueSequence(player);
            }
        }
    }
    
    [ContextMenu("Debug - Reset Greeting State")]
    public void DebugResetGreeting()
    {
        ResetGreeting();
        Debug.Log("Greeting state reset");
    }
    
    [ContextMenu("Debug - Print Shop Keeper Info")]
    public void DebugPrintShopKeeperInfo()
    {
        Debug.Log($"=== Shop Keeper Info ===\n" +
                 $"Name: {GetShopKeeperName()}\n" +
                 $"Has Greeted: {hasGreeted}\n" +
                 $"Dialogue Enabled: {enableDialogue}\n" +
                 $"Require Dialogue: {requireDialogueBeforeShop}\n" +
                 $"In Dialogue: {isInDialogue}\n" +
                 $"Cooldown: {dialogueCooldown}s");
    }
    
    // ===== Gizmos =====
    
    void OnDrawGizmosSelected()
    {
        // Draw dialogue indicator
        if (enableDialogue)
        {
            Gizmos.color = isInDialogue ? Color.yellow : Color.blue;
            Gizmos.DrawWireSphere(transform.position + Vector3.up * 1.5f, 0.3f);
        }
        
        // Draw shop keeper info
        #if UNITY_EDITOR
        if (shopController != null)
        {
            string info = $"Shop Keeper\n{GetShopKeeperName()}\n" +
                         $"{(hasGreeted ? "Met" : "New")}\n" +
                         $"{(enableDialogue ? "Dialogue ON" : "Dialogue OFF")}";
                         
            UnityEditor.Handles.Label(
                transform.position + Vector3.up * 3f,
                info
            );
        }
        #endif
    }
    
    // ===== Cleanup =====
    
    void OnDestroy()
    {
        UnsubscribeFromEvents();
        
        // Make sure to unsubscribe from dialogue events
        DialogueManager.OnDialogueEnded -= OnDialogueComplete;
        DialogueManager.OnDialogueEnded -= OnFarewellDialogueComplete;
    }
}