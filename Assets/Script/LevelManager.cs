using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using UnityEngine.SceneManagement;
using TMPro; // For TextMeshPro
using UnityEngine.UI; // <-- REQUIRED for Button UI

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

    // --- NEW: Pause Menu UI ---
    [Header("Pause Menu")]
    [SerializeField] private GameObject pauseMenuPanel; // The panel holding Resume, Restart, Exit

    // --- Private Game State ---
    private int _score = 0;
    private int _bombsCount = 0;
    private int _candiesRevealed = 0;
    private int _totalCandies;
    private float _currentTime;
    private bool _isTimerRunning = false;
    private bool _isGamePaused = false; // NEW: Tracks pause state

    public bool IsTimerRunning()
    {
        return _isTimerRunning;
    }

    // --- Unity Methods ---

    void Start()
    {
        // Ensure the template is active
        if(originalBox != null)
        {
            originalBox.gameObject.SetActive(true);
        }
        
        // --- NEW: Hide pause menu on start ---
        _isGamePaused = false;
        pauseMenuPanel.SetActive(false);
        Time.timeScale = 1f; // Ensure time is running
        
        LoadLevel(currentGridSize);
    }
    
    void Update()
    {
        // Only run the timer if the game is NOT paused
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

    // --- Public Game Flow ---

    public void BoxClicked(BoxCell box)
    {
        // --- MODIFIED: Don't allow clicks if paused ---
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

    //
    // --- NEW: PAUSE MENU FUNCTIONS ---
    //

    // This function is for the main "Pause" button in the corner
    public void TogglePauseMenu()
    {
        _isGamePaused = !_isGamePaused; // Flip the pause state

        if (_isGamePaused)
        {
            // Pause the game
            pauseMenuPanel.SetActive(true);
            Time.timeScale = 0f; // This freezes all game time (and physics)
        }
        else
        {
            // Resume the game
            pauseMenuPanel.SetActive(false);
            Time.timeScale = 1f; // This resumes game time
        }
    }

    // This function is for the "Resume" button INSIDE the panel
    public void OnResumeButton()
    {
        _isGamePaused = false;
        pauseMenuPanel.SetActive(false);
        Time.timeScale = 1f;
    }

    // This function is for the "Restart" button INSIDE the panel
    public void OnRestartButton()
    {
        // Unpause time before reloading the scene
        Time.timeScale = 1f; 
        SceneManager.LoadScene(SceneManager.GetActiveScene().buildIndex);
    }

    // This function is for the "Exit" button INSIDE the panel
    public void OnExitButton()
    {
        Debug.Log("Quitting game...");
        Application.Quit(); // Note: This only works in a built game, not in the editor
    }

    //
    // --- (Rest of the script is the same) ---
    //

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