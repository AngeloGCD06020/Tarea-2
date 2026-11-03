using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using LolSuperman.Tarea08;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;
using UnityEngine.UI;
using UnityEngine.XR.ARFoundation;
using UnityEngine.XR.ARSubsystems;
using Object = UnityEngine.Object;

// This records the real scene/UI/Physics raycasts, with explicitly simulated AR tracking.
// It runs only in the Editor and does not add a simulation path to the Android app.
public static class Tarea08DemoCapture
{
    private const int Width = 1080;
    private const int Height = 1920;
    private const int FramesPerSecond = 12;
    private const int FramesPerStage = 30;

    [MenuItem("Tareas/Tarea 08/Grabar demostración de editor")]
    public static void Capture()
    {
        string output = Tarea08Automation.Output + "/DemoFrames";
        Directory.CreateDirectory(output);
        EditorSceneManager.OpenScene(Tarea08Automation.ScenePath, OpenSceneMode.Single);
        var tracking = Object.FindFirstObjectByType<ImageModelTrackingManager>();
        var raycast = Object.FindFirstObjectByType<LolSuperman.Tarea08.RaycastManager>();
        var panel = Object.FindFirstObjectByType<ModelInfoPanel>();
        var camera = raycast.SelectionCamera;
        var bindings = tracking.Bindings.ToArray();
        foreach (var behaviour in Object.FindObjectsByType<Behaviour>(FindObjectsInactive.Include, FindObjectsSortMode.None))
        {
            if (behaviour is ARSession || behaviour is ARInputManager || behaviour is ARTrackedImageManager ||
                behaviour is ARCameraManager || behaviour is ARCameraBackground || behaviour.GetType().Name == "TrackedPoseDriver")
                behaviour.enabled = false;
        }
        tracking.Configure(null, bindings);
        camera.transform.SetParent(null, true);
        camera.transform.SetPositionAndRotation(new Vector3(0, 0.68f, -4), Quaternion.identity);
        camera.orthographic = true;
        camera.orthographicSize = 1.6f;
        camera.clearFlags = CameraClearFlags.SolidColor;
        camera.backgroundColor = new Color(0.065f, 0.10f, 0.16f);
        camera.nearClipPlane = 0.01f;
        camera.farClipPlane = 100;
        camera.stereoTargetEye = StereoTargetEyeMask.None;
        var target = new RenderTexture(Width, Height, 24, RenderTextureFormat.ARGB32);
        target.Create();
        camera.targetTexture = target;
        camera.aspect = (float)Width / Height;

        var canvas = panel.GetComponentInParent<Canvas>();
        canvas.renderMode = RenderMode.ScreenSpaceCamera;
        canvas.worldCamera = camera;
        canvas.planeDistance = 0.5f;
        foreach (var safeArea in canvas.GetComponentsInChildren<SafeAreaPanel>()) safeArea.enabled = false;
        var header = canvas.transform.Find("Safe Area/Header").GetComponent<RectTransform>();
        raycast.Configure(tracking, camera, panel, new[] { header, panel.GetComponent<RectTransform>() });
        var font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
        var watermark = AddLabel(canvas.transform, "Simulation Watermark", font, 21, new Color(1, 0.79f, 0.35f));
        watermark.text = "DEMOSTRACIÓN DE EDITOR · SEGUIMIENTO SIMULADO";
        Position(watermark.rectTransform, new Vector2(0, 1), Vector2.one, new Vector2(24, -302), new Vector2(-24, -268));
        var caption = AddLabel(canvas.transform, "Demo Step", font, 26, Color.white);
        Position(caption.rectTransform, new Vector2(0, 1), Vector2.one, new Vector2(24, -365), new Vector2(-24, -312));
        var touch = new GameObject("Simulated Touch", typeof(RectTransform), typeof(Image)).GetComponent<Image>();
        touch.transform.SetParent(canvas.transform, false);
        touch.sprite = AssetDatabase.LoadAssetAtPath<Sprite>("Assets/Tarea08/UI/RoundedPanel.png");
        touch.type = Image.Type.Sliced;
        touch.color = new Color(0.25f, 0.92f, 0.76f, 0.65f);
        touch.raycastTarget = false;
        touch.gameObject.SetActive(false);

        var ids = new[] { new TrackableId(80, 1), new TrackableId(80, 2), new TrackableId(80, 3) };
        var anchors = Enumerable.Range(0, 3).Select(index => new GameObject("Demo Image Anchor " + index)).ToArray();
        var instances = new TrackedModel[3];
        var basePositions = new Vector3[3];
        var stages = new List<Action>();
        var notes = new List<StageReport>();
        int selectedIndex = -1;

        void SpawnAll()
        {
            for (int i = 0; i < 3; i++)
            {
                tracking.ApplyTracking(ids[i], bindings[i].referenceImageName, anchors[i].transform, TrackingState.Tracking);
                tracking.TryGetInstance(ids[i], out instances[i]);
                // Present the imported meshes upright for this editor-only demonstration.
                // The saved AR scene continues to use each original prefab orientation.
                instances[i].transform.localRotation = Quaternion.Euler(i == 2 ? -90 : 0, 180, 0);
                var bounds = GetBounds(instances[i]);
                float factor = 0.82f / Mathf.Max(bounds.size.y, bounds.size.x * 1.3f, bounds.size.z);
                instances[i].transform.localScale *= factor;
                bounds = GetBounds(instances[i]);
                anchors[i].transform.position += new Vector3((i - 1) * 0.58f, 0.72f, 0) - bounds.center;
                basePositions[i] = anchors[i].transform.position;
            }
        }
        void Select(int index)
        {
            Canvas.ForceUpdateCanvases();
            Physics.SyncTransforms();
            var position = camera.WorldToScreenPoint(instances[index].GetComponent<BoxCollider>().bounds.center);
            if (!raycast.SelectAtScreenPosition(position) || raycast.SelectedModel != instances[index])
                throw new InvalidOperationException("La selección real por pantalla falló durante la demo: " + bindings[index].referenceImageName);
            selectedIndex = index;
            touch.gameObject.SetActive(true);
            var viewport = camera.WorldToViewportPoint(instances[index].GetComponent<BoxCollider>().bounds.center);
            Position(touch.rectTransform, viewport, viewport, new Vector2(-20, -20), new Vector2(20, 20));
        }
        void Stage(string title, Action action)
        {
            stages.Add(() =>
            {
                selectedIndex = -1;
                touch.gameObject.SetActive(false);
                caption.text = title;
                action?.Invoke();
                notes.Add(new StageReport { stage = title, activeModels = tracking.ActiveModelCount,
                    selectedModel = raycast.SelectedModel != null ? raycast.SelectedModel.Information.ModelName : "Ninguno",
                    panelTitle = panel.DisplayedName });
            });
        }
        Stage("01  ·  Ficha original: ningún modelo seleccionado", null);
        Stage("02  ·  Tres imágenes detectadas, tres modelos", SpawnAll);
        Stage("03  ·  Tocar Greninja muestra su ficha", () => Select(0));
        Stage("04  ·  Tocar la paleta cambia los cinco datos", () => Select(1));
        Stage("05  ·  Tocar Miku muestra su información", () => Select(2));
        Stage("06  ·  Se pierde otra imagen; Miku sigue seleccionada", () =>
            tracking.ApplyTracking(ids[0], bindings[0].referenceImageName, anchors[0].transform, TrackingState.Limited));
        Stage("07  ·  Se pierde Miku: modelo oculto y ficha original", () =>
        {
            tracking.ApplyTracking(ids[2], bindings[2].referenceImageName, anchors[2].transform, TrackingState.None);
            if (panel.CurrentInformation != null || instances[2].gameObject.activeSelf)
                throw new InvalidOperationException("La pérdida del seguimiento no restauró el panel.");
        });
        Stage("08  ·  Miku vuelve sin duplicar el modelo", () =>
            tracking.ApplyTracking(ids[2], bindings[2].referenceImageName, anchors[2].transform, TrackingState.Tracking));
        Stage("09  ·  La ficha se puede consultar de nuevo", () => Select(2));
        Stage("10  ·  Retirar la imagen también reinicia la ficha", () => tracking.RemoveTrackable(ids[2]));

        var image = new Texture2D(Width, Height, TextureFormat.RGB24, false);
        int frame = 0;
        try
        {
            for (int stage = 0; stage < stages.Count; stage++)
            {
                stages[stage]();
                for (int localFrame = 0; localFrame < FramesPerStage; localFrame++, frame++)
                {
                    float time = (float)frame / FramesPerSecond;
                    for (int i = 0; i < instances.Length; i++)
                        if (instances[i] != null && instances[i].gameObject.activeSelf)
                            anchors[i].transform.position = basePositions[i] + Vector3.up * (Mathf.Sin(time * 1.5f + i) * 0.012f);
                    if (selectedIndex >= 0)
                    {
                        float alpha = Mathf.Clamp01(1 - localFrame / 12f);
                        touch.color = new Color(0.25f, 0.92f, 0.76f, alpha * 0.65f);
                        float size = 36 + localFrame * 1.5f;
                        touch.rectTransform.sizeDelta = Vector2.one * size;
                    }
                    Canvas.ForceUpdateCanvases();
                    Physics.SyncTransforms();
                    if (GraphicsSettings.currentRenderPipeline != null)
                        RenderPipeline.SubmitRenderRequest(camera, new UniversalRenderPipeline.SingleCameraRequest { destination = target });
                    else camera.Render();
                    RenderTexture.active = target;
                    image.ReadPixels(new Rect(0, 0, Width, Height), 0, 0, false);
                    image.Apply(false);
                    string path = output + "/frame-" + frame.ToString("D4") + ".png";
                    File.WriteAllBytes(path, image.EncodeToPNG());
                }
                Debug.Log("TAREA08_DEMO_STAGE " + (stage + 1) + "/" + stages.Count);
            }
            File.WriteAllText(Tarea08Automation.Output + "/demo-editor.json", JsonUtility.ToJson(new DemoReport
            { simulatedTracking = true, framesPerSecond = FramesPerSecond, frameCount = frame, stages = notes.ToArray() }, true));
            Debug.Log("TAREA08_DEMO_CAPTURED: " + frame + " cuadros reales de Unity; seguimiento simulado, raycast real.");
        }
        finally
        {
            RenderTexture.active = null;
            camera.targetTexture = null;
            Object.DestroyImmediate(image);
            target.Release();
            Object.DestroyImmediate(target);
            tracking.Configure(null, bindings);
            foreach (var anchor in anchors) if (anchor != null) Object.DestroyImmediate(anchor);
            EditorSceneManager.OpenScene(Tarea08Automation.ScenePath, OpenSceneMode.Single);
        }
    }

    private static Bounds GetBounds(TrackedModel model)
    {
        var renderers = model.GetComponentsInChildren<Renderer>(true);
        if (renderers.Length == 0) throw new InvalidOperationException("El modelo no tiene geometría visible.");
        var bounds = renderers[0].bounds;
        foreach (var renderer in renderers.Skip(1)) bounds.Encapsulate(renderer.bounds);
        return bounds;
    }

    private static UnityEngine.UI.Text AddLabel(Transform parent, string name, Font font, int size, Color color)
    {
        var text = new GameObject(name, typeof(RectTransform), typeof(UnityEngine.UI.Text)).GetComponent<UnityEngine.UI.Text>();
        text.transform.SetParent(parent, false);
        text.font = font;
        text.fontSize = size;
        text.fontStyle = FontStyle.Bold;
        text.color = color;
        text.alignment = TextAnchor.MiddleCenter;
        text.raycastTarget = false;
        text.supportRichText = false;
        return text;
    }

    private static void Position(RectTransform rect, Vector2 min, Vector2 max, Vector2 offsetMin, Vector2 offsetMax)
    {
        rect.anchorMin = min;
        rect.anchorMax = max;
        rect.offsetMin = offsetMin;
        rect.offsetMax = offsetMax;
    }

    [Serializable] private sealed class DemoReport
    {
        public bool simulatedTracking;
        public int framesPerSecond;
        public int frameCount;
        public StageReport[] stages;
    }
    [Serializable] private sealed class StageReport
    {
        public string stage;
        public int activeModels;
        public string selectedModel;
        public string panelTitle;
    }
}
