using UnityEngine;
using UnityEngine.SceneManagement;

public class DeathScreenUI : MonoBehaviour
{
    public GameObject deathPanel;
    public MechHealth playerHealth;

    private bool shown = false;

    private void Awake()
    {
        deathPanel.SetActive(false);
    }

    private void OnEnable()
    {
         playerHealth.OnDeath += OnPlayerDeath;
    }

    private void OnDisable()
    {
         playerHealth.OnDeath -= OnPlayerDeath;
    }

    private void OnPlayerDeath()
    {
        if (shown) return;
        shown = true;

        if (deathPanel != null) deathPanel.SetActive(true);

        Time.timeScale = 0f;
    }

    public void GoToMenu()
    {
        Time.timeScale = 1f;
        SceneManager.LoadScene("Menu");
    }

    public void RestartScene()
    {
        Time.timeScale = 1f;
        SceneManager.LoadScene(SceneManager.GetActiveScene().name);
    }
}
