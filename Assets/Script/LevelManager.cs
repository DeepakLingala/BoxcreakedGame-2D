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

    [Header("Main Game UI")]
    [SerializeField] private TextMeshProUGUI scoreLabel; 
    [SerializeField] private TextMeshProUGUI levelLabel; 

    [Header("Pause Menu")]
    [SerializeField] private GameObject pauseMenuPanel; 

    [Header("Game Over UI")]
    [SerializeField] private GameObject gameOverPanel;
    [SerializeField] private Image gameOverGraphic;
    [SerializeField] private Sprite gameOverSprite;
    [SerializeField] private Sprite timeOverSprite;
    [SerializeField] private Button gameOverRestartButton; 
    [SerializeField] private Button gameOverMainMenuButton; 
    [SerializeField] private AudioClip gameOverSound; 

    [Header("Scene Management")]
    [SerializeField] private string mainMenuSceneName = "MainMenu"; 

    // --- Private Game State ---
    private int _score = 0;
    private int _bombsCount = 0;
    private int _candiesRevealed = 0;
    private int _totalCandies;
    private float _currentTime;
    private bool _isTimerRunning = false;
    private bool _isGamePaused = false; 
    private AudioSource levelAudioSource; 

    public bool IsTimerRunning()
    {
        return _isTimerRunning;
    }

    void Awake() 
    {
        levelAudioSource = GetComponent<AudioSource>();
        if (levelAudioSource == null)
        {
            levelAudioSource = gameObject.AddComponent<AudioSource>();
            levelAudioSource.playOnAwake = false;
            levelAudioSource.spatialBlend = 0;
        }
    }

    void Start()
    {
        if(originalBox != null)
        {
            originalBox.gameObject.SetActive(true);
        }
        
        _isGamePaused = false;
        if (pauseMenuPanel != null) pauseMenuPanel.SetActive(false);
        if (gameOverPanel != null) gameOverPanel.SetActive(false); 

        Time.timeScale = 1f; 
        AudioListener.pause = false; 

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

    public void TogglePauseMenu()
    {
        _isGamePaused = !_isGamePaused;

        if (pauseMenuPanel == null) 
        {
            Debug.LogError("Pause Menu Panel is not assigned in LevelManager!");
            return;
        }

        if (_isGamePaused)
        {
            pauseMenuPanel.SetActive(true);
            Time.timeScale = 0f; 
            AudioListener.pause = true; 
        }
        else
        {
            pauseMenuPanel.SetActive(false);
            Time.timeScale = 1f; 
            AudioListener.pause = false; 
        }
    }

    public void OnResumeButton()
    {
        _isGamePaused = false;
        if (pauseMenuPanel != null) pauseMenuPanel.SetActive(false);
        Time.timeScale = 1f;
        AudioListener.pause = false; 
    }

    public void OnRestartButton() 
    {
        Time.timeScale = 1f; 
        AudioListener.pause = false; 
        SceneManager.LoadScene(SceneManager.GetActiveScene().buildIndex);
    }

    public void OnMainMenuButton() 
    {
        Time.timeScale = 1f; 
        AudioListener.pause = false; 
        SceneManager.LoadScene(mainMenuSceneName);
    }
    
    public void OnGameOverRestartButton()
    {
        Time.timeScale = 1f;
        AudioListener.pause = false;
        SceneManager.LoadScene(SceneManager.GetActiveScene().buildIndex);
    }

    public void OnGameOverMainMenuButton()
    {
        Time.timeScale = 1f;
        AudioListener.pause = false;
        SceneManager.LoadScene(mainMenuSceneName);
    }

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
        Time.timeScale = 0f; 
        AudioListener.pause = true; 

        if (levelAudioSource != null && gameOverSound != null)
        {
            levelAudioSource.PlayOneShot(gameOverSound);
        }

        if (gameOverPanel != null && gameOverGraphic != null)
        {
            gameOverPanel.SetActive(true);
            gameOverGraphic.sprite = timeOut ? timeOverSprite : gameOverSprite;
            gameOverGraphic.gameObject.SetActive(true);
        }
        else
        {
            Debug.LogError("Game Over Panel, Graphic, or Sprites are not assigned in LevelManager!");
            Debug.Log(timeOut ? "TIME OVER!" : "GAME OVER!");
        }
        
        // --- FIX: Add yield break; to correctly end the coroutine ---
        yield break; 
    }
}