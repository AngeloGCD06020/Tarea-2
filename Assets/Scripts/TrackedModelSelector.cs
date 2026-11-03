using System.Collections.Generic;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

public class TrackedModelSelector : MonoBehaviour
{
    [SerializeField] private MultipleImageTracking imageTracking;
    [SerializeField] private Camera selectionCamera;
    [SerializeField] private Text detectedImagesText;
    [SerializeField] private Text selectedModelText;
    [SerializeField] private RectTransform informationPanel;

    private GameObject selectedModel;
    public GameObject SelectedModel => selectedModel;

    private void OnEnable()
    {
        EnsureUI();
        if (imageTracking != null) imageTracking.ModelsChanged += RefreshInformation;
        RefreshInformation();
    }

    private void OnDisable()
    {
        if (imageTracking != null) imageTracking.ModelsChanged -= RefreshInformation;
        selectedModel = null;
        RefreshInformation();
    }

    private void Update()
    {
        var touch = Touchscreen.current;
        if (touch != null && touch.primaryTouch.press.wasPressedThisFrame)
            SelectAtScreenPosition(touch.primaryTouch.position.ReadValue());
        else if (Mouse.current != null && Mouse.current.leftButton.wasPressedThisFrame)
            SelectAtScreenPosition(Mouse.current.position.ReadValue());
    }

    public void SelectAtScreenPosition(Vector2 screenPosition)
    {
        if (imageTracking == null || selectionCamera == null) return;
        if (informationPanel != null &&
            RectTransformUtility.RectangleContainsScreenPoint(informationPanel, screenPosition)) return;

        selectedModel = null;
        Physics.SyncTransforms();
        var ray = selectionCamera.ScreenPointToRay(screenPosition);
        if (Physics.Raycast(ray, out var hit, selectionCamera.farClipPlane,
            Physics.DefaultRaycastLayers, QueryTriggerInteraction.Collide))
            imageTracking.TryGetModel(hit.transform, out selectedModel);

        RefreshInformation();
    }

    private void RefreshInformation()
    {
        if (selectedModel != null && !selectedModel.activeInHierarchy) selectedModel = null;
        if (selectedModelText != null)
            selectedModelText.text = selectedModel != null
                ? "Modelo seleccionado: " + selectedModel.name
                : "Toca un modelo para ver su nombre.";

        if (detectedImagesText == null) return;
        var lines = new List<string>();
        if (imageTracking != null)
            foreach (var model in imageTracking.ActiveModels)
                lines.Add("Imagen detectada: " + model.name);
        detectedImagesText.text = lines.Count == 0
            ? "Apunta la cámara a una imagen."
            : string.Join("\n", lines);
    }

    public void EnsureUI()
    {
        if (detectedImagesText != null && selectedModelText != null && informationPanel != null) return;

        var canvasObject = new GameObject("Image Tracking UI", typeof(RectTransform), typeof(Canvas), typeof(CanvasScaler));
        SceneManager.MoveGameObjectToScene(canvasObject, gameObject.scene);
        var canvas = canvasObject.GetComponent<Canvas>();
        canvas.renderMode = RenderMode.ScreenSpaceOverlay;
        canvas.sortingOrder = 20;
        var scaler = canvasObject.GetComponent<CanvasScaler>();
        scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
        scaler.referenceResolution = new Vector2(1080, 1920);
        scaler.screenMatchMode = CanvasScaler.ScreenMatchMode.MatchWidthOrHeight;
        scaler.matchWidthOrHeight = 0.5f;

        var panelObject = new GameObject("Model Information", typeof(RectTransform), typeof(Image));
        informationPanel = panelObject.GetComponent<RectTransform>();
        informationPanel.SetParent(canvasObject.transform, false);
        informationPanel.anchorMin = new Vector2(0, 0.05f);
        informationPanel.anchorMax = new Vector2(1, 0.05f);
        informationPanel.pivot = new Vector2(0.5f, 0);
        informationPanel.offsetMin = new Vector2(24, 0);
        informationPanel.offsetMax = new Vector2(-24, 320);
        var background = panelObject.GetComponent<Image>();
        background.color = new Color(0.025f, 0.04f, 0.07f, 0.88f);
        background.raycastTarget = false;

        var font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
        detectedImagesText = CreateText("Detected Images", font, new Vector2(0, 0.43f), new Vector2(1, 1), 32);
        selectedModelText = CreateText("Selected Model", font, Vector2.zero, new Vector2(1, 0.43f), 36);
        selectedModelText.fontStyle = FontStyle.Bold;
        selectedModelText.color = new Color(0.48f, 0.91f, 1);
        RefreshInformation();
    }

    private Text CreateText(string objectName, Font font, Vector2 anchorMin, Vector2 anchorMax, int fontSize)
    {
        var textObject = new GameObject(objectName, typeof(RectTransform), typeof(Text));
        var rect = textObject.GetComponent<RectTransform>();
        rect.SetParent(informationPanel, false);
        rect.anchorMin = anchorMin;
        rect.anchorMax = anchorMax;
        rect.offsetMin = new Vector2(28, 18);
        rect.offsetMax = new Vector2(-28, -18);
        var text = textObject.GetComponent<Text>();
        text.font = font;
        text.fontSize = fontSize;
        text.color = Color.white;
        text.alignment = TextAnchor.MiddleLeft;
        text.raycastTarget = false;
        text.supportRichText = false;
        return text;
    }
}
