using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.Serialization;

public class OnContactSceneTransition : MonoBehaviour
{
    [Header("Scene Settings")]
    [SerializeField] string sceneToLoad = "NextScene";
    [SerializeField] private float transitionDelay = 0.5f;

    [Header("Contact Settings")]
    [Tooltip("Objects that trigger the transition on contact (e.g. the player's head/camera or hands). " +
             "If one has a Collider, its bounds are tested for overlap; otherwise its position is tested. " +
             "If the list is empty, the main camera is used.")]
    [SerializeField] private List<Transform> contactTargets = new List<Transform>();
    [Tooltip("Extra distance added around this object's collider bounds.")]
    [SerializeField] private float contactPadding = 0f;

    [Header("Optional Transition Effects")]
    [Tooltip("Grow the black hole and fade its glow to white over Fade Time before the scene switches.")]
    [FormerlySerializedAs("fadeTransition")]
    [SerializeField] private bool fadeEffects = true;
    [SerializeField] private float fadeTime = 1f;
    [Tooltip("Renderer using the Black Hole material. If empty, the first Renderer on this object or its children is used.")]
    [SerializeField] private Renderer blackHoleRenderer;
    [Tooltip("The black hole's scale is multiplied by this over Fade Time.")]
    [SerializeField] private float growScaleMultiplier = 3f;

    private static readonly int OuterGlowTintId = Shader.PropertyToID("_OuterGlowTint");
    private static readonly int SpherePercentageId = Shader.PropertyToID("_SpherePercentage");
    private const float MaxSpherePercentage = 0.01f;

    private Material blackHoleMaterial; // per-instance copy, created when the effect starts
    private Collider ownCollider;
    private readonly List<Collider> targetColliders = new List<Collider>();
    private AsyncOperation preloadOperation;
    private bool hasTriggered = false;

    void Start()
    {
        ownCollider = GetComponent<Collider>();
        if (ownCollider == null)
        {
            Debug.LogError("OnContactSceneTransition: needs a Collider on this object to define the contact area.");
            enabled = false;
            return;
        }

        contactTargets.RemoveAll(t => t == null);
        if (contactTargets.Count == 0 && Camera.main != null)
        {
            contactTargets.Add(Camera.main.transform);
        }
        if (contactTargets.Count == 0)
        {
            Debug.LogError("OnContactSceneTransition: no contactTargets assigned and no main camera found.");
            enabled = false;
            return;
        }

        // Cache each target's collider (null entry = use position instead)
        foreach (var t in contactTargets)
        {
            targetColliders.Add(t.GetComponent<Collider>());
        }

        PreloadScene();
    }

    // Load the next scene in the background, but hold it at 90% until contact
    private void PreloadScene()
    {
        if (string.IsNullOrEmpty(sceneToLoad)) return;

        preloadOperation = SceneManager.LoadSceneAsync(sceneToLoad);
        if (preloadOperation != null)
        {
            preloadOperation.allowSceneActivation = false;
        }
    }

    private void ActivateScene()
    {
        if (preloadOperation != null)
        {
            preloadOperation.allowSceneActivation = true;
        }
        else
        {
            // Preload failed (e.g. scene not in Build Settings); fall back to a normal load
            SceneManager.LoadScene(sceneToLoad);
        }
    }

    // No Rigidbody needed: compare bounds directly each frame
    void Update()
    {
        if (hasTriggered) return;

        Bounds area = ownCollider.bounds;
        area.Expand(contactPadding * 2f);

        for (int i = 0; i < contactTargets.Count; i++)
        {
            var target = contactTargets[i];
            if (target == null) continue;

            var col = targetColliders[i];
            bool touching = col != null
                ? area.Intersects(col.bounds)
                : area.Contains(target.position);

            if (touching)
            {
                HandleContact(target);
                return;
            }
        }
    }

    private void HandleContact(Transform target)
    {
        if (string.IsNullOrEmpty(sceneToLoad))
        {
            Debug.LogError("Scene name is empty! Please set the scene name in the inspector.");
            enabled = false;
            return;
        }

        hasTriggered = true;
        Debug.Log("Contact with " + target.name + "! Transitioning to scene: " + sceneToLoad);

        if (transitionDelay > 0)
        {
            Invoke(nameof(LoadScene), transitionDelay);
        }
        else
        {
            LoadScene();
        }
    }

    private void LoadScene()
    {
        // Disable GrabAndLocate components to prevent null reference errors
        var grabAndLocates = FindObjectsByType<Meta.XR.MRUtilityKit.BuildingBlocks.GrabAndLocate>();
        foreach (var component in grabAndLocates)
        {
            if (component != null)
            {
                component.enabled = false;
            }
        }

        Debug.Log("LoadScene: fadeEffects=" + fadeEffects + ", fadeTime=" + fadeTime);
        if (fadeEffects)
        {
            StartCoroutine(FadeAndLoadScene());
        }
        else
        {
            ActivateScene();
        }
    }

    private System.Collections.IEnumerator FadeAndLoadScene()
    {
        if (blackHoleRenderer == null)
        {
            blackHoleRenderer = GetComponentInChildren<Renderer>();
        }

        // Renderer.material returns an instance owned by this renderer only,
        // so the shared Black Hole material asset and other black holes are untouched
        if (blackHoleRenderer != null)
        {
            blackHoleMaterial = blackHoleRenderer.material;
        }
        else
        {
            Debug.LogWarning("OnContactSceneTransition: no Renderer found; only scaling will be animated.");
        }

        bool hasGlow = blackHoleMaterial != null && blackHoleMaterial.HasProperty(OuterGlowTintId);
        bool hasSphere = blackHoleMaterial != null && blackHoleMaterial.HasProperty(SpherePercentageId);

        Vector3 startScale = transform.localScale;
        Vector3 endScale = startScale * growScaleMultiplier;
        Color startGlow = hasGlow ? blackHoleMaterial.GetColor(OuterGlowTintId) : Color.white;
        float startSphere = hasSphere ? blackHoleMaterial.GetFloat(SpherePercentageId) : MaxSpherePercentage;

        float elapsed = 0f;
        while (elapsed < fadeTime)
        {
            Debug.Log(elapsed);
            float t = elapsed / fadeTime;
            transform.localScale = Vector3.Lerp(startScale, endScale, t);
            if (hasGlow) blackHoleMaterial.SetColor(OuterGlowTintId, Color.Lerp(startGlow, Color.white, t));
            if (hasSphere) blackHoleMaterial.SetFloat(SpherePercentageId, Mathf.Lerp(startSphere, MaxSpherePercentage, t));

            elapsed += Time.deltaTime;
            yield return null;
        }

        // Land exactly on the final values
        transform.localScale = endScale;
        if (hasGlow) blackHoleMaterial.SetColor(OuterGlowTintId, Color.white);
        if (hasSphere) blackHoleMaterial.SetFloat(SpherePercentageId, MaxSpherePercentage);

        ActivateScene();
    }

    void OnDestroy()
    {
        // Free the per-instance material copy
        if (blackHoleMaterial != null)
        {
            Destroy(blackHoleMaterial);
        }
    }
}
