using UnityEngine;
using UnityEngine.Tilemaps;

public class CameraSetupHelper : MonoBehaviour
{
    [Header("References")]
    [SerializeField] private CameraFollow2D cameraFollow;
    [SerializeField] private Tilemap boundsTilemap; // 경계 계산용 타일맵 (보통 Ground Tilemap)
    [SerializeField] private MapBuilder mapBuilder;
    
    [Header("Auto Setup")]
    [SerializeField] private bool setupOnStart = true;
    [SerializeField] private float boundsPadding = 5f; // 경계 여백
    
    void Start()
    {
        if (setupOnStart)
        {
            SetupCamera();
        }
    }
    
    [ContextMenu("Setup Camera")]
    public void SetupCamera()
    {
        // CameraFollow2D 찾기
        if (cameraFollow == null)
        {
            cameraFollow = Camera.main.GetComponent<CameraFollow2D>();
            if (cameraFollow == null)
            {
                cameraFollow = Camera.main.gameObject.AddComponent<CameraFollow2D>();
                Debug.Log("CameraFollow2D 컴포넌트를 Main Camera에 추가했습니다.");
            }
        }
        
        // MapBuilder 찾기
        if (mapBuilder == null)
        {
            mapBuilder = FindObjectOfType<MapBuilder>();
        }
        
        // Ground Tilemap으로 경계 설정
        if (boundsTilemap == null && mapBuilder != null)
        {
            boundsTilemap = mapBuilder.groundTilemap;
        }
        
        // 경계 설정
        if (boundsTilemap != null && cameraFollow != null)
        {
            SetCameraBoundsFromTilemap();
        }
        
        // 플레이어 찾기
        GameObject player = GameObject.FindGameObjectWithTag("Player");
        if (player != null && cameraFollow != null)
        {
            cameraFollow.SetTarget(player.transform);
            cameraFollow.SnapToTarget(); // 즉시 플레이어 위치로
        }
    }
    
    void SetCameraBoundsFromTilemap()
    {
        if (boundsTilemap == null || cameraFollow == null) return;
        
        // 타일맵 경계 가져오기
        boundsTilemap.CompressBounds();
        Bounds tilemapBounds = boundsTilemap.localBounds;
        
        // 월드 좌표로 변환
        Vector3 min = boundsTilemap.transform.TransformPoint(tilemapBounds.min);
        Vector3 max = boundsTilemap.transform.TransformPoint(tilemapBounds.max);
        
        // 패딩 적용
        Vector2 minBounds = new Vector2(min.x - boundsPadding, min.y - boundsPadding);
        Vector2 maxBounds = new Vector2(max.x + boundsPadding, max.y + boundsPadding);
        
        // 카메라에 경계 설정
        cameraFollow.SetBounds(minBounds, maxBounds);
        
        Debug.Log($"카메라 경계 설정 완료: Min({minBounds}), Max({maxBounds})");
    }
    
    // 맵이 재생성될 때 호출
    public void OnMapRebuilt()
    {
        SetupCamera();
    }
}