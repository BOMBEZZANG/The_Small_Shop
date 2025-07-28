using UnityEngine;

public class TreeHarvest : MonoBehaviour
{
    [Header("Harvest Rewards")]
    [SerializeField] private MaterialData woodItem;  // 나무 아이템
    [SerializeField] private int minWoodAmount = 2;
    [SerializeField] private int maxWoodAmount = 5;
    [SerializeField] private int expReward = 10;
    
    [Header("Visual Effects")]
    [SerializeField] private GameObject harvestEffect; // 파티클 효과
    [SerializeField] private Sprite harvestedSprite;   // 벤 나무 스프라이트
    
    private InteractableObject interactable;
    private SpriteRenderer spriteRenderer;
    private Sprite originalSprite;
    
    void Awake()
    {
        interactable = GetComponent<InteractableObject>();
        spriteRenderer = GetComponent<SpriteRenderer>();
        originalSprite = spriteRenderer.sprite;
        
        // 이벤트 연결
        interactable.OnInteractionComplete.AddListener(OnHarvestComplete);
    }
    
    private void OnHarvestComplete(PlayerController player)
    {
        // 나무 아이템 지급
        int woodAmount = Random.Range(minWoodAmount, maxWoodAmount + 1);
        for (int i = 0; i < woodAmount; i++)
        {
            InventoryManager.instance.AddItem(woodItem);
        }
        
        // 경험치 지급
        PlayerDataManager.instance.AddExp(expReward);
        
        // 시각 효과
        if (harvestEffect != null)
        {
            Instantiate(harvestEffect, transform.position, Quaternion.identity);
        }
        
        // 벤 나무로 스프라이트 변경
        if (harvestedSprite != null)
        {
            spriteRenderer.sprite = harvestedSprite;
        }
        
        // 5분 후 원래대로 복구
        Invoke("RegrowTree", 300f);
    }
    
    private void RegrowTree()
    {
        spriteRenderer.sprite = originalSprite;
        Debug.Log("나무가 다시 자랐습니다!");
    }
}