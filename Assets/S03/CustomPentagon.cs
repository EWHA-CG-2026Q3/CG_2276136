using UnityEngine;

[RequireComponent(typeof(MeshFilter), typeof(MeshRenderer))]
public class S03_CustomPentagon : MonoBehaviour
{
    void Start()
    {
        // 1. Mesh 인스턴스 생성
        Mesh mesh = new Mesh();
        mesh.name = "PentagonMesh";

        // 2. 정오각형 정점 5개 좌표 정의 (반지름 1.5 기준)
        Vector3[] vertices = new Vector3[]
        {
            new Vector3( 0.0f,  2.0f, 0.0f), // 0번: 꼭대기
            new Vector3( 1.5f,  0.5f, 0.0f), // 1번: 오른쪽 위
            new Vector3( 1.0f, -1.5f, 0.0f), // 2번: 오른쪽 아래
            new Vector3(-1.0f, -1.5f, 0.0f), // 3번: 왼쪽 아래
            new Vector3(-1.5f,  0.5f, 0.0f)  // 4번: 왼쪽 위
        };

        // 3. 삼각형 3개 인덱스 버퍼 구성 (Winding Order: 시계 방향)
        int[] triangles = new int[]
        {
            0, 1, 2,  // 삼각형 1 (0 -> 1 -> 2)
            0, 2, 3,  // 삼각형 2 (0 -> 2 -> 3)
            0, 3, 4   // 삼각형 3 (0 -> 3 -> 4)
        };
        // 4. 메시에 데이터 주입 및 법선 재계산
        mesh.vertices = vertices;
        mesh.triangles = triangles;
        mesh.RecalculateNormals();
        mesh.RecalculateBounds();
        // 5. MeshFilter에 생성한 메시 연결
        GetComponent<MeshFilter>().mesh = mesh;
    }
}