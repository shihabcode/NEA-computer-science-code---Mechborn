using UnityEngine;
using UnityEngine.Audio;
using UnityEngine.UI;

public class OptionsMenu : MonoBehaviour
{
    [SerializeField] private GameObject optionsPanel;
    [SerializeField] private Slider volumeSlider;
    [SerializeField] private Toggle fullscreenToggle;

    [SerializeField] private AudioMixer mixer;
    [SerializeField] private string volumeParam = "MasterVolume";

    // buttons, toggles and sliders
    private void Start()
    {
        optionsPanel.SetActive(false);
    }

    public void OpenOptions()
    {
        optionsPanel.SetActive(true);
    }

    public void CloseOptions()
    {
        optionsPanel.SetActive(false);
    }

    public void SetVolume(float volumeDb)
    {
        mixer.SetFloat(volumeParam, volumeDb);
    }

    public void SetFullscreen(bool on)
    {
        Screen.fullScreen = on;
    }
}