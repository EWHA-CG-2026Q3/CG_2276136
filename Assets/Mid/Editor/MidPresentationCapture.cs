using System;
using System.IO;
using System.Linq;
using UnityEditor;
using UnityEditor.Media;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;

public static class MidPresentationCapture
{
    private const string ScenePath = "Assets/Mid/Mid_Landscape.unity";
    private const string Output = "Recordings/Mid/MovingTrailReview";
    private const float Duration = 30f;

    private struct CameraKey
    {
        public readonly float Time;
        public readonly Vector3 Position;
        public readonly Vector3 Aim;
        public readonly float FieldOfView;

        public CameraKey(float time, Vector3 position, Vector3 aim, float fieldOfView)
        {
            Time = time;
            Position = position;
            Aim = aim;
            FieldOfView = fieldOfView;
        }
    }

    private static readonly CameraKey[] PathKeys =
    {
        new CameraKey(0f, new Vector3(-2.9f, 2.42f, -6.6f), new Vector3(0f, 2.22f, 0.55f), 49f),
        new CameraKey(7f, new Vector3(-1.04f, 2.12f, -3.90f), new Vector3(0f, 1.97f, 0.55f), 48f),
        new CameraKey(13f, new Vector3(-0.72f, 2.18f, -2.28f), new Vector3(0f, 2.03f, 0.55f), 45f),
        new CameraKey(19f, new Vector3(-1.23f, 2.23f, -1.76f), new Vector3(0f, 1.96f, 0.55f), 45f),
        new CameraKey(24f, new Vector3(-1.28f, 2.60f, -1.72f), new Vector3(0f, 1.26f, 0.55f), 46f),
        new CameraKey(Duration, new Vector3(-1.15f, 2.83f, -1.59f), new Vector3(0f, 1.04f, 0.55f), 48f)
    };

    public static void Preview()
    {
        using var capture = new Capture(1600, 900);
        capture.Render(1.1f);
        capture.SaveImage("01-Moonlit-Pavilion.png");
        capture.Render(9.5f);
        capture.SaveImage("04-Approach.png");
        capture.Render(18f);
        capture.SaveImage("02-Seal-and-Orbits.png");
        capture.Render(29f);
        capture.SaveImage("03-Hearth-and-Tablet.png");
    }

    public static void PreviewAndEncode()
    {
        Preview();
        Encode();
    }

    public static void Encode()
    {
        using var capture = new Capture(1280, 720);
        const int fps = 24;
        var settings = new VideoTrackAttributes
        {
            frameRate = new MediaRational(fps),
            width = 1280,
            height = 720,
            includeAlpha = false,
            bitRateMode = VideoBitrateMode.High
        };
        using var video = new MediaEncoder(System.IO.Path.GetFullPath(Output + "/Mid_OneTake_30s.mp4"), settings);
        int frameCount = (int)(Duration * fps);
        for (int frame = 0; frame < frameCount; frame++)
        {
            capture.Render(frame * Duration / (frameCount - 1));
            if (!video.AddFrame(capture.Image))
                throw new InvalidOperationException("Media encoder rejected a frame.");
        }
        Debug.Log($"[Mid capture] Encoded {frameCount} continuous frames at {fps} fps.");
    }

    private static void SamplePath(float time, out Vector3 position, out Vector3 aim, out float fov)
    {
        int i = 0;
        while (i < PathKeys.Length - 2 && time > PathKeys[i + 1].Time)
            i++;

        var a = PathKeys[i];
        var b = PathKeys[i + 1];
        float span = b.Time - a.Time;
        float t = Mathf.Clamp01((time - a.Time) / span);
        position = Interpolate(a.Position, b.Position, Tangent(i, false), Tangent(i + 1, false), span, t);
        aim = Interpolate(a.Aim, b.Aim, Tangent(i, true), Tangent(i + 1, true), span, t);
        fov = Mathf.SmoothStep(a.FieldOfView, b.FieldOfView, t);
    }

    private static Vector3 Tangent(int index, bool useAim)
    {
        if (index == 0 || index == PathKeys.Length - 1)
            return Vector3.zero;

        var a = PathKeys[index - 1];
        var b = PathKeys[index + 1];
        Vector3 displacement = useAim ? b.Aim - a.Aim : b.Position - a.Position;
        return displacement / (b.Time - a.Time);
    }

    private static Vector3 Interpolate(Vector3 a, Vector3 b, Vector3 from, Vector3 to, float span, float t)
    {
        float t2 = t * t;
        float t3 = t2 * t;
        return (2f * t3 - 3f * t2 + 1f) * a +
            (t3 - 2f * t2 + t) * span * from +
            (-2f * t3 + 3f * t2) * b +
            (t3 - t2) * span * to;
    }
    private sealed class Capture : IDisposable
    {
        private readonly Camera camera;
        private readonly MidRelicMotion motion;
        private readonly ParticleSystem smoke;
        private readonly Renderer flame;
        private readonly MaterialPropertyBlock flameProperties = new MaterialPropertyBlock();
        private readonly RenderTexture target;
        private float previousSmokeTime = -1f;
        public readonly Texture2D Image;

        public Capture(int width, int height)
        {
            var scene = EditorSceneManager.OpenScene(ScenePath);
            var roots = scene.GetRootGameObjects();
            camera = roots.Single(root => root.name == "ShotCamera").GetComponent<Camera>();
            camera.gameObject.SetActive(true);
            camera.enabled = false;
            camera.aspect = (float)width / height;
            motion = roots.Single(root => root.name == "Seal Center").GetComponent<MidRelicMotion>();
            motion.RememberPose();
            var hearth = roots.Single(root => root.name == "Central Hearth");
            smoke = hearth.GetComponentInChildren<ParticleSystem>();
            flame = hearth.transform.Find("Short Ember Flames").GetComponent<Renderer>();
            if (smoke != null)
            {
                smoke.useAutoRandomSeed = false;
                smoke.randomSeed = 1357;
            }

            Directory.CreateDirectory(Output);
            target = new RenderTexture(width, height, 24, RenderTextureFormat.ARGB32);
            target.Create();
            Image = new Texture2D(width, height, TextureFormat.RGBA32, false);
        }

        public void Render(float time)
        {
            SamplePath(time, out var position, out var aim, out var fieldOfView);
            camera.transform.position = position;
            camera.transform.LookAt(aim);
            camera.fieldOfView = fieldOfView;
            motion.ApplyPose(time);
            flameProperties.SetFloat("_PreviewTime", time);
            flame.SetPropertyBlock(flameProperties);
            if (smoke != null)
            {
                bool restart = time < previousSmokeTime || previousSmokeTime < 0f;
                smoke.Simulate(restart ? 1.1f + time : time - previousSmokeTime, true, restart, false);
                previousSmokeTime = time;
            }
            var request = new UniversalRenderPipeline.SingleCameraRequest { destination = target };
            if (!RenderPipeline.SupportsRenderRequest(camera, request))
                throw new InvalidOperationException("URP capture request is unavailable.");
            RenderPipeline.SubmitRenderRequest(camera, request);
            var previous = RenderTexture.active;
            RenderTexture.active = target;
            Image.ReadPixels(new Rect(0, 0, target.width, target.height), 0, 0);
            Image.Apply(false);
            RenderTexture.active = previous;
        }

        public void SaveImage(string fileName)
        {
            File.WriteAllBytes(Path.Combine(Output, fileName), Image.EncodeToPNG());
            Debug.Log("[Mid capture] Saved " + fileName);
        }

        public void Dispose()
        {
            target.Release();
            UnityEngine.Object.DestroyImmediate(target);
            UnityEngine.Object.DestroyImmediate(Image);
        }
    }
}
