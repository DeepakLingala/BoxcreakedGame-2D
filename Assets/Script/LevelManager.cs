using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using UnityEngine.SceneManagement;
using TMPro;
using UnityEngine.UI;

public class LevelManager : MonoBehaviour
{
    [Header("Level Configuration")]
    [SerializeField] private BoxCell originalBox;
    [SerializeField] private float offsetX = 1.5f;
    [SerializeField] private float offsetY = 1.5f;
    [SerializeField] private int currentGridSize = 2;
    [SerializeField] private int totalLevels = 5;

    [Header("Timer Configuration")]
    [SerializeField] private float timeLimitPerLevel = 10.0f;
    [SerializeField] private TextMeshProUGUI timerLabel; 

    [Header("UI (Canvas)")]
    [SerializeField] private TextMeshProUGUI scoreLabel; 
    [SerializeField] private TextMeshProUGUI levelLabel; 

    [Header("Pause Menu")]
    [SerializeField] private GameObject pauseMenuPanel; 

    // --- NEW: Scene Name for Main Menu ---
    [Header("Scene Management")]
    [SerializeField] private string mainMenuSceneName = "MainMenu"; // Make sure this matches your Main Menu scene name!


    // --- Private Game State ---
    private int _score = 0;
    private int _bombsCount = 0;
    private int _candiesRevealed = 0;
    private int _totalCandies;
    private float _currentTime;
    private bool _isTimerRunning = false;
    private bool _isGamePaused = false; 

    public bool IsTimerRunning()
    {
        return _isTimerRunning;
    }

    void Start()
    {
        if(originalBox != null)
        {
            originalBox.gameObject.SetActive(true);
        }
        
        _isGamePaused = false;
        if (pauseMenuPanel != null) // Safety check
        {
            pauseMenuPanel.SetActive(false);
        }
        Time.timeScale = 1f; 
        AudioListener.pause = false; // Ensure sound is not paused initially

        LoadLevel(currentGridSize);
    }
    
    void Update()
    {
        if (_isTimerRunning && !_isGamePaused)
        {
            _currentTime -= Time.deltaTime;
            timerLabel.text = $"Time: {_currentTime:F1}";

            if (_currentTime <= 0)
            {
                _currentTime = 0;
                _isTimerRunning = false;
                timerLabel.text = "Time: 0.0";
                StartCoroutine(GameOver(true));
            }
        }
    }

    public void BoxClicked(BoxCell box)
    {
        if (!_isTimerRunning || _isGamePaused) return;

        if (box.contentType == BoxCell.ContentType.Bomb)
        {
            StartCoroutine(GameOver(false));
        }
        else
        {
            _candiesRevealed++;
            _score++;
            scoreLabel.text = "Score: " + _score;

            if (_candiesRevealed >= _totalCandies)
            {
                _isTimerRunning = false;
                StartCoroutine(NextLevel());
            }
        }
    }

    // --- PAUSE MENU FUNCTIONS ---

    public void TogglePauseMenu()
    {
        _isGamePaused = !_isGamePaused;

        if (pauseMenuPanel == null) // Safety check
        {
            Debug.LogError("Pause Menu Panel is not assigned in LevelManager!");
            return;
        }

        if (_isGamePaused)
        {
            pauseMenuPanel.SetActive(true);
            Time.timeScale = 0f; // Pause game time
            AudioListener.pause = true; // Pause all audio
        }
        else
        {
            pauseMenuPanel.SetActive(false);
            Time.timeScale = 1f; // Resume game time
            AudioListener.pause = false; // Resume all audio
        }
    }

    public void OnResumeButton()
    {
        _isGamePaused = false;
        if (pauseMenuPanel != null) pauseMenuPanel.SetActive(false);
        Time.timeScale = 1f;
        AudioListener.pause = false; // Ensure audio is resumed
    }

    public void OnRestartButton()
    {
        Time.timeScale = 1f; 
        AudioListener.pause = false; // Ensure audio is resumed
        SceneManager.LoadScene(SceneManager.GetActiveScene().buildIndex);
    }

    // --- MODIFIED: Exit button now goes to Main Menu ---
    public void OnMainMenuButton() 
    {
        Time.timeScale = 1f; // Always unpause time before changing scenes
        AudioListener.pause = false; // Always unpause audio
        SceneManager.LoadScene(mainMenuSceneName);
    }

    // --- Core Level Logic (unchanged) ---

    private void LoadLevel(int size)
    {
        CleanupLevel();
        currentGridSize = size;
        int totalCells = size * size;
        _bombsCount = size - 1; 
        _totalCandies = totalCells - _bombsCount;
        _candiesRevealed = 0;

        levelLabel.text = "Level " + (size - 1);
        scoreLabel.text = "Score: " + _score;

        _currentTime = timeLimitPerLevel;
        _isTimerRunning = true;
        timerLabel.text = $"Time: {_currentTime:F1}";

        List<BoxCell.ContentType> contentList = new List<BoxCell.ContentType>();
        for (int i = 0; i < _totalCandies; i++) contentList.Add(BoxCell.ContentType.Candy);
        for (int i = 0; i < _bombsCount; i++) contentList.Add(BoxCell.ContentType.Bomb);
        
        ShuffleContentList(contentList);

        Vector3 startPos = originalBox.transform.position;
        float centerOffset = (size - 1) * offsetX / 2f; 

        for (int row = 0; row < size; row++)
        {
            for (int col = 0; col < size; col++)
            {
                BoxCell box;
                int index = row * size + col;
                
                if (index == 0)
                {
                    box = originalBox;
                    box.ResetBox();
                }
                else
                {
                    box = Instantiate(originalBox);
                }

                float posX = startPos.x + (col * offsetX) - centerOffset;
                float posY = startPos.y - (row * offsetY) + centerOffset;
                box.transform.position = new Vector3(posX, posY, startPos.z);
                
                box.SetContent(contentList[index]);
                box.gameObject.SetActive(true);
            }
        }
    }

    private void ShuffleContentList(List<BoxCell.ContentType> list)
    {
        for (int i = list.Count - 1; i > 0; i--)
        {
            int r = Random.Range(0, i + 1);
            BoxCell.ContentType temp = list[i];
            list[i] = list[r];
            list[r] = temp;
        }
    }

    private void CleanupLevel()
    {
        BoxCell[] oldBoxes = FindObjectsOfType<BoxCell>();
        foreach (BoxCell box in oldBoxes)
        {
            if (box != originalBox)
            {
                Destroy(box.gameObject);
            }
        }
    }

    private IEnumerator NextLevel()
    {
        yield return new WaitForSeconds(1.5f);
        if (currentGridSize + 1 <= totalLevels + 1)
        {
            LoadLevel(currentGridSize + 1);
        }
        else
        {
            levelLabel.text = "YOU WIN!";
        }
    }

    private IEnumerator GameOver(bool timeOut)
    {
        _isTimerRunning = false; 

        if (timeOut)
        {
            levelLabel.text = "TIME OVER!"; 
        }
        else
        {
            levelLabel.text = "GAME OVER!"; 
        }
        
        yield return new WaitForSeconds(3.0f);
        SceneManager.LoadScene(SceneManager.GetActiveScene().buildIndex);
    }
}