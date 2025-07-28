using UnityEngine;
using System.Collections;

[RequireComponent(typeof(SpriteRenderer))]
public class InteractionVisualizer : MonoBehaviour
{
    [Header("Highlight Settings")]
    [SerializeField] private bool useOutline = true;
    [SerializeField] private float outlineWidth = 0.1f;
    [SerializeField] private bool usePulseEffect = true;
    [SerializeField] private float pulseSpeed = 2f;
    [SerializeField] private float pulseAmount = 0.1f;
    
    // 컴포넌트
    private SpriteRenderer spriteRenderer;
    private Material originalMaterial;
    private Material outlineMaterial;
    
    // 상태
    private bool isHighlighted = false;
    private Coroutine pulseCoroutine;
    private Vector3 originalScale;
    
    void Awake()
    {
        spriteRenderer = GetComponent<SpriteRenderer>();
        originalMaterial = spriteRenderer.material;
        originalScale = transform.localScale;
        
        // 아웃라인 머티리얼 생성 (간단한 방법)
        if (useOutline)
        {
            CreateOutlineMaterial();
        }
    }
    
    // ===== 하이라이트 설정 =====
    public void SetHighlight(bool highlighted, Color highlightColor)
    {
        if (isHighlighted == highlighted) return;
        
        isHighlighted = highlighted;
        
        if (highlighted)
        {
            ApplyHighlight(highlightColor);
        }
        else
        {
            RemoveHighlight();
        }
    }
    
    // ===== 하이라이트 적용 =====
    private void ApplyHighlight(Color color)
    {
        // 색상 변경
        spriteRenderer.color = Color.Lerp(Color.white, color, 0.5f);
        
        // 아웃라인 효과
        if (useOutline && outlineMaterial != null)
        {
            spriteRenderer.material = outlineMaterial;
            spriteRenderer.material.SetColor("_OutlineColor", color);
        }
        
        // 펄스 효과
        if (usePulseEffect)
        {
            if (pulseCoroutine != null) StopCoroutine(pulseCoroutine);
            pulseCoroutine = StartCoroutine(PulseEffect());
        }
    }
    
    // ===== 하이라이트 제거 =====
    private void RemoveHighlight()
    {
        // 색상 복원
        spriteRenderer.color = Color.white;
        
        // 머티리얼 복원
        if (useOutline)
        {
            spriteRenderer.material = originalMaterial;
        }
        
        // 펄스 효과 중지
        if (pulseCoroutine != null)
        {
            StopCoroutine(pulseCoroutine);
            pulseCoroutine = null;
            transform.localScale = originalScale;
        }
    }
    
    // ===== 펄스 효과 =====
    private IEnumerator PulseEffect()
    {
        while (isHighlighted)
        {
            float scale = 1f + Mathf.Sin(Time.time * pulseSpeed) * pulseAmount;
            transform.localScale = originalScale * scale;
            yield return null;
        }
    }
    
    // ===== 아웃라인 머티리얼 생성 =====
    private void CreateOutlineMaterial()
    {
        // 간단한 아웃라인 효과를 위한 임시 방법
        // 실제로는 Shader를 사용하는 것이 좋습니다
        outlineMaterial = new Material(originalMaterial);
        
        // 스프라이트를 약간 크게 그려서 아웃라인 효과
        // 실제 구현시에는 적절한 Outline Shader 사용 권장
    }
    
    // ===== 상호작용 진행 표시 =====
    public void ShowProgress(float progress)
    {
        // 진행률에 따른 시각 효과
        // 예: 색상 변화, 채우기 효과 등
        float fillAmount = Mathf.Clamp01(progress);
        spriteRenderer.color = Color.Lerp(Color.white, Color.green, fillAmount);
    }
    
    // ===== 추가 효과 =====
    public void PlayInteractionEffect()
    {
        // 상호작용 시작시 효과
        StartCoroutine(InteractionEffectCoroutine());
    }
    
    private IEnumerator InteractionEffectCoroutine()
    {
        // 빠른 점멸 효과
        for (int i = 0; i < 3; i++)
        {
            spriteRenderer.enabled = false;
            yield return new WaitForSeconds(0.1f);
            spriteRenderer.enabled = true;
            yield return new WaitForSeconds(0.1f);
        }
    }
    
    void OnDestroy()
    {
        if (pulseCoroutine != null)
        {
            StopCoroutine(pulseCoroutine);
        }
    }
}