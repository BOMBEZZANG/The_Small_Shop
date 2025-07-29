using UnityEngine;
using UnityEditor;
using System.Collections.Generic;

public class TagSetup
{
    [MenuItem("Tools/Tilemap System/Setup Tags and Layers")]
    public static void SetupTagsAndLayers()
    {
        // 필요한 태그들
        List<string> requiredTags = new List<string>
        {
            "NPC",
            "Interactable",
            "Building",
            "Collectible"
        };
        
        // 필요한 레이어들
        List<string> requiredLayers = new List<string>
        {
            "Ground",
            "Buildings", 
            "Decorations",
            "Collision",
            "NPCs",
            "Interactables"
        };
        
        // 태그 추가
        foreach (string tag in requiredTags)
        {
            AddTag(tag);
        }
        
        // 레이어 추가
        int layerIndex = 8; // 사용자 정의 레이어는 8번부터 시작
        foreach (string layer in requiredLayers)
        {
            AddLayer(layer, layerIndex);
            layerIndex++;
        }
        
        Debug.Log("태그와 레이어 설정 완료!");
        EditorUtility.DisplayDialog("Setup Complete", "모든 태그와 레이어가 설정되었습니다!", "OK");
    }
    
    static void AddTag(string tagName)
    {
        // 태그가 이미 존재하는지 확인
        SerializedObject tagManager = new SerializedObject(AssetDatabase.LoadAllAssetsAtPath("ProjectSettings/TagManager.asset")[0]);
        SerializedProperty tagsProp = tagManager.FindProperty("tags");
        
        // 이미 존재하는지 확인
        bool found = false;
        for (int i = 0; i < tagsProp.arraySize; i++)
        {
            SerializedProperty t = tagsProp.GetArrayElementAtIndex(i);
            if (t.stringValue.Equals(tagName))
            {
                found = true;
                break;
            }
        }
        
        // 태그 추가
        if (!found)
        {
            tagsProp.InsertArrayElementAtIndex(tagsProp.arraySize);
            SerializedProperty newTag = tagsProp.GetArrayElementAtIndex(tagsProp.arraySize - 1);
            newTag.stringValue = tagName;
            tagManager.ApplyModifiedProperties();
            Debug.Log($"태그 '{tagName}' 추가됨");
        }
        else
        {
            Debug.Log($"태그 '{tagName}'는 이미 존재합니다");
        }
    }
    
    static void AddLayer(string layerName, int layerIndex)
    {
        if (layerIndex < 8 || layerIndex > 31)
        {
            Debug.LogError($"레이어 인덱스 {layerIndex}는 유효하지 않습니다 (8-31 사이여야 함)");
            return;
        }
        
        SerializedObject tagManager = new SerializedObject(AssetDatabase.LoadAllAssetsAtPath("ProjectSettings/TagManager.asset")[0]);
        SerializedProperty layersProp = tagManager.FindProperty("layers");
        
        // 해당 인덱스의 레이어가 비어있는지 확인
        SerializedProperty layerProp = layersProp.GetArrayElementAtIndex(layerIndex);
        if (string.IsNullOrEmpty(layerProp.stringValue))
        {
            layerProp.stringValue = layerName;
            tagManager.ApplyModifiedProperties();
            Debug.Log($"레이어 '{layerName}' (인덱스 {layerIndex}) 추가됨");
        }
        else
        {
            Debug.Log($"레이어 인덱스 {layerIndex}는 이미 '{layerProp.stringValue}'로 사용 중입니다");
        }
    }
    
    // 현재 태그와 레이어 확인
    [MenuItem("Tools/Tilemap System/Check Tags and Layers")]
    public static void CheckTagsAndLayers()
    {
        Debug.Log("=== 현재 태그 목록 ===");
        
        SerializedObject tagManager = new SerializedObject(AssetDatabase.LoadAllAssetsAtPath("ProjectSettings/TagManager.asset")[0]);
        SerializedProperty tagsProp = tagManager.FindProperty("tags");
        
        for (int i = 0; i < tagsProp.arraySize; i++)
        {
            SerializedProperty t = tagsProp.GetArrayElementAtIndex(i);
            Debug.Log($"태그 [{i}]: {t.stringValue}");
        }
        
        Debug.Log("\n=== 현재 레이어 목록 ===");
        
        SerializedProperty layersProp = tagManager.FindProperty("layers");
        for (int i = 0; i < layersProp.arraySize; i++)
        {
            SerializedProperty l = layersProp.GetArrayElementAtIndex(i);
            if (!string.IsNullOrEmpty(l.stringValue))
            {
                Debug.Log($"레이어 [{i}]: {l.stringValue}");
            }
        }
    }
}