using UnityEngine;
using UnityEngine.SceneManagement;
using BlackHoleMotion;

public class OnConvergeSceneTransition : MonoBehaviour
{
    [Header("Scene Settings")]
    [SerializeField] private string sceneToLoad = "Black Hole PCA";
    [Tooltip("Seconds to wait after the black holes arrive before switching scenes.")]
    [SerializeField] private float transitionDelay = 0f;

    [Header("Trigger")]
    [Tooltip("Leave empty to find the BlackHoleConverge in the scene.")]
    [SerializeField] private BlackHoleConverge converge;

    private AsyncOperation preloadOperation;
    private bool hasTriggered = false;

    void Start()
    {
        if (converge == null)
        {
            converge = FindFirstObjectByType<BlackHoleConverge>();
        }
        if (converge == null)
        {
            Debug.LogError("OnConvergeSceneTransition: no BlackHoleConverge found in the scene.");
            enabled = false;
            return;
        }
        if (string.IsNullOrEmpty(sceneToLoad))
        {
            Debug.LogError("OnConvergeSceneTransition: Scene name is empty! Please set it in the inspector.");
            enabled = false;
            return;
        }

        // Load the next scene in the background, but hold it at 90% until the holes arrive
        preloadOperation = SceneManager.LoadSceneAsync(sceneToLoad);
        if (preloadOperation != null)
        {
            preloadOperation.allowSceneActivation = false;
        }
    }

    void Update()
    {
        if (hasTriggered || !converge.HasConverged) return;

        hasTriggered = true;
        Debug.Log("Black holes converged! Transitioning to scene: " + sceneToLoad);

        if (transitionDelay > 0)
        {
            Invoke(nameof(ActivateScene), transitionDelay);
        }
        else
        {
            ActivateScene();
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
}
