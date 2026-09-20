using UnityEngine;

[RequireComponent(typeof(MeshFilter), typeof(MeshRenderer))]
public class S04_CustomDiamondMesh : MonoBehaviour
{
    void Start()
    {
        // 1. mesh container 생성
        Mesh mesh = new Mesh();
        mesh.name = "DiamondMesh";

        // 2. 정팔면체 정점 6개 좌표 정의
        Vector3[] vertices = new Vector3[]
        {
            new Vector3( 0.0f,  1.5f,  0.0f), // 0: 상단 꼭짓점 (Top)
            new Vector3(-1.0f,  0.0f,  0.0f), // 1: 좌측 허리 (Left)
            new Vector3( 0.0f,  0.0f,  1.0f), // 2: 전방 허리 (Front)
            new Vector3( 1.0f,  0.0f,  0.0f), // 3: 우측 허리 (Right)
            new Vector3( 0.0f,  0.0f, -1.0f), // 4: 후방 허리 (Back)
            new Vector3( 0.0f, -1.5f,  0.0f)  // 5: 하단 꼭짓점 (Bottom)
        };

        // 3. 8개 삼각형 면 인덱스 구성 (바깥쪽 기준 시계 방향 Winding Order)
        int[] triangles = new int[]
        {
            // --- 상부 피라미드 4개 면 (v0 기준) ---
            0, 2, 1,  // 앞-왼쪽 면
            0, 3, 2,  // 앞-오른쪽 면
            0, 4, 3,  // 뒤-오른쪽 면
            0, 1, 4,  // 뒤-왼쪽 면

            // --- 하부 역피라미드 4개 면 (v5 기준) ---
            5, 1, 2,  // 앞-왼쪽 하단 면
            5, 2, 3,  // 앞-오른쪽 하단 면
            5, 3, 4,  // 뒤-오른쪽 하단 면
            5, 4, 1   // 뒤-왼쪽 하단 면
        };

        // 4. mesh에 데이터 할당 및 법선 재계산
        mesh.vertices = vertices;
        mesh.triangles = triangles;
        mesh.RecalculateNormals(); // 빛 반사 법선 벡터 자동 계산
        mesh.RecalculateBounds();

        // 5. MeshFilter에 연결
        GetComponent<MeshFilter>().mesh = mesh;
    }
}