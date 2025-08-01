using UnityEngine;
using UnityEngine.UI;
using TMPro;
using System.Collections.Generic;

public class TransactionConfirmationUI : MonoBehaviour
{
    [Header("Main References")]
    [SerializeField] private GameObject modalBackground;
    [SerializeField] private GameObject confirmationWindow;
    
    [Header("Header")]
    [SerializeField] private TextMeshProUGUI titleText;
    [SerializeField] private Button closeButton;
    
    [Header("Item Display")]
    [SerializeField] private Image itemIcon;
    [SerializeField] private TextMeshProUGUI itemNameText;
    [SerializeField] private TextMeshProUGUI itemDescriptionText;
    [SerializeField] private TextMeshProUGUI unitPriceLabel;
    [SerializeField] private TextMeshProUGUI unitPriceText;
    
    [Header("Quantity Controls")]
    [SerializeField] private TextMeshProUGUI quantityLabel;
    [SerializeField] private Button minusButton;
    [SerializeField] private Button plusButton;
    [SerializeField] private TMP_InputField quantityInputField;
    [SerializeField] private Button maxButton;
    [SerializeField] private TextMeshProUGUI maxButtonText;
    
    [Header("Price Breakdown")]
    [SerializeField] private GameObject priceBreakdownPanel;
    [SerializeField] private TextMeshProUGUI unitPriceRowLabel;
    [SerializeField] private TextMeshProUGUI unitPriceValue;
    [SerializeField] private TextMeshProUGUI quantityRowLabel;
    [SerializeField] private TextMeshProUGUI quantityValue;
    [SerializeField] private Image separator;
    [SerializeField] private TextMeshProUGUI totalLabel;
    [SerializeField] private TextMeshProUGUI totalPriceText;
    
    [Header("Button Area")]
    [SerializeField] private Button cancelButton;
    [SerializeField] private TextMeshProUGUI cancelButtonText;
    [SerializeField] private Button confirmButton;
    [SerializeField] private TextMeshProUGUI confirmButtonText;
    
    [Header("Settings")]
    [SerializeField] private bool enableQuantityAdjustment = true;
    [SerializeField] private int minQuantity = 1;
    [SerializeField] private Color buyButtonColor = new Color(0.3f, 0.69f, 0.31f); // #4CAF50
    [SerializeField] private Color sellButtonColor = new Color(1f, 0.6f, 0f); // #FF9800
    
    [Header("Audio")]
    [SerializeField] private AudioClip confirmSound;
    [SerializeField] private AudioClip cancelSound;
    [SerializeField] private AudioClip adjustQuantitySound;
    [SerializeField] private AudioClip errorSound;
    
    // Current transaction data
    private System.Action currentConfirmAction;
    private TransactionType currentTransactionType;
    private MaterialData currentItem;
    private ShopItemData currentShopItem;
    private int currentQuantity;
    private int currentUnitPrice;
    private int maxQuantityAvailable;
    
    // Components
    private AudioSource audioSource;
    private CanvasGroup canvasGroup;
    
    // Properties
    public bool IsShowing => confirmationWindow != null && confirmationWindow.activeInHierarchy;
    public TransactionType CurrentTransactionType => currentTransactionType;
    
    void Awake()
    {
        audioSource = GetComponent<AudioSource>();
        canvasGroup = GetComponent<CanvasGroup>();
        SetupUI();
    }
    
    void Start()
    {
        // Hide initially
        HideConfirmation();
    }
    
    void Update()
    {
        // Handle escape key to cancel
        if (IsShowing && Input.GetKeyDown(KeyCode.Escape))
        {
            CancelTransaction();
        }
    }
    
    private void SetupUI()
    {
        // Setup buttons
        if (confirmButton != null)
            confirmButton.onClick.AddListener(ConfirmTransaction);
            
        if (cancelButton != null)
            cancelButton.onClick.AddListener(CancelTransaction);
            
        if (closeButton != null)
            closeButton.onClick.AddListener(CancelTransaction);
        
        // Setup quantity controls
        if (minusButton != null)
            minusButton.onClick.AddListener(DecreaseQuantity);
            
        if (plusButton != null)
            plusButton.onClick.AddListener(IncreaseQuantity);
            
        if (maxButton != null)
            maxButton.onClick.AddListener(SetMaxQuantity);
            
        if (quantityInputField != null)
        {
            quantityInputField.onEndEdit.AddListener(OnQuantityInputChanged);
            quantityInputField.contentType = TMP_InputField.ContentType.IntegerNumber;
        }
        
        // Setup modal background click to cancel
        if (modalBackground != null)
        {
            Button bgButton = modalBackground.GetComponent<Button>();
            if (bgButton == null)
            {
                bgButton = modalBackground.AddComponent<Button>();
                bgButton.transition = Selectable.Transition.None;
            }
            bgButton.onClick.AddListener(CancelTransaction);
        }
    }
    
    // ===== Show Confirmation Methods =====
    
    public void ShowBuyConfirmation(MaterialData item, int quantity, int totalCost, System.Action onConfirm)
    {
        if (item == null) return;
        
        currentTransactionType = TransactionType.Buy;
        currentItem = item;
        currentQuantity = quantity;
        currentUnitPrice = item.baseValue;
        currentConfirmAction = onConfirm;
        
        // Calculate max buyable quantity
        int playerGold = GoldManager.instance?.GetGold() ?? 0;
        int unitBuyPrice = Mathf.RoundToInt(currentUnitPrice * 1.2f); // Assuming 1.2x multiplier
        maxQuantityAvailable = Mathf.FloorToInt((float)playerGold / unitBuyPrice);
        
        SetupBuyUI();
        ShowConfirmation();
    }
    
    public void ShowBuyConfirmation(ShopItemData shopItem, int quantity, int totalCost, System.Action onConfirm)
    {
        if (shopItem == null || shopItem.materialData == null) return;
        
        currentTransactionType = TransactionType.Buy;
        currentShopItem = shopItem;
        currentItem = shopItem.materialData;
        currentQuantity = quantity;
        currentUnitPrice = shopItem.materialData?.baseValue ?? shopItem.GetBuyPrice();
        currentConfirmAction = onConfirm;
        
        // Calculate max buyable quantity
        int playerGold = GoldManager.instance?.GetGold() ?? 0;
        int unitBuyPrice = ShopManager.instance?.GetBuyPrice(shopItem, 1) ?? currentUnitPrice;
        maxQuantityAvailable = Mathf.Min(
            Mathf.FloorToInt((float)playerGold / unitBuyPrice),
            shopItem.hasUnlimitedStock ? 999 : shopItem.CurrentStock
        );
        
        SetupBuyUI();
        ShowConfirmation();
    }
    
    public void ShowSellConfirmation(MaterialData item, int quantity, int totalPrice, System.Action onConfirm)
    {
        if (item == null) return;
        
        currentTransactionType = TransactionType.Sell;
        currentItem = item;
        currentQuantity = quantity;
        currentUnitPrice = item.baseValue;
        currentConfirmAction = onConfirm;
        
        // Max sellable is player's inventory
        maxQuantityAvailable = InventoryManager.instance?.GetItemQuantity(item) ?? 0;
        
        SetupSellUI();
        ShowConfirmation();
    }
    
    // ===== Bulk Transaction Support =====
    
    public void ShowBulkSellConfirmation(List<ItemQuantityPair> items, int totalPrice, System.Action onConfirm)
    {
        if (items == null || items.Count == 0) return;
        
        // For bulk selling, disable quantity adjustment
        enableQuantityAdjustment = false;
        
        // Use first item for display (this could be enhanced to show all items)
        currentTransactionType = TransactionType.Sell;
        currentItem = items[0].item;
        currentQuantity = items.Count; // Show number of item types
        currentUnitPrice = 0; // Not applicable for bulk
        currentConfirmAction = onConfirm;
        
        SetupBulkSellUI(items, totalPrice);
        ShowConfirmation();
    }
    
    // ===== UI Setup Methods =====
    
    private void SetupBuyUI()
    {
        // Title
        if (titleText != null)
            titleText.text = "구매 확인";
        
        // Item info
        SetupItemDisplay();
        
        // Button colors
        if (confirmButton != null)
        {
            var colors = confirmButton.colors;
            colors.normalColor = buyButtonColor;
            confirmButton.colors = colors;
        }
        
        if (confirmButtonText != null)
            confirmButtonText.text = "구매";
        
        // Quantity controls
        SetupQuantityControls();
        
        // Price breakdown
        UpdatePriceBreakdown();
    }
    
    private void SetupSellUI()
    {
        // Title
        if (titleText != null)
            titleText.text = "판매 확인";
        
        // Item info
        SetupItemDisplay();
        
        // Button colors
        if (confirmButton != null)
        {
            var colors = confirmButton.colors;
            colors.normalColor = sellButtonColor;
            confirmButton.colors = colors;
        }
        
        if (confirmButtonText != null)
            confirmButtonText.text = "판매";
        
        // Quantity controls
        SetupQuantityControls();
        
        // Price breakdown
        UpdatePriceBreakdown();
    }
    
    private void SetupBulkSellUI(List<ItemQuantityPair> items, int totalPrice)
    {
        // Title
        if (titleText != null)
            titleText.text = "전체 판매 확인";
        
        // Item display for bulk
        if (itemNameText != null)
            itemNameText.text = $"{items.Count}가지 아이템";
            
        if (itemDescriptionText != null)
        {
            int totalQuantity = 0;
            foreach (var item in items)
                totalQuantity += item.quantity;
            itemDescriptionText.text = $"총 {totalQuantity}개 아이템을 판매합니다";
        }
        
        // Hide quantity controls for bulk
        if (quantityInputField != null)
            quantityInputField.transform.parent.gameObject.SetActive(false);
        
        // Show total price directly
        if (totalPriceText != null)
            totalPriceText.text = $"{totalPrice:N0} 골드";
        
        if (confirmButtonText != null)
            confirmButtonText.text = "전체 판매";
    }
    
    private void SetupItemDisplay()
    {
        if (currentItem == null) return;
        
        // Icon
        if (itemIcon != null && currentItem.materialIcon != null)
        {
            itemIcon.sprite = currentItem.materialIcon;
            itemIcon.gameObject.SetActive(true);
        }
        
        // Name
        if (itemNameText != null)
            itemNameText.text = currentItem.materialName;
            
        // Description
        if (itemDescriptionText != null)
            itemDescriptionText.text = currentItem.materialDescription ?? "";
            
        // Unit price
        if (unitPriceLabel != null)
            unitPriceLabel.text = currentTransactionType == TransactionType.Buy ? "구매 단가" : "판매 단가";
            
        if (unitPriceText != null)
        {
            int displayPrice = currentTransactionType == TransactionType.Buy ?
                ShopManager.instance?.GetBuyPrice(currentShopItem, 1) ?? currentUnitPrice :
                ShopManager.instance?.GetSellPrice(currentItem, 1) ?? Mathf.RoundToInt(currentUnitPrice * 0.6f);
            unitPriceText.text = $"{displayPrice:N0} 골드";
        }
    }
    
    private void SetupQuantityControls()
    {
        bool canAdjust = enableQuantityAdjustment && maxQuantityAvailable > 1;
        
        // Show/hide quantity controls
        if (quantityInputField != null)
        {
            quantityInputField.interactable = canAdjust;
            quantityInputField.text = currentQuantity.ToString();
        }
        
        if (minusButton != null)
            minusButton.interactable = canAdjust && currentQuantity > minQuantity;
            
        if (plusButton != null)
            plusButton.interactable = canAdjust && currentQuantity < maxQuantityAvailable;
            
        if (maxButton != null)
        {
            maxButton.interactable = canAdjust && currentQuantity < maxQuantityAvailable;
            if (maxButtonText != null)
                maxButtonText.text = $"최대 ({maxQuantityAvailable})";
        }
        
        // Quantity label
        if (quantityLabel != null)
            quantityLabel.text = "수량";
    }
    
    // ===== Quantity Adjustment =====
    
    private void DecreaseQuantity()
    {
        if (currentQuantity > minQuantity)
        {
            currentQuantity--;
            UpdateQuantityDisplay();
            PlaySound(adjustQuantitySound);
        }
    }
    
    private void IncreaseQuantity()
    {
        if (currentQuantity < maxQuantityAvailable)
        {
            currentQuantity++;
            UpdateQuantityDisplay();
            PlaySound(adjustQuantitySound);
        }
    }
    
    private void SetMaxQuantity()
    {
        if (currentQuantity != maxQuantityAvailable)
        {
            currentQuantity = maxQuantityAvailable;
            UpdateQuantityDisplay();
            PlaySound(adjustQuantitySound);
        }
    }
    
    private void OnQuantityInputChanged(string input)
    {
        if (int.TryParse(input, out int newQuantity))
        {
            currentQuantity = Mathf.Clamp(newQuantity, minQuantity, maxQuantityAvailable);
            UpdateQuantityDisplay();
        }
        else
        {
            // Reset to current quantity if invalid input
            if (quantityInputField != null)
                quantityInputField.text = currentQuantity.ToString();
        }
    }
    
    private void UpdateQuantityDisplay()
    {
        // Update input field
        if (quantityInputField != null)
            quantityInputField.text = currentQuantity.ToString();
        
        // Update button states
        if (minusButton != null)
            minusButton.interactable = currentQuantity > minQuantity;
            
        if (plusButton != null)
            plusButton.interactable = currentQuantity < maxQuantityAvailable;
            
        if (maxButton != null)
            maxButton.interactable = currentQuantity < maxQuantityAvailable;
        
        // Update price breakdown
        UpdatePriceBreakdown();
    }
    
    // ===== Price Breakdown =====
    
    private void UpdatePriceBreakdown()
    {
        if (priceBreakdownPanel == null) return;
        
        // Calculate prices
        int unitPrice = 0;
        int totalPrice = 0;
        
        if (currentTransactionType == TransactionType.Buy && currentShopItem != null)
        {
            unitPrice = ShopManager.instance?.GetBuyPrice(currentShopItem, 1) ?? currentUnitPrice;
            totalPrice = ShopManager.instance?.GetBuyPrice(currentShopItem, currentQuantity) ?? (unitPrice * currentQuantity);
        }
        else if (currentTransactionType == TransactionType.Sell && currentItem != null)
        {
            unitPrice = ShopManager.instance?.GetSellPrice(currentItem, 1) ?? Mathf.RoundToInt(currentUnitPrice * 0.6f);
            totalPrice = ShopManager.instance?.GetSellPrice(currentItem, currentQuantity) ?? (unitPrice * currentQuantity);
        }
        
        // Update displays
        if (unitPriceRowLabel != null)
            unitPriceRowLabel.text = "단가";
            
        if (unitPriceValue != null)
            unitPriceValue.text = $"{unitPrice:N0} 골드";
            
        if (quantityRowLabel != null)
            quantityRowLabel.text = "수량";
            
        if (quantityValue != null)
            quantityValue.text = $"x {currentQuantity}";
            
        if (totalLabel != null)
            totalLabel.text = currentTransactionType == TransactionType.Buy ? "총 구매 비용" : "총 판매 수익";
            
        if (totalPriceText != null)
        {
            totalPriceText.text = $"{totalPrice:N0} 골드";
            
            // Color based on affordability
            if (currentTransactionType == TransactionType.Buy)
            {
                int playerGold = GoldManager.instance?.GetGold() ?? 0;
                totalPriceText.color = totalPrice > playerGold ? Color.red : Color.white;
            }
        }
        
        // Update confirm button state
        ValidateTransaction();
    }
    
    // ===== Transaction Actions =====
    
    private void ConfirmTransaction()
    {
        if (!ValidateTransaction())
        {
            PlaySound(errorSound);
            return;
        }
        
        // Update the action with new quantity if it was adjusted
        if (enableQuantityAdjustment && currentTransactionType == TransactionType.Buy && currentShopItem != null)
        {
            // Create new action with updated quantity
            currentConfirmAction = () => TransactionManager.instance?.BuyItem(currentShopItem, currentQuantity);
        }
        else if (enableQuantityAdjustment && currentTransactionType == TransactionType.Sell && currentItem != null)
        {
            // Create new action with updated quantity
            currentConfirmAction = () => TransactionManager.instance?.SellItem(currentItem, currentQuantity);
        }
        
        // Play sound
        PlaySound(confirmSound);
        
        // Execute transaction
        currentConfirmAction?.Invoke();
        
        // Hide confirmation
        HideConfirmation();
        
        Debug.Log($"Transaction confirmed: {currentTransactionType} x{currentQuantity}");
    }
    
    private void CancelTransaction()
    {
        // Play sound
        PlaySound(cancelSound);
        
        // Hide confirmation
        HideConfirmation();
        
        Debug.Log("Transaction cancelled");
    }
    
    private bool ValidateTransaction()
    {
        bool isValid = true;
        
        if (currentTransactionType == TransactionType.Buy)
        {
            // Check gold
            int playerGold = GoldManager.instance?.GetGold() ?? 0;
            int totalCost = 0;
            
            if (currentShopItem != null)
            {
                totalCost = ShopManager.instance?.GetBuyPrice(currentShopItem, currentQuantity) ?? (currentUnitPrice * currentQuantity);
            }
            
            if (playerGold < totalCost)
            {
                isValid = false;
                if (confirmButton != null)
                    confirmButton.interactable = false;
            }
            else
            {
                if (confirmButton != null)
                    confirmButton.interactable = true;
            }
        }
        else if (currentTransactionType == TransactionType.Sell)
        {
            // Check inventory
            if (currentItem != null)
            {
                int playerQuantity = InventoryManager.instance?.GetItemQuantity(currentItem) ?? 0;
                isValid = playerQuantity >= currentQuantity;
                
                if (confirmButton != null)
                    confirmButton.interactable = isValid;
            }
        }
        
        return isValid;
    }
    
    // ===== Show/Hide =====
    
    private void ShowConfirmation()
    {
        if (modalBackground != null)
            modalBackground.SetActive(true);
            
        if (confirmationWindow != null)
            confirmationWindow.SetActive(true);
        
        // Simple show without animation
        if (canvasGroup != null)
        {
            canvasGroup.alpha = 1f;
        }
        
        if (confirmationWindow != null)
        {
            confirmationWindow.transform.localScale = Vector3.one;
        }
    }
    
    public void HideConfirmation()
    {
        // Simple hide without animation
        if (modalBackground != null)
            modalBackground.SetActive(false);
        if (confirmationWindow != null)
            confirmationWindow.SetActive(false);
        
        // Clear current transaction data
        currentConfirmAction = null;
        currentItem = null;
        currentShopItem = null;
        enableQuantityAdjustment = true; // Reset for next use
    }
    
    // ===== Utility =====
    
    private void PlaySound(AudioClip clip)
    {
        if (audioSource != null && clip != null)
        {
            audioSource.PlayOneShot(clip);
        }
    }
}