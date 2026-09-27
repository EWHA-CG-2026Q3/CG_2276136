using UnityEngine;

public class S06_GLTriangle : MonoBehaviour
{
    private Material glMaterial;

    void CreateLineMaterial()
    {
        if (!glMaterial)
        {
            // GL 전용 유니티 내장 컬러 셰이더 (분홍색 에러 방지용)
            Shader shader = Shader.Find("Hidden/Internal-Colored");
            glMaterial = new Material(shader);
            glMaterial.hideFlags = HideFlags.HideAndDontSave;
        }
    }

    void OnRenderObject()
    {
        CreateLineMaterial();

        // 1. GPU 패스 준비
        glMaterial.SetPass(0);

        // 2. 2D 직교 투영 좌표계로 변환 (화면 비율 0.0 ~ 1.0)
        GL.PushMatrix();
        GL.LoadOrtho();

        // 3. GL Immediate Mode 렌더링 시작
        GL.Begin(GL.TRIANGLES);

        // 상단 꼭짓점 (빨강)
        GL.Color(new Color(1f, 0.2f, 0.2f, 1f));
        GL.Vertex3(0.5f, 0.8f, 0f);

        // 좌측 하단 꼭짓점 (초록)
        GL.Color(new Color(0.2f, 1f, 0.2f, 1f));
        GL.Vertex3(0.2f, 0.2f, 0f);

        // 우측 하단 꼭짓점 (파랑)
        GL.Color(new Color(0.2f, 0.4f, 1f, 1f));
        GL.Vertex3(0.8f, 0.2f, 0f);

        GL.End();
        GL.PopMatrix();
    }
}