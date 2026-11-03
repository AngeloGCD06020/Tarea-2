using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.XR.ARFoundation;
using UnityEngine.XR.ARSubsystems;

public class MultipleImageTracking : MonoBehaviour
{
    [Serializable]
    public class ImagePrefab
    {
        public string imageName;
        public GameObject imagePrefab;
    }

    [SerializeField] private ARTrackedImageManager imageManager;
    [SerializeField] private List<ImagePrefab> imagePrefabs;

    private readonly Dictionary<TrackableId, GameObject> spawnedObjects = new Dictionary<TrackableId, GameObject>();

    public event Action ModelsChanged;

    public IEnumerable<GameObject> ActiveModels
    {
        get
        {
            foreach (var model in spawnedObjects.Values)
                if (model != null && model.activeInHierarchy)
                    yield return model;
        }
    }

    private void OnEnable()
    {
        if (imageManager == null) return;
        imageManager.trackablesChanged.AddListener(OnImagesChanged);
        foreach (var image in imageManager.trackables)
            UpdateImage(image.trackableId, image.referenceImage.name, image.transform, image.trackingState);
    }

    private void OnDisable()
    {
        if (imageManager != null)
            imageManager.trackablesChanged.RemoveListener(OnImagesChanged);
        foreach (var model in spawnedObjects.Values)
            if (model != null) model.SetActive(false);
        ModelsChanged?.Invoke();
    }
    private void OnImagesChanged(ARTrackablesChangedEventArgs<ARTrackedImage> args)
    {
        foreach (var image in args.added)
            UpdateImage(image.trackableId, image.referenceImage.name, image.transform, image.trackingState);
        foreach (var image in args.updated)
            UpdateImage(image.trackableId, image.referenceImage.name, image.transform, image.trackingState);
        foreach (var removed in args.removed)
            RemoveImage(removed.Key);
    }

    private void UpdateImage(TrackableId id, string imageName, Transform anchor, TrackingState trackingState)
    {
        bool isTracking = trackingState == TrackingState.Tracking;
        if (!spawnedObjects.TryGetValue(id, out var model) || model == null)
        {
            if (!isTracking || imagePrefabs == null) return;
            foreach (var item in imagePrefabs)
            {
                if (item.imageName != imageName || item.imagePrefab == null) continue;
                model = Instantiate(item.imagePrefab, anchor);
                model.name = item.imageName;
                model.transform.localPosition = Vector3.zero;
                // Keep the rotation and scale authored in the referenced prefab.
                AddSelectionCollider(model);
                spawnedObjects[id] = model;
                model.SetActive(true);
                ModelsChanged?.Invoke();
                return;
            }
            return;
        }

        if (model.activeSelf == isTracking) return;
        model.SetActive(isTracking);
        ModelsChanged?.Invoke();
    }

    private void RemoveImage(TrackableId id)
    {
        if (!spawnedObjects.TryGetValue(id, out var model)) return;
        spawnedObjects.Remove(id);
        if (model != null)
        {
            model.SetActive(false);
            if (Application.isPlaying) Destroy(model);
            else DestroyImmediate(model);
        }
        ModelsChanged?.Invoke();
    }

    public bool TryGetModel(Transform hit, out GameObject model)
    {
        foreach (var candidate in ActiveModels)
        {
            if (hit == candidate.transform || hit.IsChildOf(candidate.transform))
            {
                model = candidate;
                return true;
            }
        }
        model = null;
        return false;
    }

    private static void AddSelectionCollider(GameObject model)
    {
        // Renderer bounds work for both imported meshes and animated characters,
        // without enabling Read/Write on the original model assets.
        bool hasBounds = false;
        Bounds bounds = default;
        foreach (var renderer in model.GetComponentsInChildren<Renderer>(true))
        {
            var local = renderer.localBounds;
            var toRoot = model.transform.worldToLocalMatrix * renderer.localToWorldMatrix;
            for (int corner = 0; corner < 8; corner++)
            {
                var point = local.center + Vector3.Scale(local.extents, new Vector3(
                    (corner & 1) == 0 ? -1 : 1,
                    (corner & 2) == 0 ? -1 : 1,
                    (corner & 4) == 0 ? -1 : 1));
                point = toRoot.MultiplyPoint3x4(point);
                if (!hasBounds) { bounds = new Bounds(point, Vector3.zero); hasBounds = true; }
                else bounds.Encapsulate(point);
            }
        }
        if (!hasBounds) return;
        var collider = model.AddComponent<BoxCollider>();
        collider.center = bounds.center;
        collider.size = Vector3.Max(bounds.size, Vector3.one * 0.01f);
        collider.isTrigger = true;
    }
}
