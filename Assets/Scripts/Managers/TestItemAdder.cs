using UnityEngine;

public class TestItemAdder : MonoBehaviour
{
    // --- 모든 코드는 이 중괄호 안에 있어야 합니다 ---
    public MaterialData itemToAdd;

    void Start()
    {
        InventoryManager.instance.AddItem(itemToAdd);
    }
}