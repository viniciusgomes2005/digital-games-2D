using UnityEngine;
using UnityEngine.SceneManagement;

public static class MobileRuntimeInitializer
{
    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
    private static void InitializeOnFirstScene()
    {
        SceneManager.sceneLoaded += HandleSceneLoaded;
        SetupScene(SceneManager.GetActiveScene());
    }

    private static void HandleSceneLoaded(Scene scene, LoadSceneMode mode)
    {
        SetupScene(scene);
    }

    private static void SetupScene(Scene scene)
    {
        if (!scene.isLoaded)
        {
            return;
        }

        GameObject bootstrapObject = GameObject.Find("MobileRuntime");
        if (bootstrapObject == null)
        {
            bootstrapObject = new GameObject("MobileRuntime");
        }

        if (bootstrapObject.GetComponent<MobileInputController>() == null)
        {
            bootstrapObject.AddComponent<MobileInputController>();
        }

        MobileControlsBootstrap controlsBootstrap = bootstrapObject.GetComponent<MobileControlsBootstrap>();
        if (controlsBootstrap == null)
        {
            controlsBootstrap = bootstrapObject.AddComponent<MobileControlsBootstrap>();
        }

        bool isGameplayScene = scene.name == "Main";
        controlsBootstrap.enabled = isGameplayScene;

        if (!isGameplayScene)
        {
            return;
        }

        if (bootstrapObject.GetComponent<PauseMenuController>() == null)
        {
            bootstrapObject.AddComponent<PauseMenuController>();
        }

        if (bootstrapObject.GetComponent<GameStateController>() == null)
        {
            bootstrapObject.AddComponent<GameStateController>();
        }

        if (bootstrapObject.GetComponent<MobilePerformanceSettings>() == null)
        {
            bootstrapObject.AddComponent<MobilePerformanceSettings>();
        }
    }
}
