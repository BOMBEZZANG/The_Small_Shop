using UnityEngine;

[RequireComponent(typeof(InteractableObject))]
public class CollectableItem : MonoBehaviour
{
    [Header("Item Settings")]
    [SerializeField] private MaterialData itemData;  // 수집할 아이템 데이터
    [SerializeField] private int itemAmount = 1;     // 수집 개수
    
    [Header("Collection Effects")]
    [SerializeField] private GameObject collectEffect;     // 수집 시 파티클 효과
    [SerializeField] private AudioClip collectSound;       // 수집 사운드
    [SerializeField] private float effectDuration = 1f;    // 효과 지속 시간
    
    [Header("Visual")]
    [SerializeField] private bool autoRotate = true;           // 자동 회전 여부
    [SerializeField] private float rotationSpeed = 50f;        // 회전 속도
    [SerializeField] private bool floatAnimation = true;       // 위아래 움직임
    [SerializeField] private float floatHeight = 0.2f;         // 움직임 높이
    [SerializeField] private float floatSpeed = 2f;            // 움직임 속도
    
    private InteractableObject interactable;
    private Vector3 startPosition;
    private float floatTimer = 0f;
    
    void Awake()
    {
        // InteractableObject 컴포넌트 가져오기
        interactable = GetComponent<InteractableObject>();
        
        // 수집 타입으로 설정 (인스펙터에서도 설정 가능)
        // interactable의 interactionType은 private이므로 인스펙터에서 설정
        
        // 시작 위치 저장 (부유 애니메이션용)
        startPosition = transform.position;
        
        // 이벤트 연결
        interactable.OnInteractionComplete.AddListener(OnCollect);
        
        // 아이템 데이터 검증
        if (itemData == null)
        {
            Debug.LogError($"{gameObject.name}: MaterialData가 설정되지 않았습니다!");
        }
        
        // 스프라이트 자동 설정 (선택사항)
        SetupVisual();
    }
    
    void Update()
    {
        // 자동 회전
        if (autoRotate)
        {
            transform.Rotate(Vector3.up, rotationSpeed * Time.deltaTime);
        }
        
        // 위아래 부유 애니메이션
        if (floatAnimation)
        {
            floatTimer += Time.deltaTime * floatSpeed;
            float newY = startPosition.y + Mathf.Sin(floatTimer) * floatHeight;
            transform.position = new Vector3(transform.position.x, newY, transform.position.z);
        }
    }
    
    // 아이템 수집 처리
    private void OnCollect(PlayerController player)
    {
        if (itemData == null)
        {
            Debug.LogError("수집할 아이템 데이터가 없습니다!");
            return;
        }
        
        // 인벤토리에 아이템 추가
        for (int i = 0; i < itemAmount; i++)
        {
            InventoryManager.instance.AddItem(itemData);
        }
        
        // 수집 효과 재생
        PlayCollectEffects();
        
        // UI 알림 (선택사항)
        if (UIManager.instance != null)
        {
            string message = itemAmount > 1 
                ? $"{itemData.materialName} x{itemAmount} 획득!" 
                : $"{itemData.materialName} 획득!";
            UIManager.instance.ShowNotification(message, 2f);
        }
        
        // 오브젝트 제거
        // InteractableObject가 isReusable = false면 자동으로 비활성화되므로
        // 추가 처리가 필요한 경우만 여기서 처리
    }
    
    // 수집 효과 재생
    private void PlayCollectEffects()
    {
        // 파티클 효과
        if (collectEffect != null)
        {
            GameObject effect = Instantiate(collectEffect, transform.position, Quaternion.identity);
            Destroy(effect, effectDuration);
        }
        
        // 사운드 재생
        if (collectSound != null)
        {
            // AudioSource가 없으면 임시로 생성해서 재생
            GameObject audioObject = new GameObject("CollectSound");
            audioObject.transform.position = transform.position;
            AudioSource audioSource = audioObject.AddComponent<AudioSource>();
            audioSource.clip = collectSound;
            audioSource.Play();
            Destroy(audioObject, collectSound.length);
        }
    }
    
    // 비주얼 자동 설정
    private void SetupVisual()
    {
        if (itemData != null && itemData.materialIcon != null)
        {
            SpriteRenderer spriteRenderer = GetComponent<SpriteRenderer>();
            if (spriteRenderer != null)
            {
                spriteRenderer.sprite = itemData.materialIcon;
            }
        }
    }
    
    // 아이템 데이터 동적 설정 (드롭 시스템에서 사용)
    public void SetItemData(MaterialData data, int amount = 1)
    {
        itemData = data;
        itemAmount = amount;
        SetupVisual();
    }
    
    // 현재 아이템 정보 가져오기
    public MaterialData GetItemData() => itemData;
    public int GetItemAmount() => itemAmount;
    
    // 에디터용 기능
    #if UNITY_EDITOR
    void OnValidate()
    {
        // 인스펙터에서 아이템 데이터 변경 시 자동으로 비주얼 업데이트
        // 에디터가 로드 중이거나 컴파일 중일 때는 실행하지 않음
        if (itemData != null && !Application.isPlaying)
        {
            // 에디터에서 안전하게 실행되도록 지연 호출
            UnityEditor.EditorApplication.delayCall += () =>
            {
                if (this != null)
                {
                    SetupVisual();
                }
            };
        }
    }
    
    void OnDrawGizmosSelected()
    {
        // 수집 가능 아이템 표시
        Gizmos.color = Color.yellow;
        Gizmos.DrawWireSphere(transform.position, 0.5f);
        
        if (itemData != null)
        {
            UnityEditor.Handles.Label(
                transform.position + Vector3.up, 
                $"[{itemData.materialName}] x{itemAmount}"
            );
        }
    }
    #endif
}