using UnityEngine;
using UnityEditor;
using System.IO;
using System.Collections.Generic;

public class CaptureTool : MonoBehaviour
{
    public Camera captureCamera;
    public Animator characterAnimator;
    public string animationStateName;
    public int frameCount;
    public int frameRate;
    public int resolution;
    public int pixelSize;
    public string saveFolder;

    private int currentFrame = 0;
    private float timer = 0f;
    private bool capturing = false;
    private RenderTexture renderTexture;
    private List<Texture2D> capturedFrames = new List<Texture2D>();

    void Start()
    {
        renderTexture = new RenderTexture(resolution, resolution, 24);
        renderTexture.Create();
        captureCamera.targetTexture = renderTexture;
        captureCamera.clearFlags = CameraClearFlags.SolidColor;
        captureCamera.backgroundColor = new Color(0, 0, 0, 0);

        characterAnimator.Play(animationStateName, 0, 0);

        capturing = true;
        currentFrame = 0;
        timer = 0f;
        capturedFrames.Clear();
    }

    void Update()
    {
        if (!capturing) return;

        timer += Time.deltaTime;
        if (timer >= 1f / frameRate)
        {
            timer = 0f;
            CaptureFrame();
            currentFrame++;

            if (currentFrame >= frameCount)
            {
                capturing = false;
                FinalizeCapture();
            }
        }
    }

    void CaptureFrame()
    {
        RenderTexture.active = renderTexture;
        captureCamera.Render();

        Texture2D tex = new Texture2D(resolution, resolution, TextureFormat.RGBA32, false);
        tex.ReadPixels(new Rect(0, 0, resolution, resolution), 0, 0);
        tex.Apply();

        capturedFrames.Add(Pixelate(tex));
        Destroy(tex);
        RenderTexture.active = null;
    }

    Texture2D Pixelate(Texture2D tex)
    {
        Texture2D result = new Texture2D(tex.width, tex.height);
        for (int y = 0; y < tex.height; y += pixelSize)
        {
            for (int x = 0; x < tex.width; x += pixelSize)
            {
                Color avg = tex.GetPixel(x, y);
                for (int dy = 0; dy < pixelSize; dy++)
                {
                    for (int dx = 0; dx < pixelSize; dx++)
                    {
                        if (x + dx < tex.width && y + dy < tex.height)
                            result.SetPixel(x + dx, y + dy, avg);
                    }
                }
            }
        }
        result.Apply();
        return result;
    }

    void FinalizeCapture()
    {
        string dir = Path.Combine(Application.dataPath, saveFolder);
        Directory.CreateDirectory(dir);

        int w = capturedFrames[0].width;
        int h = capturedFrames[0].height;
        Texture2D sheet = new Texture2D(w * frameCount, h);

        for (int i = 0; i < capturedFrames.Count; i++)
        {
            sheet.SetPixels(i * w, 0, w, h, capturedFrames[i].GetPixels());
        }
        sheet.Apply();

        byte[] png = sheet.EncodeToPNG();
        File.WriteAllBytes(Path.Combine(dir, "sprite_sheet.png"), png);
        AssetDatabase.Refresh();

        string assetPath = Path.Combine("Assets", saveFolder, "sprite_sheet.png");
        TextureImporter importer = AssetImporter.GetAtPath(assetPath) as TextureImporter;
        if (importer != null)
        {
            importer.textureType = TextureImporterType.Sprite;
            importer.spriteImportMode = SpriteImportMode.Multiple;
            List<SpriteMetaData> metas = new List<SpriteMetaData>();

            for (int i = 0; i < frameCount; i++)
            {
                metas.Add(new SpriteMetaData
                {
                    name = $"frame_{i}",
                    rect = new Rect(i * w, 0, w, h),
                    alignment = (int)SpriteAlignment.Center,
                    pivot = new Vector2(0.5f, 0.0f)
                });
            }

            importer.spritesheet = metas.ToArray();
            EditorUtility.SetDirty(importer);
            importer.SaveAndReimport();
        }

        CreateAnimClip(w, h);
    }

    void CreateAnimClip(int w, int h)
    {
        string assetPath = Path.Combine("Assets", saveFolder, "sprite_sheet.png");
        var allAssets = AssetDatabase.LoadAllAssetsAtPath(assetPath);
        List<Sprite> sprites = new List<Sprite>();
        foreach (var a in allAssets)
            if (a is Sprite s) sprites.Add(s);

        sprites.Sort((a, b) => a.name.CompareTo(b.name));

        AnimationClip clip = new AnimationClip();
        clip.frameRate = frameRate;
        EditorCurveBinding bind = new EditorCurveBinding();
        bind.type = typeof(SpriteRenderer);
        bind.path = "";
        bind.propertyName = "m_Sprite";

        ObjectReferenceKeyframe[] keys = new ObjectReferenceKeyframe[frameCount];
        for (int i = 0; i < frameCount; i++)
        {
            keys[i] = new ObjectReferenceKeyframe { time = i / (float)frameRate, value = sprites[i] };
        }

        AnimationUtility.SetObjectReferenceCurve(clip, bind, keys);
        AssetDatabase.CreateAsset(clip, Path.Combine("Assets", saveFolder, "GeneratedAnim.anim"));
        AssetDatabase.SaveAssets();

        CreatePrefab(sprites[0], clip);
    }

    void CreatePrefab(Sprite startSprite, AnimationClip clip)
    {
        GameObject go = new GameObject("PixelCharacter");
        SpriteRenderer sr = go.AddComponent<SpriteRenderer>();
        sr.sprite = startSprite;
        Animator animator = go.AddComponent<Animator>();
        var controller = UnityEditor.Animations.AnimatorController.CreateAnimatorControllerAtPath(Path.Combine("Assets", saveFolder, "GeneratedController.controller"));
        controller.AddMotion(clip);
        animator.runtimeAnimatorController = controller;

        PrefabUtility.SaveAsPrefabAsset(go, Path.Combine("Assets", saveFolder, "GeneratedPrefab.prefab"));
        DestroyImmediate(go);
    }
}
