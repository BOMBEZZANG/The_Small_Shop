using UnityEngine;
using UnityEngine.UI;
using TMPro;
using System.Collections.Generic;
using System.Linq;

public class ShopCategoryUI : MonoBehaviour
{
    [Header("UI References")]
    [SerializeField] private GameObject categoryPanel;
    [SerializeField] private Transform categoryButtonContainer;
    [SerializeField] private Button categoryButtonPrefab;
    [SerializeField] private ScrollRect categoryScrollRect;
    
    [Header("All Categories Button")]
    [SerializeField] private Button showAllButton;
    [SerializeField] private TextMeshProUGUI showAllButtonText;
    [SerializeField] private string showAllText = "전체";
    
    [Header("Selected Category Display")]
    [SerializeField] private TextMeshProUGUI selectedCategoryText;
    [SerializeField] private Image selectedCategoryIcon;
    [SerializeField] private TextMeshProUGUI categoryDescriptionText;
    
    [Header("Category Counters")]
    [SerializeField] private bool showItemCounts = true;
    [SerializeField] private TextMeshProUGUI totalItemsText;
    
    [Header("Visual Settings")]
    [SerializeField] private Color selectedColor = Color.white;
    [SerializeField] private Color unselectedColor = Color.gray;
    [SerializeField] private Color disabledColor = Color.red;
    [SerializeField] private bool enableCategoryIcons = true;
    
    [Header("Animation")]
    [SerializeField] private bool enableCategoryAnimation = true;
    [SerializeField] private float animationDuration = 0.2f;
    
    // Current state
    private List<ShopCategoryData> availableCategories = new List<ShopCategoryData>();
    private ShopCategoryData selectedCategory = null;
    private List<Button> categoryButtons = new List<Button>();
    
    // Events
    public System.Action<ShopCategoryData> OnCategorySelected;
    public System.Action OnShowAllSelected;
    
    // Properties
    public ShopCategoryData SelectedCategory => selectedCategory;
    public bool HasCategories => availableCategories.Count > 0;
    public int CategoryCount => availableCategories.Count;
    
    void Awake()
    {
        SetupUI();
    }
    
    void Start()
    {
        ShopDebugLogger.Log("=== SHOP CATEGORY UI STARTED ===", "ShopCategoryUI");
        
        // Check all critical references
        ShopDebugLogger.Log($"selectedCategoryText: {(selectedCategoryText != null ? "Assigned" : "NULL")}", "ShopCategoryUI");
        ShopDebugLogger.Log($"selectedCategoryIcon: {(selectedCategoryIcon != null ? "Assigned" : "NULL")}", "ShopCategoryUI");
        ShopDebugLogger.Log($"categoryDescriptionText: {(categoryDescriptionText != null ? "Assigned" : "NULL")}", "ShopCategoryUI");
        ShopDebugLogger.Log($"showAllButton: {(showAllButton != null ? "Assigned" : "NULL")}", "ShopCategoryUI");
        ShopDebugLogger.Log($"showAllButtonText: {(showAllButtonText != null ? "Assigned" : "NULL")}", "ShopCategoryUI");
        ShopDebugLogger.Log($"categoryButtonPrefab: {(categoryButtonPrefab != null ? "Assigned" : "NULL")}", "ShopCategoryUI");
        ShopDebugLogger.Log($"categoryButtonContainer: {(categoryButtonContainer != null ? "Assigned" : "NULL")}", "ShopCategoryUI");
        ShopDebugLogger.Log($"categoryPanel: {(categoryPanel != null ? "Assigned" : "NULL")}", "ShopCategoryUI");
        
        if (selectedCategoryText == null)
            ShopDebugLogger.LogError("selectedCategoryText is NULL! This will prevent category names from showing!", "ShopCategoryUI");
    }
    
    private void SetupUI()
    {
        // Setup show all button
        if (showAllButton != null)
        {
            showAllButton.onClick.AddListener(SelectShowAll);
        }
        
        if (showAllButtonText != null)
        {
            showAllButtonText.text = showAllText;
        }
    }

    // ===== Category Setup =====

    public void SetupCategories(List<ShopCategoryData> categories)
    {
        ShopDebugLogger.Log("=== SETUP CATEGORIES CALLED ===", "ShopCategoryUI");
        ShopDebugLogger.Log($"Received {categories?.Count ?? 0} categories", "ShopCategoryUI");
        
        if (categories == null)
        {
            ShopDebugLogger.LogError("Categories list is NULL!", "ShopCategoryUI");
            return;
        }
        
        // Log each category before filtering
        for (int i = 0; i < categories.Count; i++)
        {
            if (categories[i] != null)
            {
                ShopDebugLogger.Log($"Input Category [{i}]: {categories[i].categoryName}, ShowInUI: {categories[i].showInShopUI}", "ShopCategoryUI");
            }
        }
        
        // Filter and sort categories
        availableCategories = categories
            .Where(cat => cat != null && cat.showInShopUI)
            .OrderBy(cat => cat.sortOrder)
            .ThenBy(cat => cat.categoryName)
            .ToList();

        ShopDebugLogger.Log($"After filtering: {availableCategories.Count} available categories", "ShopCategoryUI");

        // Create category buttons
        CreateCategoryButtons();

        // Show/hide panel based on available categories
        bool hasCategories = availableCategories.Count > 0;
        if (categoryPanel != null)
        {
            categoryPanel.SetActive(hasCategories);
            ShopDebugLogger.Log($"Category panel set to: {(hasCategories ? "ACTIVE" : "INACTIVE")}", "ShopCategoryUI");
        }
        else
        {
            ShopDebugLogger.LogError("categoryPanel reference is NULL!", "ShopCategoryUI");
        }

        // Select "Show All" by default
        if (hasCategories)
        {
            ShopDebugLogger.Log("Selecting 'Show All' by default", "ShopCategoryUI");
            SelectShowAll();
        }
    }
    
    private void CreateCategoryButtons()
    {
        ShopDebugLogger.Log("=== CREATE CATEGORY BUTTONS ===", "ShopCategoryUI");
        
        if (categoryButtonContainer == null)
        {
            ShopDebugLogger.LogError("categoryButtonContainer is NULL! Cannot create buttons!", "ShopCategoryUI");
            return;
        }
        
        if (categoryButtonPrefab == null)
        {
            ShopDebugLogger.LogError("categoryButtonPrefab is NULL! Cannot create buttons!", "ShopCategoryUI");
            return;
        }
        
        ShopDebugLogger.Log($"Container: {categoryButtonContainer.name}, Prefab: {categoryButtonPrefab.name}", "ShopCategoryUI");
        
        // Clear existing buttons
        ClearCategoryButtons();
        
        // Create button for each category
        ShopDebugLogger.Log($"Creating {availableCategories.Count} category buttons", "ShopCategoryUI");
        foreach (var category in availableCategories)
        {
            CreateCategoryButton(category);
        }
        
        ShopDebugLogger.Log($"Created {categoryButtons.Count} buttons total", "ShopCategoryUI");
        
        // Update layout
        if (categoryScrollRect != null)
        {
            Canvas.ForceUpdateCanvases();
        }
    }
    
    private void CreateCategoryButton(ShopCategoryData category)
    {
        ShopDebugLogger.Log($"Creating button for category: {category.categoryName}", "ShopCategoryUI");
        
        GameObject buttonObj = Instantiate(categoryButtonPrefab.gameObject, categoryButtonContainer);
        
        // Force activate the button if it's inactive
        if (!buttonObj.activeSelf)
        {
            buttonObj.SetActive(true);
            ShopDebugLogger.Log($"Activated button GameObject: {buttonObj.name}", "ShopCategoryUI");
        }
        
        Button button = buttonObj.GetComponent<Button>();
        
        if (button != null)
        {
            ShopDebugLogger.Log($"Button created successfully: {buttonObj.name}", "ShopCategoryUI");
            
            // Setup button click
            button.onClick.AddListener(() => SelectCategory(category));
            
            // Setup button appearance
            SetupCategoryButtonAppearance(button, category);
            
            categoryButtons.Add(button);
        }
        else
        {
            ShopDebugLogger.LogError($"Failed to get Button component from instantiated prefab!", "ShopCategoryUI");
        }
    }
    
    private void SetupCategoryButtonAppearance(Button button, ShopCategoryData category)
    {
        ShopDebugLogger.Log($"Setting up appearance for button: {category.categoryName}", "ShopCategoryUI");
        
        // Find components in button
        var texts = button.GetComponentsInChildren<TextMeshProUGUI>();
        var images = button.GetComponentsInChildren<Image>();
        
        ShopDebugLogger.Log($"Found {texts.Length} text components and {images.Length} image components", "ShopCategoryUI");
        
        // Setup text (first text component is usually the label)
        if (texts.Length > 0)
        {
            texts[0].text = category.categoryName;
            ShopDebugLogger.Log($"Set button text[0] to: '{category.categoryName}'", "ShopCategoryUI");
        }
        else
        {
            ShopDebugLogger.LogError("No TextMeshProUGUI components found in button prefab!", "ShopCategoryUI");
        }
        
        // Setup icon (look for image that's not the button background)
        if (enableCategoryIcons && category.categoryIcon != null)
        {
            var iconImage = images.FirstOrDefault(img => img != button.image);
            if (iconImage != null)
            {
                iconImage.sprite = category.categoryIcon;
                iconImage.color = category.categoryColor;
                ShopDebugLogger.Log("Category icon set", "ShopCategoryUI");
            }
        }
        
        // Setup item count if enabled
        if (showItemCounts && texts.Length > 1)
        {
            int itemCount = GetCategoryItemCount(category);
            texts[1].text = $"({itemCount})";
            ShopDebugLogger.Log($"Set item count text to: '({itemCount})'", "ShopCategoryUI");
        }
        
        // Store category reference
        var categoryRef = button.gameObject.AddComponent<CategoryReference>();
        categoryRef.category = category;
        
        // Debug button visibility
        var rectTransform = button.GetComponent<RectTransform>();
        if (rectTransform != null)
        {
            ShopDebugLogger.Log($"Button position: {rectTransform.anchoredPosition}, Size: {rectTransform.sizeDelta}, Scale: {rectTransform.localScale}", "ShopCategoryUI");
        }
    }
    
    private void ClearCategoryButtons()
    {
        ShopDebugLogger.Log($"Clearing {categoryButtons.Count} existing buttons", "ShopCategoryUI");
        
        foreach (var button in categoryButtons)
        {
            if (button != null)
            {
                Destroy(button.gameObject);
            }
        }
        
        categoryButtons.Clear();
        
        // Also clear any remaining children in container (in case of orphaned buttons)
        if (categoryButtonContainer != null)
        {
            int childCount = categoryButtonContainer.childCount;
            for (int i = childCount - 1; i >= 0; i--)
            {
                var child = categoryButtonContainer.GetChild(i);
                if (child.name.Contains("CategoryButtonPrefab"))
                {
                    ShopDebugLogger.Log($"Destroying orphaned button: {child.name}", "ShopCategoryUI");
                    Destroy(child.gameObject);
                }
            }
        }
    }
    
    // ===== Category Selection =====
    
    public void SelectCategory(ShopCategoryData category)
    {
        if (selectedCategory == category) return;
        
        selectedCategory = category;
        
        // Update button visuals
        UpdateCategoryButtonVisuals();
        
        // Update selected category display
        UpdateSelectedCategoryDisplay();
        
        // Update item counts
        UpdateItemCounts();
        
        // Fire event
        OnCategorySelected?.Invoke(selectedCategory);
        
        // Animate selection if enabled
        if (enableCategoryAnimation)
        {
            AnimateCategorySelection();
        }
        
        Debug.Log($"Selected category: {category?.categoryName ?? "None"}");
    }
    
    public void SelectShowAll()
    {
        selectedCategory = null;
        
        // Update button visuals
        UpdateCategoryButtonVisuals();
        
        // Update selected category display
        UpdateSelectedCategoryDisplay();
        
        // Update item counts
        UpdateItemCounts();
        
        // Fire event
        OnShowAllSelected?.Invoke();
        
        Debug.Log("Selected: Show All Categories");
    }
    
    // ===== Visual Updates =====
    
    private void UpdateCategoryButtonVisuals()
    {
        // Update show all button
        if (showAllButton != null)
        {
            var colors = showAllButton.colors;
            colors.normalColor = selectedCategory == null ? selectedColor : unselectedColor;
            showAllButton.colors = colors;
        }
        
        // Update category buttons
        foreach (var button in categoryButtons)
        {
            if (button != null)
            {
                var categoryRef = button.GetComponent<CategoryReference>();
                if (categoryRef != null)
                {
                    bool isSelected = categoryRef.category == selectedCategory;
                    bool isAvailable = IsCategoryAvailable(categoryRef.category);
                    
                    var colors = button.colors;
                    
                    if (!isAvailable)
                    {
                        colors.normalColor = disabledColor;
                        button.interactable = false;
                    }
                    else
                    {
                        colors.normalColor = isSelected ? selectedColor : unselectedColor;
                        button.interactable = true;
                    }
                    
                    button.colors = colors;
                }
            }
        }
    }
    
    private void UpdateSelectedCategoryDisplay()
    {
        ShopDebugLogger.Log("=== UPDATE SELECTED CATEGORY DISPLAY ===", "ShopCategoryUI");
        ShopDebugLogger.Log($"Current selected category: {selectedCategory?.categoryName ?? "None (Show All)"}", "ShopCategoryUI");
        
        if (selectedCategoryText != null)
        {
            string textToSet = selectedCategory?.categoryName ?? showAllText;
            selectedCategoryText.text = textToSet;
            ShopDebugLogger.Log($"Set selectedCategoryText to: '{textToSet}'", "ShopCategoryUI");
        }
        else
        {
            ShopDebugLogger.LogError("selectedCategoryText is NULL - cannot update display!", "ShopCategoryUI");
        }
        
        if (selectedCategoryIcon != null)
        {
            if (selectedCategory?.categoryIcon != null)
            {
                selectedCategoryIcon.sprite = selectedCategory.categoryIcon;
                selectedCategoryIcon.color = selectedCategory.categoryColor;
                selectedCategoryIcon.gameObject.SetActive(true);
                ShopDebugLogger.Log("Category icon set and activated", "ShopCategoryUI");
            }
            else
            {
                selectedCategoryIcon.gameObject.SetActive(false);
                ShopDebugLogger.Log("Category icon deactivated (no icon)", "ShopCategoryUI");
            }
        }
        
        if (categoryDescriptionText != null)
        {
            string descText = selectedCategory?.categoryDescription ?? "모든 카테고리의 아이템을 표시합니다.";
            categoryDescriptionText.text = descText;
            ShopDebugLogger.Log($"Set category description to: '{descText}'", "ShopCategoryUI");
        }
    }
    
    private void UpdateItemCounts()
    {
        if (!showItemCounts) return;
        
        // Update total items text
        if (totalItemsText != null)
        {
            int totalItems = GetCurrentlyVisibleItemCount();
            string totalText = selectedCategory != null ? 
                $"{selectedCategory.categoryName}: {totalItems}개 아이템" :
                $"전체: {totalItems}개 아이템";
            totalItemsText.text = totalText;
        }
        
        // Update individual category button counts
        foreach (var button in categoryButtons)
        {
            var categoryRef = button.GetComponent<CategoryReference>();
            if (categoryRef != null)
            {
                var texts = button.GetComponentsInChildren<TextMeshProUGUI>();
                if (texts.Length > 1)
                {
                    int itemCount = GetCategoryItemCount(categoryRef.category);
                    texts[1].text = $"({itemCount})";
                }
            }
        }
    }
    
    // ===== Animation =====
    
    private void AnimateCategorySelection()
    {
        if (!enableCategoryAnimation) return;
        
        // Find the selected button and animate it
        foreach (var button in categoryButtons)
        {
            var categoryRef = button.GetComponent<CategoryReference>();
            if (categoryRef != null && categoryRef.category == selectedCategory)
            {
                // Simple scale animation
                StartCoroutine(AnimateButtonScale(button.transform));
                break;
            }
        }
        
        // Also animate show all button if selected
        if (selectedCategory == null && showAllButton != null)
        {
            StartCoroutine(AnimateButtonScale(showAllButton.transform));
        }
    }
    
    private System.Collections.IEnumerator AnimateButtonScale(Transform buttonTransform)
    {
        Vector3 originalScale = buttonTransform.localScale;
        Vector3 targetScale = originalScale * 1.1f;
        
        float elapsedTime = 0f;
        
        // Scale up
        while (elapsedTime < animationDuration / 2)
        {
            elapsedTime += Time.deltaTime;
            float progress = elapsedTime / (animationDuration / 2);
            buttonTransform.localScale = Vector3.Lerp(originalScale, targetScale, progress);
            yield return null;
        }
        
        elapsedTime = 0f;
        
        // Scale back down
        while (elapsedTime < animationDuration / 2)
        {
            elapsedTime += Time.deltaTime;
            float progress = elapsedTime / (animationDuration / 2);
            buttonTransform.localScale = Vector3.Lerp(targetScale, originalScale, progress);
            yield return null;
        }
        
        buttonTransform.localScale = originalScale;
    }
    
    // ===== Utility Methods =====
    
    private int GetCategoryItemCount(ShopCategoryData category)
    {
        if (category == null) return 0;
        
        // Count items based on current shop context
        if (ShopManager.instance != null && ShopManager.instance.IsShopOpen)
        {
            var items = ShopManager.instance.GetItemsByCategory(category, true); // For selling
            var buyItems = ShopManager.instance.GetItemsByCategory(category, false); // For buying
            
            // Return the count based on current context (you might want to adapt this)
            return Mathf.Max(items.Count, buyItems.Count);
        }
        
        return category.categoryItems.Count;
    }
    
    private int GetCurrentlyVisibleItemCount()
    {
        if (selectedCategory != null)
        {
            return GetCategoryItemCount(selectedCategory);
        }
        else
        {
            // Count all items
            int total = 0;
            foreach (var category in availableCategories)
            {
                total += GetCategoryItemCount(category);
            }
            return total;
        }
    }
    
    private bool IsCategoryAvailable(ShopCategoryData category)
    {
        if (category == null) return false;
        
        // Check if category has any items available
        return GetCategoryItemCount(category) > 0;
    }
    
    // ===== Public Interface =====
    
    public void RefreshCategories()
    {
        UpdateCategoryButtonVisuals();
        UpdateItemCounts();
    }
    
    public void SetShowItemCounts(bool show)
    {
        showItemCounts = show;
        UpdateItemCounts();
    }
    
    public void SetAnimationEnabled(bool enabled)
    {
        enableCategoryAnimation = enabled;
    }
    
    public List<ShopCategoryData> GetAvailableCategories()
    {
        return new List<ShopCategoryData>(availableCategories);
    }
    
    // ===== Debug Methods =====
    
    [ContextMenu("Debug - Refresh Categories")]
    public void DebugRefreshCategories()
    {
        RefreshCategories();
    }
    
    [ContextMenu("Debug - Select First Category")]
    public void DebugSelectFirstCategory()
    {
        if (availableCategories.Count > 0)
        {
            SelectCategory(availableCategories[0]);
        }
    }
    
    [ContextMenu("Debug - Select Show All")]
    public void DebugSelectShowAll()
    {
        SelectShowAll();
    }
    
    [ContextMenu("Debug - Print Category Info")]
    public void DebugPrintCategoryInfo()
    {
        Debug.Log($"=== Category UI Info ===\n" +
                 $"Available Categories: {availableCategories.Count}\n" +
                 $"Selected Category: {selectedCategory?.categoryName ?? "Show All"}\n" +
                 $"Category Buttons: {categoryButtons.Count}\n" +
                 $"Show Item Counts: {showItemCounts}");
        
        foreach (var category in availableCategories)
        {
            int itemCount = GetCategoryItemCount(category);
            Debug.Log($"- {category.categoryName}: {itemCount} items");
        }
    }
}

// Helper component to store category reference on buttons
public class CategoryReference : MonoBehaviour
{
    public ShopCategoryData category;
}