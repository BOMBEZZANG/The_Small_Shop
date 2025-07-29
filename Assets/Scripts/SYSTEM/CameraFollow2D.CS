using UnityEngine;
using UnityEngine.Tilemaps;

public class CameraFollow2D : MonoBehaviour
{
    [Header("Target")]
    [SerializeField] private Transform target; // 플레이어 Transform
    [SerializeField] private bool autoFindPlayer = true;
    
    [Header("Follow Settings")]
    [SerializeField] private Vector3 offset = new Vector3(0, 0, -10); // 카메라 오프셋
    [SerializeField] private float smoothSpeed = 5f; // 부드러운 이동 속도
    [SerializeField] private bool smoothFollow = true; // 부드러운 추적 여부
    
    [Header("Camera Bounds")]
    [SerializeField] private bool useBounds = true; // 경계 제한 사용
    [SerializeField] private Vector2 minBounds = new Vector2(-25, -25); // 최소 경계
    [SerializeField] private Vector2 maxBounds = new Vector2(25, 25); // 최대 경계
    
    [Header("Dead Zone")]
    [SerializeField] private bool useDeadZone = false; // 데드존 사용
    [SerializeField] private Vector2 deadZoneSize = new Vector2(2f, 2f); // 데드존 크기
    
    [Header("Camera Shake")]
    [SerializeField] private float shakeDuration = 0f;
    [SerializeField] private float shakeMagnitude = 0.1f;
    
    [Header("Zoom")]
    [SerializeField] private float defaultZoom = 5f; // 기본 줌 (Orthographic Size)
    [SerializeField] private float minZoom = 3f;
    [SerializeField] private float maxZoom = 10f;
    [SerializeField] private float zoomSpeed = 2f;
    
    // 내부 변수
    private Camera cam;
    private Vector3 desiredPosition;
    private Vector3 smoothedPosition;
    private Vector3 shakeOffset;
    private float currentShakeDuration = 0f;
    
    // 카메라 영역
    private float camHeight;
    private float camWidth;
    
    void Awake()
    {
        cam = GetComponent<Camera>();
        if (cam == null)
        {
            cam = gameObject.AddComponent<Camera>();
        }
        
        // 2D 카메라 설정
        cam.orthographic = true;
        cam.orthographicSize = defaultZoom;
    }
    
    void Start()
    {
        // 플레이어 자동 찾기
        if (autoFindPlayer && target == null)
        {
            GameObject player = GameObject.FindGameObjectWithTag("Player");
            if (player != null)
            {
                target = player.transform;
                Debug.Log("CameraFollow2D: 플레이어를 자동으로 찾았습니다.");
            }
            else
            {
                Debug.LogWarning("CameraFollow2D: 'Player' 태그를 가진 오브젝트를 찾을 수 없습니다!");
            }
        }
        
        // 초기 위치 설정
        if (target != null)
        {
            transform.position = target.position + offset;
        }
        
        UpdateCameraBounds();
    }
    
    void LateUpdate()
    {
        if (target == null) return;
        
        // 줌 처리
        HandleZoom();
        
        // 카메라 이동
        FollowTarget();
        
        // 카메라 흔들림
        HandleCameraShake();
    }
    
    void FollowTarget()
    {
        // 목표 위치 계산
        desiredPosition = target.position + offset;
        
        // 데드존 처리
        if (useDeadZone)
        {
            Vector3 currentPos = transform.position;
            float deltaX = desiredPosition.x - currentPos.x;
            float deltaY = desiredPosition.y - currentPos.y;
            
            if (Mathf.Abs(deltaX) > deadZoneSize.x / 2f)
            {
                currentPos.x = desiredPosition.x - Mathf.Sign(deltaX) * deadZoneSize.x / 2f;
            }
            
            if (Mathf.Abs(deltaY) > deadZoneSize.y / 2f)
            {
                currentPos.y = desiredPosition.y - Mathf.Sign(deltaY) * deadZoneSize.y / 2f;
            }
            
            desiredPosition = new Vector3(currentPos.x, currentPos.y, desiredPosition.z);
        }
        
        // 부드러운 이동
        if (smoothFollow)
        {
            smoothedPosition = Vector3.Lerp(transform.position, desiredPosition, smoothSpeed * Time.deltaTime);
        }
        else
        {
            smoothedPosition = desiredPosition;
        }
        
        // 경계 제한
        if (useBounds)
        {
            UpdateCameraBounds();
            smoothedPosition.x = Mathf.Clamp(smoothedPosition.x, minBounds.x + camWidth, maxBounds.x - camWidth);
            smoothedPosition.y = Mathf.Clamp(smoothedPosition.y, minBounds.y + camHeight, maxBounds.y - camHeight);
        }
        
        // 최종 위치 적용
        transform.position = smoothedPosition + shakeOffset;
    }
    
    void UpdateCameraBounds()
    {
        camHeight = cam.orthographicSize;
        camWidth = camHeight * cam.aspect;
    }
    
    void HandleZoom()
    {
        // 마우스 휠로 줌
        float scrollInput = Input.GetAxis("Mouse ScrollWheel");
        if (scrollInput != 0)
        {
            float newZoom = cam.orthographicSize - scrollInput * zoomSpeed;
            cam.orthographicSize = Mathf.Clamp(newZoom, minZoom, maxZoom);
            UpdateCameraBounds();
        }
        
        // 키보드로 줌 (선택적)
        if (Input.GetKey(KeyCode.KeypadPlus) || Input.GetKey(KeyCode.Equals))
        {
            cam.orthographicSize = Mathf.Clamp(cam.orthographicSize - zoomSpeed * Time.deltaTime, minZoom, maxZoom);
            UpdateCameraBounds();
        }
        else if (Input.GetKey(KeyCode.KeypadMinus) || Input.GetKey(KeyCode.Minus))
        {
            cam.orthographicSize = Mathf.Clamp(cam.orthographicSize + zoomSpeed * Time.deltaTime, minZoom, maxZoom);
            UpdateCameraBounds();
        }
    }
    
    void HandleCameraShake()
    {
        if (currentShakeDuration > 0)
        {
            shakeOffset = Random.insideUnitCircle * shakeMagnitude;
            currentShakeDuration -= Time.deltaTime;
        }
        else
        {
            shakeOffset = Vector3.zero;
        }
    }
    
    // ===== Public Methods =====
    
    // 카메라 흔들기 시작
    public void StartShake(float duration, float magnitude)
    {
        currentShakeDuration = duration;
        shakeMagnitude = magnitude;
    }
    
    // 타겟 변경
    public void SetTarget(Transform newTarget)
    {
        target = newTarget;
    }
    
    // 즉시 타겟 위치로 이동
    public void SnapToTarget()
    {
        if (target != null)
        {
            transform.position = target.position + offset;
        }
    }
    
    // 줌 설정
    public void SetZoom(float zoom)
    {
        cam.orthographicSize = Mathf.Clamp(zoom, minZoom, maxZoom);
        UpdateCameraBounds();
    }
    
    // 경계 설정
    public void SetBounds(Vector2 min, Vector2 max)
    {
        minBounds = min;
        maxBounds = max;
    }
    
    // 맵에 맞춰 경계 자동 설정
    public void SetBoundsFromTilemap(Tilemap tilemap)
    {
        if (tilemap == null) return;
        
        tilemap.CompressBounds();
        Vector3 tilemapMin = tilemap.transform.TransformPoint(tilemap.localBounds.min);
        Vector3 tilemapMax = tilemap.transform.TransformPoint(tilemap.localBounds.max);
        
        minBounds = new Vector2(tilemapMin.x, tilemapMin.y);
        maxBounds = new Vector2(tilemapMax.x, tilemapMax.y);
        
        Debug.Log($"카메라 경계 설정: Min({minBounds}), Max({maxBounds})");
    }
    
    // ===== Gizmos =====
    void OnDrawGizmosSelected()
    {
        // 카메라 경계 표시
        if (useBounds)
        {
            Gizmos.color = Color.yellow;
            Vector3 bottomLeft = new Vector3(minBounds.x, minBounds.y, 0);
            Vector3 topRight = new Vector3(maxBounds.x, maxBounds.y, 0);
            Vector3 topLeft = new Vector3(minBounds.x, maxBounds.y, 0);
            Vector3 bottomRight = new Vector3(maxBounds.x, minBounds.y, 0);
            
            Gizmos.DrawLine(bottomLeft, bottomRight);
            Gizmos.DrawLine(bottomRight, topRight);
            Gizmos.DrawLine(topRight, topLeft);
            Gizmos.DrawLine(topLeft, bottomLeft);
        }
        
        // 데드존 표시
        if (useDeadZone)
        {
            Gizmos.color = Color.red;
            Gizmos.DrawWireCube(transform.position, deadZoneSize);
        }
        
        // 타겟 연결선
        if (target != null)
        {
            Gizmos.color = Color.green;
            Gizmos.DrawLine(transform.position, target.position);
        }
    }
}