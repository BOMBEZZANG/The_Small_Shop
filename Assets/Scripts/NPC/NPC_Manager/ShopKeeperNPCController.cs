using UnityEngine;
using System.Collections;

public class ShopKeeperNPCController : ResidentNPCController
{
    [Header("Shop Keeper Specific")]
    [SerializeField] private ShopKeeperNPCData shopKeeperData;
    [SerializeField] private bool enableCustomerRecognition = true;
    [SerializeField] private float greetingCooldown = 30f; // Seconds between greetings to same player
    
    // State tracking
    private bool hasGreetedCurrentCustomer = false;
    private float lastGreetingTime = 0f;
    private PlayerController currentCustomer = null;
    private int customerTransactionCount = 0;
    
    // Shop integration
    private ShopController associatedShopController;
    private bool isOnShopDuty = false;
    
    // Properties
    public ShopKeeperNPCData ShopKeeperData => shopKeeperData;
    public bool IsWorkingHours => shopKeeperData?.IsWorkingHours() ?? true;
    public bool IsOnShopDuty => isOnShopDuty;
    public ShopData AssociatedShop => shopKeeperData?.AssociatedShop;
    
    protected override void Start()
    {
        base.Start();
        
        // Cast NPCData to ShopKeeperNPCData if not already assigned
        if (shopKeeperData == null && npcData is ShopKeeperNPCData)
        {
            shopKeeperData = npcData as ShopKeeperNPCData;
        }
        
        InitializeShopKeeperBehavior();
    }
    
    // ===== Abstract Method Implementations =====
    
    protected override void InitializeNPC()
    {
        // This is called by the base NPCController.Start()
        // Additional initialization is done in InitializeShopKeeperBehavior()
    }
    
    protected override void UpdateNPCBehavior()
    {
        // Update shop keeper specific behavior
        UpdateShopKeeperBehavior();
        
        // Handle work schedule
        UpdateWorkSchedule();
    }
    
    protected override void Update()
    {
        base.Update();
        
        // The UpdateNPCBehavior() method is called by base.Update()
        // so we don't need to call UpdateShopKeeperBehavior() here again
    }
    
    private void InitializeShopKeeperBehavior()
    {
        if (shopKeeperData == null)
        {
            Debug.LogError($"ShopKeeperNPCController on {gameObject.name} needs ShopKeeperNPCData!");
            return;
        }
        
        // Find associated shop controller
        if (shopKeeperData.AssociatedShop != null)
        {
            var shopControllers = FindObjectsOfType<ShopController>();
            foreach (var controller in shopControllers)
            {
                if (controller.ShopData == shopKeeperData.AssociatedShop)
                {
                    associatedShopController = controller;
                    break;
                }
            }
        }
        
        // Set work position if specified
        if (shopKeeperData.WorkPosition != null)
        {
            // Move towards work position using the movement system
            // This would be handled by the patrol/movement system in ResidentNPCController
        }
        
        // Initialize customer relationship tracking
        LoadCustomerData();
        
        Debug.Log($"ShopKeeper '{shopKeeperData.npcName}' initialized for shop '{shopKeeperData.AssociatedShop?.shopName}'");
    }
    
    private void UpdateShopKeeperBehavior()
    {
        if (shopKeeperData == null) return;
        
        // Update duty status based on work hours
        bool shouldBeOnDuty = IsWorkingHours && shopKeeperData.AssociatedShop != null;
        if (isOnShopDuty != shouldBeOnDuty)
        {
            isOnShopDuty = shouldBeOnDuty;
            OnDutyStatusChanged(isOnShopDuty);
        }
        
        // Handle always at shop behavior
        if (shopKeeperData.AlwaysAtShop && shopKeeperData.WorkPosition != null)
        {
            float distanceToWorkPosition = Vector2.Distance(transform.position, shopKeeperData.WorkPosition.position);
            if (distanceToWorkPosition > 1f && GetCurrentState() != NPCState.Talking)
            {
                // Move towards work position using base class movement system
                ChangeState(NPCState.Moving);
            }
        }
    }
    
    private void UpdateWorkSchedule()
    {
        if (shopKeeperData == null) return;
        
        bool isWorkTime = IsWorkingHours;
        
        // Handle going off duty
        if (!isWorkTime && GetCurrentState() != NPCState.Disabled)
        {
            if (currentCustomer != null)
            {
                // Politely end interaction when going off duty
                HandleOffDutyTransition();
            }
        }
    }
    
    // ===== Customer Interaction =====
    
    public void StartDialogue(PlayerController player)
    {
        if (shopKeeperData == null)
        {
            // Fall back to base behavior by calling protected StartDialogue
            base.StartDialogue();
            return;
        }
        
        currentCustomer = player;
        
        // Check if shop is open
        bool shopIsOpen = IsWorkingHours && (associatedShopController?.IsShopOpen ?? true);
        
        // Determine if customer is regular
        bool isRegularCustomer = enableCustomerRecognition && IsRegularCustomer(player);
        
        // Check if we need to greet
        bool needsGreeting = !hasGreetedCurrentCustomer || 
                           (Time.time - lastGreetingTime) > greetingCooldown;
        
        // Get appropriate dialogue
        DialogueData dialogueToUse = shopKeeperData.GetAppropriateDialogue(
            hasGreetedCurrentCustomer, 
            isRegularCustomer, 
            shopIsOpen
        );
        
        if (dialogueToUse != null)
        {
            // Use specific dialogue
            StartSpecificDialogue(dialogueToUse, player);
        }
        else
        {
            // Fall back to base behavior
            base.StartDialogue();
        }
        
        // Mark as greeted
        if (needsGreeting)
        {
            hasGreetedCurrentCustomer = true;
            lastGreetingTime = Time.time;
        }
    }
    
    private void StartSpecificDialogue(DialogueData dialogue, PlayerController player)
    {
        // Set the dialogue and start interaction
        if (DialogueManager.instance != null)
        {
            // Subscribe to dialogue end event for shop opening
            DialogueManager.OnDialogueEnded += OnDialogueCompleted;
            
            // Start the dialogue
            DialogueManager.instance.StartDialogue(dialogue, shopKeeperData, transform);
            ChangeState(NPCState.Talking);
            
            Debug.Log($"ShopKeeper dialogue started: {dialogue.name}");
        }
    }
    
    protected virtual void OnDialogueCompleted()
    {
        base.OnDialogueEnded();
        
        // Unsubscribe from dialogue events
        DialogueManager.OnDialogueEnded -= OnDialogueCompleted;
        
        // Check if we should open shop after dialogue
        if (currentCustomer != null && IsWorkingHours && associatedShopController != null)
        {
            bool shouldOpenShop = true;
            
            // Add any additional conditions here
            if (shopKeeperData.RequiresGreeting && !hasGreetedCurrentCustomer)
            {
                shouldOpenShop = false;
            }
            
            if (shouldOpenShop)
            {
                StartCoroutine(OpenShopAfterDialogue());
            }
        }
        
        // Reset customer reference
        currentCustomer = null;
    }
    
    private IEnumerator OpenShopAfterDialogue()
    {
        // Small delay to let dialogue UI close
        yield return new WaitForSeconds(0.5f);
        
        if (associatedShopController != null && currentCustomer != null)
        {
            associatedShopController.OpenShopDirectly();
            Debug.Log($"Shop opened by {shopKeeperData.npcName} after dialogue");
        }
    }
    
    // ===== Transaction Handling =====
    
    public void OnTransactionCompleted(bool successful, int amount)
    {
        if (currentCustomer == null) return;
        
        // Update customer transaction count
        if (successful)
        {
            customerTransactionCount++;
            SaveCustomerData();
        }
        
        // Get transaction dialogue
        DialogueData transactionDialogue = shopKeeperData.GetTransactionDialogue(successful);
        if (transactionDialogue != null)
        {
            StartSpecificDialogue(transactionDialogue, currentCustomer);
        }
        
        // Handle personality-based reactions
        HandleTransactionReaction(successful, amount);
    }
    
    private void HandleTransactionReaction(bool successful, int amount)
    {
        if (shopKeeperData == null) return;
        
        switch (shopKeeperData.Personality)
        {
            case ShopKeeperPersonality.Friendly:
                if (successful)
                    Debug.Log($"{shopKeeperData.npcName}: 감사합니다! 또 오세요!");
                break;
                
            case ShopKeeperPersonality.Greedy:
                if (successful && amount > 1000)
                    Debug.Log($"{shopKeeperData.npcName}: 하하! 좋은 거래였습니다!");
                break;
                
            case ShopKeeperPersonality.Professional:
                if (successful)
                    Debug.Log($"{shopKeeperData.npcName}: 거래가 완료되었습니다.");
                break;
                
            case ShopKeeperPersonality.Grumpy:
                if (!successful)
                    Debug.Log($"{shopKeeperData.npcName}: 돈도 없으면서...");
                break;
        }
    }
    
    // ===== Customer Recognition =====
    
    private bool IsRegularCustomer(PlayerController player)
    {
        if (!enableCustomerRecognition) return false;
        
        // Simple implementation - could be expanded with save/load system
        return customerTransactionCount >= shopKeeperData.LoyaltyDiscountThreshold;
    }
    
    public bool ShouldOfferLoyaltyDiscount(PlayerController player)
    {
        return shopKeeperData.ShouldOfferDiscount(customerTransactionCount);
    }
    
    // ===== Work Schedule Management =====
    
    private void OnDutyStatusChanged(bool onDuty)
    {
        if (onDuty)
        {
            Debug.Log($"{shopKeeperData.npcName} is now on shop duty");
            
            // Move to work position
            if (shopKeeperData.WorkPosition != null)
            {
                // Use base class movement system
                ChangeState(NPCState.Moving);
            }
        }
        else
        {
            Debug.Log($"{shopKeeperData.npcName} is now off duty");
            
            // Handle off-duty behavior
            if (!shopKeeperData.AlwaysAtShop)
            {
                // Resume normal patrol behavior from ResidentNPCController
                ChangeState(NPCState.Idle);
            }
        }
    }
    
    private void HandleOffDutyTransition()
    {
        if (currentCustomer != null)
        {
            // Show closing dialogue if available
            DialogueData closingDialogue = shopKeeperData.GetFarewellDialogue();
            if (closingDialogue != null)
            {
                StartSpecificDialogue(closingDialogue, currentCustomer);
            }
        }
    }
    
    // ===== Special Behaviors =====
    
    private void HandleSpecialBehaviors()
    {
        if (shopKeeperData == null) return;
        
        // Announce new items
        if (shopKeeperData.AnnouncesNewItems && associatedShopController != null)
        {
            // Could implement new item announcements
        }
        
        // Give shopping tips
        if (shopKeeperData.GivesShoppingTips && currentCustomer != null)
        {
            // Could implement tip system based on player's inventory
        }
        
        // Share personal stories
        if (shopKeeperData.OffersPersonalStories && Random.value < 0.1f)
        {
            // Could implement random story dialogue triggers
        }
    }
    
    // ===== Data Persistence =====
    
    private void LoadCustomerData()
    {
        // Simple implementation - could be expanded with save system
        string key = $"ShopKeeper_{shopKeeperData.npcName}_CustomerCount";
        customerTransactionCount = PlayerPrefs.GetInt(key, 0);
    }
    
    private void SaveCustomerData()
    {
        string key = $"ShopKeeper_{shopKeeperData.npcName}_CustomerCount";
        PlayerPrefs.SetInt(key, customerTransactionCount);
        PlayerPrefs.Save();
    }
    
    // ===== Public Interface =====
    
    public void SetShopKeeperData(ShopKeeperNPCData newData)
    {
        shopKeeperData = newData;
        InitializeShopKeeperBehavior();
    }
    
    public void ResetCustomerRelationship()
    {
        customerTransactionCount = 0;
        hasGreetedCurrentCustomer = false;
        SaveCustomerData();
    }
    
    public int GetCustomerTransactionCount()
    {
        return customerTransactionCount;
    }
    
    // ===== Debug Methods =====
    
    [ContextMenu("Debug - Print Shop Keeper Info")]
    public void DebugPrintShopKeeperInfo()
    {
        if (shopKeeperData != null)
        {
            Debug.Log(shopKeeperData.GetShopKeeperDebugInfo());
        }
        else
        {
            Debug.Log("No ShopKeeperNPCData assigned");
        }
    }
    
    [ContextMenu("Debug - Reset Customer Data")]
    public void DebugResetCustomerData()
    {
        ResetCustomerRelationship();
        Debug.Log("Customer relationship data reset");
    }
    
    [ContextMenu("Debug - Simulate Transaction")]
    public void DebugSimulateSuccessfulTransaction()
    {
        OnTransactionCompleted(true, 500);
    }
    
    // ===== Cleanup =====
    
    protected virtual void OnDestroy()
    {
        // Note: base.OnDestroy() doesn't exist in ResidentNPCController
        // but the base class OnDestroy in ResidentNPCController handles cleanup
        
        // Unsubscribe from events
        DialogueManager.OnDialogueEnded -= OnDialogueCompleted;
        
        // Save customer data
        SaveCustomerData();
    }
}