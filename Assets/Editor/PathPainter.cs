using System.Collections.Generic;
using System.Linq;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;

/// <summary>
/// One-off helper: repaints the Ground terrain with the path layout from the user's sketch
/// (paths around the brazier) and removes trees standing on the paths.
/// Safe to delete once the paths look right.
/// </summary>
public static class PathPainter
{
    const string GroundName = "Ground";
    const string BrazierName = "brazier";
    const string TreesName = "Trees";

    const float PathHalfWidth = 1.6f;
    const float PathEdgeSoftness = 0.5f;
    const float TreeClearance = 1.5f;

    // The sketch was a 325 px square (the ground) with its top-left corner at (18, 22) and the brazier at (162, 178).
    static readonly Vector2 SketchOrigin = new Vector2(18f, 22f);
    const float SketchSize = 325f;
    static readonly Vector2 SketchBrazier = new Vector2(162f, 178f);

    // Each path is a list of points in sketch pixels, traced from the drawing.
    static readonly Vector2[][] SketchPaths =
    {
        // Arc above the brazier, from the left to the big S-curve.
        new[] { P(90, 142), P(105, 128), P(130, 117), P(160, 110), P(190, 108), P(215, 112), P(230, 122) },
        // Big S-curve from the top right down past the brazier's right side.
        new[] { P(298, 70), P(275, 66), P(248, 70), P(230, 85), P(226, 105), P(230, 125), P(240, 148), P(242, 170), P(232, 190), P(212, 203), P(185, 210), P(165, 215), P(157, 222) },
        // Short stub below the brazier.
        new[] { P(157, 217), P(157, 240), P(157, 265) },
        // Path to the lower left.
        new[] { P(157, 237), P(130, 243), P(100, 250), P(75, 257), P(58, 264), P(46, 252) },
        // Path to the lower right.
        new[] { P(180, 212), P(198, 222), P(212, 240), P(222, 265), P(228, 290), P(238, 315), P(255, 328), P(275, 330) },
    };

    [MenuItem("Papartis/Paint Paths From Sketch")]
    public static void Paint()
    {
        var scene = SceneManager.GetActiveScene();
        var roots = scene.GetRootGameObjects();

        var ground = roots.FirstOrDefault(root => root.name == GroundName);
        var terrain = ground != null ? ground.GetComponent<Terrain>() : null;
        if (terrain == null || terrain.terrainData.alphamapLayers < 2)
        {
            Debug.LogError($"No '{GroundName}' terrain with leaf and mud layers found.");
            return;
        }

        var brazier = Object.FindObjectsByType<Transform>(FindObjectsInactive.Exclude, FindObjectsSortMode.None)
            .FirstOrDefault(candidate => candidate.name == BrazierName);
        var anchor = brazier != null ? brazier.position : Vector3.zero;

        var data = terrain.terrainData;
        var paths = SketchPaths.Select(path => Smooth(path.Select(point => ToWorld(point, anchor, data.size.x)).ToArray())).ToArray();

        Undo.RegisterCompleteObjectUndo(data, "Paint Paths From Sketch");
        PaintAlphamap(data, terrain.transform.position, paths);

        var removed = RemoveTreesOnPaths(roots.FirstOrDefault(root => root.name == TreesName), paths);

        // The painted weights live in the terrain's splat textures, which must be saved explicitly.
        EditorUtility.SetDirty(data);
        foreach (var splat in data.alphamapTextures)
            EditorUtility.SetDirty(splat);
        AssetDatabase.SaveAssetIfDirty(data);
        EditorSceneManager.MarkSceneDirty(scene);
        Debug.Log($"Painted {paths.Length} paths around '{(brazier != null ? brazier.name : "scene origin")}', removed {removed} trees standing on them");
    }

    // Entry point for running headless: unity -batchmode -executeMethod PathPainter.PaintInBatch
    public static void PaintInBatch()
    {
        EditorSceneManager.OpenScene("Assets/Scenes/MainScene.unity");
        SavePreview("Logs/paths_before.png");
        Paint();
        SavePreview("Logs/paths_after.png");
        EditorSceneManager.SaveOpenScenes();
        AssetDatabase.SaveAssets();
    }

    // Writes the mud weights as a top-down image (top = +Z) for checking the result without opening Unity.
    static void SavePreview(string path)
    {
        var ground = Object.FindFirstObjectByType<Terrain>();
        if (ground == null)
            return;
        var data = ground.terrainData;
        var size = data.alphamapResolution;
        var weights = data.GetAlphamaps(0, 0, size, size);
        var image = new Texture2D(size, size);
        for (var row = 0; row < size; row++)
            for (var column = 0; column < size; column++)
                image.SetPixel(column, row, Color.Lerp(new Color(0.2f, 0.35f, 0.15f), new Color(0.75f, 0.55f, 0.35f), weights[row, column, 1]));
        System.IO.File.WriteAllBytes(path, image.EncodeToPNG());
        Object.DestroyImmediate(image);
    }

    static Vector2 P(float x, float y) => new Vector2(x, y);

    // Sketch pixels to world XZ, keeping the sketch's brazier on the real brazier. Image Y grows down, world Z grows up.
    static Vector2 ToWorld(Vector2 pixel, Vector3 anchor, float groundSize)
    {
        var offset = (pixel - SketchBrazier) / SketchSize * groundSize;
        return new Vector2(anchor.x + offset.x, anchor.z - offset.y);
    }

    // Catmull-Rom through the traced points so the paths curve instead of zig-zagging.
    static Vector2[] Smooth(Vector2[] points)
    {
        const int steps = 8;
        var result = new List<Vector2>();
        for (var i = 0; i < points.Length - 1; i++)
        {
            var p0 = points[Mathf.Max(i - 1, 0)];
            var p1 = points[i];
            var p2 = points[i + 1];
            var p3 = points[Mathf.Min(i + 2, points.Length - 1)];
            for (var step = 0; step < steps; step++)
            {
                var t = step / (float)steps;
                result.Add(0.5f * (2f * p1 + (p2 - p0) * t + (2f * p0 - 5f * p1 + 4f * p2 - p3) * t * t + (3f * p1 - p0 - 3f * p2 + p3) * t * t * t));
            }
        }

        result.Add(points[points.Length - 1]);
        return result.ToArray();
    }

    static void PaintAlphamap(TerrainData data, Vector3 terrainPosition, Vector2[][] paths)
    {
        var resolution = data.alphamapResolution;
        var layers = data.alphamapLayers;
        var weights = new float[resolution, resolution, layers];
        for (var row = 0; row < resolution; row++)
        {
            var z = terrainPosition.z + (row + 0.5f) / resolution * data.size.z;
            for (var column = 0; column < resolution; column++)
            {
                var x = terrainPosition.x + (column + 0.5f) / resolution * data.size.x;
                var distance = DistanceToPaths(new Vector2(x, z), paths);
                var mud = 1f - Mathf.SmoothStep(0f, 1f, (distance - PathHalfWidth) / PathEdgeSoftness + 0.5f);
                // Layer 0 is leaves, layer 1 is mud; any extra layers are cleared.
                weights[row, column, 0] = 1f - mud;
                weights[row, column, 1] = mud;
            }
        }

        data.SetAlphamaps(0, 0, weights);
    }

    static int RemoveTreesOnPaths(GameObject trees, Vector2[][] paths)
    {
        if (trees == null)
            return 0;

        var blocking = trees.transform.Cast<Transform>()
            .Where(tree => DistanceToPaths(new Vector2(tree.position.x, tree.position.z), paths) < PathHalfWidth + TreeClearance)
            .ToList();
        foreach (var tree in blocking)
            Undo.DestroyObjectImmediate(tree.gameObject);
        return blocking.Count;
    }

    static float DistanceToPaths(Vector2 point, Vector2[][] paths)
    {
        var closest = float.MaxValue;
        foreach (var path in paths)
        {
            for (var i = 0; i < path.Length - 1; i++)
            {
                var a = path[i];
                var segment = path[i + 1] - a;
                if (segment.sqrMagnitude < 0.0001f)
                    continue;
                var t = Mathf.Clamp01(Vector2.Dot(point - a, segment) / segment.sqrMagnitude);
                closest = Mathf.Min(closest, Vector2.Distance(point, a + segment * t));
            }
        }

        return closest;
    }
}
