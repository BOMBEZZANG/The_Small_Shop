using UnityEngine;

public class PlayerStartPosition : MonoBehaviour
{
    [Header("Spawn Settings")]
    [SerializeField] private bool isDefaultSpawn = true; // 기본 스폰 위치인지
    [SerializeField] private string spawnPointID = "default"; // 스폰 포인트 식별자
    
    [Header("Visual Settings")]
    [SerializeField] private Color gizmoColor = Color.green;
    [SerializeField] private float gizmoSize = 1f;
    
    private static PlayerStartPosition defaultSpawnPoint;
    private static System.Collections.Generic.Dictionary<string, PlayerStartPosition> spawnPoints = new System.Collections.Generic.Dictionary<string, PlayerStartPosition>();
    
    void Awake()
    {
        // 스폰 포인트 등록
        if (isDefaultSpawn)
        {
            if (defaultSpawnPoint != null && defaultSpawnPoint != this)
            {
                Debug.LogWarning($"기본 스폰 포인트가 이미 존재합니다. {gameObject.name}는 일반 스폰 포인트로 설정됩니다.");
                isDefaultSpawn = false;
            }
            else
            {
                defaultSpawnPoint = this;
            }
        }
        
        // ID로 스폰 포인트 등록
        if (!string.IsNullOrEmpty(spawnPointID))
        {
            if (spawnPoints.ContainsKey(spawnPointID))
            {
                Debug.LogWarning($"스폰 포인트 ID '{spawnPointID}'가 이미 존재합니다. {gameObject.name}");
            }
            else
            {
                spawnPoints[spawnPointID] = this;
            }
        }
    }
    
    void Start()
    {
        // 게임 시작 시 플레이어를 기본 스폰 위치로 이동
        if (isDefaultSpawn)
        {
            SpawnPlayerHere();
        }
    }
    
    void OnDestroy()
    {
        // 등록 해제
        if (defaultSpawnPoint == this)
        {
            defaultSpawnPoint = null;
        }
        
        if (spawnPoints.ContainsValue(this))
        {
            spawnPoints.Remove(spawnPointID);
        }
    }
    
    // 플레이어를 이 위치로 스폰
    public void SpawnPlayerHere()
    {
        GameObject player = GameObject.FindGameObjectWithTag("Player");
        if (player == null)
        {
            Debug.LogError("Player 태그를 가진 오브젝트를 찾을 수 없습니다!");
            return;
        }
        
        player.transform.position = transform.position;
        
        // 카메라도 함께 이동 (CameraFollow2D가 있는 경우)
        var cameraFollow = Camera.main?.GetComponent<CameraFollow2D>();
        if (cameraFollow != null)
        {
            cameraFollow.SnapToTarget();
        }
        
        Debug.Log($"플레이어가 {gameObject.name} 위치로 스폰되었습니다.");
    }
    
    // Static 메서드들
    
    // 기본 스폰 위치로 플레이어 스폰
    public static void SpawnPlayerAtDefault()
    {
        if (defaultSpawnPoint != null)
        {
            defaultSpawnPoint.SpawnPlayerHere();
        }
        else
        {
            Debug.LogWarning("기본 스폰 포인트가 설정되지 않았습니다!");
        }
    }
    
    // 특정 ID의 스폰 위치로 플레이어 스폰
    public static void SpawnPlayerAtPoint(string pointID)
    {
        if (spawnPoints.ContainsKey(pointID))
        {
            spawnPoints[pointID].SpawnPlayerHere();
        }
        else
        {
            Debug.LogWarning($"스폰 포인트 ID '{pointID}'를 찾을 수 없습니다!");
            SpawnPlayerAtDefault(); // 기본 위치로 스폰
        }
    }
    
    // 가장 가까운 스폰 포인트 찾기
    public static PlayerStartPosition GetNearestSpawnPoint(Vector3 position)
    {
        PlayerStartPosition nearest = defaultSpawnPoint;
        float nearestDistance = float.MaxValue;
        
        foreach (var spawnPoint in spawnPoints.Values)
        {
            float distance = Vector3.Distance(position, spawnPoint.transform.position);
            if (distance < nearestDistance)
            {
                nearestDistance = distance;
                nearest = spawnPoint;
            }
        }
        
        return nearest;
    }
    
    // 에디터용 기능
    #if UNITY_EDITOR
    void OnDrawGizmos()
    {
        // 스폰 포인트 시각화
        Gizmos.color = gizmoColor;
        
        // 원형 표시
        Gizmos.DrawWireSphere(transform.position, gizmoSize * 0.5f);
        
        // 플레이어 모양 표시
        Vector3 headPos = transform.position + Vector3.up * gizmoSize * 0.3f;
        Gizmos.DrawWireSphere(headPos, gizmoSize * 0.2f); // 머리
        
        Vector3 bodyTop = transform.position + Vector3.up * gizmoSize * 0.1f;
        Vector3 bodyBottom = transform.position - Vector3.up * gizmoSize * 0.3f;
        Gizmos.DrawLine(bodyTop, bodyBottom); // 몸통
        
        // 팔
        Vector3 leftArm = transform.position - Vector3.right * gizmoSize * 0.3f;
        Vector3 rightArm = transform.position + Vector3.right * gizmoSize * 0.3f;
        Gizmos.DrawLine(leftArm, rightArm);
        
        // 다리
        Vector3 leftLeg = bodyBottom - Vector3.right * gizmoSize * 0.1f - Vector3.up * gizmoSize * 0.3f;
        Vector3 rightLeg = bodyBottom + Vector3.right * gizmoSize * 0.1f - Vector3.up * gizmoSize * 0.3f;
        Gizmos.DrawLine(bodyBottom, leftLeg);
        Gizmos.DrawLine(bodyBottom, rightLeg);
        
        // 방향 표시 (앞쪽)
        Vector3 forward = transform.position + transform.up * gizmoSize;
        Gizmos.color = Color.blue;
        Gizmos.DrawLine(transform.position, forward);
        
        // 라벨 표시
        string label = isDefaultSpawn ? $"[기본]\n{spawnPointID}" : spawnPointID;
        UnityEditor.Handles.Label(transform.position + Vector3.up * gizmoSize, label);
    }
    
    void OnDrawGizmosSelected()
    {
        // 선택됐을 때 더 큰 원 표시
        Gizmos.color = gizmoColor;
        Gizmos.DrawWireSphere(transform.position, gizmoSize);
    }
    #endif
}