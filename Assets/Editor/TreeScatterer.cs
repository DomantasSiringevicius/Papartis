using System.Collections.Generic;
using System.Linq;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;

/// <summary>
/// Scatters the Dry_Trees prefabs randomly over the floor of the open scene,
/// leaving a winding walkable path along the Z axis and a clearing around the player start.
/// </summary>
public static class TreeScatterer
{
    const string PrefabFolder = "Assets/Dry_Trees/Prefab";
    const string ParentName = "Trees";

    const string FloorName = "Floor";

    // The trees are roughly 8 m tall with wide crowns, so they need metres of room each.
    const float MinTreeSpacing = 7f;
    const float PathHalfWidth = 3f;
    const float ClearingRadius = 4f;
    const float EdgeMargin = 1f;
    const float FallbackHalfSize = 10f;

    [MenuItem("Papartis/Scatter Trees")]
    public static void Scatter()
    {
        var prefabs = AssetDatabase.FindAssets("t:Prefab", new[] { PrefabFolder })
            .Select(guid => AssetDatabase.LoadAssetAtPath<GameObject>(AssetDatabase.GUIDToAssetPath(guid)))
            .Where(prefab => prefab != null)
            .ToArray();
        if (prefabs.Length == 0)
        {
            Debug.LogError($"No tree prefabs found in {PrefabFolder}");
            return;
        }

        // Re-running replaces the previous layout with a new random one.
        var scene = SceneManager.GetActiveScene();
        var previous = scene.GetRootGameObjects().FirstOrDefault(root => root.name == ParentName);
        if (previous != null)
            Undo.DestroyObjectImmediate(previous);

        var parent = new GameObject(ParentName);
        Undo.RegisterCreatedObjectUndo(parent, "Scatter Trees");

        var area = new Bounds(Vector3.zero, Vector3.one * FallbackHalfSize * 2f);
        var floor = scene.GetRootGameObjects().FirstOrDefault(root => root.name == FloorName);
        if (floor != null && floor.TryGetComponent<Renderer>(out var floorRenderer))
            area = floorRenderer.bounds;
        else
            Debug.LogWarning($"No '{FloorName}' object found, scattering over a {FallbackHalfSize * 2f} m square");
        area.Expand(-EdgeMargin * 2f);
        var targetCount = Mathf.CeilToInt(area.size.x * area.size.z / (MinTreeSpacing * MinTreeSpacing));
        var placed = new List<Vector2>();
        for (var attempt = 0; attempt < targetCount * 40 && placed.Count < targetCount; attempt++)
        {
            var point = new Vector2(
                Random.Range(area.min.x, area.max.x),
                Random.Range(area.min.z, area.max.z));

            if (Mathf.Abs(point.x - PathCenterX(point.y)) < PathHalfWidth)
                continue;
            if (point.magnitude < ClearingRadius)
                continue;
            if (placed.Any(other => Vector2.Distance(other, point) < MinTreeSpacing))
                continue;

            var prefab = prefabs[Random.Range(0, prefabs.Length)];
            var tree = (GameObject)PrefabUtility.InstantiatePrefab(prefab, parent.transform);
            tree.transform.position = new Vector3(point.x, 0f, point.y);
            tree.transform.rotation = Quaternion.Euler(0f, Random.Range(0f, 360f), 0f) * tree.transform.rotation;
            tree.transform.localScale *= Random.Range(0.85f, 1.15f);
            placed.Add(point);
        }

        EditorSceneManager.MarkSceneDirty(scene);
        Debug.Log($"Scattered {placed.Count} trees under '{ParentName}'");
    }

    static float PathCenterX(float z)
    {
        return 3f * Mathf.Sin(z * 0.15f);
    }
}
