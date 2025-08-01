using UnityEngine;
using System.Collections.Generic;

public class ShopInteractionController : MonoBehaviour
{
    [Header("Interaction Settings")]
    [SerializeField] private float maxInteractionDistance = 3f;
    [SerializeField] private LayerMask shopLayerMask = -1;
    [SerializeField] private KeyCode interactionKey = KeyCode.E;
    [SerializeField] private bool showDebugGizmos = false;
    
    [Header("UI Integration")]
    [SerializeField] private bool showInteractionPrompts = true;
    [SerializeField] private float promptUpdateInterval = 0.1f;
    
    // Components
    private PlayerController playerController;
    
    // State
    private ShopController nearestShop = null;
    private List<ShopController> shopsInRange = new List<ShopController>();
    private float lastPromptUpdate = 0f;
    private bool isInteractionLocked = false;
    
    // Events
    public System.Action<ShopController> OnNearestShopChanged;
    public System.Action<ShopController> OnShopInteractionStarted;
    public System.Action OnShopInteractionEnded;
    
    // Properties
    public ShopController NearestShop => nearestShop;
    public bool HasShopsInRange => shopsInRange.Count > 0;
    public int ShopsInRangeCount => shopsInRange.Count;
    
    void Awake()
    {
        playerController = GetComponent<PlayerController>();
        
        if (playerController == null)
        {
            Debug.LogError("ShopInteractionController requires a PlayerController component!");
        }
    }
    
    void Start()
    {
        // Subscribe to shop manager events
        SubscribeToEvents();
        
        Debug.Log("ShopInteractionController initialized");
    }
    
    void Update()
    {
        // Update shops in range
        UpdateShopsInRange();
        
        // Update UI prompts
        if (showInteractionPrompts && Time.time - lastPromptUpdate > promptUpdateInterval)
        {
            UpdateInteractionPrompts();
            lastPromptUpdate = Time.time;
        }
        
        // Handle interaction input
        HandleInteractionInput();
    }
    
    // ===== Shop Detection =====
    
    private void UpdateShopsInRange()
    {
        shopsInRange.Clear();
        ShopController newNearestShop = null;
        float nearestDistance = float.MaxValue;
        
        // Find all shop controllers in scene
        var allShops = FindObjectsOfType<ShopController>();
        
        foreach (var shop in allShops)
        {
            if (shop == null || !shop.gameObject.activeInHierarchy) continue;
            
            float distance = Vector2.Distance(transform.position, shop.transform.position);
            
            // Check if shop is in interaction range
            if (distance <= maxInteractionDistance)
            {
                shopsInRange.Add(shop);
                
                // Track nearest shop
                if (distance < nearestDistance)
                {
                    nearestDistance = distance;
                    newNearestShop = shop;
                }
            }
        }
        
        // Update nearest shop
        if (newNearestShop != nearestShop)
        {
            var previousShop = nearestShop;
            nearestShop = newNearestShop;
            
            OnNearestShopChanged?.Invoke(nearestShop);
            
            Debug.Log($"Nearest shop changed: {previousShop?.ShopName ?? "None"} → {nearestShop?.ShopName ?? "None"}");
        }
    }
    
    // ===== Input Handling =====
    
    private void HandleInteractionInput()
    {
        if (isInteractionLocked) return;
        
        if (Input.GetKeyDown(interactionKey))
        {
            TryInteractWithNearestShop();
        }
        
        // Alternative: mouse click interaction (optional)
        if (Input.GetMouseButtonDown(0))
        {
            TryInteractWithClickedShop();
        }
    }
    
    public void TryInteractWithNearestShop()
    {
        if (nearestShop == null)
        {
            Debug.Log("No shop in interaction range");
            return;
        }
        
        if (!CanInteractWithShop(nearestShop))
        {
            return;
        }
        
        StartShopInteraction(nearestShop);
    }
    
    private void TryInteractWithClickedShop()
    {
        // Get mouse position in world space
        Vector2 mousePos = Camera.main.ScreenToWorldPoint(Input.mousePosition);
        
        // Check if clicked on a shop
        Collider2D hitCollider = Physics2D.OverlapPoint(mousePos, shopLayerMask);
        
        if (hitCollider != null)
        {
            var clickedShop = hitCollider.GetComponent<ShopController>();
            
            if (clickedShop != null && shopsInRange.Contains(clickedShop))
            {
                StartShopInteraction(clickedShop);
            }
        }
    }
    
    // ===== Interaction Logic =====
    
    private bool CanInteractWithShop(ShopController shop)
    {
        if (shop == null) return false;
        
        // Check if shop is active
        if (!shop.gameObject.activeInHierarchy) return false;
        
        // Check if player is in range
        float distance = Vector2.Distance(transform.position, shop.transform.position);
        if (distance > maxInteractionDistance) return false;
        
        // Check if shop manager allows interaction
        if (ShopManager.instance == null) return false;
        
        // Don't interrupt if already in dialogue or shop is open
        if (ShopManager.instance.IsShopOpen)
        {
            // Allow interaction if it's the same shop (to close it)
            return ShopManager.instance.CurrentShop == shop.ShopData;
        }
        
        return true;
    }
    
    private void StartShopInteraction(ShopController shop)
    {
        if (shop == null) return;
        
        Debug.Log($"Starting interaction with shop: {shop.ShopName}");
        
        // Lock interactions temporarily
        isInteractionLocked = true;
        
        // Fire event
        OnShopInteractionStarted?.Invoke(shop);
        
        // Try to interact with the shop
        shop.TryInteractWithShop();
        
        // Unlock after a short delay
        Invoke(nameof(UnlockInteraction), 0.5f);
    }
    
    private void UnlockInteraction()
    {
        isInteractionLocked = false;
    }
    
    // ===== UI Management =====
    
    private void UpdateInteractionPrompts()
    {
        if (UIManager.instance == null || !showInteractionPrompts) return;
        
        if (nearestShop != null)
        {
            string promptText = GetInteractionPromptText(nearestShop);
            UIManager.instance.ShowInteractionPrompt(promptText);
        }
        else
        {
            UIManager.instance.HideInteractionPrompt();
        }
    }
    
    private string GetInteractionPromptText(ShopController shop)
    {
        if (shop == null) return "";
        
        // Check if shop is currently open in UI
        if (ShopManager.instance.IsShopOpen && ShopManager.instance.CurrentShop == shop.ShopData)
        {
            return $"[{interactionKey}] {shop.ShopName} 닫기";
        }
        
        // Check if shop is available
        if (!shop.IsShopOpen)
        {
            return $"{shop.ShopName} (영업 종료)";
        }
        
        // Regular interaction prompt
        return $"[{interactionKey}] {shop.ShopName}에서 거래하기";
    }
    
    // ===== Event Management =====
    
    private void SubscribeToEvents()
    {
        ShopManager.OnShopOpened += OnShopOpened;
        ShopManager.OnShopClosed += OnShopClosed;
    }
    
    private void UnsubscribeFromEvents()
    {
        ShopManager.OnShopOpened -= OnShopOpened;
        ShopManager.OnShopClosed -= OnShopClosed;
    }
    
    private void OnShopOpened(ShopData shopData)
    {
        Debug.Log($"Shop opened: {shopData.shopName}");
        
        // Update prompts
        if (showInteractionPrompts)
        {
            UpdateInteractionPrompts();
        }
    }
    
    private void OnShopClosed(ShopData shopData)
    {
        Debug.Log($"Shop closed: {shopData.shopName}");
        
        // Fire interaction ended event
        OnShopInteractionEnded?.Invoke();
        
        // Update prompts
        if (showInteractionPrompts)
        {
            UpdateInteractionPrompts();
        }
    }
    
    // ===== Public Interface =====
    
    public List<ShopController> GetShopsInRange()
    {
        return new List<ShopController>(shopsInRange);
    }
    
    public ShopController GetNearestShopOfType(ShopType shopType)
    {
        ShopController nearestOfType = null;
        float nearestDistance = float.MaxValue;
        
        foreach (var shop in shopsInRange)
        {
            if (shop.ShopData != null && shop.ShopData.shopType == shopType)
            {
                float distance = Vector2.Distance(transform.position, shop.transform.position);
                if (distance < nearestDistance)
                {
                    nearestDistance = distance;
                    nearestOfType = shop;
                }
            }
        }
        
        return nearestOfType;
    }
    
    public void SetInteractionKey(KeyCode newKey)
    {
        interactionKey = newKey;
    }
    
    public void SetMaxInteractionDistance(float distance)
    {
        maxInteractionDistance = Mathf.Max(0.1f, distance);
    }
    
    public void EnableInteractionPrompts(bool enable)
    {
        showInteractionPrompts = enable;
        
        if (!enable && UIManager.instance != null)
        {
            UIManager.instance.HideInteractionPrompt();
        }
    }
    
    // ===== Forced Interactions (for external systems) =====
    
    public bool TryInteractWithShop(string shopName)
    {
        var shop = shopsInRange.Find(s => s.ShopName.Equals(shopName, System.StringComparison.OrdinalIgnoreCase));
        
        if (shop != null)
        {
            StartShopInteraction(shop);
            return true;
        }
        
        return false;
    }
    
    public bool TryInteractWithShop(ShopData shopData)
    {
        var shop = shopsInRange.Find(s => s.ShopData == shopData);
        
        if (shop != null)
        {
            StartShopInteraction(shop);
            return true;
        }
        
        return false;
    }
    
    // ===== Debug Methods =====
    
    [ContextMenu("Debug - List Shops In Range")]
    public void DebugListShopsInRange()
    {
        Debug.Log($"=== Shops in Range ({shopsInRange.Count}) ===");
        
        for (int i = 0; i < shopsInRange.Count; i++)
        {
            var shop = shopsInRange[i];
            float distance = Vector2.Distance(transform.position, shop.transform.position);
            
            Debug.Log($"{i + 1}. {shop.ShopName} - Distance: {distance:F1}m - " +
                     $"Status: {(shop.IsShopOpen ? "OPEN" : "CLOSED")}");
        }
        
        if (nearestShop != null)
        {
            Debug.Log($"Nearest: {nearestShop.ShopName}");
        }
    }
    
    [ContextMenu("Debug - Force Interact With Nearest")]
    public void DebugForceInteract()
    {
        TryInteractWithNearestShop();
    }
    
    [ContextMenu("Debug - Toggle Debug Gizmos")]
    public void DebugToggleGizmos()
    {
        showDebugGizmos = !showDebugGizmos;
    }
    
    // ===== Gizmos =====
    
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
    
    void OnDrawGizmos()
    {
        if (!showDebugGizmos) return;
        
        // Draw interaction range
        Gizmos.color = Color.cyan;
        DrawWireCircle(transform.position, maxInteractionDistance);
        
        // Draw lines to shops in range
        Gizmos.color = Color.green;
        foreach (var shop in shopsInRange)
        {
            if (shop != null)
            {
                Gizmos.DrawLine(transform.position, shop.transform.position);
            }
        }
        
        // Highlight nearest shop
        if (nearestShop != null)
        {
            Gizmos.color = Color.yellow;
            Gizmos.DrawLine(transform.position, nearestShop.transform.position);
            Gizmos.DrawWireSphere(nearestShop.transform.position, 0.5f);
        }
    }
    
    void OnDrawGizmosSelected()
    {
        // Always draw interaction range when selected
        Gizmos.color = Color.cyan;
        DrawWireCircle(transform.position, maxInteractionDistance);
        
        // Draw shop info
        #if UNITY_EDITOR
        string info = $"Shop Interaction\nRange: {maxInteractionDistance}m\n" +
                     $"Shops in Range: {shopsInRange.Count}\n" +
                     $"Nearest: {nearestShop?.ShopName ?? "None"}";
                     
        UnityEditor.Handles.Label(
            transform.position + Vector3.up * 2f,
            info
        );
        #endif
    }
    
    // ===== Cleanup =====
    
    void OnDestroy()
    {
        UnsubscribeFromEvents();
        
        // Clear UI prompts
        if (UIManager.instance != null)
        {
            UIManager.instance.HideInteractionPrompt();
        }
    }
}