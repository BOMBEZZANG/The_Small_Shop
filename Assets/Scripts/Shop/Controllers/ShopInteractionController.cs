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
    private bool isInitialized = false;
    
    // Events
    public System.Action<ShopController> OnNearestShopChanged;
    public System.Action<ShopController> OnShopInteractionStarted;
    public System.Action OnShopInteractionEnded;
    
    // Properties
    public ShopController NearestShop => nearestShop;
    public bool HasShopsInRange => shopsInRange.Count > 0;
    public int ShopsInRangeCount => shopsInRange.Count;
    public bool IsInitialized => isInitialized;
    
    void Awake()
    {
        // Awake에서는 아무것도 하지 않음 - DontDestroyOnLoad 환경 대응
    }
    
    // OnEnable은 오브젝트가 활성화될 때마다 호출됨 (씬 전환 후에도)
    void OnEnable()
    {
        // 컴포넌트 초기화 시도
        TryInitializeComponents();
    }
    
    private void TryInitializeComponents()
    {
        if (isInitialized) return;
        
        // Method 1: Try to find PlayerController using FindObjectOfType (most reliable for cross-scene)
        playerController = FindObjectOfType<PlayerController>();
        
        // Method 2: Try to find by Player tag if the above fails
        if (playerController == null)
        {
            GameObject playerObj = GameObject.FindGameObjectWithTag("Player");
            if (playerObj != null)
            {
                playerController = playerObj.GetComponent<PlayerController>();
            }
        }
        
        // Method 3: If we're on the player object itself, try local component search
        if (playerController == null)
        {
            playerController = GetComponent<PlayerController>();
            if (playerController == null)
            {
                playerController = GetComponentInParent<PlayerController>();
            }
            if (playerController == null)
            {
                playerController = GetComponentInChildren<PlayerController>();
            }
        }
        
        if (playerController != null)
        {
            isInitialized = true;
            Debug.Log($"ShopInteractionController initialized successfully. PlayerController found on: {playerController.gameObject.name}");
        }
        else
        {
            Debug.LogWarning("PlayerController not found, will retry...");
        }
    }
    
    void Start()
    {
        // 초기화되지 않았으면 재시도
        if (!isInitialized)
        {
            TryInitializeComponents();
        }
        
        // 초기화되었으면 이벤트 구독
        if (isInitialized)
        {
            // Subscribe to shop manager events
            SubscribeToEvents();
            
            Debug.Log("ShopInteractionController initialized");
        }
        else
        {
            // Start coroutine for delayed initialization
            StartCoroutine(DelayedInitialization());
        }
    }
    
    private System.Collections.IEnumerator DelayedInitialization()
    {
        float timeout = 5f; // 5 seconds timeout
        float elapsed = 0f;
        
        Debug.Log($"ShopInteractionController starting delayed initialization on GameObject: {gameObject.name}");
        
        while (!isInitialized && elapsed < timeout)
        {
            yield return new WaitForSeconds(0.1f); // Check every 0.1 seconds
            elapsed += 0.1f;
            
            TryInitializeComponents();
            
            if (isInitialized)
            {
                SubscribeToEvents();
                Debug.Log($"ShopInteractionController initialized after {elapsed:F1} seconds");
                yield break;
            }
        }
        
        if (!isInitialized)
        {
            // Additional debug information
            Debug.LogError($"ShopInteractionController failed to initialize after {timeout} seconds on GameObject: {gameObject.name}");
            Debug.LogError($"Current scene: {UnityEngine.SceneManagement.SceneManager.GetActiveScene().name}");
            
            // Try to find all PlayerControllers in the scene for debugging
            var allPlayerControllers = FindObjectsOfType<PlayerController>();
            Debug.LogError($"Found {allPlayerControllers.Length} PlayerController(s) in scene");
            foreach (var pc in allPlayerControllers)
            {
                Debug.LogError($"  - PlayerController found on: {pc.gameObject.name} (active: {pc.gameObject.activeInHierarchy})");
            }
            
            // Check for Player tagged objects
            var playerTaggedObjects = GameObject.FindGameObjectsWithTag("Player");
            Debug.LogError($"Found {playerTaggedObjects.Length} GameObject(s) with 'Player' tag");
            foreach (var obj in playerTaggedObjects)
            {
                Debug.LogError($"  - Player tagged object: {obj.name} (active: {obj.activeInHierarchy})");
            }
        }
    }
    
    void Update()
    {
        // Skip if not initialized (the coroutine will handle initialization)
        if (!isInitialized)
        {
            return;
        }
        
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
    
    void OnDisable()
    {
        // 씬 전환 시 상태 리셋
        ClearShopInteractions();
        UnsubscribeFromEvents();
    }
    
    private void ClearShopInteractions()
    {
        nearestShop = null;
        shopsInRange.Clear();
        isInteractionLocked = false;
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
        if (distance > maxInteractionDistance)
        {
            return false;
        }
        
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
        
        // Don't show prompts if shop is already open
        if (ShopManager.instance != null && ShopManager.instance.IsShopOpen)
        {
            UIManager.instance.HideInteractionPrompt();
            if (InteractionPromptManager.instance != null)
            {
                InteractionPromptManager.instance.HideInteractionPrompt();
            }
            return;
        }
        
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
            return $"[{interactionKey}] {shop.ShopName} Close";
        }
        
        // Check if shop is available
        if (!shop.IsShopOpen)
        {
            return $"{shop.ShopName} (Close)";
        }
        
        // Regular interaction prompt
        return $"[Buy&Sell";
    }
    
    // ===== Event Management =====
    
    private void SubscribeToEvents()
    {
        if (ShopManager.instance != null)
        {
            ShopManager.OnShopOpened += OnShopOpened;
            ShopManager.OnShopClosed += OnShopClosed;
        }
    }
    
    private void UnsubscribeFromEvents()
    {
        if (ShopManager.instance != null)
        {
            ShopManager.OnShopOpened -= OnShopOpened;
            ShopManager.OnShopClosed -= OnShopClosed;
        }
    }
    
    private void OnShopOpened(ShopData shopData)
    {
        Debug.Log($"Shop opened: {shopData.shopName}");
        
        // Hide interaction prompts when shop opens (both systems)
        if (UIManager.instance != null)
        {
            UIManager.instance.HideInteractionPrompt();
        }
        
        if (InteractionPromptManager.instance != null)
        {
            InteractionPromptManager.instance.HideInteractionPrompt();
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
                     $"Status: {(shop.IsShopOpen ? "Open" : "Closed")}");
        }
    }
    
    [ContextMenu("Debug - Force Reinitialize")]
    private void ForceReinitialize()
    {
        isInitialized = false;
        playerController = null;
        UnsubscribeFromEvents();
        TryInitializeComponents();
        if (isInitialized)
        {
            SubscribeToEvents();
        }
    }
    
    [ContextMenu("Debug - Check Component State")]
    private void DebugCheckComponentState()
    {
        Debug.Log($"=== ShopInteractionController Debug Info ===\n" +
                 $"Is Initialized: {isInitialized}\n" +
                 $"PlayerController: {(playerController != null ? "Found" : "Not Found")}\n" +
                 $"Shops in Range: {shopsInRange.Count}\n" +
                 $"Nearest Shop: {(nearestShop != null ? nearestShop.ShopName : "None")}\n" +
                 $"Interaction Locked: {isInteractionLocked}\n" +
                 $"Show Prompts: {showInteractionPrompts}\n" +
                 $"Max Distance: {maxInteractionDistance}");
    }
    
    // ===== Gizmos =====
    
    void OnDrawGizmosSelected()
    {
        if (!showDebugGizmos) return;
        
        // Draw interaction range (2D circle using 3D sphere)
        Gizmos.color = Color.yellow;
        Gizmos.DrawWireSphere(transform.position, maxInteractionDistance);
        
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
            Gizmos.color = Color.red;
            Gizmos.DrawWireCube(nearestShop.transform.position, Vector3.one * 0.5f);
        }
    }
    
    // Inspector에서 컴포넌트 상태 확인용 (Play 모드에서만)
    void OnValidate()
    {
        if (Application.isPlaying && !isInitialized)
        {
            TryInitializeComponents();
        }
    }
}