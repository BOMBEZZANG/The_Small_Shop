using UnityEngine;

// 이 메뉴를 통해 .asset 파일을 생성할 수 있게 해주는 속성입니다.
[CreateAssetMenu(fileName = "New MaterialData", menuName = "Game Data/Material Data")]
public class MaterialData : ScriptableObject
{
    // 인스펙터 창에 표시될 데이터 항목들입니다.
    public int materialID;
    public string materialName;
    public Sprite materialIcon;
    public GameObject materialPrefab; // 바닥에 떨어졌을 때의 모습
    [TextArea]
    public string materialDescription;
}