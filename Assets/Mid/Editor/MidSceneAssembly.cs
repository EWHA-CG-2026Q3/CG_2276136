using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;
using Object = UnityEngine.Object;

public static class MidSceneAssembly
{
    private const string SourceScenePath = "Assets/S15/S15_Landscape.unity";
    private const string ScenePath = "Assets/Mid/Mid_Landscape.unity";
    private const string Art = "Assets/Mid/Art/";
    private static readonly Vector3 Center = new Vector3(0f, 0.86f, 0.55f);

    public static void BuildAndPreview()
    {
        Build();
        MidPresentationCapture.Preview();
    }

    public static void Build()
    {
        if (!AssetDatabase.IsValidFolder(Art.TrimEnd('/')))
            AssetDatabase.CreateFolder("Assets/Mid", "Art");

        var scene = EditorSceneManager.OpenScene(SourceScenePath);
        var roots = scene.GetRootGameObjects();
        GameObject Find(string name) => roots.FirstOrDefault(root => root != null && root.name == name);

        var pavilion = Find("Pavilion");
        var hearth = Find("FireBowl");
        var floorFlow = Find("Floor_FlowQuad");

        foreach (string name in new[] {
            "Table", "Sphere", "Pentagon", "Cylinder", "Diamond_Big",
            "Diamond_Small", "Cube", "Spot Light_Table"
        })
        {
            var old = Find(name);
            if (old != null)
                Object.DestroyImmediate(old);
        }

        Lightmapping.lightingDataAsset = null;
        RenderSettings.ambientMode = AmbientMode.Flat;
        RenderSettings.ambientLight = new Color(0.15f, 0.18f, 0.27f);
        RenderSettings.fog = true;
        RenderSettings.fogColor = new Color(0.048f, 0.061f, 0.104f);
        RenderSettings.fogDensity = 0.032f;

        var moon = Find("Directional Light").GetComponent<Light>();
        moon.color = new Color(0.50f, 0.66f, 0.98f);
        moon.intensity = 1.65f;
        moon.shadows = LightShadows.Soft;
        moon.transform.rotation = Quaternion.Euler(33f, -36f, 0f);

        var fill = Find("Point Light").GetComponent<Light>();
        fill.transform.position = new Vector3(2.6f, 3.1f, -2.5f);
        fill.color = new Color(0.46f, 0.56f, 0.77f);
        fill.intensity = 1.5f;
        fill.shadows = LightShadows.None;
        fill.range = 9.5f;

        // 1. Darken Pavilion wood/roof materials into weathered occult ebony & remove built-in side benches.
        ApplyOccultPavilionTheme(pavilion);

        // The S15 flow material becomes a shallow sheet outside the pavilion.
        if (floorFlow != null)
            Object.DestroyImmediate(floorFlow);
        AddShallowWater(Find("Plane_Ground"));

        // 3. Concentric Software Rasterized Seal Tablet right at the altar center (underneath the hearth).
        var texture = MidSealRasterizer.Draw(out var sealGlow);
        var ink = texture.GetPixels().Where(pixel => pixel.a > 0.95f).ToArray();
        Require(ink.Length > 2500 && ink.Distinct().Count() > 100,
            "Step 4: seal tablet texture has over 2,500 rasterized pixels and interpolated colors.");
        var sealTexture = SaveAsset(texture, "RasterizedSeal.asset");

        var tabletMaterial = MaterialAsset("SealTablet", "Universal Render Pipeline/Lit",
            Color.white, 0.08f, 0.24f);
        tabletMaterial.SetTexture("_BaseMap", sealTexture);
        tabletMaterial.SetTexture("_MainTex", sealTexture);
        tabletMaterial.SetTexture("_EmissionMap", SaveAsset(sealGlow, "SealJunctionGlow.asset"));
        tabletMaterial.SetColor("_EmissionColor", new Color(0.64f, 0.90f, 1.02f));
        tabletMaterial.EnableKeyword("_EMISSION");
        tabletMaterial.globalIlluminationFlags = MaterialGlobalIlluminationFlags.RealtimeEmissive;
        EditorUtility.SetDirty(tabletMaterial);

        var tablet = MeshObject("Software Rasterized Seal Tablet",
            SaveAsset(CreateTablet(1.62f), "Tablet.asset"), tabletMaterial, null);
        tablet.transform.position = Center + new Vector3(0f, 0.015f, 0f);
        var lightGeometry = SaveAsset(new Material(Shader.Find("CG/Mid_ShardAfterglow")),
            "Mat_Mid_ShardAfterglow.mat");
        var sealLight = MeshObject("Low Light Above Seal Junctions",
            SaveAsset(CreateSealLight(), "SealLight.asset"), lightGeometry, tablet.transform);
        sealLight.GetComponent<MeshRenderer>().shadowCastingMode = ShadowCastingMode.Off;

        var mesh = MidSealGeometry.CreateStone();
        var meshAsset = SaveAsset(mesh, "Icosahedron.asset");
        BuildOccultHearth(hearth, meshAsset);

        Require(pavilion.GetComponentsInChildren<Renderer>().Length > 0 &&
            hearth.GetComponent<Renderer>() != null &&
            !scene.GetRootGameObjects().Any(root => root.name == "Table"),
            "Step 1: pavilion and central hearth are present, table is removed.");

        // 5. Loop-generated regular icosahedron seal stone + glowing runes + 5 orbiting shards.
        var stoneMaterial = MaterialAsset("SealStone", "Universal Render Pipeline/Lit",
            new Color(0.091f, 0.047f, 0.151f), 0.08f, 0.30f);
        stoneMaterial.EnableKeyword("_EMISSION");
        stoneMaterial.SetColor("_EmissionColor", new Color(0.06f, 0.02f, 0.12f));
        EditorUtility.SetDirty(stoneMaterial);

        var runeMaterial = MaterialAsset("EngravedRunes", "Universal Render Pipeline/Lit",
            new Color(0.25f, 0.95f, 0.88f), 0f, 0.35f);
        runeMaterial.EnableKeyword("_EMISSION");
        runeMaterial.SetColor("_EmissionColor", new Color(0.18f, 1.23f, 0.98f));
        runeMaterial.globalIlluminationFlags = MaterialGlobalIlluminationFlags.RealtimeEmissive;
        EditorUtility.SetDirty(runeMaterial);

        var corners = meshAsset.vertices;
        var edges = new float[corners.Length];
        for (int i = 0; i < corners.Length; i += 3)
        {
            edges[i] = Vector3.Distance(corners[i], corners[i + 1]);
            edges[i + 1] = Vector3.Distance(corners[i + 1], corners[i + 2]);
            edges[i + 2] = Vector3.Distance(corners[i + 2], corners[i]);
        }
        Require(meshAsset.triangles.Length == 60 &&
            corners.Select(v => new Vector3(
                Mathf.Round(v.x * 10000), Mathf.Round(v.y * 10000), Mathf.Round(v.z * 10000)))
                .Distinct().Count() == 12 &&
            edges.Max() - edges.Min() < 0.0001f,
            "Step 2: loop-generated regular icosahedron has 12 corners and 20 equal triangular faces.");

        var center = new GameObject("Seal Center");
        center.transform.position = Center + Vector3.up * 1.36f;
        var stone = MeshObject("Icosahedral Seal Stone", meshAsset, stoneMaterial, center.transform);
        stone.transform.localRotation = Quaternion.Euler(14f, -28f, -11f);
        stone.transform.localScale = Vector3.one * 0.44f;
        var inlayMaterial = MaterialAsset("SealEdgeInlay", "Universal Render Pipeline/Lit",
            new Color(0.25f, 0.48f, 0.41f), 0.29f, 0.4f);
        inlayMaterial.EnableKeyword("_EMISSION");
        inlayMaterial.SetColor("_EmissionColor", new Color(0.08f, 0.28f, 0.22f));
        EditorUtility.SetDirty(inlayMaterial);
        var inscriptions = MeshObject("Luminous Face Inscriptions",
            SaveAsset(CreateRunes(meshAsset), "Runes.asset"), runeMaterial, stone.transform);
        inscriptions.GetComponent<MeshRenderer>().sharedMaterials = new[] { runeMaterial, inlayMaterial };

        var shardMaterial = MaterialAsset("OrbitShard", "Universal Render Pipeline/Lit",
            new Color(0.11f, 0.06f, 0.19f), 0.28f, 0.46f);
        shardMaterial.EnableKeyword("_EMISSION");
        shardMaterial.SetColor("_EmissionColor", new Color(0.035f, 0.055f, 0.11f));
        shardMaterial.globalIlluminationFlags = MaterialGlobalIlluminationFlags.RealtimeEmissive;
        EditorUtility.SetDirty(shardMaterial);

        var edgeMaterial = MaterialAsset("ShardEdge", "Universal Render Pipeline/Lit",
            new Color(0.24f, 0.88f, 0.82f), 0.08f, 0.35f);
        edgeMaterial.EnableKeyword("_EMISSION");
        edgeMaterial.SetColor("_EmissionColor", new Color(0.24f, 0.92f, 0.84f));
        edgeMaterial.globalIlluminationFlags = MaterialGlobalIlluminationFlags.RealtimeEmissive;
        EditorUtility.SetDirty(edgeMaterial);

        var shardMesh = SaveAsset(CreateShard(), "OrbitShard.asset");
        // Radius, tilt, heading, phase, angular speed, and size relative to the largest shard.
        var tracks = new[] {
            (1.55f, 3f, 14f, 158f, 18f, 1f),
            (0.63f, 10f, 88f, 321f, -31f, 0.75f),
            (1.08f, 35f, 12f, 235f, 25f, 0.62f),
            (1.28f, 55f, 119f, 65f, -23f, 0.50f),
            (0.84f, 70f, 64f, 7f, 34f, 0.38f)
        };
        var orbits = new MidRelicMotion.Orbit[tracks.Length];
        var shards = new Transform[tracks.Length];
        for (int i = 0; i < tracks.Length; i++)
        {
            var (radius, tilt, heading, phase, speed, size) = tracks[i];
            var axis = new GameObject($"Shard Orbit Axis {i + 1}").transform;
            axis.SetParent(center.transform, false);
            axis.localRotation = Quaternion.AngleAxis(heading, Vector3.up) *
                Quaternion.AngleAxis(tilt, Vector3.forward);
            var shard = MeshObject($"Orbiting Shard {i + 1}", shardMesh, shardMaterial, axis);
            shard.GetComponent<MeshRenderer>().sharedMaterials = new[] { shardMaterial, edgeMaterial };
            float radians = phase * Mathf.Deg2Rad;
            shard.transform.localPosition = new Vector3(Mathf.Cos(radians), 0f, Mathf.Sin(radians)) * radius;
            shard.transform.localRotation = Quaternion.Euler(24f + i * 22f, i * 39f, 34f + i * 31f);
            shard.transform.localScale = Vector3.one * (0.165f * size);
            orbits[i] = new MidRelicMotion.Orbit
            {
                axis = axis,
                degreesPerSecond = speed
            };
            shards[i] = shard.transform;
        }

        var motion = center.AddComponent<MidRelicMotion>();
        motion.Bind(orbits, stone.transform);
        motion.RememberPose();
        var start = center.transform.position;
        var shardStarts = shards.Select(shard => shard.position).ToArray();
        motion.ApplyPose(1f);
        Require(orbits.All(orbit => orbit.axis.parent == center.transform && orbit.axis.childCount == 1) &&
            center.transform.position.y > start.y &&
            shards.Select((shard, i) => Vector3.Distance(shard.position, shardStarts[i])).All(delta => delta > 0.15f),
            "Step 3: five separate axes inherit the center's hover and move five orbiting grandchildren.");

        for (int i = 0; i < tracks.Length; i++)
        {
            Require(Vector3.Angle(orbits[i].axis.up, Vector3.up) > 1f,
                $"Orbit {i + 1}: its plane is tilted relative to the floor.");
            for (int j = i + 1; j < tracks.Length; j++)
            {
                float gap = Mathf.Abs(tracks[i].Item1 - tracks[j].Item1);
                if (gap <= shards[i].localScale.x + shards[j].localScale.x)
                    throw new InvalidOperationException($"Orbit shells {i + 1} and {j + 1} overlap.");
            }
        }
        motion.ApplyPose(0f);
        var glow = new GameObject("Seal Reflection Light").AddComponent<Light>();
        glow.transform.SetParent(center.transform, false);
        glow.transform.localPosition = new Vector3(-0.45f, 0.20f, -0.55f);
        glow.type = LightType.Point;
        glow.color = new Color(0.28f, 0.95f, 0.88f);
        glow.intensity = 0.65f;
        glow.range = 1.8f;

        var player = Find("Player").transform;
        player.position = new Vector3(-2.6f, 1.72f, -4.8f);
        player.rotation = Quaternion.LookRotation(Center + Vector3.up * 0.9f - player.position);
        var mainCamera = player.GetComponentInChildren<Camera>();
        mainCamera.backgroundColor = RenderSettings.fogColor;
        mainCamera.fieldOfView = 52f;
        var shot = Find("ShotCamera").GetComponent<Camera>();
        shot.backgroundColor = RenderSettings.fogColor;
        shot.GetUniversalAdditionalCameraData().renderPostProcessing = true;

        AssetDatabase.SaveAssets();
        EditorSceneManager.MarkSceneDirty(scene);
        Require(EditorSceneManager.SaveScene(scene, ScenePath), "Step 5: Mid scene saved with the complete setup.");
    }

    private static void AddShallowWater(GameObject ground)
    {
        var source = AssetDatabase.LoadAssetAtPath<Material>("Assets/S15/Mat_S15_SG_Flow.mat");
        if (ground == null || source == null)
            throw new InvalidOperationException("Shallow water requires the original ground and S15 flow material.");

        var material = new Material(source) { name = "Mat_Mid_ShallowWater" };
        var bounds = ground.GetComponent<Renderer>().bounds;
        foreach (string slot in new[] { "_MainTex1", "_MainTex2" })
        {
            var sourceTexture = source.GetTexture(slot);
            material.SetTexture(slot, SaveAsset(ToneWaterTexture(sourceTexture), "ShallowWater" + slot + ".asset"));
        }
        material.SetVector("_Tiling", new Vector4(
            Mathf.Max(2f, bounds.size.x / 2.3f), Mathf.Max(2f, bounds.size.z / 2.3f), 0f, 0f));
        var water = GameObject.CreatePrimitive(PrimitiveType.Plane);
        water.name = "Shallow Water Outside Pavilion";
        water.transform.position = new Vector3(bounds.center.x, bounds.max.y + 0.018f, bounds.center.z);
        water.transform.localScale = new Vector3(bounds.size.x / 10f, 1f, bounds.size.z / 10f);
        var renderer = water.GetComponent<Renderer>();
        renderer.sharedMaterial = SaveAsset(material, "Mat_Mid_ShallowWater.mat");
        renderer.shadowCastingMode = ShadowCastingMode.Off;
        Object.DestroyImmediate(water.GetComponent<Collider>());
    }

    private static Texture2D ToneWaterTexture(Texture source)
    {
        var target = RenderTexture.GetTemporary(source.width, source.height, 0, RenderTextureFormat.ARGB32);
        var previous = RenderTexture.active;
        var texture = new Texture2D(source.width, source.height, TextureFormat.RGBA32, true);
        try
        {
            Graphics.Blit(source, target);
            RenderTexture.active = target;
            texture.ReadPixels(new Rect(0, 0, source.width, source.height), 0, 0, false);
            var pixels = texture.GetPixels();
            for (int i = 0; i < pixels.Length; i++)
            {
                Color color = pixels[i];
                float gray = color.grayscale;
                Color muted = Color.Lerp(color, new Color(gray, gray, gray, color.a), 0.64f);
                pixels[i] = new Color(muted.r * 0.52f, muted.g * 0.52f, muted.b * 0.54f, color.a);
            }
            texture.SetPixels(pixels);
            texture.wrapMode = TextureWrapMode.Repeat;
            texture.filterMode = FilterMode.Trilinear;
            texture.Apply();
        }
        finally
        {
            RenderTexture.active = previous;
            RenderTexture.ReleaseTemporary(target);
        }
        return texture;
    }

    private static LineRenderer AddOrbitTrail(Transform axis, Material material, int index)
    {
        var trail = new GameObject($"Moving Shard Trail {index + 1}").AddComponent<LineRenderer>();
        trail.transform.SetParent(axis.parent, false);
        trail.transform.localRotation = axis.localRotation;
        trail.useWorldSpace = false;
        trail.positionCount = 96;
        trail.widthMultiplier = Mathf.Lerp(0.038f, 0.022f, index / 4f);
        trail.widthCurve = new AnimationCurve(
            new Keyframe(0f, 1.0f),
            new Keyframe(0.12f, 0.68f),
            new Keyframe(0.35f, 0.38f),
            new Keyframe(0.68f, 0.18f),
            new Keyframe(1f, 0.02f));

        var gradient = new Gradient();
        var headCore = new Color(0.82f, 0.99f, 1.00f);
        var midCyan = new Color(0.28f, 0.90f, 0.88f);
        var tailViolet = new Color(0.36f, 0.32f, 0.82f);
        float peak = index == 0 || index == 2 ? 0.95f : 0.84f;
        gradient.SetKeys(
            new[] {
                new GradientColorKey(headCore, 0f),
                new GradientColorKey(midCyan, 0.22f),
                new GradientColorKey(Color.Lerp(midCyan, tailViolet, 0.5f), 0.58f),
                new GradientColorKey(tailViolet, 1f)
            },
            new[] {
                new GradientAlphaKey(peak, 0f),
                new GradientAlphaKey(peak * 0.78f, 0.14f),
                new GradientAlphaKey(peak * 0.42f, 0.42f),
                new GradientAlphaKey(peak * 0.16f, 0.74f),
                new GradientAlphaKey(0f, 1f)
            });
        trail.colorGradient = gradient;
        trail.sharedMaterial = material;
        trail.shadowCastingMode = ShadowCastingMode.Off;
        trail.receiveShadows = false;
        return trail;
    }

    private static Mesh CreateSealLight()
    {
        var vertices = new List<Vector3>();
        var colors = new List<Color>();
        var triangles = new List<int>();
        var tint = new Color(0.31f, 0.66f, 0.73f);
        for (int sector = 0; sector < 3; sector++)
        {
            float angle = (90f + sector * 120f) * Mathf.Deg2Rad;
            var direction = new Vector3(Mathf.Cos(angle), 0f, Mathf.Sin(angle));
            Vector3 point = direction * (2f * 1.62f * 0.297f);
            for (int side = 0; side < 2; side++)
            {
                Vector3 across = side == 0 ? direction : Vector3.Cross(Vector3.up, direction);
                int start = vertices.Count;
                for (int row = 0; row < 3; row++)
                {
                    float height = row == 0 ? 0.005f : row == 1 ? 0.04f : 0.125f;
                    float halfWidth = row == 2 ? 0.022f : 0.048f;
                    for (int column = -1; column <= 1; column++)
                    {
                        vertices.Add(point + Vector3.up * height + across * (halfWidth * column));
                        float opacity = column != 0 || row == 2 ? 0f : row == 0 ? 0.44f : 0.23f;
                        colors.Add(new Color(tint.r, tint.g, tint.b, opacity));
                    }
                }

                for (int row = 0; row < 2; row++)
                {
                    for (int column = 0; column < 2; column++)
                    {
                        int a = start + row * 3 + column;
                        triangles.AddRange(new[] { a, a + 1, a + 3, a + 1, a + 4, a + 3 });
                    }
                }
            }
        }

        var mesh = new Mesh { name = "Low Seal Junction Light" };
        mesh.SetVertices(vertices);
        mesh.SetColors(colors);
        mesh.SetTriangles(triangles, 0);
        return mesh;
    }

    private static void ApplyOccultPavilionTheme(GameObject pavilion)
    {
        // Darken Pavilion materials into charred occult wood with subtle crimson/violet undertone.
        foreach (var renderer in pavilion.GetComponentsInChildren<MeshRenderer>())
        {
            var shared = renderer.sharedMaterials;
            for (int i = 0; i < shared.Length; i++)
            {
                if (shared[i] == null) continue;
                string matName = "Mat_Mid_Pavilion_" + shared[i].name;
                var copy = new Material(shared[i]) { name = matName };
                bool isRoof = shared[i].name.Contains("Roof");
                copy.SetColor("_BaseColor", isRoof
                    ? new Color(0.16f, 0.14f, 0.22f, 1f)
                    : new Color(0.22f, 0.17f, 0.24f, 1f));
                copy.SetColor("_Color", copy.GetColor("_BaseColor"));
                copy.SetFloat("_Smoothness", isRoof ? 0.22f : 0.36f);
                shared[i] = SaveAsset(copy, matName + ".mat");
            }
            renderer.sharedMaterials = shared;
        }

        // Remove built-in side bench seats & backrests from the Pavilion body mesh while keeping pillars, railings, and floor intact.
        var bodyFilter = pavilion.GetComponentsInChildren<MeshFilter>()
            .FirstOrDefault(f => f.gameObject.name == "Pavilion" && f.sharedMesh != null);
        if (bodyFilter != null)
        {
            var src = bodyFilter.sharedMesh;
            var verts = src.vertices;
            var norms = src.normals;
            var uvs = src.uv;

            var cleanMesh = new Mesh
            {
                name = "Mid_Pavilion_NoBenches",
                vertices = verts,
                normals = norms,
                uv = uvs,
                subMeshCount = src.subMeshCount
            };

            for (int s = 0; s < src.subMeshCount; s++)
            {
                var tris = src.GetTriangles(s);
                var kept = new List<int>(tris.Length);
                for (int t = 0; t < tris.Length; t += 3)
                {
                    Vector3 v0 = verts[tris[t]];
                    Vector3 v1 = verts[tris[t + 1]];
                    Vector3 v2 = verts[tris[t + 2]];
                    Vector3 c = (v0 + v1 + v2) / 3f;
                    Vector3 n = Vector3.Cross(v1 - v0, v2 - v0).normalized;

                    // Side benches sit inside the rear pavilion bay (z > -0.75) at seat/backrest height (0.87 < y < 1.95).
                    // Main corner/middle pillars go up to y > 3.2, and outer side railings are at |x| > 1.97.
                    float maxY = Mathf.Max(v0.y, Mathf.Max(v1.y, v2.y));
                    float minY = Mathf.Min(v0.y, Mathf.Min(v1.y, v2.y));
                    bool isInteriorBenchZone =
                        c.z > -0.75f && c.z < 2.15f &&
                        Mathf.Abs(c.x) > 0.65f && Mathf.Abs(c.x) < 1.95f &&
                        minY > 0.865f && maxY < 1.95f;

                    // Also catch horizontal seat planks that extend slightly toward the wall (up to |x| < 1.99) around y ~ 1.25.
                    bool isBenchSeatPlank =
                        c.z > -0.75f && c.z < 2.15f &&
                        Mathf.Abs(c.x) > 0.65f && Mathf.Abs(c.x) < 1.99f &&
                        c.y > 0.88f && c.y < 1.65f &&
                        Mathf.Abs(n.y) > 0.55f;

                    if (isInteriorBenchZone || isBenchSeatPlank)
                        continue;

                    kept.Add(tris[t]);
                    kept.Add(tris[t + 1]);
                    kept.Add(tris[t + 2]);
                }
                cleanMesh.SetTriangles(kept, s);
            }
            cleanMesh.RecalculateBounds();
            bodyFilter.sharedMesh = SaveAsset(cleanMesh, "Mid_Pavilion_NoBenches.asset");
        }
    }

    private static void BuildOccultHearth(GameObject hearth, Mesh coalMesh)
    {
        var bronze = MaterialAsset("Hearth", "Universal Render Pipeline/Lit",
            new Color(0.28f, 0.16f, 0.09f), 0.53f, 0.36f);
        var trim = MaterialAsset("HearthTrim", "Universal Render Pipeline/Lit",
            new Color(0.46f, 0.29f, 0.15f), 0.52f, 0.46f);
        var pedestal = MaterialAsset("AltarStone", "Universal Render Pipeline/Lit",
            new Color(0.17f, 0.145f, 0.16f), 0.04f, 0.25f);
        var coal = MaterialAsset("EldritchCoal", "Universal Render Pipeline/Lit",
            new Color(0.115f, 0.071f, 0.062f), 0f, 0.21f);
        coal.EnableKeyword("_EMISSION");
        coal.SetColor("_EmissionColor", new Color(0.105f, 0.027f, 0.008f));
        EditorUtility.SetDirty(coal);

        hearth.name = "Central Hearth";
        hearth.transform.SetPositionAndRotation(Center, Quaternion.identity);
        hearth.transform.localScale = Vector3.one;
        foreach (var filter in hearth.GetComponentsInChildren<MeshFilter>())
        {
            if (filter.gameObject != hearth)
                Object.DestroyImmediate(filter.gameObject);
        }

        var bowl = SaveAsset(CreateBowl(), "HearthBowl.asset");
        hearth.GetComponent<MeshFilter>().sharedMesh = bowl;
        hearth.GetComponent<MeshRenderer>().sharedMaterials = new[] { bronze, trim };
        var originalCollider = hearth.GetComponent<Collider>();
        if (originalCollider != null)
            Object.DestroyImmediate(originalCollider);

        var foot = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
        foot.name = "Altar Stone Pedestal";
        foot.transform.SetParent(hearth.transform, false);
        foot.transform.localPosition = Vector3.up * 0.052f;
        foot.transform.localScale = new Vector3(0.58f, 0.037f, 0.58f);
        foot.GetComponent<Renderer>().sharedMaterial = pedestal;

        for (int i = 0; i < 3; i++)
        {
            float angle = i * Mathf.PI * 2f / 3f;
            var radial = new Vector3(Mathf.Cos(angle), 0f, Mathf.Sin(angle));
            var leg = GameObject.CreatePrimitive(PrimitiveType.Cube);
            leg.name = $"Splayed Bronze Support {i + 1}";
            leg.transform.SetParent(hearth.transform, false);
            leg.transform.localPosition = radial * 0.184f + Vector3.up * 0.147f;
            leg.transform.localRotation = Quaternion.FromToRotation(Vector3.up, (Vector3.up - radial * 0.55f).normalized);
            leg.transform.localScale = new Vector3(0.051f, 0.17f, 0.063f);
            leg.GetComponent<Renderer>().sharedMaterial = bronze;
            Object.DestroyImmediate(leg.GetComponent<Collider>());
        }

        for (int i = 0; i < 7; i++)
        {
            float angle = i * 2.4f;
            float radius = 0.067f + i * 0.017f;
            var ember = MeshObject($"Coal Fragment {i + 1}", coalMesh, coal, hearth.transform);
            ember.transform.localPosition = new Vector3(Mathf.Cos(angle) * radius,
                0.316f + (i % 3) * 0.012f, Mathf.Sin(angle) * radius);
            ember.transform.localRotation = Quaternion.Euler(i * 37f, i * 23f, i * 49f);
            ember.transform.localScale = new Vector3(0.076f, 0.039f + (i % 3) * 0.009f, 0.059f);
        }

        var flameMaterial = MaterialAsset("HearthFlame", "CG/Mid_HearthFlame", Color.white, 0f, 0f);
        var flames = MeshObject("Short Ember Flames", SaveAsset(CreateFlames(), "HearthFlames.asset"),
            flameMaterial, hearth.transform);
        flames.transform.localPosition = Vector3.up * 0.332f;
        flames.GetComponent<Renderer>().shadowCastingMode = ShadowCastingMode.Off;

        var smoke = hearth.GetComponentInChildren<ParticleSystem>();
        if (smoke != null)
        {
            smoke.transform.localPosition = Vector3.up * 0.38f;
            var emission = smoke.emission;
            emission.rateOverTimeMultiplier *= 0.8f;
            var main = smoke.main;
            main.startColor = new Color(0.53f, 0.55f, 0.64f, 0.14f);
            main.startSize = new ParticleSystem.MinMaxCurve(0.065f, 0.14f);
        }

        var fire = hearth.GetComponentInChildren<Light>();
        if (fire != null)
        {
            fire.transform.localPosition = Vector3.up * 0.46f;
            fire.color = new Color(1f, 0.73f, 0.46f);
            fire.intensity = 0.64f;
            fire.shadows = LightShadows.None;
            fire.range = 1.40f;
        }
    }

    private static Mesh CreateBowl()
    {
        var profile = new Vector2[] {
            new Vector2(0.12f, 0.205f), new Vector2(0.16f, 0.219f),
            new Vector2(0.221f, 0.257f), new Vector2(0.273f, 0.316f),
            new Vector2(0.295f, 0.339f), new Vector2(0.297f, 0.349f),
            new Vector2(0.28f, 0.362f), new Vector2(0.258f, 0.358f),
            new Vector2(0.246f, 0.342f), new Vector2(0.203f, 0.291f),
            new Vector2(0.127f, 0.263f), new Vector2(0f, 0.255f)
        };
        const int sides = 64;
        var vertices = new Vector3[profile.Length * sides];
        var surface = new List<int>();
        var rim = new List<int>();

        for (int row = 0; row < profile.Length; row++)
        {
            for (int side = 0; side < sides; side++)
            {
                float angle = side * 2f * Mathf.PI / sides;
                int a = row * sides + side;
                vertices[a] = new Vector3(profile[row].x * Mathf.Cos(angle), profile[row].y,
                    profile[row].x * Mathf.Sin(angle));
                if (row == profile.Length - 1)
                    continue;

                int b = row * sides + (side + 1) % sides;
                var triangles = row == 3 || row == 5 || row == 6 ? rim : surface;
                triangles.AddRange(new[] { a, a + sides, b, b, a + sides, b + sides });
            }
        }

        var mesh = new Mesh { name = "Turned Bronze Brazier", vertices = vertices, subMeshCount = 2 };
        mesh.SetTriangles(surface, 0);
        mesh.SetTriangles(rim, 1);
        mesh.RecalculateNormals();
        return mesh;
    }

    private static Mesh CreateFlames()
    {
        var vertices = new List<Vector3>();
        var colors = new List<Color>();
        var uv = new List<Vector2>();
        var triangles = new List<int>();

        for (int tongue = 0; tongue < 3; tongue++)
        {
            float angle = tongue * 2.4f;
            var start = new Vector3(Mathf.Cos(angle), 0f, Mathf.Sin(angle)) * 0.087f;
            float height = tongue == 0 ? 0.23f : 0.16f + 0.021f * tongue;
            for (int cross = 0; cross < 2; cross++)
            {
                float facing = angle + cross * Mathf.PI * 0.5f;
                var side = new Vector3(Mathf.Cos(facing), 0f, Mathf.Sin(facing));
                for (int row = 0; row <= 8; row++)
                {
                    float v = row / 8f;
                    float width = 0.047f * Mathf.Pow(1f - v, 0.82f) + 0.002f;
                    var middle = start + Vector3.up * (height * v) + side * (0.039f * v * v);
                    var color = Color.Lerp(new Color(1.25f, 0.49f, 0.11f),
                        new Color(1.0f, 0.29f, 0.10f), v);
                    int a = vertices.Count;
                    vertices.Add(middle - side * width);
                    vertices.Add(middle + side * width);
                    colors.Add(color);
                    colors.Add(color);
                    uv.Add(new Vector2(0f, v));
                    uv.Add(new Vector2(1f, v));
                    if (row > 0)
                        triangles.AddRange(new[] { a - 2, a, a - 1, a - 1, a, a + 1 });
                }
            }
        }

        var mesh = new Mesh { name = "Three Tapered Flame Tongues" };
        mesh.SetVertices(vertices);
        mesh.SetColors(colors);
        mesh.SetUVs(0, uv);
        mesh.SetTriangles(triangles, 0);
        return mesh;
    }
    private static Mesh CreateShard()
    {
        var tips = new[] {
            new Vector3(0.06f, 1f, 0f),
            new Vector3(-0.19f, -0.74f, 0.10f)
        };
        var belt = new[] {
            new Vector3(-0.47f, -0.19f, -0.10f), new Vector3(-0.14f, 0.16f, 0.36f),
            new Vector3(0.42f, -0.15f, 0.12f), new Vector3(0.19f, 0.20f, -0.36f)
        };
        var vertices = new List<Vector3>(64);
        var surface = new List<int>(24);
        var edges = new List<int>(24);
        foreach (Vector3 tip in tips)
        {
            for (int i = 0; i < belt.Length; i++)
            {
                Vector3 a = belt[i];
                Vector3 b = belt[(i + 1) % belt.Length];
                if (Vector3.Dot(Vector3.Cross(a - tip, b - tip), tip + a + b) < 0f)
                    (a, b) = (b, a);
                int start = vertices.Count;
                vertices.AddRange(new[] { tip, a, b });
                surface.AddRange(new[] { start, start + 1, start + 2 });

                // Faint glowing fracture seam along the shard's cleaved ridges
                if (i % 2 == 0)
                {
                    Vector3 lift = Vector3.Cross(a - tip, b - tip).normalized * 0.0045f;
                    start = vertices.Count;
                    vertices.Add(Vector3.Lerp(tip, a, 0.12f) + lift);
                    vertices.Add(Vector3.Lerp(tip, a, 0.86f) + lift);
                    vertices.Add(tip * 0.18f + a * 0.70f + b * 0.12f + lift);
                    edges.AddRange(new[] { start, start + 1, start + 2 });
                }
            }
        }

        var mesh = new Mesh { name = "Sharp Orbit Shard" };
        mesh.SetVertices(vertices);
        mesh.subMeshCount = 2;
        mesh.SetTriangles(surface, 0);
        mesh.SetTriangles(edges, 1);
        mesh.RecalculateNormals();
        return mesh;
    }

    private static Mesh CreateRunes(Mesh stone)
    {
        var vertices = new List<Vector3>();
        var letters = new List<int>();
        var seams = new List<int>();
        var faces = stone.vertices;

        for (int face = 0; face < 20; face++)
        {
            Vector3 a = faces[face * 3];
            Vector3 b = faces[face * 3 + 1];
            Vector3 c = faces[face * 3 + 2];
            Vector3 normal = Vector3.Cross(b - a, c - a).normalized;
            Vector3 vertical = (Vector3.up - Vector3.Dot(normal, Vector3.up) * normal).normalized;
            Vector3 horizontal = Vector3.Cross(vertical, normal);
            Vector3 center = (a + b + c) / 3f + normal * 0.0025f;

            // Five letter skeletons and four diacritics give each face a unique inscription.
            switch (face % 5)
            {
                case 0:
                    Glyph(-0.13f, 0f, 0f, 0.15f, 0.13f, 0f, 0f, -0.15f, -0.13f, 0f);
                    Glyph(-0.13f, 0f, 0.13f, 0f);
                    break;
                case 1:
                    Glyph(0.04f, 0.15f, -0.10f, 0.065f, 0.09f, -0.03f, -0.055f, -0.15f);
                    Glyph(0.09f, -0.03f, 0.15f, -0.105f);
                    break;
                case 2:
                    Glyph(-0.13f, 0.11f, 0f, -0.02f, 0.13f, 0.11f);
                    Glyph(0f, 0.15f, 0f, -0.15f);
                    Glyph(-0.09f, -0.105f, 0.09f, -0.105f);
                    break;
                case 3:
                    Glyph(-0.115f, -0.10f, -0.115f, 0.10f, 0.12f, 0.10f, 0.12f, -0.10f);
                    Glyph(-0.115f, -0.10f, 0.12f, 0.10f);
                    break;
                default:
                    Glyph(-0.14f, 0.085f, -0.035f, 0.015f, -0.035f, -0.12f, 0.095f, -0.055f, 0.095f, 0.10f);
                    Glyph(0.095f, 0.10f, 0.016f, 0.15f, -0.055f, 0.105f);
                    break;
            }

            int mark = face / 5;
            float markX = mark < 2 ? -0.167f : 0.167f;
            float markY = mark % 2 == 0 ? 0.096f : -0.091f;
            Glyph(markX - 0.017f, markY + 0.026f, markX + 0.016f, markY - 0.008f);

            Bridge(a, b);
            Bridge(b, c);
            Bridge(c, a);

            void Glyph(params float[] points)
            {
                for (int i = 0; i < points.Length - 2; i += 2)
                {
                    Vector3 from = center + horizontal * points[i] + vertical * points[i + 1];
                    Vector3 to = center + horizontal * points[i + 2] + vertical * points[i + 3];
                    Stroke(from, to, 0.011f, letters);
                }
            }

            void Bridge(Vector3 from, Vector3 to)
            {
                Vector3 midpoint = (from + to) * 0.5f;
                if (Mathf.Abs(midpoint.y + 0.71f * midpoint.x) > 0.58f)
                    return;

                // Both adjacent faces use the same crossing point in object space.
                Vector3 crossing = midpoint * 1.004f;
                Vector3 along = (to - from).normalized;
                if (Vector3.Dot(along, new Vector3(1f, 2f, 3f)) < 0f)
                    along = -along;
                Vector3 elbow = Vector3.Lerp(crossing, center, 0.24f) + along * 0.057f;
                Vector3 cap = Vector3.Lerp(crossing, center, 0.32f) + along * 0.127f;
                Stroke(crossing, elbow, 0.0064f, seams);
                Stroke(elbow, cap, 0.0064f, seams);
            }

            void Stroke(Vector3 from, Vector3 to, float width, List<int> indices)
            {
                Vector3 edge = Vector3.Cross(normal, to - from).normalized * width;
                int start = vertices.Count;
                vertices.AddRange(new[] { from - edge, to - edge, to + edge, from + edge });
                indices.AddRange(new[] { start, start + 1, start + 2, start, start + 2, start + 3 });
            }
        }

        var mesh = new Mesh { name = "Face Letters and Interlocking Inlay" };
        mesh.SetVertices(vertices);
        mesh.subMeshCount = 2;
        mesh.SetTriangles(letters, 0);
        mesh.SetTriangles(seams, 1);
        mesh.RecalculateNormals();
        return mesh;
    }
    private static Mesh CreateTablet(float radius)
    {
        const int sides = 64;
        var vertices = new Vector3[sides + 1];
        var uv = new Vector2[vertices.Length];
        var indices = new int[sides * 3];
        uv[0] = Vector2.one * 0.5f;
        for (int i = 0; i < sides; i++)
        {
            float angle = i * Mathf.PI * 2f / sides;
            var offset = new Vector2(Mathf.Cos(angle), Mathf.Sin(angle));
            vertices[i + 1] = new Vector3(offset.x, 0f, offset.y) * radius;
            uv[i + 1] = Vector2.one * 0.5f + offset * 0.5f;
            indices[i * 3] = 0;
            indices[i * 3 + 1] = (i + 1) % sides + 1;
            indices[i * 3 + 2] = i + 1;
        }

        var mesh = new Mesh { name = "Seal Tablet Disc", vertices = vertices, uv = uv, triangles = indices };
        mesh.RecalculateNormals();
        return mesh;
    }

    private static GameObject MeshObject(string name, Mesh mesh, Material material, Transform parent)
    {
        var instance = new GameObject(name, typeof(MeshFilter), typeof(MeshRenderer));
        instance.transform.SetParent(parent, false);
        instance.GetComponent<MeshFilter>().sharedMesh = mesh;
        instance.GetComponent<MeshRenderer>().sharedMaterial = material;
        return instance;
    }

    private static Material MaterialAsset(string name, string shader, Color color, float metallic, float smoothness)
    {
        var material = new Material(Shader.Find(shader)) { name = name };
        material.SetColor("_BaseColor", color);
        material.SetFloat("_Metallic", metallic);
        material.SetFloat("_Smoothness", smoothness);
        return SaveAsset(material, "Mat_Mid_" + name + ".mat");
    }

    private static T SaveAsset<T>(T asset, string file) where T : Object
    {
        var existing = AssetDatabase.LoadAssetAtPath<T>(Art + file);
        if (existing == null)
        {
            AssetDatabase.CreateAsset(asset, Art + file);
            return asset;
        }
        if (existing is Mesh existingMesh && asset is Mesh newMesh)
        {
            existingMesh.Clear();
            EditorUtility.CopySerialized(newMesh, existingMesh);
            Object.DestroyImmediate(asset);
            return existing;
        }
        EditorUtility.CopySerialized(asset, existing);
        Object.DestroyImmediate(asset);
        return existing;
    }

    private static void Require(bool condition, string message)
    {
        if (!condition)
            throw new InvalidOperationException(message);
        Debug.Log("[Mid verified] " + message);
    }
}
