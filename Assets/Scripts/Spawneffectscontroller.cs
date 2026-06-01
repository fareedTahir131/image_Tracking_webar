using System.Collections;
using UnityEngine;

/// <summary>
/// ─────────────────────────────────────────────────────────────────────────────
///  SpawnEffectsController  —  Imagine WebAR (Unity Asset Store)
/// ─────────────────────────────────────────────────────────────────────────────
///  Plays a spawn animation and sound effect the first time (or every time)
///  the image target is detected by the Imagine WebAR Image Tracker.
///
///  HOW TO SET UP
///  ─────────────
///  1. Select your AR Content GameObject (the child object that sits under the
///     ImageTarget in the hierarchy — i.e. the model that appears in AR).
///
///  2. Attach THIS script to that same GameObject.
///
///  3. On your ImageTarget GameObject, find the "Image Tracker" component.
///     In its Inspector you will see:
///       • On Target Found  (UnityEvent)
///       • On Target Lost   (UnityEvent)
///
///  4. Click the (+) button under "On Target Found".
///       • Drag YOUR AR Content GameObject into the object slot.
///       • From the function dropdown choose:
///           SpawnEffectsController → OnTargetFound ()
///
///  5. Click the (+) button under "On Target Lost".
///       • Drag the same object.
///       • From the dropdown choose:
///           SpawnEffectsController → OnTargetLost ()
///
///  6. Fill in the Inspector fields on THIS component:
///       • Model Animator   — the Animator on your 3-D model
///       • Spawn SFX        — your AudioClip (e.g. a whoosh sound)
///       • Spawn Trigger    — name of the Trigger in your Animator Controller
///                            (default: "Spawn")
///       • Play On Every Detection — enable to re-trigger each time the target
///                                   is re-found after being lost
///
///  NOTE ON FBX MODELS
///  ──────────────────
///  Unity cannot use .fbx animation clips directly at runtime.
///  In your Project panel, expand your .fbx file, select each Animation Clip
///  inside it, then drag those clips into your Animator Controller states.
///  Make sure your Animator Controller has a Trigger parameter called "Spawn"
///  (or whatever name you set below) that transitions to the spawn animation.
/// </summary>
[RequireComponent(typeof(AudioSource))]
public class SpawnEffectsController : MonoBehaviour
{
    // ── Inspector ────────────────────────────────────────────────────────────

    [Header("Animation")]
    [Tooltip("Animator on the model. Auto-found in children if left blank.")]
    public Animator modelAnimator;

    [Tooltip("Trigger parameter name in your Animator Controller.")]
    public string spawnTrigger = "Spawn";

    [Header("Sound Effect")]
    [Tooltip("AudioClip to play when the model is first detected.")]
    public AudioClip spawnSFX;

    [Tooltip("Playback volume (0 = silent, 1 = full).")]
    [Range(0f, 1f)]
    public float sfxVolume = 1f;

    [Header("Behaviour")]
    [Tooltip("If true, effects replay every time the target is re-detected after being lost.")]
    public bool playOnEveryDetection = false;

    // ── Private state ────────────────────────────────────────────────────────

    private AudioSource _audio;
    private bool _hasPlayedOnce = false;

    // ── Unity lifecycle ──────────────────────────────────────────────────────

    private void Awake()
    {
        // Auto-resolve Animator if not set in Inspector
        if (modelAnimator == null)
            modelAnimator = GetComponentInChildren<Animator>();

        // Configure AudioSource
        _audio = GetComponent<AudioSource>();
        _audio.playOnAwake = false;
        _audio.loop = false;

        // Start hidden — Imagine WebAR will show/hide this object,
        // but if you manage visibility yourself, disable the line below.
        // gameObject.SetActive(false);
    }

    // ── Public callbacks (wire these to ImageTracker's UnityEvents) ──────────

    /// <summary>
    /// Called by the Imagine WebAR ImageTracker's "On Target Found" UnityEvent.
    /// </summary>
    public void OnTargetFound()
    {
        if (!playOnEveryDetection && _hasPlayedOnce)
            return;

        _hasPlayedOnce = true;
        PlaySpawnAnimation();
        PlaySpawnSFX();
    }

    /// <summary>
    /// Called by the Imagine WebAR ImageTracker's "On Target Lost" UnityEvent.
    /// Optional: stops audio/animation when tracking is lost.
    /// </summary>
    public void OnTargetLost()
    {
        // Stop SFX if still playing
        if (_audio.isPlaying)
            _audio.Stop();

        // Optionally reset the Animator so spawn plays fresh next time
        if (playOnEveryDetection && modelAnimator != null)
            modelAnimator.Rebind();
    }

    // ── Internal helpers ─────────────────────────────────────────────────────

    private void PlaySpawnAnimation()
    {
        if (modelAnimator == null)
        {
            Debug.LogWarning("[SpawnEffectsController] No Animator found. " +
                             "Assign one in the Inspector or attach an Animator to the model.");
            return;
        }

        if (HasTrigger(modelAnimator, spawnTrigger))
        {
            modelAnimator.SetTrigger(spawnTrigger);
        }
        else
        {
            Debug.LogWarning($"[SpawnEffectsController] Animator has no Trigger named '{spawnTrigger}'. " +
                             "Check your Animator Controller and update the Spawn Trigger field.");
        }
    }

    private void PlaySpawnSFX()
    {
        if (spawnSFX == null)
        {
            Debug.LogWarning("[SpawnEffectsController] No Spawn SFX clip assigned.");
            return;
        }

        _audio.volume = sfxVolume;
        _audio.PlayOneShot(spawnSFX);
    }

    /// <summary>Checks whether the Animator has a Trigger parameter with the given name.</summary>
    private static bool HasTrigger(Animator animator, string triggerName)
    {
        foreach (AnimatorControllerParameter p in animator.parameters)
            if (p.type == AnimatorControllerParameterType.Trigger && p.name == triggerName)
                return true;
        return false;
    }
}