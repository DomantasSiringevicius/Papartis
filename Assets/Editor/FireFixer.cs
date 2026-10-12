using System.Linq;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.Rendering;

/// <summary>
/// One-off helper: configures the existing "Fire" particle system in the open scene
/// and gives it a flickering point light. Safe to delete once the fire is set up.
/// </summary>
public static class FireFixer
{
    const string FireName = "Fire";
    const string LightName = "Fire Light";
    const string MaterialPath = "Assets/Effects/Fire/Fire.mat";

    [MenuItem("Papartis/Fix Fire")]
    public static void Fix()
    {
        var particles = FindFire();
        if (particles == null)
        {
            Debug.LogError($"No particle system named '{FireName}' found. Select the fire object and try again.");
            return;
        }

        var material = AssetDatabase.LoadAssetAtPath<Material>(MaterialPath);
        if (material == null)
        {
            Debug.LogError($"Material not found at {MaterialPath}");
            return;
        }

        var fire = particles.transform;
        var renderer = particles.GetComponent<ParticleSystemRenderer>();
        Undo.RecordObjects(new Object[] { particles, fire, renderer }, "Fix Fire");

        // The cone emits along local Z, so point Z up. Cancel the parent's scale so the
        // values below are in metres even though the Blender model's nodes are scaled.
        fire.rotation = Quaternion.Euler(-90f, 0f, 0f);
        if (fire.parent != null)
        {
            var parentScale = fire.parent.lossyScale;
            fire.localScale = new Vector3(1f / parentScale.x, 1f / parentScale.y, 1f / parentScale.z);
        }

        var radius = GetBowlRadius(fire);
        ConfigureParticles(particles, radius);

        renderer.sharedMaterial = material;
        renderer.shadowCastingMode = ShadowCastingMode.Off;
        renderer.receiveShadows = false;

        var previousLight = fire.Find(LightName);
        if (previousLight != null)
            Undo.DestroyObjectImmediate(previousLight.gameObject);

        var lightObject = new GameObject(LightName, typeof(Light), typeof(FireLightFlicker));
        Undo.RegisterCreatedObjectUndo(lightObject, "Fix Fire");
        lightObject.transform.SetParent(fire, false);
        lightObject.transform.position = fire.position + Vector3.up * radius * 1.5f;
        var light = lightObject.GetComponent<Light>();
        light.type = LightType.Point;
        light.color = new Color(1f, 0.54f, 0.19f);
        light.intensity = 3f;
        light.range = 7f;
        light.shadows = LightShadows.None;

        particles.Clear();
        particles.Play();
        EditorSceneManager.MarkSceneDirty(fire.gameObject.scene);
        Selection.activeGameObject = fire.gameObject;
        Debug.Log($"Fire fixed on '{fire.name}' (radius {radius:0.00} m)");
    }

    static ParticleSystem FindFire()
    {
        var selected = Selection.activeGameObject;
        if (selected != null && selected.scene.IsValid() && selected.TryGetComponent<ParticleSystem>(out var selectedParticles))
            return selectedParticles;

        return Object.FindObjectsByType<ParticleSystem>(FindObjectsInactive.Include, FindObjectsSortMode.None)
            .FirstOrDefault(candidate => candidate.name == FireName);
    }

    // Sizes the fire from the brazier model the fire sits in.
    static float GetBowlRadius(Transform fire)
    {
        var root = fire.parent != null ? fire.parent.root : null;
        var prefabRoot = fire.parent != null ? PrefabUtility.GetOutermostPrefabInstanceRoot(fire.parent.gameObject) : null;
        if (prefabRoot != null)
            root = prefabRoot.transform;
        if (root == null)
            return 0.25f;

        var meshes = root.GetComponentsInChildren<MeshRenderer>();
        if (meshes.Length == 0)
            return 0.25f;

        var bounds = meshes[0].bounds;
        foreach (var mesh in meshes)
            bounds.Encapsulate(mesh.bounds);
        return Mathf.Clamp(Mathf.Min(bounds.extents.x, bounds.extents.z) * 0.6f, 0.05f, 1f);
    }

    static void ConfigureParticles(ParticleSystem particles, float radius)
    {
        var main = particles.main;
        main.loop = true;
        main.playOnAwake = true;
        main.startLifetime = new ParticleSystem.MinMaxCurve(0.6f, 1.1f);
        main.startSpeed = new ParticleSystem.MinMaxCurve(radius * 2f, radius * 4f);
        main.startSize = new ParticleSystem.MinMaxCurve(radius * 1.2f, radius * 2f);
        main.startRotation = new ParticleSystem.MinMaxCurve(0f, Mathf.PI * 2f);
        main.startColor = Color.white;
        main.simulationSpace = ParticleSystemSimulationSpace.World;
        main.scalingMode = ParticleSystemScalingMode.Hierarchy;
        main.maxParticles = 100;

        var emission = particles.emission;
        emission.enabled = true;
        emission.rateOverTime = 25f;

        var shape = particles.shape;
        shape.enabled = true;
        shape.shapeType = ParticleSystemShapeType.Cone;
        shape.angle = 8f;
        shape.radius = radius * 0.6f;

        var gradient = new Gradient();
        gradient.SetKeys(
            new[]
            {
                new GradientColorKey(new Color(1f, 0.9f, 0.5f), 0f),
                new GradientColorKey(new Color(1f, 0.5f, 0.1f), 0.4f),
                new GradientColorKey(new Color(0.6f, 0.1f, 0.02f), 1f),
            },
            new[]
            {
                new GradientAlphaKey(0f, 0f),
                new GradientAlphaKey(1f, 0.15f),
                new GradientAlphaKey(0.8f, 0.5f),
                new GradientAlphaKey(0f, 1f),
            });
        var colorOverLifetime = particles.colorOverLifetime;
        colorOverLifetime.enabled = true;
        colorOverLifetime.color = new ParticleSystem.MinMaxGradient(gradient);

        var sizeOverLifetime = particles.sizeOverLifetime;
        sizeOverLifetime.enabled = true;
        sizeOverLifetime.size = new ParticleSystem.MinMaxCurve(1f,
            new AnimationCurve(new Keyframe(0f, 0.6f), new Keyframe(0.2f, 1f), new Keyframe(1f, 0.1f)));

        var noise = particles.noise;
        noise.enabled = true;
        noise.strength = radius * 0.3f;
        noise.frequency = 1.5f;
    }
}
