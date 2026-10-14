using System;
using System.IO;
using System.Linq;
using UnityEditor;
using UnityEditor.Animations;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.U2D;
using UnityEngine.U2D.Animation;

// Editor-only experiment. Source PSB and existing scenes are never written.
public static class FeiForgeExperiment
{
    public const string Root = "Assets/SpriteForgeFeiTest";
    public const string Copy = Root + "/Fei_Copy.psb";
    public const string Walk = Root + "/Fei_Walk.anim";
    public const string RigPrefab = Root + "/Fei_RigWalk.prefab";
    public const float Duration = 1.2f;
    public const int FrameCount = 24;
    public static string Output => Path.GetFullPath("output/SpriteForgeFeiTest");

    public static string Build()
    {
        var source = AssetDatabase.LoadAssetAtPath<GameObject>(Copy);
        if (source == null) throw new InvalidOperationException("Missing copied PSB: " + Copy);
        if (AssetDatabase.LoadAssetAtPath<AnimationClip>(Walk) != null)
            throw new InvalidOperationException("Walk already exists; refusing replacement: " + Walk);
        var clip = new AnimationClip { name = "Fei_Walk", frameRate = 20 };
        Curve(clip, source, "bone_8", "localEulerAnglesRaw.z", p => 19 * Mathf.Sin(p));
        Curve(clip, source, "bone_9", "localEulerAnglesRaw.z", p => -19 * Mathf.Sin(p));
        Curve(clip, source, "bone_8/bone_22", "localEulerAnglesRaw.z", p => -32 * Mathf.Pow(Mathf.Max(0, -Mathf.Sin(p)), 2));
        Curve(clip, source, "bone_9/bone_21", "localEulerAnglesRaw.z", p => -32 * Mathf.Pow(Mathf.Max(0, Mathf.Sin(p)), 2));
        Curve(clip, source, "bone_6", "localEulerAnglesRaw.z", p => -8 * Mathf.Sin(p));
        Curve(clip, source, "bone_6/bone_20", "localEulerAnglesRaw.z", p => 5 * Mathf.Sin(p + .5f));
        Curve(clip, source, "bone_7", "localEulerAnglesRaw.z", p => 5 * Mathf.Sin(p));
        Curve(clip, source, "bone_10", "localEulerAnglesRaw.z", p => 1.4f * Mathf.Sin(p));
        foreach (var path in new[] { "body", "scarf", "hair", "amulet", "bone_6", "bone_7", "bone_10" })
            Curve(clip, source, path, "m_LocalPosition.y", p => .035f * (1 - Mathf.Cos(2 * p)));
        Curve(clip, source, "hair", "localEulerAnglesRaw.z", p => 1.8f * Mathf.Sin(p - .5f));
        Curve(clip, source, "scarf", "localEulerAnglesRaw.z", p => 2.8f * Mathf.Sin(p - .7f));
        var settings = AnimationUtility.GetAnimationClipSettings(clip);
        settings.loopTime = true;
        AnimationUtility.SetAnimationClipSettings(clip, settings);
        AssetDatabase.CreateAsset(clip, Walk);
        var controller = AnimatorController.CreateAnimatorControllerAtPath(Root + "/Fei_RigWalk.controller");
        var state = controller.layers[0].stateMachine.AddState("Walk");
        state.motion = clip;
        controller.layers[0].stateMachine.defaultState = state;
        var scene = EditorSceneManager.NewPreviewScene();
        try
        {
            var instance = UnityEngine.Object.Instantiate(source);
            UnityEngine.SceneManagement.SceneManager.MoveGameObjectToScene(instance, scene);
            instance.name = "Fei_RigWalk";
            var animator = instance.AddComponent<Animator>();
            animator.runtimeAnimatorController = controller;
            animator.cullingMode = AnimatorCullingMode.AlwaysAnimate;
            foreach (var skin in instance.GetComponentsInChildren<SpriteSkin>()) skin.alwaysUpdate = true;
            PrefabUtility.SaveAsPrefabAsset(instance, RigPrefab);
        }
        finally { EditorSceneManager.ClosePreviewScene(scene); }
        AssetDatabase.SaveAssetIfDirty(clip);
        AssetDatabase.SaveAssetIfDirty(controller);
        return "Created " + Walk + ", " + RigPrefab + "; 24 frames, 1.2 second cycle, source PSB unchanged.";
    }

    static void Curve(AnimationClip clip, GameObject source, string path, string property, Func<float, float> offset)
    {
        var t = source.transform.Find(path);
        if (t == null) throw new InvalidOperationException("Missing copied Fei bone/layer: " + path);
        var rest = property == "m_LocalPosition.y" ? t.localPosition.y : Mathf.DeltaAngle(0, t.localEulerAngles.z);
        var keys = Enumerable.Range(0, FrameCount + 1).Select(i =>
            new Keyframe(i * Duration / FrameCount, rest + offset(i * Mathf.PI * 2 / FrameCount))).ToArray();
        var curve = new AnimationCurve(keys);
        for (int i = 0; i < keys.Length; i++)
        {
            AnimationUtility.SetKeyLeftTangentMode(curve, i, AnimationUtility.TangentMode.ClampedAuto);
            AnimationUtility.SetKeyRightTangentMode(curve, i, AnimationUtility.TangentMode.ClampedAuto);
        }
        AnimationUtility.SetEditorCurve(clip, EditorCurveBinding.FloatCurve(path, typeof(Transform), property), curve);
    }

    public static string ExportFrames()
    {
        var frames = Path.Combine(Output, "raw");
        if (Directory.Exists(frames)) throw new InvalidOperationException("Raw frames already exist: " + frames);
        Directory.CreateDirectory(frames);
        using (var renderer = new FeiRenderer())
        {
            for (int i = 0; i < FrameCount; i++)
            {
                var tex = renderer.Render(i * Duration / FrameCount, 512, 640);
                File.WriteAllBytes(Path.Combine(frames, "walk-" + i.ToString("D2") + ".png"), tex.EncodeToPNG());
                UnityEngine.Object.DestroyImmediate(tex);
            }
            var first = renderer.Render(0, 512, 640);
            var seam = renderer.Render(Duration, 512, 640);
            bool equal = first.GetPixels32().SequenceEqual(seam.GetPixels32());
            UnityEngine.Object.DestroyImmediate(first);
            UnityEngine.Object.DestroyImmediate(seam);
            return "Exported 24 native-alpha Unity-rendered frames to " + frames + "; t=0 versus t=1.2 pixel equality=" + equal;
        }
    }

    public sealed class FeiRenderer : IDisposable
    {
        readonly PreviewRenderUtility preview;
        readonly GameObject instance;
        readonly AnimationClip clip;
        readonly Layer[] layers;
        sealed class Layer
        {
            public SpriteRenderer renderer;
            public SpriteSkin skin;
            public Mesh mesh;
            public Material material;
            public Vector3[] rest;
            public BoneWeight[] weights;
            public Matrix4x4[] bind;
        }

        public FeiRenderer(string prefabPath = RigPrefab, string clipPath = Walk, bool jumping = false)
        {
            var prefab = AssetDatabase.LoadAssetAtPath<GameObject>(prefabPath);
            clip = AssetDatabase.LoadAssetAtPath<AnimationClip>(clipPath);
            var shader = Shader.Find("Sprites/Default");
            if (prefab == null || clip == null || shader == null)
                throw new InvalidOperationException("Missing Fei_RigWalk prefab, walk clip or Sprites/Default shader.");
            preview = new PreviewRenderUtility();
            instance = UnityEngine.Object.Instantiate(prefab);
            instance.hideFlags = HideFlags.HideAndDontSave;
            preview.AddSingleGO(instance);
            var animator = instance.GetComponent<Animator>();
            if (animator != null) animator.enabled = false;
            layers = instance.GetComponentsInChildren<SpriteRenderer>().OrderBy(r => r.sortingOrder).Select(r =>
            {
                if (r.sprite == null) throw new InvalidOperationException("Missing sprite on copied layer " + r.name);
                var s = r.sprite;
                var skin = r.GetComponent<SpriteSkin>();
                var verts = s.vertices.Select(v => (Vector3)v).ToArray();
                var mesh = new Mesh { name = "Fei preview " + r.name, vertices = verts, uv = s.uv,
                    triangles = s.triangles.Select(v => (int)v).ToArray(), colors = Enumerable.Repeat(Color.white, verts.Length).ToArray() };
                var layer = new Layer { renderer = r, skin = skin, mesh = mesh, rest = verts,
                    material = new Material(shader) { mainTexture = s.texture } };
                if (skin != null)
                {
                    layer.weights = s.GetVertexAttribute<BoneWeight>(VertexAttribute.BlendWeight).ToArray();
                    layer.bind = s.GetBindPoses().ToArray();
                    if (layer.weights.Length != verts.Length || skin.boneTransforms.Any(b => b == null))
                        throw new InvalidOperationException("Invalid skin weights/bones: " + r.name);
                    skin.enabled = false;
                }
                r.enabled = false;
                return layer;
            }).ToArray();
            preview.camera.orthographic = true;
            preview.camera.orthographicSize = 3.7f;
            preview.camera.transform.position = new Vector3(.4f, 2.9f, -10);
            preview.camera.transform.rotation = Quaternion.identity;
            preview.camera.clearFlags = CameraClearFlags.SolidColor;
            preview.camera.backgroundColor = Color.clear;
            preview.camera.nearClipPlane = .1f;
            preview.camera.farClipPlane = 30;
            if (jumping)
            {
                preview.camera.orthographicSize = 4.4f;
                preview.camera.transform.position = new Vector3(.4f, 3.55f, -10);
            }
        }

        public Texture2D Render(float time, int width, int height)
        {
            clip.SampleAnimation(instance, time);
            preview.BeginPreview(new Rect(0, 0, width, height), GUIStyle.none);
            int order = 0;
            foreach (var l in layers)
            {
                if (l.skin != null)
                {
                    var matrices = l.skin.boneTransforms.Select((b, i) =>
                        l.renderer.transform.worldToLocalMatrix * b.localToWorldMatrix * l.bind[i]).ToArray();
                    var vertices = new Vector3[l.rest.Length];
                    for (int i = 0; i < vertices.Length; i++)
                    {
                        var w = l.weights[i]; var v = l.rest[i];
                        vertices[i] = matrices[w.boneIndex0].MultiplyPoint3x4(v) * w.weight0
                            + matrices[w.boneIndex1].MultiplyPoint3x4(v) * w.weight1
                            + matrices[w.boneIndex2].MultiplyPoint3x4(v) * w.weight2
                            + matrices[w.boneIndex3].MultiplyPoint3x4(v) * w.weight3;
                    }
                    l.mesh.vertices = vertices;
                    l.mesh.RecalculateBounds();
                }
                var matrix = l.renderer.transform.localToWorldMatrix;
                matrix.m23 = -order++ * .001f;
                preview.DrawMesh(l.mesh, matrix, l.material, 0);
            }
            preview.Render(true);
            var rendered = preview.EndPreview();
            var previous = RenderTexture.active;
            // Preview surfaces may be scaled by the Editor's display DPI.
            var exactSize = RenderTexture.GetTemporary(width, height, 0, RenderTextureFormat.ARGB32);
            Graphics.Blit(rendered, exactSize);
            RenderTexture.active = exactSize;
            var texture = new Texture2D(width, height, TextureFormat.RGBA32, false);
            texture.ReadPixels(new Rect(0, 0, width, height), 0, 0);
            texture.Apply();
            RenderTexture.active = previous;
            RenderTexture.ReleaseTemporary(exactSize);
            var colors = texture.GetPixels();
            for (int i = 0; i < colors.Length; i++)
            {
                var c = colors[i];
                colors[i] = c.a > 0 ? new Color(c.r / c.a, c.g / c.a, c.b / c.a, c.a) : Color.clear;
            }
            texture.SetPixels(colors);
            texture.Apply();
            return texture;
        }

        public void Dispose()
        {
            foreach (var l in layers)
            {
                UnityEngine.Object.DestroyImmediate(l.mesh);
                UnityEngine.Object.DestroyImmediate(l.material);
            }
            preview.Cleanup();
        }
    }
}

public class FeiForgePreviewWindow : EditorWindow
{
    FeiForgeExperiment.FeiRenderer renderer;
    Texture2D image;
    double start;
    bool playing = true;
    int action;
    [MenuItem("Tools/Sprite Forge/Fei Walk Preview")]
    public static void Open() => GetWindow<FeiForgePreviewWindow>("Fei Walk Preview").Show();
    [MenuItem("Tools/Sprite Forge/Fei Jump Preview")]
    public static void OpenJump()
    {
        var window = GetWindow<FeiForgePreviewWindow>("Fei Animation Preview");
        window.ChangeAction(1);
        window.Show();
    }
    void ChangeAction(int value)
    {
        if (renderer != null) { renderer.Dispose(); renderer = null; }
        if (image != null) { DestroyImmediate(image); image = null; }
        action = value;
        start = EditorApplication.timeSinceStartup;
    }
    void OnEnable() { start = EditorApplication.timeSinceStartup; EditorApplication.update += Tick; }
    void Tick() { if (playing) Repaint(); }
    void OnDisable()
    {
        EditorApplication.update -= Tick;
        if (renderer != null) renderer.Dispose();
        if (image != null) DestroyImmediate(image);
    }
    void OnGUI()
    {
        GUILayout.Label("Fei · Bone Animation Preview", EditorStyles.boldLabel);
        int next = GUILayout.Toolbar(action, new[] { "Walk", "Jump / Land" });
        if (next != action) ChangeAction(next);
        playing = GUILayout.Toggle(playing, "Play");
        if (renderer == null) renderer = action == 0 ? new FeiForgeExperiment.FeiRenderer()
            : new FeiForgeExperiment.FeiRenderer(FeiJumpExperiment.PrefabPath, FeiJumpExperiment.ClipPath, true);
        if (image == null || playing)
        {
            if (image != null) DestroyImmediate(image);
            float duration = action == 0 ? FeiForgeExperiment.Duration : FeiJumpExperiment.Duration;
            float t = (float)((EditorApplication.timeSinceStartup - start) % (duration + (action == 1 ? .4f : 0)));
            t = Mathf.Min(t, duration);
            image = renderer.Render(t, 512, 640);
        }
        var rect = GUILayoutUtility.GetRect(position.width, Mathf.Max(200, position.height - 80));
        EditorGUI.DrawRect(rect, new Color(.11f, .14f, .2f));
        GUI.DrawTexture(rect, image, ScaleMode.ScaleToFit, true);
    }
}
