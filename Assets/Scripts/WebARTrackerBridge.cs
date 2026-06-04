using UnityEngine;
using System.Collections.Generic;
using Imagine.WebAR;

[RequireComponent(typeof(ImageTracker))]
public class WebARTrackerBridge : MonoBehaviour
{
    [System.Serializable]
    public struct TargetMapping
    {
        [Tooltip("The string ID exactly matching the ID entered inside the ImageTracker list configuration.")]
        public string imageId;
        [Tooltip("The controller handling the audio/animation for this specific target id.")]
        public WebARTrackedMediaController mediaController;
    }

    [Header("Target Mapping Profiles")]
    [SerializeField] private List<TargetMapping> targetsRegistry = new List<TargetMapping>();

    private Dictionary<string, WebARTrackedMediaController> registryCache = new Dictionary<string, WebARTrackedMediaController>();
    private ImageTracker imageTracker;

    private void Awake()
    {
        imageTracker = GetComponent<ImageTracker>();

        // Cache the inspector registry list into a high-performance dictionary look-up table
        foreach (var mapping in targetsRegistry)
        {
            if (mapping.mediaController == null || string.IsNullOrEmpty(mapping.imageId)) continue;

            if (!registryCache.ContainsKey(mapping.imageId))
            {
                registryCache.Add(mapping.imageId, mapping.mediaController);
            }
        }
    }

    private void OnEnable()
    {
        if (imageTracker != null)
        {
            // Direct subscription to the SDK's UnityEvent<string> definitions
            imageTracker.OnImageFound.AddListener(OnSDKImageFound);
            imageTracker.OnImageLost.AddListener(OnSDKImageLost);
        }
    }

    private void OnDisable()
    {
        if (imageTracker != null)
        {
            imageTracker.OnImageFound.RemoveListener(OnSDKImageFound);
            imageTracker.OnImageLost.RemoveListener(OnSDKImageLost);
        }
    }

    private void OnSDKImageFound(string id)
    {
        if (registryCache.TryGetValue(id, out WebARTrackedMediaController controller))
        {
            controller.PlayMedia();
        }
    }

    private void OnSDKImageLost(string id)
    {
        if (registryCache.TryGetValue(id, out WebARTrackedMediaController controller))
        {
            controller.PauseOrStopMedia();
        }
    }
}