using UnityEngine;
using UnityEngine.SceneManagement;

public class MusicManager : MonoBehaviour
{
    public static MusicManager Instance;

    public AudioSource menuMusic;
    public AudioSource gameMusic;

    void Awake()
    {
        if (Instance != null)
        {
            Destroy(gameObject);
            return;
        }

        Instance = this;
        DontDestroyOnLoad(gameObject);

        SceneManager.sceneLoaded += OnSceneLoaded;
    }

    void Start()
    {
        // start music on launch
        PlayMenuMusic();
    }

    void OnSceneLoaded(Scene scene, LoadSceneMode mode)
    {
        // switch music
        if (scene.name.Contains("Menu"))
            PlayMenuMusic();
        else
            PlayGameMusic();
    }

    void PlayMenuMusic()
    {
        if (gameMusic.isPlaying)
            gameMusic.Stop();

        if (!menuMusic.isPlaying)
            menuMusic.Play();
    }

    void PlayGameMusic()
    {
        if (menuMusic.isPlaying)
            menuMusic.Stop();

        if (!gameMusic.isPlaying)
            gameMusic.Play();
    }
}
