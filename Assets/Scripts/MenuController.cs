using UnityEngine;
using UnityEngine.SceneManagement;

public class MenuController : MonoBehaviour
{
    [SerializeField] private string sceneToLoad = "Main";
    [SerializeField] private string menuSceneName = "Menu";

    public void PlayGame()
    {
        SceneManager.LoadScene(sceneToLoad);
    }

    public void LoadMenu()
    {
        SceneManager.LoadScene(menuSceneName);
    }

    public void QuitGame()
    {
        Application.Quit();
    }
}
