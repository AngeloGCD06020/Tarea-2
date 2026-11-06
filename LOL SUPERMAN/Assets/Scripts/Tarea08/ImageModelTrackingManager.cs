using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.XR.ARFoundation;
using UnityEngine.XR.ARSubsystems;

namespace LolSuperman.Tarea08
{
    [DisallowMultipleComponent]
    public sealed class ImageModelTrackingManager : MonoBehaviour
    {
        [Serializable]
        public sealed class ModelBinding
        {
            public string referenceImageName;
            public GameObject prefab;
            public ModelInfo information;
        }

        [SerializeField] private ARTrackedImageManager imageManager;
        [SerializeField] private List<ModelBinding> models = new List<ModelBinding>();
        private readonly Dictionary<TrackableId, TrackedModel> instances = new Dictionary<TrackableId, TrackedModel>();

        public event Action ModelsChanged;
        public IReadOnlyList<ModelBinding> Bindings => models;
        public ARTrackedImageManager ImageManager => imageManager;
        public IEnumerable<TrackedModel> ActiveModels
        {
            get
            {
                foreach (var model in instances.Values)
                    if (model != null && model.gameObject.activeInHierarchy)
                        yield return model;
            }
        }

        public int ActiveModelCount
        {
            get
            {
                int count = 0;
                foreach (var model in ActiveModels) count++;
                return count;
            }
        }

        public void Configure(ARTrackedImageManager manager, IEnumerable<ModelBinding> bindings)
        {
            Unsubscribe();
            ClearInstances();
            imageManager = manager;
            models = new List<ModelBinding>(bindings);
            if (isActiveAndEnabled) Subscribe();
        }

        private void OnEnable() => Subscribe();

        private void Subscribe()
        {
            if (imageManager == null) return;
            imageManager.trackablesChanged.RemoveListener(OnImagesChanged);
            imageManager.trackablesChanged.AddListener(OnImagesChanged);
            foreach (var image in imageManager.trackables)
                ApplyTracking(image.trackableId, image.referenceImage.name, image.transform, image.trackingState);
        }

        private void Unsubscribe()
        {
            if (imageManager != null)
                imageManager.trackablesChanged.RemoveListener(OnImagesChanged);
        }

        private void OnDisable()
        {
            Unsubscribe();
            foreach (var model in instances.Values)
                if (model != null) model.gameObject.SetActive(false);
            ModelsChanged?.Invoke();
        }

        private void OnDestroy() => ClearInstances();

        private void OnImagesChanged(ARTrackablesChangedEventArgs<ARTrackedImage> changes)
        {
            foreach (var image in changes.added)
                ApplyTracking(image.trackableId, image.referenceImage.name, image.transform, image.trackingState);
            foreach (var image in changes.updated)
                ApplyTracking(image.trackableId, image.referenceImage.name, image.transform, image.trackingState);
            foreach (var removed in changes.removed)
                RemoveTrackable(removed.Key);
        }

        // The AR event and the deterministic verification use the same lifecycle.
        public void ApplyTracking(TrackableId id, string imageName, Transform anchor, TrackingState state)
        {
            bool visible = state == TrackingState.Tracking;
            if (!instances.TryGetValue(id, out var instance) || instance == null)
            {
                if (!visible || anchor == null) return;
                var binding = models.Find(item => item.referenceImageName == imageName);
                if (binding == null || binding.prefab == null || binding.information == null) return;

                var root = Instantiate(binding.prefab, anchor, false);
                root.name = binding.information.ModelName;
                root.transform.localPosition = Vector3.zero;
                root.transform.localRotation = binding.prefab.transform.localRotation;
                root.transform.localScale = binding.prefab.transform.localScale;
                instance = root.GetComponent<TrackedModel>();
                if (instance == null) instance = root.AddComponent<TrackedModel>();
                instance.SetInformation(binding.information);
                AddSelectionCollider(root);
                instances[id] = instance;
                root.SetActive(true);
                ModelsChanged?.Invoke();
                return;
            }

            if (instance.gameObject.activeSelf == visible) return;
            instance.gameObject.SetActive(visible);
            ModelsChanged?.Invoke();
        }

        public void RemoveTrackable(TrackableId id)
        {
            if (!instances.TryGetValue(id, out var model)) return;
            instances.Remove(id);
            Release(model);
            ModelsChanged?.Invoke();
        }

        public bool TryGetModel(Transform hit, out TrackedModel model)
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

        public bool TryGetInstance(TrackableId id, out TrackedModel model) => instances.TryGetValue(id, out model);

        private void ClearInstances()
        {
            foreach (var model in instances.Values) Release(model);
            instances.Clear();
        }

        private static void Release(TrackedModel model)
        {
            if (model == null) return;
            model.gameObject.SetActive(false);
            if (Application.isPlaying) Destroy(model.gameObject);
            else DestroyImmediate(model.gameObject);
        }

        private static void AddSelectionCollider(GameObject root)
        {
            bool hasBounds = false;
            Bounds bounds = default;
            foreach (var renderer in root.GetComponentsInChildren<Renderer>(true))
            {
                var local = renderer.localBounds;
                var toRoot = root.transform.worldToLocalMatrix * renderer.localToWorldMatrix;
                for (int corner = 0; corner < 8; corner++)
                {
                    var sign = new Vector3((corner & 1) == 0 ? -1 : 1,
                        (corner & 2) == 0 ? -1 : 1, (corner & 4) == 0 ? -1 : 1);
                    var point = toRoot.MultiplyPoint3x4(local.center + Vector3.Scale(local.extents, sign));
                    if (!hasBounds) { bounds = new Bounds(point, Vector3.zero); hasBounds = true; }
                    else bounds.Encapsulate(point);
                }
            }
            if (!hasBounds) return;
            var collider = root.AddComponent<BoxCollider>();
            collider.center = bounds.center;
            collider.size = Vector3.Max(bounds.size, Vector3.one * 0.01f);
            collider.isTrigger = true;
        }
    }
}
