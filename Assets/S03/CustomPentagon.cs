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

        mesh.vertices = vertices;
    }
}