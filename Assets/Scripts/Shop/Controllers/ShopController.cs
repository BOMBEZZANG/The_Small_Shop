using UnityEngine;
using System.Collections.Generic;

[RequireComponent(typeof(Collider2D))]
public class ShopController : MonoBehaviour
{
    [Header("Shop Configuration")]
    [SerializeField] private ShopData shopData;
    [SerializeField] private bool autoRegisterOnStart = true;
    
    [Header("Shop State")]
    [SerializeField] private bool isShopActive = true;
    [SerializeField] private float interactionRange = 2f;
    
    [Header("Shopkeeper Requirements")]
    [SerializeField] private bool requireShopkeeperPresence = true;
    [SerializeField] private Transform shopkeeperPosition; // Where NPC should be
    [SerializeField] private float shopkeeperCheckRadius = 3f;
    [SerializeField] private string shopkeeperTag = "NPC";
    [SerializeField] private NPCController requiredShopkeeper; // Specific NPC
    
    [Header("Visual Indicators")]
    [SerializeField] private GameObject shopIndicator; // 상점 표시 아이콘
    [SerializeField] private SpriteRenderer shopSignRenderer;
    [SerializeField] private Color openColor = Color.green;
    [SerializeField] private Color closedColor = Color.red;
    
    // Components
    private Collider2D shopCollider;
    private ShopKeeperController shopKeeper;
    
    // State
    private bool playerInRange = false;
    private PlayerController currentPlayer = null;
    
    // Events
    public System.Action<ShopController> OnPlayerEnterRange;
    public System.Action<ShopController> OnPlayerExitRange;
    public System.Action<ShopController, PlayerController> OnShopInteraction;
    
    // Properties
    public ShopData ShopData => shopData;
    public bool IsShopOpen => shopData != null && shopData.IsShopOpen() && isShopActive && IsShopkeeperPresent();
    public bool PlayerInRange => playerInRange;
    public string ShopName => shopData?.shopName ?? "Unknown Shop";
    
    void Awake()
    {
        // Get components
        shopCollider = GetComponent<Collider2D>();
        shopKeeper = GetComponent<ShopKeeperController>();
        
        // Configure collider as trigger
        if (shopCollider != null)
        {
            shopCollider.isTrigger = true;
        }
        
        // Validate shop data
        if (shopData == null)
        {
            Debug.LogError($"ShopController on {gameObject.name} has no ShopData assigned!");
        }
    }
    
    void Start()
    {
        // Register with ShopManager
        if (autoRegisterOnStart && shopData != null)
        {
            ShopManager.instance?.RegisterShop(shopData);
        }
        
        // Subscribe to events
        SubscribeToEvents();
        
        // Initialize visual state
        UpdateVisualState();
        
        Debug.Log($"Shop '{ShopName}' initialized at {transform.position}");
    }
    
    void Update()
    {
        // Update visual indicators based on shop state
        UpdateVisualState();
        
        // Handle interaction input when player is in range
        if (playerInRange && currentPlayer != null && Input.GetKeyDown(KeyCode.E))
        {
            TryInteractWithShop();
        }
    }
    
    // ===== Player Detection =====
    
    void OnTriggerEnter2D(Collider2D other)
    {
        if (other.CompareTag("Player"))
        {
            var player = other.GetComponent<PlayerController>();
            if (player != null)
            {
                OnPlayerEnterShopRange(player);
            }
        }
    }
    
    void OnTriggerExit2D(Collider2D other)
    {
        if (other.CompareTag("Player"))
        {
            var player = other.GetComponent<PlayerController>();
            if (player != null && player == currentPlayer)
            {
                OnPlayerExitShopRange();
            }
        }
    }
    
    private void OnPlayerEnterShopRange(PlayerController player)
    {
        currentPlayer = player;
        playerInRange = true;
        
        Debug.Log($"Player entered {ShopName} range");
        
        // Show interaction prompt
        ShowInteractionPrompt(true);
        
        // Fire event
        OnPlayerEnterRange?.Invoke(this);
        
        // Notify Interaction Prompt Manager
        if (InteractionPromptManager.instance != null)
        {
            bool hasShopkeeper = IsShopkeeperPresent();
            bool isOpen = shopData != null && shopData.IsShopOpen() && isShopActive;
            
            InteractionPromptManager.instance.ShowShopPrompt(ShopName, isOpen, hasShopkeeper);
        }
    }
    
    private void OnPlayerExitShopRange()
    {
        currentPlayer = null;
        playerInRange = false;
        
        Debug.Log($"Player exited {ShopName} range");
        
        // Hide interaction prompt
        ShowInteractionPrompt(false);
        
        // Fire event
        OnPlayerExitRange?.Invoke(this);
        
        // Close shop if it's open
        if (ShopManager.instance.IsShopOpen && ShopManager.instance.CurrentShop == shopData)
        {
            ShopManager.instance.CloseShop();
        }
        
        // Hide interaction prompt
        if (InteractionPromptManager.instance != null)
        {
            InteractionPromptManager.instance.HideInteractionPrompt();
        }
    }
    
    // ===== Shopkeeper Presence Check =====
    
    public bool IsShopkeeperPresent()
    {
        if (!requireShopkeeperPresence) return true;
        
        // Method 1: Check specific assigned shopkeeper
        if (requiredShopkeeper != null)
        {
            if (!requiredShopkeeper.gameObject.activeInHierarchy) return false;
            
            float distance = Vector2.Distance(
                shopkeeperPosition != null ? shopkeeperPosition.position : transform.position,
                requiredShopkeeper.transform.position
            );
            
            return distance <= shopkeeperCheckRadius;
        }
        
        // Method 2: Check for any NPC with tag in radius
        if (shopkeeperPosition != null)
        {
            Collider2D[] npcsInRange = Physics2D.OverlapCircleAll(
                shopkeeperPosition.position, 
                shopkeeperCheckRadius
            );
            
            foreach (var collider in npcsInRange)
            {
                if (collider.CompareTag(shopkeeperTag))
                {
                    var npc = collider.GetComponent<NPCController>();
                    if (npc != null && npc.GetCurrentState() != NPCState.Disabled)
                    {
                        return true;
                    }
                }
            }
        }
        
        return false;
    }
    
    private string GetShopkeeperAbsenceMessage()
    {
        if (shopData.shopKeeperName != null)
        {
            return $"{shopData.shopKeeperName}이(가) 자리에 없습니다.";
        }
        return "점원이 자리에 없습니다.";
    }
    
    // ===== Shop Interaction =====
    
    public void TryInteractWithShop()
    {
        if (!playerInRange || currentPlayer == null)
        {
            Debug.Log("Player not in range for shop interaction");
            return;
        }
        
        // Check if shopkeeper is present
        if (!IsShopkeeperPresent())
        {
            string absenceMessage = GetShopkeeperAbsenceMessage();
            
            if (UIManager.instance != null)
            {
                UIManager.instance.ShowNotification(absenceMessage, 3f);
            }
            
            Debug.Log(absenceMessage);
            return;
        }
        
        if (!IsShopOpen)
        {
            // Show closed message
            string closedMessage = shopData.hasOperatingHours ? 
                $"{ShopName}은(는) 영업시간이 아닙니다. (영업시간: {shopData.openHour}시 - {shopData.closeHour}시)" :
                $"{ShopName}은(는) 현재 이용할 수 없습니다.";
                
            if (UIManager.instance != null)
            {
                UIManager.instance.ShowNotification(closedMessage, 3f);
            }
            
            Debug.Log(closedMessage);
            return;
        }
        
        // Try to interact through shop keeper first (dialogue system)
        if (shopKeeper != null && shopKeeper.enabled)
        {
            shopKeeper.TryStartShopInteraction(currentPlayer);
        }
        else
        {
            // Direct shop interaction
            OpenShopDirectly();
        }
        
        // Fire event
        OnShopInteraction?.Invoke(this, currentPlayer);
    }
    
    public void OpenShopDirectly()
    {
        if (ShopManager.instance.OpenShop(shopData))
        {
            Debug.Log($"Opened shop: {ShopName}");
            
            // Hide interaction prompt since shop UI is now open
            ShowInteractionPrompt(false);
            if (UIManager.instance != null)
            {
                UIManager.instance.HideInteractionPrompt();
            }
        }
        else
        {
            Debug.LogWarning($"Failed to open shop: {ShopName}");
        }
    }
    
    // ===== Visual Management =====
    
    private void UpdateVisualState()
    {
        // Update shop sign color
        if (shopSignRenderer != null)
        {
            shopSignRenderer.color = IsShopOpen ? openColor : closedColor;
        }
        
        // Update shop indicator
        if (shopIndicator != null)
        {
            shopIndicator.SetActive(isShopActive && shopData != null);
        }
    }
    
    private void ShowInteractionPrompt(bool show)
    {
        // This could animate an interaction indicator above the shop
        // For now, we'll rely on the UIManager for prompts
        
        if (shopIndicator != null)
        {
            // Maybe add a pulsing effect or highlight when player is in range
            var indicator = shopIndicator.GetComponent<Animator>();
            if (indicator != null)
            {
                indicator.SetBool("PlayerNear", show && IsShopOpen);
            }
        }
    }
    
    // ===== Event Management =====
    
    private void SubscribeToEvents()
    {
        // Subscribe to shop manager events
        ShopManager.OnShopOpened += OnShopOpened;
        ShopManager.OnShopClosed += OnShopClosed;
        ShopManager.OnShopError += OnShopError;
    }
    
    private void UnsubscribeFromEvents()
    {
        // Unsubscribe from shop manager events
        ShopManager.OnShopOpened -= OnShopOpened;
        ShopManager.OnShopClosed -= OnShopClosed;
        ShopManager.OnShopError -= OnShopError;
    }
    
    private void OnShopOpened(ShopData openedShop)
    {
        if (openedShop == shopData)
        {
            Debug.Log($"This shop was opened: {ShopName}");
        }
    }
    
    private void OnShopClosed(ShopData closedShop)
    {
        if (closedShop == shopData)
        {
            Debug.Log($"This shop was closed: {ShopName}");
            
            // Show interaction prompt again if player is still in range
            if (playerInRange && IsShopOpen)
            {
                ShowInteractionPrompt(true);
                if (InteractionPromptManager.instance != null)
                {
                    InteractionPromptManager.instance.ShowShopPrompt(ShopName, IsShopOpen, IsShopkeeperPresent());
                }
            }
        }
    }
    
    private void OnShopError(string error)
    {
        Debug.LogWarning($"Shop error at {ShopName}: {error}");
    }
    
    // ===== Public Interface =====
    
    public void SetShopData(ShopData newShopData)
    {
        shopData = newShopData;
        
        if (ShopManager.instance != null && newShopData != null)
        {
            ShopManager.instance.RegisterShop(newShopData);
        }
        
        UpdateVisualState();
    }
    
    public void SetShopActive(bool active)
    {
        isShopActive = active;
        UpdateVisualState();
        
        if (!active && playerInRange)
        {
            OnPlayerExitShopRange();
        }
    }
    
    public float GetDistanceToPlayer()
    {
        if (currentPlayer == null) return float.MaxValue;
        
        return Vector2.Distance(transform.position, currentPlayer.transform.position);
    }
    
    // ===== Debug Methods =====
    
    [ContextMenu("Debug - Force Open Shop")]
    public void DebugForceOpenShop()
    {
        if (shopData != null)
        {
            ShopManager.instance?.OpenShop(shopData);
        }
    }
    
    [ContextMenu("Debug - Print Shop Info")]
    public void DebugPrintShopInfo()
    {
        if (shopData != null)
        {
            Debug.Log($"=== {ShopName} Info ===\n{shopData.GetShopDebugInfo()}");
        }
        else
        {
            Debug.Log("No shop data assigned");
        }
    }
    
    [ContextMenu("Debug - Refresh Shop Inventory")]
    public void DebugRefreshInventory()
    {
        if (shopData != null)
        {
            shopData.RefreshShopInventory();
            Debug.Log($"Refreshed inventory for {ShopName}");
        }
    }
    
    // ===== Utility Methods =====
    
    private void DrawWireCircle(Vector3 center, float radius)
    {
        // Draw a circle using line segments
        int segments = 32;
        float angleStep = 360f / segments;
        Vector3 prevPoint = center + new Vector3(radius, 0, 0);
        
        for (int i = 1; i <= segments; i++)
        {
            float angle = i * angleStep * Mathf.Deg2Rad;
            Vector3 newPoint = center + new Vector3(Mathf.Cos(angle) * radius, Mathf.Sin(angle) * radius, 0);
            Gizmos.DrawLine(prevPoint, newPoint);
            prevPoint = newPoint;
        }
    }
    
    // ===== Gizmos =====
    
    void OnDrawGizmosSelected()
    {
        // Draw interaction range
        Gizmos.color = IsShopOpen ? Color.green : Color.red;
        DrawWireCircle(transform.position, interactionRange);
        
        // Draw shopkeeper check radius
        if (requireShopkeeperPresence && shopkeeperPosition != null)
        {
            Gizmos.color = IsShopkeeperPresent() ? Color.cyan : Color.yellow;
            DrawWireCircle(shopkeeperPosition.position, shopkeeperCheckRadius);
            
            // Draw line to required shopkeeper
            if (requiredShopkeeper != null)
            {
                Gizmos.DrawLine(shopkeeperPosition.position, requiredShopkeeper.transform.position);
            }
        }
        
        // Draw shop info
        #if UNITY_EDITOR
        string shopInfo = shopData != null ? 
            $"{shopData.shopName}\n{(IsShopOpen ? "OPEN" : "CLOSED")}" : 
            "No Shop Data";
            
        if (requireShopkeeperPresence)
        {
                shopInfo += $"\nShopkeeper: {(IsShopkeeperPresent() ? "Present" : "Absent")}";
        }
            
        UnityEditor.Handles.Label(
            transform.position + Vector3.up * 2f,
            shopInfo
        );
        #endif
    }
    
    void OnDrawGizmos()
    {
        // Draw shop icon
        if (shopData != null && shopData.shopIcon != null)
        {
            Gizmos.color = Color.white;
            Gizmos.DrawIcon(transform.position + Vector3.up, "ShopIcon");
        }
    }
    
    // ===== Cleanup =====
    
    void OnDestroy()
    {
        UnsubscribeFromEvents();
        
        // Unregister from ShopManager
        if (ShopManager.instance != null && shopData != null)
        {
            ShopManager.instance.UnregisterShop(shopData);
        }
        
        // Close shop if this shop is currently open
        if (ShopManager.instance != null && 
            ShopManager.instance.IsShopOpen && 
            ShopManager.instance.CurrentShop == shopData)
        {
            ShopManager.instance.CloseShop();
        }
    }
}