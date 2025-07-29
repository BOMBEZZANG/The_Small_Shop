using UnityEngine;
using UnityEditor;

#if UNITY_EDITOR
public class NPCFactory : MonoBehaviour
{
    [MenuItem("GameObject/2D Object/NPC/Basic NPC", false, 10)]
    static void CreateBasicNPC()
    {
        // 기본 GameObject 생성
        GameObject npcObject = new GameObject("New NPC");
        
        // 필수 컴포넌트 추가
        // Sprite Renderer
        SpriteRenderer spriteRenderer = npcObject.AddComponent<SpriteRenderer>();
        spriteRenderer.sortingLayerName = "Characters"; // 레이어가 있다면
        
        // Rigidbody2D
        Rigidbody2D rb = npcObject.AddComponent<Rigidbody2D>();
        rb.gravityScale = 0f;
        rb.freezeRotation = true;
        
        // Collider
        CapsuleCollider2D collider = npcObject.AddComponent<CapsuleCollider2D>();
        collider.size = new Vector2(1f, 2f);
        collider.offset = new Vector2(0f, 1f);
        collider.isTrigger = true;
        
        // Animator
        npcObject.AddComponent<Animator>();
        
        // 인디케이터 생성
        GameObject indicatorHolder = new GameObject("Indicators");
        indicatorHolder.transform.SetParent(npcObject.transform);
        indicatorHolder.transform.localPosition = new Vector3(0, 2.5f, 0);
        
        // NPCIndicator 추가
        NPCIndicator indicator = indicatorHolder.AddComponent<NPCIndicator>();
        
        // 아이콘들 생성
        CreateIndicatorIcon(indicatorHolder.transform, "Exclamation", "!");
        CreateIndicatorIcon(indicatorHolder.transform, "Question", "?");
        CreateIndicatorIcon(indicatorHolder.transform, "Dots", "...");
        CreateIndicatorIcon(indicatorHolder.transform, "Heart", "♥");
        
        // 선택
        Selection.activeGameObject = npcObject;
        
        Debug.Log("기본 NPC가 생성되었습니다. NPCController를 추가하고 NPCData를 설정하세요.");
    }
    
    static GameObject CreateIndicatorIcon(Transform parent, string name, string text)
    {
        // Canvas 생성 (World Space)
        GameObject canvasObj = new GameObject(name);
        canvasObj.transform.SetParent(parent);
        canvasObj.transform.localPosition = Vector3.zero;
        
        Canvas canvas = canvasObj.AddComponent<Canvas>();
        canvas.renderMode = RenderMode.WorldSpace;
        canvas.sortingLayerName = "UI";
        
        RectTransform canvasRect = canvas.GetComponent<RectTransform>();
        canvasRect.sizeDelta = new Vector2(1, 1);
        
        // Text 생성
        GameObject textObj = new GameObject("Text");
        textObj.transform.SetParent(canvasObj.transform);
        
        RectTransform textRect = textObj.AddComponent<RectTransform>();
        textRect.anchorMin = Vector2.zero;
        textRect.anchorMax = Vector2.one;
        textRect.sizeDelta = Vector2.zero;
        textRect.anchoredPosition = Vector2.zero;
        
        UnityEngine.UI.Text textComponent = textObj.AddComponent<UnityEngine.UI.Text>();
        textComponent.text = text;
        textComponent.fontSize = 32;
        textComponent.alignment = TextAnchor.MiddleCenter;
        textComponent.color = Color.yellow;
        
        // 기본적으로 숨김
        canvasObj.SetActive(false);
        
        return canvasObj;
    }
}
#endif

// NPC 생성 시 필요한 설정을 도와주는 컴포넌트
[AddComponentMenu("NPC/NPC Setup Helper")]
public class NPCSetupHelper : MonoBehaviour
{
    [Header("Quick Setup")]
    public NPCType npcType = NPCType.Resident;
    public NPCData npcDataToApply;
    
    [Header("Components Check")]
    [SerializeField] private bool hasRequiredComponents = false;
    
    [ContextMenu("Setup NPC Components")]
    public void SetupComponents()
    {
        // 필수 컴포넌트 확인 및 추가
        if (!GetComponent<Rigidbody2D>())
        {
            Rigidbody2D rb = gameObject.AddComponent<Rigidbody2D>();
            rb.gravityScale = 0f;
            rb.freezeRotation = true;
        }
        
        if (!GetComponent<Collider2D>())
        {
            CapsuleCollider2D col = gameObject.AddComponent<CapsuleCollider2D>();
            col.size = new Vector2(1f, 2f);
            col.offset = new Vector2(0f, 1f);
            col.isTrigger = true;
        }
        
        if (!GetComponent<SpriteRenderer>())
        {
            gameObject.AddComponent<SpriteRenderer>();
        }
        
        if (!GetComponent<Animator>())
        {
            gameObject.AddComponent<Animator>();
        }
        
        // 적절한 Controller 추가
        NPCController existingController = GetComponent<NPCController>();
        if (existingController != null)
        {
            Debug.Log("NPCController가 이미 있습니다.");
        }
        else
        {
            switch (npcType)
            {
                case NPCType.Resident:
                    Debug.Log("ResidentNPCController를 추가하세요.");
                    break;
                case NPCType.Visitor:
                    Debug.Log("VisitorNPCController를 추가하세요.");
                    break;
                default:
                    Debug.Log("적절한 NPCController를 추가하세요.");
                    break;
            }
        }
        
        hasRequiredComponents = true;
        Debug.Log("NPC 컴포넌트 설정 완료!");
    }
    
    [ContextMenu("Apply NPC Data")]
    public void ApplyNPCData()
    {
        if (npcDataToApply == null)
        {
            Debug.LogError("NPCData가 설정되지 않았습니다!");
            return;
        }
        
        NPCController controller = GetComponent<NPCController>();
        if (controller == null)
        {
            Debug.LogError("NPCController가 없습니다!");
            return;
        }
        
        // Reflection을 사용해 private field 설정 (에디터 전용)
        #if UNITY_EDITOR
        var field = typeof(NPCController).GetField("npcData", 
            System.Reflection.BindingFlags.NonPublic | 
            System.Reflection.BindingFlags.Instance);
        if (field != null)
        {
            field.SetValue(controller, npcDataToApply);
            Debug.Log($"NPCData '{npcDataToApply.npcName}' 적용 완료!");
        }
        #endif
    }
}