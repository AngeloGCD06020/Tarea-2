using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using LolSuperman.Tarea08;
using UnityEditor;
using UnityEditor.Build.Reporting;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem.UI;
using UnityEngine.SceneManagement;
using UnityEngine.UI;
using UnityEngine.XR.ARFoundation;
using UnityEngine.XR.ARSubsystems;
using Object = UnityEngine.Object;

public static class Tarea08Automation
{
    public const string ScenePath = "Assets/Scenes/Tarea 08.unity";
    public const string Output = "Builds/Tarea08";
    private const string AssetRoot = "Assets/Tarea08";
    private static readonly Color Ink = new Color(0.035f, 0.055f, 0.095f, 0.96f);
    private static readonly Color Accent = new Color(0.25f, 0.92f, 0.76f);
    private static readonly Color Muted = new Color(0.64f, 0.71f, 0.81f);

    [MenuItem("Tareas/Tarea 08/Preparar escena y verificar")]
    public static void SetupAndVerify()
    {
        Directory.CreateDirectory(Output);
        SetupScene();
        VerifyScene();
        Debug.Log("TAREA08_SETUP_AND_VERIFICATION_PASSED");
    }

    [MenuItem("Tareas/Tarea 08/Preparar escena")]
    public static void SetupScene()
    {
        Directory.CreateDirectory(AssetRoot + "/ModelInfo");
        Directory.CreateDirectory(AssetRoot + "/Icons");
        Directory.CreateDirectory(AssetRoot + "/UI");
        AssetDatabase.Refresh();

        var scene = EditorSceneManager.OpenScene("Assets/Scenes/ARImageTracking2.unity", OpenSceneMode.Single);
        var previous = Object.FindFirstObjectByType<MultipleImageTracking>();
        if (previous == null) throw new InvalidOperationException("No se encontró el seguimiento de la clase anterior.");
        var previousSettings = new SerializedObject(previous);
        var manager = (ARTrackedImageManager)previousSettings.FindProperty("imageManager").objectReferenceValue;
        var sourceBindings = previousSettings.FindProperty("imagePrefabs");
        var prefabs = new Dictionary<string, GameObject>();
        for (int i = 0; i < sourceBindings.arraySize; i++)
        {
            var binding = sourceBindings.GetArrayElementAtIndex(i);
            prefabs.Add(binding.FindPropertyRelative("imageName").stringValue,
                (GameObject)binding.FindPropertyRelative("imagePrefab").objectReferenceValue);
        }
        var librarySettings = new SerializedObject(manager);
        var library = (XRReferenceImageLibrary)librarySettings.FindProperty("m_SerializedLibrary").objectReferenceValue;
        if (library == null || library.count != 3) throw new InvalidOperationException("Se esperaba la biblioteca de tres imágenes de la clase anterior.");

        foreach (var canvas in Object.FindObjectsByType<Canvas>(FindObjectsInactive.Include, FindObjectsSortMode.None))
            if (canvas.gameObject.scene == scene) Object.DestroyImmediate(canvas.gameObject);
        foreach (var selector in Object.FindObjectsByType<TrackedModelSelector>(FindObjectsInactive.Include, FindObjectsSortMode.None))
            Object.DestroyImmediate(selector);
        var managers = previous.gameObject;
        Object.DestroyImmediate(previous);
        managers.name = "Tarea 08 - Managers";
        managers.transform.SetPositionAndRotation(Vector3.zero, Quaternion.identity);

        var bindings = new List<ImageModelTrackingManager.ModelBinding>();
        foreach (var reference in library)
        {
            if (!prefabs.TryGetValue(reference.name, out var prefab) || prefab == null)
                throw new InvalidOperationException("Falta el prefab para " + reference.name);
            var icon = CreateIcon(reference);
            string path = AssetRoot + "/ModelInfo/" + reference.name + ".asset";
            var information = AssetDatabase.LoadAssetAtPath<ModelInfo>(path);
            if (information == null)
            {
                information = ScriptableObject.CreateInstance<ModelInfo>();
                AssetDatabase.CreateAsset(information, path);
                FillInformation(information, reference.name, icon);
            }
            bindings.Add(new ImageModelTrackingManager.ModelBinding
            {
                referenceImageName = reference.name, prefab = prefab, information = information
            });
        }

        var tracking = managers.AddComponent<ImageModelTrackingManager>();
        tracking.Configure(manager, bindings);
        var camera = scene.GetRootGameObjects().SelectMany(root => root.GetComponentsInChildren<Camera>(true))
            .First(item => item.CompareTag("MainCamera"));
        camera.nearClipPlane = 0.01f;
        camera.farClipPlane = 100f;
        var panel = CreateUI(out var regions);
        var raycast = managers.AddComponent<LolSuperman.Tarea08.RaycastManager>();
        raycast.Configure(tracking, camera, panel, regions);

        var eventSystem = new GameObject("EventSystem", typeof(EventSystem), typeof(InputSystemUIInputModule));
        eventSystem.GetComponent<InputSystemUIInputModule>().AssignDefaultActions();

        PlayerSettings.defaultInterfaceOrientation = UIOrientation.Portrait;
        var buildScenes = new List<EditorBuildSettingsScene> { new EditorBuildSettingsScene(ScenePath, true) };
        buildScenes.AddRange(EditorBuildSettings.scenes.Where(item => item.path != ScenePath)
            .Select(item => new EditorBuildSettingsScene(item.path, false)));
        EditorBuildSettings.scenes = buildScenes.ToArray();
        AssetDatabase.SaveAssets();
        EditorSceneManager.MarkSceneDirty(scene);
        if (!EditorSceneManager.SaveScene(scene, ScenePath)) throw new IOException("No se pudo guardar Tarea 08.");
        AssetDatabase.Refresh();
        Selection.activeGameObject = panel.gameObject;
        Debug.Log("Tarea 08 guardada: biblioteca de tres imágenes, tres ModelInfo, canvas y raycast conectados.");
    }

    private static Sprite CreateIcon(XRReferenceImage reference)
    {
        string source = AssetDatabase.GUIDToAssetPath(reference.textureGuid.ToString("N"));
        if (string.IsNullOrEmpty(source)) throw new InvalidOperationException("No se encontró la imagen de " + reference.name);
        string destination = AssetRoot + "/Icons/" + reference.name + Path.GetExtension(source);
        if (!File.Exists(destination) && !AssetDatabase.CopyAsset(source, destination))
            throw new IOException("No se pudo copiar el icono de " + reference.name);
        var importer = (TextureImporter)AssetImporter.GetAtPath(destination);
        importer.textureType = TextureImporterType.Sprite;
        importer.spriteImportMode = SpriteImportMode.Single;
        importer.maxTextureSize = 512;
        importer.alphaIsTransparency = true;
        importer.wrapMode = TextureWrapMode.Clamp;
        importer.SaveAndReimport();
        return AssetDatabase.LoadAssetAtPath<Sprite>(destination);
    }

    private static void FillInformation(ModelInfo info, string imageName, Sprite icon)
    {
        switch (imageName)
        {
            case "Greninja":
                info.SetContent("Greninja", "Modelo de Greninja con sus materiales y pose originales.\nSu imagen de referencia activa este personaje en realidad aumentada.", "Personaje · Pokémon", 3, icon);
                break;
            case "Paletas":
                info.SetContent("Paleta Payaso", "Una Paleta Payaso acompañada de dulces.\nEste modelo de producto aparece sobre la fotografía de las paletas.", "Producto · Dulces", 1, icon);
                break;
            case "Miku":
                info.SetContent("Hatsune Miku", "Modelo de Hatsune Miku, personaje virtual de cabello turquesa.\nPuedes seleccionarlo mientras su imagen permanece en seguimiento.", "Personaje virtual", 5, icon);
                break;
            default: throw new InvalidOperationException("No hay ficha para " + imageName);
        }
        EditorUtility.SetDirty(info);
    }

    private static ModelInfoPanel CreateUI(out RectTransform[] blockingRegions)
    {
        var canvasObject = new GameObject("Tarea 08 - Canvas", typeof(RectTransform), typeof(Canvas), typeof(CanvasScaler), typeof(GraphicRaycaster));
        var canvas = canvasObject.GetComponent<Canvas>();
        canvas.renderMode = RenderMode.ScreenSpaceOverlay;
        canvas.sortingOrder = 20;
        var scaler = canvasObject.GetComponent<CanvasScaler>();
        scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
        scaler.referenceResolution = new Vector2(1080, 1920);
        scaler.matchWidthOrHeight = 0.5f;
        var safeArea = Rect(canvas.transform, "Safe Area", Vector2.zero, Vector2.one, Vector2.zero, Vector2.zero);
        safeArea.gameObject.AddComponent<SafeAreaPanel>();
        var shape = RoundedSprite();

        var header = Rect(safeArea, "Header", new Vector2(0, 1), Vector2.one, new Vector2(32, -254), new Vector2(-32, -32));
        Background(header, Ink, shape);
        Text(header, "Assignment", "LOL SUPERMAN  /  TAREA 08", 23, Accent, FontStyle.Bold,
            new Vector2(28, -55), new Vector2(-28, -20));
        Text(header, "Title", "MODEL INFO", 56, Color.white, FontStyle.Bold,
            new Vector2(28, -130), new Vector2(-28, -60));
        var status = Text(header, "Tracking Status", "", 24, Accent, FontStyle.Bold,
            new Vector2(28, -184), new Vector2(-28, -142));
        Text(header, "Hint", "RECONOCE  ·  TOCA  ·  EXPLORA", 19, Muted, FontStyle.Normal,
            new Vector2(28, -216), new Vector2(-28, -184));

        var card = Rect(safeArea, "Model Information Panel", Vector2.zero, new Vector2(1, 0), new Vector2(32, 76), new Vector2(-32, 636));
        Background(card, Ink, shape);
        var accentLine = Rect(card, "Accent", new Vector2(0, 1), Vector2.one, new Vector2(28, -9), new Vector2(-28, -4));
        Background(accentLine, Accent, shape);

        var iconFrame = Rect(card, "Icon Frame", new Vector2(0, 1), new Vector2(0, 1), new Vector2(28, -199), new Vector2(212, -31));
        Background(iconFrame, new Color(0.12f, 0.17f, 0.23f), shape);
        var iconRect = Rect(iconFrame, "Model Icon", Vector2.zero, Vector2.one, new Vector2(10, 10), new Vector2(-10, -10));
        var icon = iconRect.gameObject.AddComponent<Image>();
        icon.preserveAspect = true;
        icon.raycastTarget = false;
        var placeholder = Rect(iconFrame, "Empty Icon", Vector2.zero, Vector2.one, Vector2.zero, Vector2.zero);
        var placeholderText = placeholder.gameObject.AddComponent<UnityEngine.UI.Text>();
        Style(placeholderText, "?", 70, Accent, FontStyle.Bold);
        placeholderText.alignment = TextAnchor.MiddleCenter;

        Text(card, "Eyebrow", "FICHA DEL MODELO", 19, Accent, FontStyle.Bold,
            new Vector2(242, -60), new Vector2(-28, -25));
        var title = Text(card, "Model Name", "", 40, Color.white, FontStyle.Bold,
            new Vector2(242, -176), new Vector2(-28, -65));
        var description = Text(card, "Model Description", "", 29, new Color(0.86f, 0.9f, 0.96f), FontStyle.Normal,
            new Vector2(28, -365), new Vector2(-28, -220));
        description.alignment = TextAnchor.UpperLeft;
        var divider = Rect(card, "Divider", new Vector2(0, 1), Vector2.one, new Vector2(28, -391), new Vector2(-28, -389));
        Background(divider, new Color(0.26f, 0.32f, 0.4f), null);
        Text(card, "Category Label", "CATEGORÍA", 19, Muted, FontStyle.Bold,
            new Vector2(28, -437), new Vector2(-205, -404));
        var category = Text(card, "Model Category", "", 29, Color.white, FontStyle.Bold,
            new Vector2(28, -505), new Vector2(-205, -443));
        Text(card, "Level Label", "NIVEL", 19, Muted, FontStyle.Bold,
            new Vector2(-175, -437), new Vector2(-28, -404), true);
        var level = Text(card, "Model Level", "", 42, Accent, FontStyle.Bold,
            new Vector2(-175, -510), new Vector2(-28, -442), true);
        var instruction = Text(card, "Instruction", "", 20, Muted, FontStyle.Normal,
            new Vector2(28, -551), new Vector2(-28, -519));
        instruction.alignment = TextAnchor.MiddleCenter;

        var footer = Rect(safeArea, "Footer", Vector2.zero, new Vector2(1, 0), new Vector2(32, 18), new Vector2(-32, 66));
        var footerText = footer.gameObject.AddComponent<UnityEngine.UI.Text>();
        Style(footerText, "El modelo y su ficha se ocultan al perder el seguimiento", 20, Color.white, FontStyle.Normal);
        footerText.alignment = TextAnchor.MiddleCenter;
        var panel = card.gameObject.AddComponent<ModelInfoPanel>();
        panel.Configure(title, description, category, level, icon, placeholder.gameObject, status, instruction);
        blockingRegions = new[] { header, card };
        return panel;
    }

    private static RectTransform Rect(Transform parent, string name, Vector2 min, Vector2 max, Vector2 offsetMin, Vector2 offsetMax)
    {
        var rect = new GameObject(name, typeof(RectTransform)).GetComponent<RectTransform>();
        rect.SetParent(parent, false);
        rect.anchorMin = min;
        rect.anchorMax = max;
        rect.offsetMin = offsetMin;
        rect.offsetMax = offsetMax;
        return rect;
    }

    private static UnityEngine.UI.Text Text(Transform parent, string name, string value, int size, Color color,
        FontStyle style, Vector2 offsetMin, Vector2 offsetMax, bool rightAnchored = false)
    {
        var min = new Vector2(rightAnchored ? 1 : 0, 1);
        var rect = Rect(parent, name, min, Vector2.one, offsetMin, offsetMax);
        var text = rect.gameObject.AddComponent<UnityEngine.UI.Text>();
        Style(text, value, size, color, style);
        return text;
    }

    private static void Style(UnityEngine.UI.Text text, string value, int size, Color color, FontStyle style)
    {
        text.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
        text.fontSize = size;
        text.color = color;
        text.fontStyle = style;
        text.alignment = TextAnchor.MiddleLeft;
        text.horizontalOverflow = HorizontalWrapMode.Wrap;
        text.verticalOverflow = VerticalWrapMode.Truncate;
        text.supportRichText = false;
        text.raycastTarget = false;
        text.text = value;
    }

    private static void Background(RectTransform rect, Color color, Sprite sprite)
    {
        var image = rect.gameObject.AddComponent<Image>();
        image.color = color;
        image.sprite = sprite;
        image.type = sprite != null ? Image.Type.Sliced : Image.Type.Simple;
    }

    private static Sprite RoundedSprite()
    {
        string path = AssetRoot + "/UI/RoundedPanel.png";
        if (!File.Exists(path))
        {
            const int size = 64;
            const float radius = 14;
            var texture = new Texture2D(size, size, TextureFormat.RGBA32, false);
            for (int y = 0; y < size; y++)
                for (int x = 0; x < size; x++)
                {
                    float dx = Mathf.Max(radius - x - 0.5f, x + 0.5f - (size - radius));
                    float dy = Mathf.Max(radius - y - 0.5f, y + 0.5f - (size - radius));
                    float distance = new Vector2(Mathf.Max(dx, 0), Mathf.Max(dy, 0)).magnitude;
                    float alpha = Mathf.Clamp01(radius - distance + 0.5f);
                    texture.SetPixel(x, y, new Color(1, 1, 1, alpha));
                }
            texture.Apply();
            File.WriteAllBytes(path, texture.EncodeToPNG());
            Object.DestroyImmediate(texture);
            AssetDatabase.ImportAsset(path);
        }
        var importer = (TextureImporter)AssetImporter.GetAtPath(path);
        importer.textureType = TextureImporterType.Sprite;
        importer.spriteImportMode = SpriteImportMode.Single;
        importer.spriteBorder = new Vector4(18, 18, 18, 18);
        importer.alphaIsTransparency = true;
        importer.textureCompression = TextureImporterCompression.Uncompressed;
        importer.SaveAndReimport();
        return AssetDatabase.LoadAssetAtPath<Sprite>(path);
    }

    [MenuItem("Tareas/Tarea 08/Verificar comportamiento")]
    public static void VerifyScene()
    {
        Directory.CreateDirectory(Output);
        EditorSceneManager.OpenScene(ScenePath, OpenSceneMode.Single);
        var tracking = Object.FindFirstObjectByType<ImageModelTrackingManager>();
        var raycast = Object.FindFirstObjectByType<LolSuperman.Tarea08.RaycastManager>();
        var panel = Object.FindFirstObjectByType<ModelInfoPanel>();
        var checks = new List<string>();
        void Check(bool condition, string message)
        {
            if (!condition) throw new InvalidOperationException("Tarea 08: " + message);
            checks.Add(message);
        }
        bool IsOriginal() => panel.CurrentInformation == null && panel.DisplayedName == ModelInfoPanel.OriginalName &&
            panel.DisplayedDescription == ModelInfoPanel.OriginalDescription && panel.DisplayedCategory == ModelInfoPanel.OriginalCategory &&
            panel.DisplayedLevel == ModelInfoPanel.OriginalLevel && panel.DisplayedIcon == null;
        Ray RayTo(TrackedModel model)
        {
            Physics.SyncTransforms();
            var bounds = model.GetComponent<BoxCollider>().bounds;
            return new Ray(bounds.center - Vector3.forward * (bounds.extents.z + 2), Vector3.forward);
        }
        Check(tracking != null && raycast != null && panel != null, "Gestores y panel conectados en la escena guardada");
        var bindings = tracking.Bindings.ToArray();
        Check(bindings.Length == 3, "Un registro para cada una de las tres imágenes de referencia");
        Check(bindings.Select(item => item.information).Distinct().Count() == 3, "Tres ScriptableObjects independientes");
        Check(bindings.All(item => item.prefab != null && item.information != null && item.information.Icon != null &&
            !string.IsNullOrWhiteSpace(item.information.ModelName) && !string.IsNullOrWhiteSpace(item.information.Description) &&
            !string.IsNullOrWhiteSpace(item.information.Category) && item.information.Level >= 1), "Las cinco propiedades están asignadas en cada ficha");

        // Editor verification drives the same public entry point as the AR image event.
        var imageManager = tracking.ImageManager;
        imageManager.enabled = false;
        tracking.Configure(null, bindings);
        raycast.Configure(tracking, raycast.SelectionCamera, panel,
            new[] { panel.GetComponent<RectTransform>() });
        var anchors = new List<GameObject>();
        var ids = new[] { new TrackableId(8, 1), new TrackableId(8, 2), new TrackableId(8, 3) };
        var instances = new TrackedModel[3];
        try
        {
            Check(IsOriginal(), "El panel comienza con el contenido original y sin icono seleccionado");
            for (int i = 0; i < bindings.Length; i++)
            {
                var anchor = new GameObject("Verification Anchor " + i);
                anchor.transform.position = new Vector3(i * 100, 0, 0);
                anchors.Add(anchor);
                tracking.ApplyTracking(ids[i], bindings[i].referenceImageName, anchor.transform, TrackingState.Limited);
                Check(!tracking.TryGetInstance(ids[i], out _), "No se instancia " + bindings[i].referenceImageName + " con seguimiento limitado");
                tracking.ApplyTracking(ids[i], bindings[i].referenceImageName, anchor.transform, TrackingState.Tracking);
                tracking.TryGetInstance(ids[i], out instances[i]);
                Check(instances[i] != null && instances[i].gameObject.activeSelf, "Se muestra " + bindings[i].referenceImageName + " al rastrear su imagen");
                Check(instances[i].transform.parent == anchor.transform && instances[i].transform.localPosition == Vector3.zero,
                    "El modelo " + bindings[i].referenceImageName + " permanece anclado a su imagen");
                Check(instances[i].transform.localScale == bindings[i].prefab.transform.localScale &&
                    Quaternion.Angle(instances[i].transform.localRotation, bindings[i].prefab.transform.localRotation) < 0.01f,
                    "Se conserva la escala y orientación del prefab " + bindings[i].referenceImageName);
            }
            Check(tracking.ActiveModelCount == 3, "Pueden coexistir tres imágenes y tres modelos");
            for (int i = 0; i < bindings.Length; i++)
            {
                Check(raycast.SelectRay(RayTo(instances[i])) && raycast.SelectedModel == instances[i],
                    "Physics.Raycast selecciona el collider de " + bindings[i].referenceImageName);
                var info = bindings[i].information;
                Check(panel.DisplayedName == info.ModelName && panel.DisplayedDescription == info.Description &&
                    panel.DisplayedCategory == info.Category && panel.DisplayedLevel == info.Level.ToString() && panel.DisplayedIcon == info.Icon,
                    "El panel muestra los cinco datos del ScriptableObject de " + bindings[i].referenceImageName);
            }
            tracking.ApplyTracking(ids[0], bindings[0].referenceImageName, anchors[0].transform, TrackingState.Limited);
            Check(!instances[0].gameObject.activeSelf && raycast.SelectedModel == instances[2],
                "Perder otra imagen no borra la selección que sigue rastreada");
            tracking.ApplyTracking(ids[2], bindings[2].referenceImageName, anchors[2].transform, TrackingState.None);
            Check(!instances[2].gameObject.activeSelf && raycast.SelectedModel == null && IsOriginal(),
                "TrackingState.None oculta el seleccionado y restaura TODOS los campos e icono");
            tracking.ApplyTracking(ids[2], bindings[2].referenceImageName, anchors[2].transform, TrackingState.Tracking);
            tracking.TryGetInstance(ids[2], out var reacquired);
            Check(reacquired == instances[2] && reacquired.gameObject.activeSelf && IsOriginal(),
                "Al recuperar la imagen se reutiliza su modelo sin duplicarlo ni seleccionarlo automáticamente");
            raycast.SelectRay(RayTo(instances[1]));
            var panelPosition = RectTransformUtility.WorldToScreenPoint(null, panel.transform.position);
            raycast.SelectAtScreenPosition(panelPosition);
            Check(raycast.SelectedModel == instances[1], "Tocar el panel no cambia la selección del modelo");
            tracking.ApplyTracking(ids[1], bindings[1].referenceImageName, anchors[1].transform, TrackingState.Limited);
            Check(IsOriginal() && raycast.SelectedModel == null, "TrackingState.Limited también restaura el panel seleccionado");
            raycast.SelectRay(RayTo(instances[2]));
            tracking.RemoveTrackable(ids[2]);
            Check(!tracking.TryGetInstance(ids[2], out _) && IsOriginal(), "Eliminar un trackable destruye su modelo y reinicia la ficha");
            tracking.ApplyTracking(ids[2], bindings[2].referenceImageName, anchors[2].transform, TrackingState.Tracking);
            tracking.TryGetInstance(ids[2], out var replacement);
            Check(replacement != null && replacement != instances[2], "Una imagen retirada puede volver a detectarse correctamente");
            raycast.SelectRay(RayTo(replacement));
            Check(!raycast.SelectRay(new Ray(new Vector3(-99999, 0, 0), Vector3.up)) && IsOriginal(),
                "Tocar un espacio vacío restablece la ficha original");
            int beforeUnknown = tracking.ActiveModelCount;
            tracking.ApplyTracking(new TrackableId(8, 99), "No configurada", anchors[0].transform, TrackingState.Tracking);
            Check(tracking.ActiveModelCount == beforeUnknown, "Una imagen no configurada no genera modelos incorrectos");
            var report = new VerificationReport { passed = true, checkCount = checks.Count, checks = checks.ToArray() };
            File.WriteAllText(Output + "/verificacion.json", JsonUtility.ToJson(report, true));
            Debug.Log("TAREA08_VERIFIED: " + checks.Count + " comprobaciones correctas.");
        }
        finally
        {
            tracking.Configure(null, bindings);
            foreach (var anchor in anchors) if (anchor != null) Object.DestroyImmediate(anchor);
            EditorSceneManager.OpenScene(ScenePath, OpenSceneMode.Single);
        }
    }

    [Serializable]
    private sealed class VerificationReport
    {
        public bool passed;
        public int checkCount;
        public string[] checks;
    }

    [MenuItem("Tareas/Tarea 08/Generar APK Android")]
    public static void BuildAndroid()
    {
        VerifyScene();
        var options = new BuildPlayerOptions
        {
            scenes = new[] { ScenePath },
            locationPathName = Output + "/Tarea08-ModelInfo.apk",
            target = BuildTarget.Android,
            options = BuildOptions.None
        };
        EditorUserBuildSettings.buildAppBundle = false;
        var report = BuildPipeline.BuildPlayer(options);
        File.WriteAllText(Output + "/compilacion.txt", "Resultado: " + report.summary.result +
            "\nEscena: " + ScenePath + "\nTamaño: " + report.summary.totalSize + " bytes\nErrores: " +
            report.summary.totalErrors + "\nAdvertencias: " + report.summary.totalWarnings + "\n");
        if (report.summary.result != BuildResult.Succeeded)
            throw new InvalidOperationException("La compilación de Android no terminó correctamente.");
        Debug.Log("TAREA08_APK_BUILD_SUCCEEDED: " + options.locationPathName);
    }
}
