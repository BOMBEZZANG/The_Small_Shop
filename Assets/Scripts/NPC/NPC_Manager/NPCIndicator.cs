using UnityEngine;
using System.Collections;

public class NPCIndicator : MonoBehaviour
{
    [Header("Indicator Icons")]
    [SerializeField] private GameObject exclamationIcon;    // ! 아이콘
    [SerializeField] private GameObject questionIcon;       // ? 아이콘
    [SerializeField] private GameObject dotsIcon;          // ... 아이콘
    [SerializeField] private GameObject heartIcon;         // ♥ 아이콘 (호감도)
    
    [Header("Animation")]
    [SerializeField] private float bobSpeed = 2f;          // 위아래 움직임 속도
    [SerializeField] private float bobHeight = 0.3f;       // 위아래 움직임 높이
    [SerializeField] private float fadeSpeed = 5f;         // 페이드 속도
    [SerializeField] private AnimationCurve bobCurve = AnimationCurve.EaseInOut(0, 0, 1, 1);
    
    private GameObject currentIcon;
    private Vector3 originalPosition;
    private Coroutine animationCoroutine;
    private CanvasGroup canvasGroup;
    
    void Awake()
    {
        // 모든 아이콘 숨기기
        HideAllIcons();
        
        // CanvasGroup 추가 (페이드 효과용)
        canvasGroup = GetComponent<CanvasGroup>();
        if (canvasGroup == null)
        {
            canvasGroup = gameObject.AddComponent<CanvasGroup>();
        }
        
        originalPosition = transform.localPosition;
    }
    
    // ===== 아이콘 표시 =====
    public void ShowIcon(NPCIndicatorType type)
    {
        GameObject iconToShow = GetIconByType(type);
        
        if (iconToShow == null)
        {
            Debug.LogWarning($"NPCIndicator: {type} 아이콘이 설정되지 않았습니다!");
            return;
        }
        
        // 현재 아이콘과 같으면 무시
        if (currentIcon == iconToShow && currentIcon.activeSelf)
        {
            return;
        }
        
        // 기존 아이콘 숨기기
        HideAllIcons();
        
        // 새 아이콘 표시
        currentIcon = iconToShow;
        currentIcon.SetActive(true);
        
        // 애니메이션 시작
        if (animationCoroutine != null)
        {
            StopCoroutine(animationCoroutine);
        }
        animationCoroutine = StartCoroutine(ShowAnimation());
    }
    
    // ===== 아이콘 숨기기 =====
    public void HideIcon()
    {
        if (animationCoroutine != null)
        {
            StopCoroutine(animationCoroutine);
        }
        animationCoroutine = StartCoroutine(HideAnimation());
    }
    
    // ===== 모든 아이콘 숨기기 =====
    private void HideAllIcons()
    {
        if (exclamationIcon) exclamationIcon.SetActive(false);
        if (questionIcon) questionIcon.SetActive(false);
        if (dotsIcon) dotsIcon.SetActive(false);
        if (heartIcon) heartIcon.SetActive(false);
        currentIcon = null;
    }
    
    // ===== 타입별 아이콘 가져오기 =====
    private GameObject GetIconByType(NPCIndicatorType type)
    {
        switch (type)
        {
            case NPCIndicatorType.Exclamation:
                return exclamationIcon;
            case NPCIndicatorType.Question:
                return questionIcon;
            case NPCIndicatorType.Dots:
                return dotsIcon;
            case NPCIndicatorType.Heart:
                return heartIcon;
            default:
                return null;
        }
    }
    
    // ===== 표시 애니메이션 =====
    private IEnumerator ShowAnimation()
    {
        // 페이드 인
        float elapsed = 0f;
        while (elapsed < 1f / fadeSpeed)
        {
            elapsed += Time.deltaTime;
            canvasGroup.alpha = Mathf.Lerp(0f, 1f, elapsed * fadeSpeed);
            yield return null;
        }
        canvasGroup.alpha = 1f;
        
        // 위아래 움직임
        yield return StartCoroutine(BobAnimation());
    }
    
    // ===== 숨김 애니메이션 =====
    private IEnumerator HideAnimation()
    {
        // 페이드 아웃
        float elapsed = 0f;
        float startAlpha = canvasGroup.alpha;
        
        while (elapsed < 1f / fadeSpeed)
        {
            elapsed += Time.deltaTime;
            canvasGroup.alpha = Mathf.Lerp(startAlpha, 0f, elapsed * fadeSpeed);
            yield return null;
        }
        
        canvasGroup.alpha = 0f;
        HideAllIcons();
    }
    
    // ===== 위아래 움직임 애니메이션 =====
    private IEnumerator BobAnimation()
    {
        while (currentIcon != null && currentIcon.activeSelf)
        {
            float bobOffset = Mathf.Sin(Time.time * bobSpeed) * bobHeight;
            transform.localPosition = originalPosition + Vector3.up * bobOffset;
            yield return null;
        }
    }
    
    // ===== 특수 효과 =====
    public void PlayPulseEffect()
    {
        if (currentIcon != null)
        {
            StartCoroutine(PulseEffect());
        }
    }
    
    private IEnumerator PulseEffect()
    {
        Transform iconTransform = currentIcon.transform;
        Vector3 originalScale = iconTransform.localScale;
        
        // 확대
        float elapsed = 0f;
        while (elapsed < 0.2f)
        {
            elapsed += Time.deltaTime;
            float scale = Mathf.Lerp(1f, 1.3f, elapsed / 0.2f);
            iconTransform.localScale = originalScale * scale;
            yield return null;
        }
        
        // 축소
        elapsed = 0f;
        while (elapsed < 0.2f)
        {
            elapsed += Time.deltaTime;
            float scale = Mathf.Lerp(1.3f, 1f, elapsed / 0.2f);
            iconTransform.localScale = originalScale * scale;
            yield return null;
        }
        
        iconTransform.localScale = originalScale;
    }
    
    // ===== 빠른 전환 =====
    public void SwitchIcon(NPCIndicatorType type)
    {
        GameObject newIcon = GetIconByType(type);
        if (newIcon == null || newIcon == currentIcon) return;
        
        // 즉시 전환
        if (currentIcon != null) currentIcon.SetActive(false);
        currentIcon = newIcon;
        currentIcon.SetActive(true);
        
        // 펄스 효과
        PlayPulseEffect();
    }
    
    void OnDestroy()
    {
        if (animationCoroutine != null)
        {
            StopCoroutine(animationCoroutine);
        }
    }
}

// 인디케이터 타입
public enum NPCIndicatorType
{
    None,
    Exclamation,    // ! - 새로운 퀘스트/대화
    Question,       // ? - 일반 대화 가능
    Dots,          // ... - 생각 중/대기 중
    Heart          // ♥ - 호감도 상승
}