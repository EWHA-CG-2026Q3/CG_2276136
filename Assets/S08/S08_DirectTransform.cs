using UnityEngine;

[RequireComponent(typeof(MeshFilter), typeof(MeshRenderer))]
public class S08_DirectTransform : MonoBehaviour
{
    public enum DemoMode
    {
        Original,
        TranslateThenScale, // 이동 먼저 -> 스케일 나중
        ScaleThenTranslate  // 스케일 먼저 -> 이동 나중
    }

    [Header("변환 모드 선택")]
    public DemoMode currentMode = DemoMode.Original;

    [Header("변환 값 설정")]
    public Vector3 translation = new Vector3(2f, 1f, 0f);
    public Vector3 scale = new Vector3(2f, 2f, 2f);

    private Mesh originalMesh;
    private Mesh instanceMesh;
    private Vector3[] originalVertices;

    void Start()
    {
        MeshFilter mf = GetComponent<MeshFilter>();
        originalMesh = mf.sharedMesh;

        // 원본 메시가 망가지지 않도록 인스턴스 복제본 생성
        instanceMesh = Instantiate(originalMesh);
        mf.mesh = instanceMesh;

        originalVertices = originalMesh.vertices;
        ApplyTransformation();
    }

    void OnValidate()
    {
        // 에디터 인스펙터에서 값을 바꿀 때 실시간 반영
        if (instanceMesh != null && originalVertices != null)
        {
            ApplyTransformation();
        }
    }

    void ApplyTransformation()
    {
        Vector3[] transformed = new Vector3[originalVertices.Length];

        for (int i = 0; i < originalVertices.Length; i++)
        {
            Vector3 v = originalVertices[i];

            switch (currentMode)
            {
                case DemoMode.Original:
                    transformed[i] = v;
                    break;

                // [TODO 1]: TranslateThenScale
                // 점을 먼저 이동시킨 뒤, 그 결과에 스케일을 곱함
                case DemoMode.TranslateThenScale:
                    Vector3 translated = v + translation;
                    transformed[i] = Vector3.Scale(translated, scale);
                    break;

                // [TODO 2]: ScaleThenTranslate
                // 점에 스케일을 먼저 곱한 뒤, 그 결과에 이동 벡터를 더함
                case DemoMode.ScaleThenTranslate:
                    Vector3 scaled = Vector3.Scale(v, scale);
                    transformed[i] = scaled + translation;
                    break;
            }
        }

        instanceMesh.vertices = transformed;
        instanceMesh.RecalculateBounds();
        instanceMesh.RecalculateNormals();
    }
}