using UnityEngine;
using System.Collections;

public class WebARTrackedMediaController : MonoBehaviour
{
    [Header("Media Components")]
    [Tooltip("The Animator component attached to your moving target model.")]
    [SerializeField] private Animator targetAnimator;
    
    [Tooltip("The persistent AudioSource component (Keep this GameObject always Active in the hierarchy).")]
    [SerializeField] private AudioSource targetAudioSource;

    [Header("Playback Settings")]
    [Tooltip("If true, the audio/animation will loop continuously WHILE tracked. It will still restart from the beginning on a fresh spawn.")]
    [SerializeField] private bool loopMedia = true;
    
    [Tooltip("The name of the animation state to play. Leave empty if using default state.")]
    [SerializeField] private string animationStateName = "";

    private Coroutine audioCoroutine;

    private void Awake()
    {
        ConfigureMediaSettings();
    }

    private void ConfigureMediaSettings()
    {
        if (targetAudioSource != null)
        {
            targetAudioSource.loop = loopMedia;
            targetAudioSource.playOnAwake = false;
            
            // WebGL Subsystem Multi-platform Engine Optimizations
            targetAudioSource.spatialBlend = 0f; // Forces 2D bypass so distance/position won't cut the volume
            targetAudioSource.mute = false;
            targetAudioSource.volume = 1f;
        }
    }

    public void PlayMedia()
    {
        // 1. Reset and play animation from frame 0
        if (targetAnimator != null)
        {
            targetAnimator.enabled = true;
            if (!string.IsNullOrEmpty(animationStateName))
                targetAnimator.Play(animationStateName, 0, 0f);
            else
                targetAnimator.Play(0, -1, 0f); // Default state, layer 0, normalized time 0f
        }

        // 2. Play global audio cleanly from start via WebGL frame allocation
        if (targetAudioSource != null && targetAudioSource.clip != null)
        {
            if (audioCoroutine != null) StopCoroutine(audioCoroutine);
            audioCoroutine = StartCoroutine(PlayAudioFromStartWebGL());
        }
    }

    private IEnumerator PlayAudioFromStartWebGL()
    {
        // Give the WebGL tracking calculation 1 frame to stabilize before generating an audio hardware voice
        yield return null;

        if (targetAudioSource != null)
        {
            // Crucial: Hard stop any lingering playback and reset the tracking playback head position to 0
            targetAudioSource.Stop(); 
            targetAudioSource.time = 0f; 
            targetAudioSource.Play();
        }
    }

    public void PauseOrStopMedia()
    {
        if (audioCoroutine != null) StopCoroutine(audioCoroutine);

        // Crucial: Hard stop the audio on tracking loss instead of pausing it. 
        // This resets its internal pointer buffer so it can't resume from where it cut off.
        if (targetAudioSource != null)
        {
            targetAudioSource.Stop();
        }

        // Disable animator component to freeze execution threads safely
        if (targetAnimator != null)
        {
            targetAnimator.enabled = false;
        }
    }
}