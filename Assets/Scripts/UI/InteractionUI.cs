using UnityEngine;
using UnityEngine.UI;
using TMPro;
using System.Collections;

public class InteractionUI : MonoBehaviour
{
    [Header("UI References")]
    [SerializeField] private GameObject uiContainer;
    [SerializeField] private GameObject iconContainer;
    [SerializeField] private GameObject textContainer;
    [SerializeField] private GameObject progressContainer;
    
    [SerializeField] private Image iconImage;
    [SerializeField] private TextMeshProUGUI keyText;          // [E] 표시
    [SerializeField] private TextMeshProUGUI promptText;       // 상호작용 설명
    [SerializeField] private TextMeshProUGUI requirementText;  // 요구사항
    [SerializeField] private Image progressBar;                // 진행바
    [SerializeField] private TextMeshProUGUI progressText;     // 진행률 텍스트
    
    [Header("Animation Settings")]
    [SerializeField] private float fadeSpeed = 5f;
    [SerializeField] private float scaleSpeed = 10f;
    [SerializeField] private AnimationCurve scaleCurve = AnimationCurve.EaseInOut(0, 0.8f, 1, 1);
    
    // 상태
    private InteractableObject currentTarget;
    private InteractionData currentData;
    private CanvasGroup canvasGroup;
    private bool isShowing = false;
    private Coroutine fadeCoroutine;
    
    // 컴포넌트
    private Transform player;
    private Camera mainCamera;
    
    void Awake()
    {
        // CanvasGroup 컴포넌트 확인 (페이드용)
        canvasGroup = uiContainer.GetComponent<CanvasGroup>();
        if (canvasGroup == null)
        {
            canvasGroup = uiContainer.AddComponent<CanvasGroup>();
        }
        
        // 초기 상태
        uiContainer.SetActive(false);
        progressContainer.SetActive(false);
    }
    
    void Start()
    {
        player = GameObject.FindGameObjectWithTag("Player").transform;
        mainCamera = Camera.main;
        
        // 이벤트 구독
        InteractionController.OnTargetChanged += OnTargetChanged;
        InteractionController.OnInteractionStarted += OnInteractionStarted;
        InteractionController.OnInteractionCompleted += OnInteractionCompleted;
    }
    
    void OnDestroy()
    {
        // 이벤트 구독 해제
        InteractionController.OnTargetChanged -= OnTargetChanged;
        InteractionController.OnInteractionStarted -= OnInteractionStarted;
        InteractionController.OnInteractionCompleted -= OnInteractionCompleted;
    }
    
    void Update()
    {
        if (currentTarget != null && isShowing)
        {
            UpdateUIPosition();
            UpdateUIContent();
        }
    }
    
    // ===== 타겟 변경 처리 =====
    private void OnTargetChanged(InteractableObject newTarget)
    {
        currentTarget = newTarget;
        
        if (newTarget != null)
        {
            // 새 타겟의 InteractionData 가져오기
            currentData = newTarget.GetInteractionData();
            if (currentData == null)
            {
                Debug.LogWarning($"{newTarget.name}에 InteractionData가 없습니다!");
                Hide();
                return;
            }
            
            Show();
        }
        else
        {
            Hide();
        }
    }
    
    // ===== UI 표시/숨김 =====
    private void Show()
    {
        isShowing = true;
        uiContainer.SetActive(true);
        
        if (fadeCoroutine != null) StopCoroutine(fadeCoroutine);
        fadeCoroutine = StartCoroutine(FadeIn());
    }
    
    private void Hide()
    {
        isShowing = false;
        
        if (fadeCoroutine != null) StopCoroutine(fadeCoroutine);
        fadeCoroutine = StartCoroutine(FadeOut());
    }
    
    // ===== UI 위치 업데이트 =====
    private void UpdateUIPosition()
    {
        // 월드 좌표를 스크린 좌표로 변환
        Vector3 worldPos = currentTarget.transform.position + Vector3.up * 2f; // 오브젝트 위에 표시
        Vector3 screenPos = mainCamera.WorldToScreenPoint(worldPos);
        
        uiContainer.transform.position = screenPos;
    }
    
    // ===== UI 내용 업데이트 =====
    private void UpdateUIContent()
    {
        if (currentData == null) return;
        
        float distance = Vector3.Distance(player.position, currentTarget.transform.position);
        
        // 거리에 따른 표시
        bool showIcon = distance <= currentData.IconOnlyDistance || currentData.AlwaysShowIcon;
        bool showText = distance <= currentData.FullTextDistance;
        
        iconContainer.SetActive(showIcon);
        textContainer.SetActive(showText);
        
        // 아이콘 설정
        if (showIcon && currentData.IconSprite != null)
        {
            iconImage.sprite = currentData.IconSprite;
            iconImage.enabled = true;
            keyText.gameObject.SetActive(false);
        }
        else if (showIcon)
        {
            iconImage.enabled = false;
            keyText.gameObject.SetActive(true);
            keyText.text = $"[{currentData.ActionKey}]";
        }
        
        // 텍스트 설정
        if (showText)
        {
            // 상호작용 가능 여부 체크
            var playerController = player.GetComponent<PlayerController>();
            if (currentTarget.CanInteract(playerController))
            {
                promptText.text = currentData.GetPromptText();
                promptText.color = currentData.NormalColor;
                requirementText.gameObject.SetActive(false);
            }
            else
            {
                // 요구사항 표시
                ShowRequirements();
            }
        }
    }
    
    // ===== 요구사항 표시 =====
    private void ShowRequirements()
    {
        requirementText.gameObject.SetActive(true);
        promptText.color = currentData.DisabledColor;
        
        // 여기서는 간단히 처리, 실제로는 InteractableObject에서 구체적인 이유를 받아올 수 있음
        requirementText.text = "요구사항을 만족하지 않습니다";
        requirementText.color = currentData.WarningColor;
    }
    
    // ===== 상호작용 시작 =====
    private void OnInteractionStarted(InteractableObject target)
    {
        if (target != currentTarget) return;
        
        // 진행바 표시
        if (target.GetInteractionTime() > 0)
        {
            progressContainer.SetActive(true);
            progressBar.fillAmount = 0f;
            
            // 진행률 업데이트 구독
            target.OnInteractionProgress += UpdateProgress;
        }
    }
    
    // ===== 진행률 업데이트 =====
    private void UpdateProgress(float progress)
    {
        progressBar.fillAmount = progress;
        
        if (currentData != null && progressText != null)
        {
            progressText.text = currentData.GetProgressText(progress);
        }
    }
    
    // ===== 상호작용 완료 =====
    private void OnInteractionCompleted(InteractableObject target)
    {
        if (target == currentTarget)
        {
            progressContainer.SetActive(false);
            target.OnInteractionProgress -= UpdateProgress;
        }
    }
    
    // ===== 페이드 효과 =====
    private IEnumerator FadeIn()
    {
        float elapsed = 0f;
        Vector3 startScale = Vector3.one * 0.8f;
        
        while (elapsed < 1f)
        {
            elapsed += Time.deltaTime * fadeSpeed;
            float t = Mathf.Clamp01(elapsed);
            
            canvasGroup.alpha = t;
            uiContainer.transform.localScale = Vector3.Lerp(startScale, Vector3.one, scaleCurve.Evaluate(t));
            
            yield return null;
        }
    }
    
    private IEnumerator FadeOut()
    {
        float elapsed = 0f;
        float startAlpha = canvasGroup.alpha;
        
        while (elapsed < 1f)
        {
            elapsed += Time.deltaTime * fadeSpeed;
            float t = Mathf.Clamp01(elapsed);
            
            canvasGroup.alpha = Mathf.Lerp(startAlpha, 0f, t);
            
            yield return null;
        }
        
        uiContainer.SetActive(false);
    }
}