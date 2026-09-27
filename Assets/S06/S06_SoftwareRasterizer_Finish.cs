using UnityEngine;
using UnityEngine.UI;

[RequireComponent(typeof(RawImage))]
public class S06_SoftwareRasterizer : MonoBehaviour
{
    [SerializeField] private int canvasWidth = 256;
    [SerializeField] private int canvasHeight = 256;
    [SerializeField] private Color backgroundColor = Color.black;

    // 나만의 삼각형 좌표와 색상 설정
    [Header("나만의 커스텀 삼각형 설정")]
    [SerializeField] private Vector2 v0 = new Vector2(40f, 30f);    // 좌측 하단
    [SerializeField] private Vector2 v1 = new Vector2(220f, 70f);   // 우측 하단
    [SerializeField] private Vector2 v2 = new Vector2(100f, 220f);  // 상단 꼭짓점
    [SerializeField] private Color triangleColor = new Color(0.9f, 0.2f, 0.4f, 1f); // 마젠타 핑크

    private Texture2D canvasTexture;
    private RawImage targetImage;

    void Start()
    {
        targetImage = GetComponent<RawImage>();

        // 1. 도화지 텍스처 생성
        canvasTexture = new Texture2D(canvasWidth, canvasHeight);
        canvasTexture.filterMode = FilterMode.Point;

        // 2. 배경 채우기
        Color[] pixels = new Color[canvasWidth * canvasHeight];
        for (int i = 0; i < pixels.Length; i++) pixels[i] = backgroundColor;
        canvasTexture.SetPixels(pixels);

        // 3. 소프트웨어 래스터라이징 (무게중심좌표계 판정)
        DrawTriangle(v0, v1, v2, triangleColor);

        // 4. GPU 텍스처 반영 및 UI 출력
        canvasTexture.Apply();
        targetImage.texture = canvasTexture;
    }

    private void DrawTriangle(Vector2 a, Vector2 b, Vector2 c, Color col)
    {
        for (int x = 0; x < canvasWidth; x++)
        {
            for (int y = 0; y < canvasHeight; y++)
            {
                // 픽셀의 정중앙(+0.5f)을 검사 지점으로 사용
                Vector2 p = new Vector2(x + 0.5f, y + 0.5f);

                if (IsInsideTriangle(p, a, b, c))
                {
                    canvasTexture.SetPixel(x, y, col);
                }
            }
        }
    }

    // 무게중심좌표계(Barycentric Coordinates) 내부 판정 공식
    private bool IsInsideTriangle(Vector2 p, Vector2 a, Vector2 b, Vector2 c)
    {
        float denom = a.x * (b.y - c.y) + b.x * (c.y - a.y) + c.x * (a.y - b.y);
        if (Mathf.Approximately(denom, 0f)) return false;

        float w1 = (p.x * (b.y - c.y) + b.x * (c.y - p.y) + c.x * (p.y - b.y)) / denom;
        float w2 = (a.x * (p.y - c.y) + p.x * (c.y - a.y) + c.x * (a.y - p.y)) / denom;
        float w3 = 1f - w1 - w2;

        return (w1 >= 0f) && (w2 >= 0f) && (w3 >= 0f);
    }
}