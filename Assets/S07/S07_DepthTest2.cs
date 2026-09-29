using UnityEngine;
using UnityEngine.UI;

[RequireComponent(typeof(RawImage))]
public class S07_DepthTest2 : MonoBehaviour
{
    [SerializeField] private int canvasWidth = 256;
    [SerializeField] private int canvasHeight = 256;
    [SerializeField] private Color backgroundColor = Color.black;

    private Texture2D canvasTexture;
    private RawImage targetImage;
    private float[,] depthBuffer;

    void Start()
    {
        targetImage = GetComponent<RawImage>();
        canvasTexture = new Texture2D(canvasWidth, canvasHeight);
        canvasTexture.filterMode = FilterMode.Point;

        depthBuffer = new float[canvasWidth, canvasHeight];

        // 1. Z-버퍼 초기화 (가장 먼 무한대 값으로 세팅)
        for (int x = 0; x < canvasWidth; x++)
        {
            for (int y = 0; y < canvasHeight; y++)
            {
                depthBuffer[x, y] = float.MaxValue;
                canvasTexture.SetPixel(x, y, backgroundColor);
            }
        }

        // 1. 빨간 삼각형: 밑변은 앞(Z=0.1), 윗 꼭짓점은 뒤(Z=0.9)
        Vector3 redA = new Vector3(30f,  60f,  0.1f);
        Vector3 redB = new Vector3(180f, 80f,  0.3f);
        Vector3 redC = new Vector3(105f, 200f, 0.9f);
        DrawTriangle(redA, redB, redC, new Color(0.9f, 0.2f, 0.3f, 1f));

        // 2. 파란 삼각형: 밑변은 뒤(Z=0.9), 윗 꼭짓점은 앞(Z=0.1)
        Vector3 blueA = new Vector3(75f,  10f,  0.9f);
        Vector3 blueB = new Vector3(225f, 60f,  0.5f);
        Vector3 blueC = new Vector3(150f, 200f, 0.1f);
        DrawTriangle(blueA, blueB, blueC, new Color(0.2f, 0.5f, 0.9f, 1f));

        canvasTexture.Apply();
        targetImage.texture = canvasTexture;
    }

    private void DrawTriangle(Vector3 a, Vector3 b, Vector3 c, Color color)
    {
        for (int x = 0; x < canvasWidth; x++)
        {
            for (int y = 0; y < canvasHeight; y++)
            {
                Vector2 p = new Vector2(x + 0.5f, y + 0.5f);
                if (IsInsideTriangle(p, a, b, c, out float w1, out float w2, out float w3))
                {
                    // [TODO 1]: 무게중심좌표로 현재 픽셀의 Z값 선형 보간
                    float interpolatedZ = w1 * a.z + w2 * b.z + w3 * c.z;

                    // [TODO 2]: Depth Test (기존 Z보다 카메라에 더 가까울 때만 갱신)
                    if (interpolatedZ < depthBuffer[x, y])
                    {
                        canvasTexture.SetPixel(x, y, color);
                        depthBuffer[x, y] = interpolatedZ;
                    }
                }
            }
        }
    }

    private bool IsInsideTriangle(Vector2 p, Vector3 a, Vector3 b, Vector3 c, out float w1, out float w2, out float w3)
    {
        float denom = a.x * (b.y - c.y) + b.x * (c.y - a.y) + c.x * (a.y - b.y);
        if (Mathf.Approximately(denom, 0f))
        {
            w1 = w2 = w3 = 0f;
            return false;
        }

        w1 = (p.x * (b.y - c.y) + b.x * (c.y - p.y) + c.x * (p.y - b.y)) / denom;
        w2 = (a.x * (p.y - c.y) + p.x * (c.y - a.y) + c.x * (a.y - p.y)) / denom;
        w3 = 1f - w1 - w2;

        return (w1 >= 0f) && (w2 >= 0f) && (w3 >= 0f);
    }
}