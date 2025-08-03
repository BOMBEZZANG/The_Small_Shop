using UnityEngine;
using UnityEditor;
using System.Text;

public class CopyHierarchy
{
    [MenuItem("GameObject/Copy Hierarchy Path", false, 0)]
    private static void CopyHierarchyPath()
    {
        var go = Selection.activeGameObject;
        if (go == null)
        {
            return;
        }

        var sb = new StringBuilder();
        AppendObject(sb, go, 0);

        EditorGUIUtility.systemCopyBuffer = sb.ToString();
        Debug.Log("하이어라키 구조가 클립보드에 복사되었습니다.");
    }

    private static void AppendObject(StringBuilder sb, GameObject go, int indentLevel)
    {
        // 들여쓰기 추가
        for (int i = 0; i < indentLevel; i++)
        {
            sb.Append("  "); // 스페이스 2칸으로 들여쓰기
        }

        sb.AppendLine(go.name);

        // 모든 자식 오브젝트에 대해 재귀적으로 호출
        foreach (Transform child in go.transform)
        {
            AppendObject(sb, child.gameObject, indentLevel + 1);
        }
    }
}