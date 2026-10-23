using UnityEngine;
using UnityEngine.UI;
using UnityEngine.Rendering;

public class S12_VirtualCamera_Finish : MonoBehaviour
{
    [Header("그릴 오브젝트")]
    public MeshFilter[] targets;                                  // 캔버스에 그릴 오브젝트들의 MeshFilter

    [Header("캔버스에 담을 범위 (원근투영)")]
    public float fieldOfView = 90f;                               // 세로 시야각 (Degree)
    public float near = 0.3f;                                     // Near 평면 (이보다 앞은 그리지 않음)
    public float far = 20f;                                       // Far 평면 (이보다 뒤는 그리지 않음)

    [Header("캔버스")]
    public RawImage canvasImage;                                  // 캔버스를 보여 줄 RawImage
    public int width = 400;                                       // 캔버스 가로 픽셀 수
    public int height = 300;                                      // 캔버스 세로 픽셀 수
    public Color backgroundColor = new Color(0, 0, 0, 0);         // 투명 배경
    public Color lineColor = Color.white;                         // 와이어프레임 선 색

    Texture2D canvas;
    Color[] clearPixels;
    Matrix4x4 P;                                                  // 원근 투영 행렬

    void OnEnable()
    {
        CreateCanvas();
        RenderPipelineManager.beginContextRendering += OnBeginRendering;
    }

    void OnDisable()
    {
        RenderPipelineManager.beginContextRendering -= OnBeginRendering;
    }

    void OnBeginRendering(ScriptableRenderContext context, System.Collections.Generic.List<Camera> cameras)
    {
        if (canvas == null || canvas.width != width || canvas.height != height)
            CreateCanvas();

        DrawSceneOnCanvas();
    }

    void CreateCanvas()
    {
        canvas = new Texture2D(width, height, TextureFormat.RGBA32, false);
        canvas.filterMode = FilterMode.Point;
        clearPixels = new Color[width * height];
        for (int i = 0; i < clearPixels.Length; i++) clearPixels[i] = backgroundColor;
        if (canvasImage != null)
        {
            canvasImage.texture = canvas;
            canvasImage.rectTransform.sizeDelta = new Vector2(width, height);
        }
    }

    Matrix4x4 MultiplyMatrixMatrix(Matrix4x4 A, Matrix4x4 B)
    {
        Matrix4x4 result = new Matrix4x4();
        for (int col = 0; col < 4; col++)
        {
            Vector4 bCol = B.GetColumn(col);
            Vector4 resCol = bCol.x * A.GetColumn(0) 
                           + bCol.y * A.GetColumn(1) 
                           + bCol.z * A.GetColumn(2) 
                           + bCol.w * A.GetColumn(3);
            result.SetColumn(col, resCol);
        }
        return result;
    }

    Matrix4x4 BuildViewMatrix(Transform cam)
    {
        Matrix4x4 Tinv = Matrix4x4.Translate(-cam.position);
        Matrix4x4 Rinv = Matrix4x4.Rotate(Quaternion.Inverse(cam.rotation));
        return MultiplyMatrixMatrix(Rinv, Tinv);
    }

    // [핵심] 원근 투영 행렬 P 생성 함수
    Matrix4x4 BuildProjectionMatrix()
    {
        float aspect = (float)width / height;                      // 캔버스 종횡비 (실험 시 1f로 변경)
        // [실험] 1 float aspect = 1f;
        float t = Mathf.Tan(fieldOfView * 0.5f * Mathf.Deg2Rad);   // tan(fov / 2)

        Matrix4x4 P = Matrix4x4.zero;                              // 주의: zero로 초기화!
        P.m00 = 1f / (t * aspect);                                 // x 스케일
        P.m11 = 1f / t;                                            // y 스케일
        P.m32 = 1f;                                                // 4행: [0 0 1 0] -> w = z 복사!
        return P;
    }

    void DrawSceneOnCanvas()
    {
        ClearCanvas();

        Matrix4x4 V = BuildViewMatrix(transform);
        P = BuildProjectionMatrix();                               // 프레임마다 P 생성

        foreach (MeshFilter mf in targets)
        {
            if (mf == null || mf.sharedMesh == null) continue;

            Matrix4x4 VM = V * mf.transform.localToWorldMatrix;
            Vector3[] verts = mf.sharedMesh.vertices;
            int[] tris = mf.sharedMesh.triangles;

            for (int i = 0; i < tris.Length; i += 3)
            {
                Vector3 a = VM.MultiplyPoint(verts[tris[i]]);
                Vector3 b = VM.MultiplyPoint(verts[tris[i + 1]]);
                Vector3 c = VM.MultiplyPoint(verts[tris[i + 2]]);

                DrawEdge(a, b);
                DrawEdge(b, c);
                DrawEdge(c, a);
            }
        }

        canvas.Apply();
    }

    bool ToPixel(Vector3 p, out Vector2 pixel)
    {
        pixel = Vector2.zero;

        // 카메라 뒤쪽 점 역전 방지 (p.z가 near보다 작으면 제외)
        if (p.z < near || p.z > far) return false;

        // 1. 투영 행렬 P 곱하기 (점 좌표이므로 w = 1f)
        Vector4 clip = P * new Vector4(p.x, p.y, p.z, 1f);

        // 2. 동차좌표 w(= z)로 나누어 NDC 계산 (원근 나눗셈)
        Vector2 ndc = new Vector2(clip.x / clip.w, clip.y / clip.w);

        // 3. NDC(-1 ~ 1)를 캔버스 픽셀 위치로 매핑
        pixel.x = width  * 0.5f + ndc.x * (width  * 0.5f);
        pixel.y = height * 0.5f + ndc.y * (height * 0.5f);
        return true;
    }

    void DrawEdge(Vector3 a, Vector3 b)
    {
        if (!ToPixel(a, out Vector2 pa)) return;
        if (!ToPixel(b, out Vector2 pb)) return;
        DrawLine(Mathf.RoundToInt(pa.x), Mathf.RoundToInt(pa.y),
                 Mathf.RoundToInt(pb.x), Mathf.RoundToInt(pb.y));
    }

    void DrawLine(int x0, int y0, int x1, int y1)
    {
        int dx = Mathf.Abs(x1 - x0), sx = x0 < x1 ? 1 : -1;
        int dy = -Mathf.Abs(y1 - y0), sy = y0 < y1 ? 1 : -1;
        int err = dx + dy;

        while (true)
        {
            if (x0 >= 0 && x0 < width && y0 >= 0 && y0 < height)
                canvas.SetPixel(x0, y0, lineColor);

            if (x0 == x1 && y0 == y1) break;

            int e2 = 2 * err;
            if (e2 >= dy) { err += dy; x0 += sx; }
            if (e2 <= dx) { err += dx; y0 += sy; }
        }
    }

    void ClearCanvas()
    {
        canvas.SetPixels(clearPixels);
    }
}