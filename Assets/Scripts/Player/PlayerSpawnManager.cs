using UnityEngine;

// 플레이어 스폰을 관리하는 예제 스크립트
public class PlayerSpawnManager : MonoBehaviour
{
    [Header("Spawn Settings")]
    [SerializeField] private bool spawnOnStart = true; // 게임 시작 시 자동 스폰
    [SerializeField] private string defaultSpawnPointID = "default"; // 기본 스폰 포인트 ID
    
    private static PlayerSpawnManager instance;
    
    void Awake()
    {
        // 싱글톤 패턴
        if (instance == null)
        {
            instance = this;
            DontDestroyOnLoad(gameObject);
        }
        else
        {
            Destroy(gameObject);
        }
    }
    
    void Start()
    {
        if (spawnOnStart)
        {
            // 기본 스폰 포인트로 플레이어 스폰
            PlayerStartPosition.SpawnPlayerAtDefault();
        }
    }
    
    // 플레이어 리스폰
    public static void RespawnPlayer()
    {
        PlayerStartPosition.SpawnPlayerAtDefault();
    }
    
    // 특정 포인트로 플레이어 스폰
    public static void SpawnPlayerAt(string spawnPointID)
    {
        PlayerStartPosition.SpawnPlayerAtPoint(spawnPointID);
    }
    
    // 플레이어 위치에서 가장 가까운 스폰 포인트로 리스폰
    public static void RespawnAtNearest()
    {
        GameObject player = GameObject.FindGameObjectWithTag("Player");
        if (player != null)
        {
            PlayerStartPosition nearest = PlayerStartPosition.GetNearestSpawnPoint(player.transform.position);
            if (nearest != null)
            {
                nearest.SpawnPlayerHere();
            }
        }
    }
    
    // 디버그용 메서드
    [ContextMenu("Respawn Player at Default")]
    void DebugRespawnDefault()
    {
        RespawnPlayer();
    }
    
    [ContextMenu("Respawn at Nearest")]
    void DebugRespawnNearest()
    {
        RespawnAtNearest();
    }
}