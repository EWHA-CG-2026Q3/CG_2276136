using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

[RequireComponent(typeof(RawImage))]
public class S07_Clipping : MonoBehaviour
{
    [SerializeField] private int canvasWidth = 256;
    [SerializeField] private int canvasHeight = 256;
    [SerializeField] private float clipMargin = 30f; // 회색 여백선 크기

    private Texture2D canvasTexture;
    private RawImage targetImage;

    void Start()
    {
        targetImage = GetComponent<RawImage>();
        canvasTexture = new Texture2D(canvasWidth, canvasHeight);
        canvasTexture.filterMode = FilterMode.Point;

        // 배경 및 회색 여백선 그리기
        DrawBackgroundAndBoundary();

        // 왼쪽(X < 30)과 위쪽(Y > 226) 두 방향으로 삐져나가는 다각형 좌표
        List<Vector2> polygon = new List<Vector2>
        {
            new Vector2(10f, 150f),   // 왼쪽 여백선(30)을 뚫고 나감
            new Vector2(128f, 250f),  // 위쪽 여백선(226)을 뚫고 나감
            new Vector2(210f, 160f),  // 내부
            new Vector2(160f, 50f),   // 내부
            new Vector2(60f, 50f)     // 내부
        };

        // 4방향 Sutherland-Hodgman 클리핑 적용
        List<Vector2> clipped = ClipPolygon(polygon);

        // 잘려나간 다각형을 팬 삼각분할(Fan Triangulation)로 렌더링
        DrawClippedPolygon(clipped, new Color(0.2f, 0.8f, 0.4f, 1f)); // 초록색

        canvasTexture.Apply();
        targetImage.texture = canvasTexture;
    }

    private List<Vector2> ClipPolygon(List<Vector2> poly)
    {
        float left = clipMargin;
        float right = canvasWidth - clipMargin;
        float bottom = clipMargin;
        float top = canvasHeight - clipMargin;

        poly = ClipLeft(poly, left);
        poly = ClipRight(poly, right);
        poly = ClipBottom(poly, bottom);
        poly = ClipTop(poly, top);

        return poly;
    }

    private List<Vector2> ClipLeft(List<Vector2> input, float boundary)
    {
        List<Vector2> output = new List<Vector2>();
        for (int i = 0; i < input.Count; i++)
        {
            Vector2 current = input[i];
            Vector2 prev = input[(i - 1 + input.Count) % input.Count];

            bool curIn = current.x >= boundary;
            bool prevIn = prev.x >= boundary;

            if (curIn)
            {
                if (!prevIn) output.Add(GetIntersectionX(prev, current, boundary));
                output.Add(current);
            }
            else if (prevIn)
            {
                output.Add(GetIntersectionX(prev, current, boundary));
            }
        }
        return output;
    }

    // [TODO] ClipRight
    private List<Vector2> ClipRight(List<Vector2> input, float boundary)
    {
        List<Vector2> output = new List<Vector2>();
        for (int i = 0; i < input.Count; i++)
        {
            Vector2 current = input[i];
            Vector2 prev = input[(i - 1 + input.Count) % input.Count];

            bool curIn = current.x <= boundary;
            bool prevIn = prev.x <= boundary;

            if (curIn)
            {
                if (!prevIn) output.Add(GetIntersectionX(prev, current, boundary));
                output.Add(current);
            }
            else if (prevIn)
            {
                output.Add(GetIntersectionX(prev, current, boundary));
            }
        }
        return output;
    }

    // [TODO] ClipBottom
    private List<Vector2> ClipBottom(List<Vector2> input, float boundary)
    {
        List<Vector2> output = new List<Vector2>();
        for (int i = 0; i < input.Count; i++)
        {
            Vector2 current = input[i];
            Vector2 prev = input[(i - 1 + input.Count) % input.Count];

            bool curIn = current.y >= boundary;
            bool prevIn = prev.y >= boundary;

            if (curIn)
            {
                if (!prevIn) output.Add(GetIntersectionY(prev, current, boundary));
                output.Add(current);
            }
            else if (prevIn)
            {
                output.Add(GetIntersectionY(prev, current, boundary));
            }
        }
        return output;
    }

    // [TODO] ClipTop
    private List<Vector2> ClipTop(List<Vector2> input, float boundary)
    {
        List<Vector2> output = new List<Vector2>();
        for (int i = 0; i < input.Count; i++)
        {
            Vector2 current = input[i];
            Vector2 prev = input[(i - 1 + input.Count) % input.Count];

            bool curIn = current.y <= boundary;
            bool prevIn = prev.y <= boundary;

            if (curIn)
            {
                if (!prevIn) output.Add(GetIntersectionY(prev, current, boundary));
                output.Add(current);
            }
            else if (prevIn)
            {
                output.Add(GetIntersectionY(prev, current, boundary));
            }
        }
        return output;
    }

    private Vector2 GetIntersectionX(Vector2 p1, Vector2 p2, float boundaryX)
    {
        float t = (boundaryX - p1.x) / (p2.x - p1.x);
        return new Vector2(boundaryX, p1.y + t * (p2.y - p1.y));
    }

    // [TODO] GetIntersectionY
    private Vector2 GetIntersectionY(Vector2 p1, Vector2 p2, float boundaryY)
    {
        float t = (boundaryY - p1.y) / (p2.y - p1.y);
        return new Vector2(p1.x + t * (p2.x - p1.x), boundaryY);
    }

    private void DrawBackgroundAndBoundary()
    {
        for (int x = 0; x < canvasWidth; x++)
        {
            for (int y = 0; y < canvasHeight; y++)
            {
                bool isBorder = (x == (int)clipMargin || x == (int)(canvasWidth - clipMargin) ||
                                 y == (int)clipMargin || y == (int)(canvasHeight - clipMargin));
                canvasTexture.SetPixel(x, y, isBorder ? Color.gray : Color.black);
            }
        }
    }

    private void DrawClippedPolygon(List<Vector2> poly, Color col)
    {
        if (poly.Count < 3) return;
        // 0번 꼭짓점을 축으로 삼아 N-2개 삼각형으로 분할 렌더링
        for (int i = 1; i < poly.Count - 1; i++)
        {
            DrawTriangle(poly[0], poly[i], poly[i + 1], col);
        }
    }

    private void DrawTriangle(Vector2 a, Vector2 b, Vector2 c, Color color)
    {
        int minX = Mathf.Clamp((int)Mathf.Min(a.x, Mathf.Min(b.x, c.x)), 0, canvasWidth - 1);
        int maxX = Mathf.Clamp((int)Mathf.Max(a.x, Mathf.Max(b.x, c.x)), 0, canvasWidth - 1);
        int minY = Mathf.Clamp((int)Mathf.Min(a.y, Mathf.Min(b.y, c.y)), 0, canvasHeight - 1);
        int maxY = Mathf.Clamp((int)Mathf.Max(a.y, Mathf.Max(b.y, c.y)), 0, canvasHeight - 1);

        for (int x = minX; x <= maxX; x++)
        {
            for (int y = minY; y <= maxY; y++)
            {
                Vector2 p = new Vector2(x + 0.5f, y + 0.5f);
                float denom = a.x * (b.y - c.y) + b.x * (c.y - a.y) + c.x * (a.y - b.y);
                if (Mathf.Approximately(denom, 0f)) continue;

                float w1 = (p.x * (b.y - c.y) + b.x * (c.y - p.y) + c.x * (p.y - b.y)) / denom;
                float w2 = (a.x * (p.y - c.y) + p.x * (c.y - a.y) + c.x * (a.y - p.y)) / denom;
                float w3 = 1f - w1 - w2;

                if (w1 >= 0f && w2 >= 0f && w3 >= 0f)
                {
                    canvasTexture.SetPixel(x, y, color);
                }
            }
        }
    }
}