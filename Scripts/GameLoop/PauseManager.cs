using UnityEngine;
using UnityEngine.SceneManagement;
public class PauseManager : MonoBehaviour
{
    public GameObject pausePanel;

    public bool IsPaused {  get; private set; }

    void Start()
    {
        SetPaused(false);
    }

    void Update()
    {
        if (Input.GetKeyDown(KeyCode.Escape)) // pause if esc is pressed
        {
            SetPaused(!IsPaused);
        }
    }

    public void Resume()
    {
        SetPaused(false);
    }

    public void GoToMenu()
    {
        SetPaused(false);
        SceneManager.LoadScene("Menu");
    }

    private void SetPaused(bool paused)
    {
        IsPaused = paused;

        if (pausePanel != null) pausePanel.SetActive(paused);

        Time.timeScale = paused ? 0f : 1f; // set time scale
    }
}
