using UnityEngine;

[CreateAssetMenu(fileName = "ShopKeeper_NPC", menuName = "Game Data/NPC/Shop Keeper NPC")]
public class ShopKeeperNPCData : NPCData
{
    [Header("Shop Keeper Specific")]
    [SerializeField] private ShopData associatedShop;
    [SerializeField] private Transform workPosition; // Where NPC stands while working
    [SerializeField] private bool requiresGreeting = true;
    [SerializeField] private bool alwaysAtShop = true; // Never leaves shop area
    
    [Header("Work Schedule")]
    [SerializeField] private bool followsShopHours = true;
    [SerializeField] private Vector2 customWorkHours = new Vector2(9, 18); // If different from shop hours
    
    [Header("Shop Keeper Dialogues")]
    [SerializeField] private DialogueData greetingCustomerDialogue;
    [SerializeField] private DialogueData regularCustomerDialogue;
    [SerializeField] private DialogueData shopClosedDialogue;
    [SerializeField] private DialogueData farewellDialogue;
    [SerializeField] private DialogueData noMoneyDialogue; // When player can't afford
    [SerializeField] private DialogueData thankYouDialogue; // After successful transaction
    
    [Header("Personality")]
    [SerializeField] private ShopKeeperPersonality personality = ShopKeeperPersonality.Friendly;
    [SerializeField] private bool remembersCustomers = true;
    [SerializeField] private int loyaltyDiscountThreshold = 5; // Transactions needed for discount
    
    [Header("Special Behaviors")]
    [SerializeField] private bool announcesNewItems = true;
    [SerializeField] private bool givesShoppingTips = true;
    [SerializeField] private bool offersPersonalStories = false;
    
    // Properties
    public ShopData AssociatedShop => associatedShop;
    public Transform WorkPosition => workPosition;
    public bool RequiresGreeting => requiresGreeting;
    public bool AlwaysAtShop => alwaysAtShop;
    public ShopKeeperPersonality Personality => personality;
    public int LoyaltyDiscountThreshold => loyaltyDiscountThreshold;
    public bool AnnouncesNewItems => announcesNewItems;
    public bool GivesShoppingTips => givesShoppingTips;
    public bool OffersPersonalStories => offersPersonalStories;
    
    // Work hours
    public Vector2 GetWorkHours()
    {
        if (followsShopHours && associatedShop != null && associatedShop.hasOperatingHours)
        {
            return new Vector2(associatedShop.openHour, associatedShop.closeHour);
        }
        return customWorkHours;
    }
    
    public bool IsWorkingHours()
    {
        if (!followsShopHours && !associatedShop?.hasOperatingHours == true)
            return true; // Always working if no schedule
            
        Vector2 workHours = GetWorkHours();
        int currentHour = TimeManager.instance?.GetHour() ?? 12;
        
        return currentHour >= workHours.x && currentHour < workHours.y;
    }
    
    // Dialogue selection
    public DialogueData GetAppropriateDialogue(bool hasGreeted, bool isRegularCustomer, bool shopIsOpen)
    {
        if (!shopIsOpen && shopClosedDialogue != null)
            return shopClosedDialogue;
            
        if (!hasGreeted && greetingCustomerDialogue != null)
            return greetingCustomerDialogue;
            
        if (isRegularCustomer && regularCustomerDialogue != null)
            return regularCustomerDialogue;
            
        return greetingCustomerDialogue ?? regularCustomerDialogue;
    }
    
    public DialogueData GetTransactionDialogue(bool successful)
    {
        if (successful && thankYouDialogue != null)
            return thankYouDialogue;
        else if (!successful && noMoneyDialogue != null)
            return noMoneyDialogue;
            
        return null;
    }
    
    public DialogueData GetFarewellDialogue()
    {
        return farewellDialogue;
    }
    
    // Customer relationship
    public bool ShouldOfferDiscount(int customerTransactionCount)
    {
        return remembersCustomers && customerTransactionCount >= loyaltyDiscountThreshold;
    }
    
    // Abstract method implementation
    public override void InitializeNPCData()
    {
        npcType = NPCType.Merchant; // Shop keepers are merchants
    }
    
    // Validation
    public override bool ValidateData()
    {
        if (!base.ValidateData()) return false;
        
        if (associatedShop == null)
        {
            Debug.LogError($"ShopKeeperNPCData '{npcName}': No associated shop assigned!");
            return false;
        }
        
        if (greetingCustomerDialogue == null && regularCustomerDialogue == null)
        {
            Debug.LogWarning($"ShopKeeperNPCData '{npcName}': No dialogue assigned!");
        }
        
        return true;
    }
    
    // Debug info
    public string GetShopKeeperDebugInfo()
    {
        string shopKeeperInfo = $"=== Shop Keeper Info ===\n";
        shopKeeperInfo += $"Associated Shop: {associatedShop?.shopName ?? "None"}\n";
        shopKeeperInfo += $"Work Hours: {GetWorkHours().x}:00 - {GetWorkHours().y}:00\n";
        shopKeeperInfo += $"Always At Shop: {alwaysAtShop}\n";
        shopKeeperInfo += $"Personality: {personality}\n";
        shopKeeperInfo += $"Remembers Customers: {remembersCustomers}\n";
        shopKeeperInfo += $"Currently Working: {IsWorkingHours()}\n";
        
        return shopKeeperInfo;
    }
}

// Shop keeper personality types
public enum ShopKeeperPersonality
{
    Friendly,       // Welcoming, chatty
    Professional,   // Polite but business-focused
    Grumpy,        // Short responses, impatient
    Eccentric,     // Odd, tells strange stories
    Shy,           // Few words, nervous
    Greedy         // Always talking about money
}