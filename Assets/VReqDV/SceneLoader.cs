using UnityEngine;
using UnityEngine.SceneManagement;

public class SceneLoader : MonoBehaviour
{
    // Function to load a scene by name
    public void LoadSceneByName(string sceneName)
    {
        SceneManager.LoadScene(sceneName);
    }

    // Optional: function to load by index if you prefer that
    public void LoadSceneByIndex(int index)
    {
        SceneManager.LoadScene(index);
    }
}
