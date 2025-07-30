using UnityEngine;
#if UNITY_EDITOR
using UnityEditor;
#endif

[ExecuteInEditMode]
public class PathEditProxy : MonoBehaviour
{
    [Header("편집 대상")]
    public VisitorNPCData targetNPCData;
    
    [Header("편집 모드")]
    public bool isEditing = false;
    
    [Header("시각화")]
    public Color pathColor = Color.yellow;
    public bool showNumbers = true;
    
    void OnEnable()
    {
        // Scene 뷰에만 표시, Game 뷰에는 숨김
        gameObject.hideFlags = HideFlags.DontSaveInBuild;
    }
    
    void OnDrawGizmos()
    {
        if (targetNPCData == null || !targetNPCData.useMovementPath) return;
        
        // 경로 그리기
        Gizmos.color = pathColor;
        
        for (int i = 0; i < targetNPCData.movementPath.Count; i++)
        {
            Vector3 pos = targetNPCData.movementPath[i];
            Gizmos.DrawWireSphere(pos, 0.3f);
            
            if (i > 0)
            {
                Gizmos.DrawLine(targetNPCData.movementPath[i-1], pos);
            }
        }
        
        if (targetNPCData.loopPath && targetNPCData.movementPath.Count > 1)
        {
            Gizmos.DrawLine(
                targetNPCData.movementPath[targetNPCData.movementPath.Count - 1], 
                targetNPCData.movementPath[0]
            );
        }
    }
}