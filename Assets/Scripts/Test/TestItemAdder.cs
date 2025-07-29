using UnityEngine;

public class TestItemAdder : MonoBehaviour
{
    // 배열로 여러 아이템을 담을 수 있게 수정
    public MaterialData[] itemsToAdd;
    
    void Start()
    {
        // 배열의 모든 아이템을 추가
        foreach (MaterialData item in itemsToAdd)
        {
            if (item != null)
            {
                InventoryManager.instance.AddItem(item);
            }
        }
    }
}