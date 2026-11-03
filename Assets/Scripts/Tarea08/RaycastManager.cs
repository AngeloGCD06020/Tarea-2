using UnityEngine;
using UnityEngine.InputSystem;

namespace LolSuperman.Tarea08
{
    public sealed class RaycastManager : MonoBehaviour
    {
        [SerializeField] private ImageModelTrackingManager imageTracking;
        [SerializeField] private Camera selectionCamera;
        [SerializeField] private ModelInfoPanel informationPanel;
        [SerializeField] private RectTransform[] blockingUI;
        [SerializeField] private LayerMask modelLayers = Physics.DefaultRaycastLayers;
        [SerializeField, Min(1)] private float maximumDistance = 100f;

        private TrackedModel selectedModel;
        public TrackedModel SelectedModel => selectedModel;
        public Camera SelectionCamera => selectionCamera;
        public ModelInfoPanel InformationPanel => informationPanel;

        public void Configure(ImageModelTrackingManager tracking, Camera camera, ModelInfoPanel panel, RectTransform[] uiRegions)
        {
            Unsubscribe();
            imageTracking = tracking;
            selectionCamera = camera;
            informationPanel = panel;
            blockingUI = uiRegions;
            if (isActiveAndEnabled) Subscribe();
            ClearSelection();
            RefreshTracking();
        }

        private void OnEnable()
        {
            Subscribe();
            ClearSelection();
            RefreshTracking();
        }

        private void Subscribe()
        {
            if (imageTracking == null) return;
            imageTracking.ModelsChanged -= RefreshTracking;
            imageTracking.ModelsChanged += RefreshTracking;
        }

        private void Unsubscribe()
        {
            if (imageTracking != null) imageTracking.ModelsChanged -= RefreshTracking;
        }

        private void OnDisable()
        {
            Unsubscribe();
            ClearSelection();
        }

        private void Update()
        {
            var touch = Touchscreen.current;
            if (touch != null && touch.primaryTouch.press.wasPressedThisFrame)
                SelectAtScreenPosition(touch.primaryTouch.position.ReadValue());
            else if (Mouse.current != null && Mouse.current.leftButton.wasPressedThisFrame)
                SelectAtScreenPosition(Mouse.current.position.ReadValue());
        }

        public bool SelectAtScreenPosition(Vector2 position)
        {
            if (selectionCamera == null) return false;
            if (blockingUI != null)
            {
                foreach (var region in blockingUI)
                {
                    if (region == null || !region.gameObject.activeInHierarchy) continue;
                    var canvas = region.GetComponentInParent<Canvas>();
                    var uiCamera = canvas != null && canvas.renderMode != RenderMode.ScreenSpaceOverlay
                        ? canvas.worldCamera : null;
                    if (RectTransformUtility.RectangleContainsScreenPoint(region, position, uiCamera)) return false;
                }
            }
            return SelectRay(selectionCamera.ScreenPointToRay(position));
        }

        public bool SelectRay(Ray ray)
        {
            ClearSelection();
            if (imageTracking == null) return false;
            Physics.SyncTransforms();
            if (!Physics.Raycast(ray, out var hit, maximumDistance, modelLayers, QueryTriggerInteraction.Collide) ||
                !imageTracking.TryGetModel(hit.transform, out var model) || model.Information == null)
                return false;

            selectedModel = model;
            selectedModel.BecameUnavailable += OnSelectedModelUnavailable;
            if (informationPanel != null) informationPanel.Show(model.Information);
            return true;
        }

        public void ClearSelection()
        {
            if (selectedModel != null) selectedModel.BecameUnavailable -= OnSelectedModelUnavailable;
            selectedModel = null;
            if (informationPanel != null) informationPanel.ResetToOriginal();
        }

        private void OnSelectedModelUnavailable(TrackedModel model) => ClearSelection();

        private void RefreshTracking()
        {
            if (selectedModel == null || !selectedModel.gameObject.activeInHierarchy)
                ClearSelection();
            if (informationPanel != null)
                informationPanel.SetTrackedCount(imageTracking != null ? imageTracking.ActiveModelCount : 0);
        }
    }
}
