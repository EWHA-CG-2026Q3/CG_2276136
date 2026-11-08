using UnityEngine;

public static class MidSealGeometry
{
    private const int Sides = 5;
    private const int PoleTop = 10;
    private const int PoleBottom = 11;

    public static Mesh CreateStone()
    {
        var points = new Vector3[12];
        float height = 1f / Mathf.Sqrt(5f);
        float radius = 2f * height;
        float step = 2f * Mathf.PI / Sides;

        for (int i = 0; i < Sides; i++)
        {
            points[i] = RingPoint(i * step, height, radius);
            points[i + Sides] = RingPoint((i + 0.5f) * step, -height, radius);
        }

        points[PoleTop] = Vector3.up;
        points[PoleBottom] = Vector3.down;
        // Duplicate each face's vertices so the stone keeps its sharp edges.
        var vertices = new Vector3[60];
        var indices = new int[vertices.Length];
        int index = 0;

        for (int i = 0; i < Sides; i++)
        {
            int next = (i + 1) % Sides;
            AddFace(points[PoleTop], points[i], points[next]);
            AddFace(points[i], points[i + Sides], points[next]);
            AddFace(points[next], points[i + Sides], points[next + Sides]);
            AddFace(points[PoleBottom], points[next + Sides], points[i + Sides]);
        }

        var mesh = new Mesh { name = "Mid Regular Icosahedron" };
        mesh.vertices = vertices;
        mesh.triangles = indices;
        mesh.RecalculateNormals();
        mesh.RecalculateBounds();
        return mesh;

        void AddFace(Vector3 a, Vector3 b, Vector3 c)
        {
            if (Vector3.Dot(Vector3.Cross(b - a, c - a), a + b + c) < 0f)
                (b, c) = (c, b);

            vertices[index++] = a;
            vertices[index++] = b;
            vertices[index++] = c;
            indices[index - 3] = index - 3;
            indices[index - 2] = index - 2;
            indices[index - 1] = index - 1;
        }
    }

    private static Vector3 RingPoint(float angle, float height, float radius)
    {
        return new Vector3(radius * Mathf.Cos(angle), height, radius * Mathf.Sin(angle));
    }
}
