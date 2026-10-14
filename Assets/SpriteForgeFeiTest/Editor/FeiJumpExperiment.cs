using System;
using System.IO;
using System.Linq;
using UnityEditor;
using UnityEditor.Animations;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.U2D;
using UnityEngine.U2D.Animation;
using UnityEngine.SceneManagement;

public static class FeiJumpExperiment
{
    public const string ClipPath = FeiForgeExperiment.Root + "/Fei_JumpLand.anim";
    public const string PrefabPath = FeiForgeExperiment.Root + "/Fei_RigJump.prefab";
    public const float Duration = 1.8f;
    public const int Frames = 36;

    public static float Height(float t)
    {
        if (t <= .34f || t >= 1.18f) return 0;
        float p = (t - .34f) / .84f;
        return 1.45f * 4 * p * (1 - p);
    }
    static float Crouch(float t) => Envelope(t, new[] { 0f, .12f, .24f, .34f, 1.18f, 1.29f, 1.50f, 1.8f },
        new[] { 0f, 0f, 1f, 0f, 0f, .8f, 0f, 0f });
    static float Tuck(float t) => Envelope(t, new[] { 0f, .34f, .56f, .80f, 1.04f, 1.18f, 1.8f },
        new[] { 0f, 0f, 1f, .75f, .2f, 0f, 0f });
    static float Envelope(float t, float[] times, float[] values)
    {
        for (int i = 1; i < times.Length; i++)
            if (t <= times[i]) return Mathf.Lerp(values[i - 1], values[i],
                Mathf.SmoothStep(0, 1, Mathf.InverseLerp(times[i - 1], times[i], t)));
        return values[values.Length - 1];
    }

    public static string Build(Action<UnityEngine.Object> register)
    {
        var source = AssetDatabase.LoadAssetAtPath<GameObject>(FeiForgeExperiment.Copy);
        if (source == null) throw new InvalidOperationException("Missing Fei copy: " + FeiForgeExperiment.Copy);
        if (AssetDatabase.LoadMainAssetAtPath(ClipPath) != null || AssetDatabase.LoadMainAssetAtPath(PrefabPath) != null)
            throw new InvalidOperationException("Jump assets already exist; refusing replacement.");
        var clip = new AnimationClip { name = "Fei_JumpLand", frameRate = 20 };
        Set(clip, "Visual", "m_LocalPosition.y", Height);
        foreach (string path in new[] { "body", "scarf", "hair", "amulet", "bone_6", "bone_7", "bone_10" })
        {
            var t = source.transform.Find(path);
            if (t == null) throw new InvalidOperationException("Missing copied Fei layer: " + path);
            float rest = t.localPosition.y;
            Set(clip, "Visual/" + path, "m_LocalPosition.y", time => rest - .22f * Crouch(time));
        }
        Leg(clip, source, "right_leg", "bone_8", "bone_22");
        Leg(clip, source, "left_leg", "bone_9", "bone_21");
        Rotation(clip, source, "bone_6", t => 12 * Crouch(t) - 24 * Tuck(t));
        Rotation(clip, source, "bone_6/bone_20", t => 20 * Tuck(t));
        Rotation(clip, source, "bone_7", t => -10 * Crouch(t) + 18 * Tuck(t));
        Rotation(clip, source, "bone_10", t => -4 * Crouch(t) + 3 * Tuck(t));
        Rotation(clip, source, "scarf", t => -8 * Tuck(t));
        Rotation(clip, source, "hair", t => 4 * Tuck(t));
        var settings = AnimationUtility.GetAnimationClipSettings(clip);
        settings.loopTime = false;
        AnimationUtility.SetAnimationClipSettings(clip, settings);
        AssetDatabase.CreateAsset(clip, ClipPath);
        register(clip);
        var controller = AnimatorController.CreateAnimatorControllerAtPath(FeiForgeExperiment.Root + "/Fei_RigJump.controller");
        register(controller);
        var state = controller.layers[0].stateMachine.AddState("JumpLand");
        state.motion = clip;
        controller.layers[0].stateMachine.defaultState = state;
        var preview = EditorSceneManager.NewPreviewScene();
        try
        {
            var root = new GameObject("Fei_RigJump");
            SceneManager.MoveGameObjectToScene(root, preview);
            var visual = UnityEngine.Object.Instantiate(source, root.transform);
            visual.name = "Visual";
            visual.transform.localPosition = Vector3.zero;
            foreach (var skin in visual.GetComponentsInChildren<SpriteSkin>()) skin.alwaysUpdate = true;
            var animator = root.AddComponent<Animator>();
            animator.runtimeAnimatorController = controller;
            animator.cullingMode = AnimatorCullingMode.AlwaysAnimate;
            animator.applyRootMotion = false;
            var prefab = PrefabUtility.SaveAsPrefabAsset(root, PrefabPath);
            if (prefab == null) throw new InvalidOperationException("Failed to save jump prefab.");
            register(prefab);
        }
        finally { EditorSceneManager.ClosePreviewScene(preview); }
        AssetDatabase.SaveAssetIfDirty(clip);
        AssetDatabase.SaveAssetIfDirty(controller);
        return "Created jump/land: 1.8s, apex 1.45 units at .76s, ground contact 1.18s; non-looping clip.";
    }

    static void Leg(AnimationClip clip, GameObject source, string layer, string hipName, string kneeName)
    {
        var hip = source.transform.Find(hipName);
        var knee = source.transform.Find(hipName + "/" + kneeName);
        var renderer = source.transform.Find(layer)?.GetComponent<SpriteRenderer>();
        if (hip == null || knee == null || renderer == null || renderer.sprite == null)
            throw new InvalidOperationException("Missing copied leg/bones: " + layer);
        var bones = renderer.sprite.GetBones();
        var distal = bones.FirstOrDefault(b => b.name == kneeName);
        if (distal.length <= 0) throw new InvalidOperationException("Missing distal bone length: " + kneeName);
        float l1 = knee.localPosition.x, l2 = distal.length;
        float a = Mathf.DeltaAngle(0, hip.localEulerAngles.z), b = Mathf.DeltaAngle(0, knee.localEulerAngles.z);
        var endpoint = new Vector2(l1 * Mathf.Cos(a * Mathf.Deg2Rad) + l2 * Mathf.Cos((a + b) * Mathf.Deg2Rad),
            l1 * Mathf.Sin(a * Mathf.Deg2Rad) + l2 * Mathf.Sin((a + b) * Mathf.Deg2Rad));
        Func<float, Vector2> angles = t =>
        {
            var target = endpoint + Vector2.up * (.20f * Crouch(t) + .23f * Tuck(t));
            float cos = Mathf.Clamp((target.sqrMagnitude - l1 * l1 - l2 * l2) / (2 * l1 * l2), -1, 1);
            float beta = Mathf.Acos(cos);
            float alpha = Mathf.Atan2(target.y, target.x) - Mathf.Atan2(l2 * Mathf.Sin(beta), l1 + l2 * Mathf.Cos(beta));
            return new Vector2(alpha * Mathf.Rad2Deg, beta * Mathf.Rad2Deg);
        };
        Set(clip, "Visual/" + hipName, "m_LocalPosition.y", t => hip.localPosition.y - .20f * Crouch(t));
        Set(clip, "Visual/" + hipName, "localEulerAnglesRaw.z", t => angles(t).x);
        Set(clip, "Visual/" + hipName + "/" + kneeName, "localEulerAnglesRaw.z", t => angles(t).y);
    }
    static void Rotation(AnimationClip clip, GameObject source, string path, Func<float, float> offset)
    {
        var t = source.transform.Find(path);
        if (t == null) throw new InvalidOperationException("Missing copied transform: " + path);
        float rest = Mathf.DeltaAngle(0, t.localEulerAngles.z);
        Set(clip, "Visual/" + path, "localEulerAnglesRaw.z", time => rest + offset(time));
    }
    static void Set(AnimationClip clip, string path, string property, Func<float, float> sample)
    {
        var times = Enumerable.Range(0, 181).Select(i => i / 100f)
            .Concat(new[] { .12f, .24f, .34f, .56f, .76f, .80f, 1.04f, 1.18f, 1.29f, 1.50f, Duration }).Distinct().OrderBy(t => t);
        var curve = new AnimationCurve(times.Select(t => new Keyframe(t, sample(t))).ToArray());
        for (int i = 0; i < curve.length; i++)
        {
            AnimationUtility.SetKeyLeftTangentMode(curve, i, AnimationUtility.TangentMode.Linear);
            AnimationUtility.SetKeyRightTangentMode(curve, i, AnimationUtility.TangentMode.Linear);
        }
        AnimationUtility.SetEditorCurve(clip, EditorCurveBinding.FloatCurve(path, typeof(Transform), property), curve);
    }

    public static string Export()
    {
        var output = Path.Combine(FeiForgeExperiment.Output, "jump");
        var raw = Path.Combine(output, "raw");
        if (Directory.Exists(raw)) throw new InvalidOperationException("Jump frame directory already exists.");
        Directory.CreateDirectory(raw);
        using (var renderer = new FeiForgeExperiment.FeiRenderer(PrefabPath, ClipPath, true))
        {
            for (int i = 0; i < Frames; i++)
            {
                var tex = renderer.Render(i * .05f, 512, 640);
                File.WriteAllBytes(Path.Combine(raw, "jump-" + i.ToString("D2") + ".png"), tex.EncodeToPNG());
                UnityEngine.Object.DestroyImmediate(tex);
            }
            var first = renderer.Render(0, 512, 640);
            var last = renderer.Render(Duration, 512, 640);
            bool equal = first.GetPixels32().SequenceEqual(last.GetPixels32());
            UnityEngine.Object.DestroyImmediate(first);
            UnityEngine.Object.DestroyImmediate(last);
            return "Rendered 36 jump frames; start/end pixel equality=" + equal;
        }
    }
}
