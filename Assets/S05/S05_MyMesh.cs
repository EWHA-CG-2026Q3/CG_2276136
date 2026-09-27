using UnityEngine;
using UnityEngine.UI;

[RequireComponent(typeof(RawImage))]
public class S05_MyMesh : MonoBehaviour
{
    [Header("캔버스 해상도")]
    [SerializeField] private int canvasWidth = 256;
    [SerializeField] private int canvasHeight = 256;

    [Header("패턴 설정")]
    [SerializeField] private int patternSize = 32; // 줄무늬 폭 또는 체스판 한 칸 크기
    [SerializeField] private Color colorA = Color.white;
    [SerializeField] private Color colorB = new Color(0.2f, 0.4f, 0.8f, 1f); // 파란색

    public enum PatternMode { VerticalStripes, Checkerboard }
    [Header("출력 모드 선택")]
    public PatternMode currentMode = PatternMode.VerticalStripes;

    private Texture2D canvasTexture;
    private RawImage targetImage;

    void Start()
    {
        targetImage = GetComponent<RawImage>();

        // 1. 메모리에 빈 텍스처 도화지 생성
        canvasTexture = new Texture2D(canvasWidth, canvasHeight);
        canvasTexture.filterMode = FilterMode.Point; // 픽셀 경계를 또렷하게 유지

        // 2. 모드에 따라 패턴 그리기
        if (currentMode == PatternMode.VerticalStripes)
        {
            FillVerticalStripes(patternSize, colorA, colorB);
        }
        else
        {
            FillCheckerboard(patternSize, colorA, colorB);
        }

        // 3. CPU에서 작업한 픽셀 메모리를 GPU로 실제 전송 (필수!)
        canvasTexture.Apply();

        // 4. 화면 RawImage에 연결
        targetImage.texture = canvasTexture;
    }

    // 1) 세로 줄무늬 알고리즘: X 좌표를 폭으로 나눈 몫이 짝수인지 판별
    private void FillVerticalStripes(int width, Color colA, Color colB)
    {
        for (int x = 0; x < canvasWidth; x++)
        {
            bool isColorA = (x / width) % 2 == 0;
            Color stripeColor = isColorA ? colA : colB;

            for (int y = 0; y < canvasHeight; y++)
            {
                canvasTexture.SetPixel(x, y, stripeColor);
            }
        }
    }

    // 2) 체스판 무늬 알고리즘: (X 칸 번호 + Y 칸 번호)의 합이 짝수인지 판별
    private void FillCheckerboard(int size, Color colA, Color colB)
    {
        for (int x = 0; x < canvasWidth; x++)
        {
            for (int y = 0; y < canvasHeight; y++)
            {
                bool isColorA = ((x / size) + (y / size)) % 2 == 0;
                Color cellColor = isColorA ? colA : colB;

                canvasTexture.SetPixel(x, y, cellColor);
            }
        }
    }
}