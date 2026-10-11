using UnityEngine;

[ExecuteAlways]
[RequireComponent(typeof(DiamondMesh))]
public class S09_Shear : MonoBehaviour
{
    [Header("전단 변환 계수 (학번 끝자리 6 -> k = 1.4)")]
    [SerializeField] float k = 1.4f;

    DiamondMesh diamondMesh;

    void Awake()
    {
        diamondMesh = GetComponent<DiamondMesh>();
    }

    void Update()
    {
        if (diamondMesh == null)
            diamondMesh = GetComponent<DiamondMesh>();

        if (diamondMesh == null || diamondMesh.BaseVertices == null) return;

        // 원본 정점 가져오기
        Vector3[] baseVerts = diamondMesh.BaseVertices;
        Vector3[] newVerts = new Vector3[baseVerts.Length];

        // 4x4 전단 변환 행렬 생성
        float[,] M = ShearMatrixRaw(k);

        // 모든 정점에 전단 변환 행렬 곱 적용
        for (int i = 0; i < baseVerts.Length; i++)
        {
            Vector4 h = ToHomogeneous(baseVerts[i]);
            h = MultiplyMatrixVectorRaw(M, h);
            newVerts[i] = FromHomogeneous(h);
        }

        // 변환된 정점을 메시에 적용
        diamondMesh.SetVertices(newVerts);

        // 꼭대기 정점 (5번 정점: 0.5, 1, 0.5) 결과 로그 출력
        if (newVerts.Length > 5)
        {
            Debug.Log($"[k = {k}] 꼭대기 정점 (0.5, 1, 0.5) 변환 결과: {newVerts[5]}");
        }
    }

    // 1. 핵심 과제: 4x4 전단 변환(Shear) 행렬 빌더
    // 점 (x, y, z) -> (x + k*y, y, z)
    float[,] ShearMatrixRaw(float k)
    {
        return new float[,] {
            { 1f,   k,  0f, 0f },  // 1행: x + k*y
            { 0f,  1f,  0f, 0f },  // 2행: y 그대로
            { 0f,  0f,  1f, 0f },  // 3행: z 그대로
            { 0f,  0f,  0f, 1f }   // 4행: w = 1
        };
    }

    // 2. 동차좌표 변환 보조 함수 (3차원 점 -> 4차원 동차좌표, w=1)
    Vector4 ToHomogeneous(Vector3 v)
    {
        return new Vector4(v.x, v.y, v.z, 1f);
    }

    // 3. 4차원 동차좌표 -> 3차원 점
    Vector3 FromHomogeneous(Vector4 h)
    {
        return new Vector3(h.x, h.y, h.z);
    }

    // 4. 4x4 행렬과 4차원 벡터 곱셈 (행렬의 각 행과 벡터의 내적)
    Vector4 MultiplyMatrixVectorRaw(float[,] M, Vector4 v)
    {
        float[] input = { v.x, v.y, v.z, v.w };
        float[] result = new float[4];
        for (int row = 0; row < 4; row++)
        {
            for (int col = 0; col < 4; col++)
            {
                result[row] += M[row, col] * input[col];
            }
        }
        return new Vector4(result[0], result[1], result[2], result[3]);
    }
}