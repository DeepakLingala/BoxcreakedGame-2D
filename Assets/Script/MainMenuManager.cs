using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI; // Required for Button components
using TMPro; // Required for TextMeshProUGUI
using System.Collections; // Required for Coroutines if needed

public class MainMenuManager : MonoBehaviour
{
    [Header("Scene Names")]
    [SerializeField] private string gameSceneName = "GameScene"; // Name of your actual game level scene
    
    [Header("UI References")]
    [SerializeField] private Button soundToggleButton; // The button that toggles sound
    [SerializeField] private Sprite soundOnSprite;     // Sprite for sound ON
    [SerializeField] private Sprite soundOffSprite;    // Sprite for sound OFF
    
    // --- NEW: Audio Control ---
    private bool isSoundOn = true; // Initial state

    void Start()
    {
        // Ensure time is running and audio is unpaused when entering main menu
        Time.timeScale = 1f; 
        
        // --- NEW: Load sound state from PlayerPrefs or default to true ---
        isSoundOn = (PlayerPrefs.GetInt("IsSoundOn", 1) == 1); 
        UpdateSoundButtonVisual();
        AudioListener.pause = !isSoundOn; // Set initial audio state

        // You might want to play background music here
        // if (GetComponent<AudioSource>() != null && !GetComponent<AudioSource>().isPlaying)
        // {
        //     GetComponent<AudioSource>().Play();
        // }
    }

    // --- Main Menu Button Functions ---

    public void OnPlayButton()
    {
        Debug.Log("Starting game...");
        SceneManager.LoadScene(gameSceneName);
    }

    public void OnSoundToggleButton()
    {
        isSoundOn = !isSoundOn; // Toggle the state
        AudioListener.pause = !isSoundOn; // Pause/unpause all audio
        PlayerPrefs.SetInt("IsSoundOn", isSoundOn ? 1 : 0); // Save the state
        PlayerPrefs.Save(); // Save changes immediately

        UpdateSoundButtonVisual();
    }

    private void UpdateSoundButtonVisual()
    {
        if (soundToggleButton != null && soundOnSprite != null && soundOffSprite != null)
        {
            Image buttonImage = soundToggleButton.GetComponent<Image>();
            if (buttonImage != null)
            {
                buttonImage.sprite = isSoundOn ? soundOnSprite : soundOffSprite;
            }
        }
        else
        {
             Debug.LogWarning("Sound toggle button or its sprites are not assigned in MainMenuManager.");
        }
    }

    public void OnExitButton()
    {
        Debug.Log("Quitting application...");
        Application.Quit(); // Works in build, ignored in editor
    }
}