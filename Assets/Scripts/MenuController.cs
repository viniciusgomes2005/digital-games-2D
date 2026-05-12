using UnityEngine;
using UnityEngine.SceneManagement;

public class MenuController : MonoBehaviour
{
    [SerializeField] private string sceneToLoad = "Main";

    public void PlayGame()
    {
        SceneManager.LoadScene(sceneToLoad);
    }
}
