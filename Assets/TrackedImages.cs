// 03/01/2026 AI-Tag
// Image-tracking anchored content + robust debug for pivot/material/scale issues.

using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.XR.ARFoundation;
using UnityEngine.XR.ARSubsystems;

[RequireComponent(typeof(ARTrackedImageManager))]
public class TrackedImages : MonoBehaviour
{
    [Header("Prefab ime mora biti ENAKO kot Reference Image name v Image Library")]
    [SerializeField] private GameObject[] arPrefabs;

    [Header("Marker (pravokotnik na sliki)")]
    [SerializeField] private bool showImageMarker = true;
    [SerializeField] private float markerYOffset = 0.001f;

    [Header("Debug - krogla na izvoru slike")]
    [SerializeField] private bool showDebugSphere = true;
    [SerializeField] private float debugSphereDiameter = 0.04f;

    [Header("Diagnostika za prefab")]
    [Tooltip("Če vklopiš, bo skripta prefabu (in debug objektom) nastavila Unlit material, da izloči shader/pipeline probleme.")]
    [SerializeField] private bool forceUnlitMaterialOnContent = true;

    [Tooltip("Začasno: premakne instanciran prefab tako, da se njegov renderer-bounds center poravna na izvor slike.")]
    [SerializeField] private bool autoCenterContentToBounds = true;

    [Tooltip("Če je model absurden po velikosti, lahko začasno skaluješ na cilj (npr. ~0.12 m).")]
    [SerializeField] private bool autoScaleContent = false;

    [Tooltip("Ciljna največja dimenzija (v metrih), če je autoScaleContent vklopljen.")]
    [SerializeField] private float targetMaxDimensionMeters = 0.12f;

    [Header("Debug log")]
    [SerializeField] private bool debugLogs = true;

    [Header("Optional: custom marker/debug material (pusti prazno, če želiš auto)")]
    [SerializeField] private Material customUnlitMaterial;

    private ARTrackedImageManager trackedImageManager;

    private readonly Dictionary<TrackableId, GameObject> spawnedContentById = new();
    private readonly Dictionary<TrackableId, GameObject> markerById = new();
    private readonly Dictionary<TrackableId, GameObject> debugSphereById = new();

    private void Awake()
    {
        trackedImageManager = GetComponent<ARTrackedImageManager>();
    }

    private void OnEnable()
    {
        trackedImageManager.trackablesChanged.AddListener(OnTrackablesChanged);
    }

    private void OnDisable()
    {
        trackedImageManager.trackablesChanged.RemoveListener(OnTrackablesChanged);
    }

    private void OnTrackablesChanged(ARTrackablesChangedEventArgs<ARTrackedImage> args)
    {
        if (debugLogs) Debug.Log("[TrackedImages] trackablesChanged fired");

        HandleAddedOrUpdated(args.added);
        HandleAddedOrUpdated(args.updated);
        HandleRemoved(args.removed);
    }

    private void HandleAddedOrUpdated(object collection)
    {
        foreach (var item in (IEnumerable)collection)
        {
            if (item is ARTrackedImage ti) { UpsertFor(ti); continue; }
            if (item is KeyValuePair<TrackableId, ARTrackedImage> kvp) { UpsertFor(kvp.Value); continue; }
        }
    }

    private void HandleRemoved(object collection)
    {
        foreach (var item in (IEnumerable)collection)
        {
            if (item is ARTrackedImage ti) { RemoveFor(ti.trackableId); continue; }
            if (item is KeyValuePair<TrackableId, ARTrackedImage> kvp) { RemoveFor(kvp.Key); continue; }
            if (item is TrackableId id) { RemoveFor(id); continue; }
        }
    }

    private void UpsertFor(ARTrackedImage trackedImage)
    {
        var id = trackedImage.trackableId;
        var imageName = trackedImage.referenceImage.name;
        bool isTracking = trackedImage.trackingState == TrackingState.Tracking;

        if (debugLogs)
            Debug.Log($"[TrackedImages] '{imageName}' state={trackedImage.trackingState} size={trackedImage.referenceImage.size}");

        // Marker
        if (showImageMarker)
        {
            var marker = EnsureMarker(id, trackedImage);
            marker.SetActive(isTracking);
        }

        // Debug sphere
        if (showDebugSphere)
        {
            var sphere = EnsureDebugSphere(id, trackedImage);
            sphere.SetActive(isTracking);
        }

        // Content
        var content = EnsureContentPrefab(id, trackedImage, imageName);
        if (content == null) return;

        content.SetActive(isTracking);

        // Keep local alignment to tracked image
        content.transform.localRotation = Quaternion.identity;

        // Diagnostika: bounds, pivot offset, materiali, skala
        if (isTracking)
        {
            DiagnoseAndFixContent(trackedImage, content);
        }
    }

    private void DiagnoseAndFixContent(ARTrackedImage trackedImage, GameObject content)
    {
        var renderers = content.GetComponentsInChildren<Renderer>(true);

        if (debugLogs)
            Debug.Log($"[TrackedImages] Content '{content.name}' localScale={content.transform.localScale} renderers={renderers.Length}");

        if (renderers.Length == 0)
        {
            Debug.LogWarning($"[TrackedImages] Content '{content.name}' has 0 renderers. Prefab nima MeshRenderer/SkinnedMeshRenderer ali je vse izklopljeno.");
            return;
        }

        // Combined bounds in WORLD space
        var b = renderers[0].bounds;
        for (int i = 1; i < renderers.Length; i++)
            b.Encapsulate(renderers[i].bounds);

        // Bounds center in TRACKED IMAGE local space
        Vector3 centerLocalToImage = trackedImage.transform.InverseTransformPoint(b.center);

        if (debugLogs)
        {
            Debug.Log($"[TrackedImages] '{content.name}' bounds.size={b.size} (m) bounds.center(localToImage)={centerLocalToImage}");
        }

        // Force unlit material (to remove URP/Standard shader issues)
        if (forceUnlitMaterialOnContent)
        {
            var mat = GetOrCreateUnlitMaterial();
            foreach (var r in renderers)
            {
                // nastavitev na vse materiale
                var mats = r.sharedMaterials;
                for (int i = 0; i < mats.Length; i++) mats[i] = mat;
                r.sharedMaterials = mats;
                r.enabled = true;
            }
        }

        // Auto-center: if pivot/mesh is offset far away, bring bounds center onto the image origin.
        if (autoCenterContentToBounds)
        {
            // premakni prefab, da center bounds-a pride v (0,0,0) slike
            // (odštejemo lokalni offset centra)
            content.transform.localPosition = content.transform.localPosition - centerLocalToImage;

            if (debugLogs)
                Debug.Log($"[TrackedImages] Auto-centered '{content.name}' by {-centerLocalToImage}");
        }
        else
        {
            // default: center prefab at image origin
            content.transform.localPosition = Vector3.zero;
        }

        // Auto-scale (optional)
        if (autoScaleContent)
        {
            float maxDim = Mathf.Max(b.size.x, Mathf.Max(b.size.y, b.size.z));
            if (maxDim > 0.0001f)
            {
                float factor = targetMaxDimensionMeters / maxDim;
                content.transform.localScale = content.transform.localScale * factor;

                if (debugLogs)
                    Debug.Log($"[TrackedImages] Auto-scaled '{content.name}' factor={factor} targetMax={targetMaxDimensionMeters}m");
            }
        }
    }

    private GameObject EnsureContentPrefab(TrackableId id, ARTrackedImage trackedImage, string imageName)
    {
        if (spawnedContentById.TryGetValue(id, out var existing) && existing != null)
        {
            if (existing.transform.parent != trackedImage.transform)
                existing.transform.SetParent(trackedImage.transform, worldPositionStays: false);
            return existing;
        }

        if (string.IsNullOrWhiteSpace(imageName))
        {
            if (debugLogs) Debug.LogWarning("[TrackedImages] Tracked image has empty reference name (ignoring).");
            return null;
        }

        var prefab = FindPrefabByName(imageName);
        if (prefab == null)
        {
            Debug.LogWarning($"[TrackedImages] No prefab found for '{imageName}'. Name must match Reference Image name.");
            return null;
        }

        var spawned = Instantiate(prefab, trackedImage.transform);
        spawned.name = imageName;

        // Keep it local to image
        spawned.transform.localPosition = Vector3.zero;
        spawned.transform.localRotation = Quaternion.identity;

        spawnedContentById[id] = spawned;
        return spawned;
    }

    private GameObject EnsureMarker(TrackableId id, ARTrackedImage trackedImage)
    {
        if (markerById.TryGetValue(id, out var existing) && existing != null)
        {
            if (existing.transform.parent != trackedImage.transform)
                existing.transform.SetParent(trackedImage.transform, worldPositionStays: false);

            UpdateMarkerTransform(existing.transform, trackedImage.referenceImage.size);
            return existing;
        }

        var marker = GameObject.CreatePrimitive(PrimitiveType.Quad);
        marker.name = $"Marker_{trackedImage.referenceImage.name}";
        marker.transform.SetParent(trackedImage.transform, worldPositionStays: false);

        var col = marker.GetComponent<Collider>();
        if (col != null) Destroy(col);

        var renderer = marker.GetComponent<MeshRenderer>();
        renderer.material = GetOrCreateUnlitMaterial();
        renderer.enabled = true;

        UpdateMarkerTransform(marker.transform, trackedImage.referenceImage.size);

        markerById[id] = marker;
        return marker;
    }

    private void UpdateMarkerTransform(Transform markerTransform, Vector2 physicalSizeMeters)
    {
        markerTransform.localRotation = Quaternion.identity;
        markerTransform.localScale = new Vector3(physicalSizeMeters.x, physicalSizeMeters.y, 1f);
        markerTransform.localPosition = Vector3.zero;
    }

    private GameObject EnsureDebugSphere(TrackableId id, ARTrackedImage trackedImage)
    {
        if (debugSphereById.TryGetValue(id, out var existing) && existing != null)
        {
            if (existing.transform.parent != trackedImage.transform)
                existing.transform.SetParent(trackedImage.transform, worldPositionStays: false);

            existing.transform.localPosition = new Vector3(0f, markerYOffset + 0.02f, 0f);
            existing.transform.localRotation = Quaternion.identity;
            existing.transform.localScale = Vector3.one * debugSphereDiameter;

            return existing;
        }

        var sphere = GameObject.CreatePrimitive(PrimitiveType.Sphere);
        sphere.name = $"DebugSphere_{trackedImage.referenceImage.name}";
        sphere.transform.SetParent(trackedImage.transform, worldPositionStays: false);

        var col = sphere.GetComponent<Collider>();
        if (col != null) Destroy(col);

        var r = sphere.GetComponent<Renderer>();
        r.material = GetOrCreateUnlitMaterial();
        r.enabled = true;

        sphere.transform.localPosition = new Vector3(0f, markerYOffset + 0.02f, 0f);
        sphere.transform.localRotation = Quaternion.identity;
        sphere.transform.localScale = Vector3.one * debugSphereDiameter;

        debugSphereById[id] = sphere;
        return sphere;
    }

    private Material GetOrCreateUnlitMaterial()
    {
        if (customUnlitMaterial != null) return customUnlitMaterial;

        Shader shader =
            Shader.Find("Universal Render Pipeline/Unlit") ??
            Shader.Find("Unlit/Color") ??
            Shader.Find("Sprites/Default");

        var mat = new Material(shader != null ? shader : Shader.Find("Standard"));
        mat.color = new Color(1f, 0.2f, 1f, 0.7f); // “magenta-ish” ampak stabilno unlit
        customUnlitMaterial = mat;
        return mat;
    }

    private void RemoveFor(TrackableId id)
    {
        if (spawnedContentById.TryGetValue(id, out var content) && content != null) Destroy(content);
        spawnedContentById.Remove(id);

        if (markerById.TryGetValue(id, out var marker) && marker != null) Destroy(marker);
        markerById.Remove(id);

        if (debugSphereById.TryGetValue(id, out var sphere) && sphere != null) Destroy(sphere);
        debugSphereById.Remove(id);
    }

    private GameObject FindPrefabByName(string imageName)
    {
        foreach (var prefab in arPrefabs)
        {
            if (prefab == null) continue;
            if (string.Equals(prefab.name, imageName, StringComparison.OrdinalIgnoreCase))
                return prefab;
        }
        return null;
    }
}
