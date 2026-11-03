using UnityEngine;
using UnityEngine.XR.ARFoundation;
using UnityEngine.XR.ARSubsystems;

public class ImageTrackingManager : MonoBehaviour
{
    [SerializeField] ARTrackedImageManager imageManager;

    private void OnEnable()
    {
        imageManager.trackablesChanged.AddListener(OnChanged);
    }

    private void OnDisable()
    {
        imageManager.trackablesChanged.RemoveListener(OnChanged);
    }

    void OnChanged(ARTrackablesChangedEventArgs<ARTrackedImage> eventArgs)
    {
        foreach(var newImage in eventArgs.added)
        {
            Debug.Log("Imagen detectada: " + newImage.referenceImage.name);
        }

        foreach(var updateImage in eventArgs.updated)
        {
            switch (updateImage.trackingState)
            {
                case TrackingState.Tracking:
                    Debug.Log("Imagen rastreada correctamente");
                    break;
                case TrackingState.Limited:
                    Debug.Log("Seguimiento limitado");
                    break;
                case TrackingState.None:
                    Debug.Log("Imagen sin seguimiento");
                    break;
            }
        }
        foreach(var removeImage in eventArgs.removed)
        {
            TrackableId removedImageTrackableID = removeImage.Key;
            Debug.Log("Imagen eliminada del tracking");
        }
    }

    void SetLibrary(ARTrackedImageManager imageManager, XRReferenceImageLibrary imageLibrary)
    {
        imageManager.referenceLibrary = imageLibrary;
    }
}
