using UnityEngine;
using UnityEngine.UI;
using TMPro;

public class InventorySlot : MonoBehaviour
{
    [Header("UI References")]
    public Image itemIcon;              // 인스펙터에서 직접 연결
    public TextMeshProUGUI itemCountText;   // 인스펙터에서 직접 연결
    
    // 현재 표시 중인 데이터 (중복 업데이트 방지용)
    private MaterialData currentItemData;
    private int currentCount;
    
    // ===== 아이템 설정 =====
    public void SetItem(MaterialData itemData, int count)
    {
        // null 체크
        if (itemData == null)
        {
            Debug.LogError("InventorySlot.SetItem: itemData is null!");
            return;
        }
        
        // 동일한 데이터면 업데이트 스킵 (성능 최적화)
        if (currentItemData == itemData && currentCount == count)
        {
            return;
        }
        
        currentItemData = itemData;
        currentCount = count;
        
        // 아이콘 업데이트
        if (itemIcon != null)
        {
            if (itemData.materialIcon != null)
            {
                itemIcon.sprite = itemData.materialIcon;
                itemIcon.enabled = true;
                itemIcon.color = Color.white;  // 투명도 복원
            }
            else
            {
                // 아이콘이 없는 경우 기본 처리
                itemIcon.enabled = false;
                Debug.LogWarning($"InventorySlot: {itemData.materialName}의 아이콘이 없습니다!");
            }
        }
        else
        {
            Debug.LogError("InventorySlot: itemIcon이 연결되지 않았습니다!");
        }
        
        // 텍스트 업데이트
        if (itemCountText != null)
        {
            itemCountText.text = $"{itemData.materialName} x {count}";
        }
        else
        {
            Debug.LogError("InventorySlot: itemCountText가 연결되지 않았습니다!");
        }
    }
    
    // ===== 슬롯 초기화 (풀로 반환될 때 호출) =====
    public void Clear()
    {
        currentItemData = null;
        currentCount = 0;
        
        if (itemIcon != null)
        {
            itemIcon.sprite = null;
            itemIcon.enabled = false;
            itemIcon.color = Color.white;
        }
        
        if (itemCountText != null)
        {
            itemCountText.text = "";
        }
    }
    
    // ===== 현재 아이템 정보 가져오기 =====
    public MaterialData GetItemData()
    {
        return currentItemData;
    }
    
    public int GetCount()
    {
        return currentCount;
    }
    
    public bool HasItem()
    {
        return currentItemData != null;
    }
    
    // ===== UI 효과 메서드들 =====
    
    // 하이라이트 효과 (마우스 오버 등)
    public void SetHighlight(bool highlighted)
    {
        if (itemIcon != null)
        {
            // 노란색 틴트 효과
            itemIcon.color = highlighted ? new Color(1f, 1f, 0.7f, 1f) : Color.white;
        }
    }
    
    // 선택 애니메이션
    public void PlaySelectAnimation()
    {
        StopAllCoroutines();  // 기존 애니메이션 중지
        StartCoroutine(SelectAnimationCoroutine());
    }
    
    private System.Collections.IEnumerator SelectAnimationCoroutine()
    {
        Vector3 originalScale = transform.localScale;
        
        // 확대
        float elapsed = 0f;
        float duration = 0.1f;
        
        while (elapsed < duration)
        {
            elapsed += Time.deltaTime;
            float t = elapsed / duration;
            float scale = Mathf.Lerp(1f, 1.1f, t);
            transform.localScale = originalScale * scale;
            yield return null;
        }
        
        // 축소
        elapsed = 0f;
        while (elapsed < duration)
        {
            elapsed += Time.deltaTime;
            float t = elapsed / duration;
            float scale = Mathf.Lerp(1.1f, 1f, t);
            transform.localScale = originalScale * scale;
            yield return null;
        }
        
        transform.localScale = originalScale;
    }
    
    // ===== 디버그용 =====
    public void LogSlotInfo()
    {
        if (currentItemData != null)
        {
            Debug.Log($"Slot: {currentItemData.materialName} x{currentCount}");
        }
        else
        {
            Debug.Log("Slot: Empty");
        }
    }
    
    // ===== Unity 이벤트 =====
    void OnDestroy()
    {
        // 정리 작업이 필요한 경우
        Clear();
    }
}