using UnityEngine;
using UnityEditor;
using System.Collections.Generic;
using UnityEditorInternal;
using System.IO;

public class CaptureToolWindow : EditorWindow
{
    private Camera captureCamera;
    private Animator characterAnimator;
    private List<string> animationStates = new List<string> { "Idle" };
    private ReorderableList animationList;
    private int frameCount = 8;
    private int frameRate = 12;
    private int resolution = 512;
    private int pixelSize = 4;
    private string saveFolder = "CapturedFrames";

    private bool usePointFilter = true;
    private bool disableMipMaps = true;
    private bool disableCompression = true;

    [MenuItem("Tools/Pixel Animator Capture")]
    public static void ShowWindow()
    {
        GetWindow<CaptureToolWindow>("Pixel Capture Tool");
    }

    private void OnEnable()
    {
        animationList = new ReorderableList(animationStates, typeof(string), true, true, true, true);
        animationList.drawHeaderCallback = (Rect rect) => {
            EditorGUI.LabelField(rect, "Animation States");
        };
        animationList.drawElementCallback = (Rect rect, int index, bool isActive, bool isFocused) => {
            animationStates[index] = EditorGUI.TextField(new Rect(rect.x, rect.y, rect.width, EditorGUIUtility.singleLineHeight), animationStates[index]);
        };
    }

    void OnGUI()
    {
        GUILayout.Label("Capture Settings", EditorStyles.boldLabel);

        captureCamera = (Camera)EditorGUILayout.ObjectField("Capture Camera", captureCamera, typeof(Camera), true);
        characterAnimator = (Animator)EditorGUILayout.ObjectField("Character Animator", characterAnimator, typeof(Animator), true);

        GUILayout.Space(5);
        animationList.DoLayoutList();

        frameCount = EditorGUILayout.IntField("Frame Count", frameCount);
        frameRate = EditorGUILayout.IntField("Frame Rate", frameRate);
        resolution = EditorGUILayout.IntField("Resolution", resolution);
        pixelSize = EditorGUILayout.IntSlider("Pixel Size", pixelSize, 1, 64);
        saveFolder = EditorGUILayout.TextField("Save Folder", saveFolder);

        GUILayout.Space(10);

        GUILayout.Label("Optional Texture Settings", EditorStyles.boldLabel);
        usePointFilter = EditorGUILayout.Toggle("Use Point Filtering", usePointFilter);
        disableMipMaps = EditorGUILayout.Toggle("Disable Mip Maps", disableMipMaps);
        disableCompression = EditorGUILayout.Toggle("Disable Compression", disableCompression);

        if (GUILayout.Button("Start Capture for All Animations"))
        {
            foreach (var animState in animationStates)
            {
                string animStateCopy = animState;

                GameObject temp = new GameObject("__CaptureToolRuntime__" + animStateCopy);
                var tool = temp.AddComponent<CaptureTool>();

                tool.captureCamera = captureCamera;
                tool.characterAnimator = characterAnimator;
                tool.animationStateName = animStateCopy;
                tool.frameCount = frameCount;
                tool.frameRate = frameRate;
                tool.resolution = resolution;
                tool.pixelSize = pixelSize;
                tool.saveFolder = saveFolder + "/" + animStateCopy;

                EditorApplication.delayCall += () =>
                {
                    string texPath = Path.Combine("Assets", saveFolder, animStateCopy, "sprite_sheet.png");

                    if (usePointFilter || disableMipMaps || disableCompression)
                    {
                        TextureImporter texImporter = AssetImporter.GetAtPath(texPath) as TextureImporter;
                        if (texImporter != null)
                        {
                            if (usePointFilter) texImporter.filterMode = FilterMode.Point;
                            if (disableCompression) texImporter.textureCompression = TextureImporterCompression.Uncompressed;
                            if (disableMipMaps) texImporter.mipmapEnabled = false;

                            EditorUtility.SetDirty(texImporter);
                            texImporter.SaveAndReimport();
                        }
                    }

                    var sprites = AssetDatabase.LoadAllAssetsAtPath(texPath);
                    foreach (var asset in sprites)
                    {
                        if (asset is Sprite sprite && sprite.name == "frame_0")
                        {
                            string prefabPath = Path.Combine("Assets", saveFolder, animStateCopy, "GeneratedPrefab.prefab");
                            GameObject go = AssetDatabase.LoadAssetAtPath<GameObject>(prefabPath);
                            if (go != null)
                            {
                                var renderer = go.GetComponent<SpriteRenderer>();
                                if (renderer != null)
                                {
                                    renderer.sprite = sprite;
                                    EditorUtility.SetDirty(go);
                                    AssetDatabase.SaveAssets();
                                }
                            }
                            break;
                        }
                    }
                };
            }
        }


        GUILayout.Label("Generated Assets Preview:", EditorStyles.boldLabel);

        foreach (var animState in animationStates)
        {
            string folder = Path.Combine("Assets", saveFolder, animState);
            string spritePath = Path.Combine(folder, "sprite_sheet.png");
            string animPath = Path.Combine(folder, "GeneratedAnim.anim");
            string prefabPath = Path.Combine(folder, "GeneratedPrefab.prefab");

            EditorGUILayout.BeginVertical("box");
            GUILayout.Label($"Animation: {animState}", EditorStyles.boldLabel);

            Texture2D tex = AssetDatabase.LoadAssetAtPath<Texture2D>(spritePath);
            if (tex != null)
            {
                float aspect = tex.width / (float)tex.height;
                float width = EditorGUIUtility.currentViewWidth - 50;
                float height = width / aspect;
                GUILayout.Label(tex, GUILayout.Width(width), GUILayout.Height(height));
            }

            UnityEngine.Object animClip = AssetDatabase.LoadAssetAtPath<AnimationClip>(animPath);
            UnityEngine.Object prefab = AssetDatabase.LoadAssetAtPath<GameObject>(prefabPath);

            EditorGUILayout.ObjectField("Anim Clip", animClip, typeof(AnimationClip), false);
            EditorGUILayout.ObjectField("Prefab", prefab, typeof(GameObject), false);

            if (prefab != null && GUILayout.Button("Instantiate in Scene"))
            {
                PrefabUtility.InstantiatePrefab(prefab);
            }

            EditorGUILayout.EndVertical();
            GUILayout.Space(10);
        }
    }
}
