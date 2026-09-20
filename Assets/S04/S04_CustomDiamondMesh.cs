using UnityEngine;

[RequireComponent(typeof(MeshFilter), typeof(MeshRenderer))]
public class S04_CustomDiamondMesh : MonoBehaviour
{
    void Start()
    {
        // 1. 메시 컨테이너 생성
        Mesh mesh = new Mesh();
        mesh.name = "DiamondMesh";

        // 2. 다이아몬드(정팔면체) 정점 6개 좌표 정의
        Vector3[] vertices = new Vector3[]
        {
            new Vector3( 0.0f,  1.5f,  0.0f), // 0: 상단 꼭짓점 (Top)
            new Vector3(-1.0f,  0.0f,  0.0f), // 1: 좌측 허리 (Left)
            new Vector3( 0.0f,  0.0f,  1.0f), // 2: 전방 허리 (Front)
            new Vector3( 1.0f,  0.0f,  0.0f), // 3: 우측 허리 (Right)
            new Vector3( 0.0f,  0.0f, -1.0f), // 4: 후방 허리 (Back)
            new Vector3( 0.0f, -1.5f,  0.0f)  // 5: 하단 꼭짓점 (Bottom)
        };

        mesh.vertices = vertices;
    }
}