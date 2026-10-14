using System;
using System.IO;
using System.Linq;
using Newtonsoft.Json.Linq;
using UnityEditor;
using UnityEditor.Animations;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;

public static class ForgeAsepriteImporter
{
    public static string ImportFei()
    {
        const string root = FeiForgeExperiment.Root;
        const string texturePath = root + "/Forge/fei-walk.png";
        const string jsonPath = root + "/Forge/fei-walk.json";
        if (!File.Exists(jsonPath) || !File.Exists(texturePath))
            throw new InvalidOperationException("Missing Forge atlas/JSON: " + jsonPath);
        if (AssetDatabase.LoadAssetAtPath<AnimationClip>(root + "/Fei_ForgeWalk.anim") != null)
            throw new InvalidOperationException("Fei Forge animation already exists.");
        var doc = JObject.Parse(File.ReadAllText(jsonPath));
        var frames = doc["frames"] as JArray;
        if (frames == null || frames.Count == 0) throw new InvalidOperationException("Missing Aseprite frame array.");
        AssetDatabase.ImportAsset(texturePath, ImportAssetOptions.ForceSynchronousImport);
        var importer = AssetImporter.GetAtPath(texturePath) as TextureImporter;
        if (importer == null) throw new InvalidOperationException("Missing texture importer: " + texturePath);
        var texture = AssetDatabase.LoadAssetAtPath<Texture2D>(texturePath);
        if (texture == null) throw new InvalidOperationException("Missing atlas texture: " + texturePath);
        var mappingPath = "output/SpriteForgeFeiTest/engine/engine-export.json";
        var mapping = JObject.Parse(File.ReadAllText(mappingPath))["mapping"];
        var pivot = mapping["unity"]["pivot"] as JArray;
        var ppu = (float)mapping["unity"]["pixels_per_unit"];
        var height = texture.height;
        var entries = frames.Select((f, i) => new SpriteMetaData
        {
            name = "walk-" + i.ToString("D2"), alignment = 9,
            pivot = new Vector2((float)pivot[0], (float)pivot[1]),
            rect = new Rect((float)f["frame"]["x"], height - (float)f["frame"]["y"] - (float)f["frame"]["h"],
                (float)f["frame"]["w"], (float)f["frame"]["h"])
        }).ToArray();
        Undo.RecordObject(importer, "Import Fei Forge atlas");
        importer.textureType = TextureImporterType.Sprite;
        importer.spriteImportMode = SpriteImportMode.Multiple;
        importer.spritePixelsPerUnit = ppu;
        importer.filterMode = FilterMode.Bilinear;
        importer.textureCompression = TextureImporterCompression.Uncompressed;
        importer.mipmapEnabled = false;
        importer.alphaIsTransparency = true;
        importer.maxTextureSize = 4096;
#pragma warning disable 618
        importer.spritesheet = entries;
#pragma warning restore 618
        importer.SaveAndReimport();
        var sprites = AssetDatabase.LoadAllAssetsAtPath(texturePath).OfType<Sprite>().OrderBy(s => s.name).ToArray();
        if (sprites.Length != frames.Count) throw new InvalidOperationException("Atlas slice count mismatch.");
        var clip = new AnimationClip { name = "Fei_ForgeWalk", frameRate = 20 };
        // Unity adds one frame of duration after the last sprite key.
        var keys = new ObjectReferenceKeyframe[sprites.Length];
        float time = 0;
        for (int i = 0; i < sprites.Length; i++)
        {
            keys[i] = new ObjectReferenceKeyframe { time = time, value = sprites[i] };
            time += (float)frames[i]["duration"] / 1000;
        }
        AnimationUtility.SetObjectReferenceCurve(clip, EditorCurveBinding.PPtrCurve("", typeof(SpriteRenderer), "m_Sprite"), keys);
        var settings = AnimationUtility.GetAnimationClipSettings(clip);
        settings.loopTime = true;
        AnimationUtility.SetAnimationClipSettings(clip, settings);
        AssetDatabase.CreateAsset(clip, root + "/Fei_ForgeWalk.anim");
        var controller = AnimatorController.CreateAnimatorControllerAtPath(root + "/Fei_ForgeWalk.controller");
        var state = controller.layers[0].stateMachine.AddState("Walk");
        state.motion = clip;
        controller.layers[0].stateMachine.defaultState = state;
        var scene = EditorSceneManager.NewPreviewScene();
        try
        {
            var go = new GameObject("Fei_ForgeWalk");
            SceneManager.MoveGameObjectToScene(go, scene);
            go.AddComponent<SpriteRenderer>().sprite = sprites[0];
            go.AddComponent<Animator>().runtimeAnimatorController = controller;
            PrefabUtility.SaveAsPrefabAsset(go, root + "/Fei_ForgeWalk.prefab");
        }
        finally { EditorSceneManager.ClosePreviewScene(scene); }
        AssetDatabase.SaveAssetIfDirty(clip);
        AssetDatabase.SaveAssetIfDirty(controller);
        return "Imported " + sprites.Length + " frames; clip duration=" + time + "; pivot=" + pivot + "; ppu=" + ppu;
    }

    public static string BuildComparisonScene()
    {
        const string path = FeiForgeExperiment.Root + "/Fei_Walk_Test.unity";
        if (AssetDatabase.LoadMainAssetAtPath(path) != null) throw new InvalidOperationException("Scene already exists: " + path);
        var originalActive = SceneManager.GetActiveScene();
        var scene = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Additive);
        try
        {
            var rig = AssetDatabase.LoadAssetAtPath<GameObject>(FeiForgeExperiment.RigPrefab);
            var forge = AssetDatabase.LoadAssetAtPath<GameObject>(FeiForgeExperiment.Root + "/Fei_ForgeWalk.prefab");
            if (rig == null || forge == null) throw new InvalidOperationException("Missing walk prefab.");
            var a = (GameObject)PrefabUtility.InstantiatePrefab(rig, scene);
            a.transform.position = new Vector3(-3, 0, 0);
            var b = (GameObject)PrefabUtility.InstantiatePrefab(forge, scene);
            b.transform.position = new Vector3(3, .75f, 0);
            b.transform.localScale = Vector3.one * 1.7f;
            var cameraObject = new GameObject("Test Camera");
            SceneManager.MoveGameObjectToScene(cameraObject, scene);
            var camera = cameraObject.AddComponent<Camera>();
            camera.orthographic = true; camera.orthographicSize = 4;
            camera.transform.position = new Vector3(0, 3, -10);
            camera.clearFlags = CameraClearFlags.SolidColor;
            camera.backgroundColor = new Color(.09f, .12f, .18f);
            Label(scene, "Rig walk / copied PSB", new Vector3(-4.8f, 6.7f, 0));
            Label(scene, "Forge atlas / 24 frames", new Vector3(1, 6.7f, 0));
            Label(scene, "20 FPS | 1.2s loop | existing artwork", new Vector3(-3.5f, -.1f, 0));
            if (!EditorSceneManager.SaveScene(scene, path)) throw new InvalidOperationException("Failed to save test scene.");
        }
        finally
        {
            if (originalActive.IsValid()) SceneManager.SetActiveScene(originalActive);
            EditorSceneManager.CloseScene(scene, true);
        }
        return "Saved isolated comparison scene " + path + "; original scene remains loaded and unsaved.";
    }

    static void Label(Scene scene, string value, Vector3 position)
    {
        var go = new GameObject(value);
        SceneManager.MoveGameObjectToScene(go, scene);
        go.transform.position = position;
        var label = go.AddComponent<TextMesh>();
        label.text = value; label.characterSize = .16f; label.fontSize = 36;
        label.color = new Color(.75f, .87f, 1);
    }
}
