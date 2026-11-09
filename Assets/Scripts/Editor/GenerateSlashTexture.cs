using UnityEngine;
using UnityEditor;
using System.IO;

public class GenerateSlashTexture
{
    [MenuItem("Tools/Generate Slash Texture")]
    public static void CreateTexture()
    {
        int size = 1024;
        Texture2D tex = new Texture2D(size, size, TextureFormat.RGBA32, false);
        Vector2 center = new Vector2(0.5f, 0.5f);

        Color cyan = new Color(0.18f, 0.95f, 1f);
        Color emerald = new Color(0.25f, 1f, 0.45f);
        Color hotWhite = new Color(1f, 1f, 1f);

        for (int y = 0; y < size; y++)
        {
            for (int x = 0; x < size; x++)
            {
                Vector2 uv = new Vector2((float)x / size, (float)y / size);
                Vector2 dir = uv - center;
                float dist = dir.magnitude;

                // Normalized angle 0 to 1 starting from right (0 rad) counter-clockwise
                float angle = (Mathf.Atan2(dir.y, dir.x) + Mathf.PI) / (2f * Mathf.PI);

                // Define arc span (~300 degrees, leaving a 60 deg opening)
                float arcStart = 0.05f;
                float arcEnd = 0.88f;

                if (angle < arcStart || angle > arcEnd)
                {
                    tex.SetPixel(x, y, Color.clear);
                    continue;
                }

                // Progress along the arc: 0 at the tail, 1 at the leading tip
                float t = Mathf.InverseLerp(arcStart, arcEnd, angle);

                // Radius profile: expands slightly toward the leading edge
                float targetRadius = Mathf.Lerp(0.32f, 0.38f, t);
                float thickness = Mathf.Lerp(0.015f, 0.085f, Mathf.Pow(t, 1.2f));

                float radialDist = Mathf.Abs(dist - targetRadius);

                if (radialDist > thickness)
                {
                    tex.SetPixel(x, y, Color.clear);
                    continue;
                }

                // Core cross-section profile (sharp edge, hot interior)
                float profile = 1f - (radialDist / thickness);
                profile = Mathf.SmoothStep(0f, 1f, profile);

                // Internal energy wisps
                float wispNoise = Mathf.Sin(angle * 70f + dist * 90f) * 0.15f;
                profile = Mathf.Clamp01(profile + wispNoise * (1f - profile));

                // Color gradient: Tail emerald -> Mid cyan -> Tip white
                Color col;
                if (t > 0.85f)
                {
                    float leadT = Mathf.InverseLerp(0.85f, 1.0f, t);
                    col = Color.Lerp(cyan, hotWhite, leadT * profile);
                }
                else
                {
                    col = Color.Lerp(emerald, cyan, t);
                }

                // Alpha fade toward tail
                float alpha = profile * Mathf.SmoothStep(0f, 1f, t * 1.5f);

                tex.SetPixel(x, y, new Color(col.r, col.g, col.b, alpha));
            }
        }

        tex.Apply();

        byte[] bytes = tex.EncodeToPNG();
        string path = Application.dataPath + "/SlashTexture_Arc.png";
        File.WriteAllBytes(path, bytes);
        AssetDatabase.Refresh();

        // Apply optimal VFX import settings
        string relativePath = "Assets/SlashTexture_Arc.png";
        TextureImporter importer = AssetImporter.GetAtPath(relativePath) as TextureImporter;
        if (importer != null)
        {
            importer.textureType = TextureImporterType.Default;
            importer.alphaIsTransparency = true;
            importer.wrapMode = TextureWrapMode.Clamp;
            importer.filterMode = FilterMode.Bilinear;
            importer.SaveAndReimport();
        }

        Debug.Log("Generated slash texture at: " + relativePath);
    }
}