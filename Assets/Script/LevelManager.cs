using UnityEngine;
using System.Collections;
using System.Collections.Generic; // Required for List

public class LevelManager : MonoBehaviour
{
    // Configuration for the level
    [Header("Level Configuration")]
    [SerializeField] private BoxCell originalBox;
    [SerializeField] private float offsetX = 1.5f; // Spacing between boxes
    [SerializeField] private float offsetY = 1.5f;
    [SerializeField] private int currentGridSize = 2; // Starts at 2x2
    [SerializeField] private int totalLevels = 5;

    [Header("Timer Configuration")]
    [SerializeField] private float timeLimitPerLevel = 10.0f; // Time in seconds
    [SerializeField] private TextMesh timerLabel; // Drag your Timer TextMesh here

    [Header("UI")]
    [SerializeField] private TextMesh scoreLabel; // Drag your Score TextMesh here
    [SerializeField] private TextMesh levelLabel; // Drag your Level TextMesh here

    // --- Private Game State ---
    private int _score = 0;
    private int _bombsCount = 0;
    private int _candiesRevealed = 0;
    private int _totalCandies;
    private float _currentTime;
    private bool _isTimerRunning = false;

    // Public getter for BoxCell to check game state
    public bool IsTimerRunning()
    {
        return _isTimerRunning;
    }

    // --- Unity Methods ---

    void Start()
    {
        // Position the UI elements based on the camera first
        PositionUI();
        // Then load the first level
        LoadLevel(currentGridSize);
    }
    
    void Update()
    {
        if (_isTimerRunning)
        {
            _currentTime -= Time.deltaTime;
            timerLabel.text = $"Time: {_currentTime:F1}"; // Display time with one decimal

            // Check for time out
            if (_currentTime <= 0)
            {
                _currentTime = 0;
                _isTimerRunning = false;
                timerLabel.text = "Time: 0.0";
                StartCoroutine(GameOver(true)); // Pass true to indicate a time-out loss
            }
        }
    }

    // --- Public Game Flow ---

    public void BoxClicked(BoxCell box)
    {
        // Ignore clicks if the timer has stopped (e.g., game over)
        if (!_isTimerRunning) return;

        if (box.contentType == BoxCell.ContentType.Bomb)
        {
            // Player loses by bomb!
            Debug.Log("Game Over! You hit a bomb!");
            StartCoroutine(GameOver(false)); // Pass false for bomb loss
        }
        else
        {
            // Player found a candy
            _candiesRevealed++;
            _score++;
            scoreLabel.text = "Score: " + _score;

            // Check for level win
            if (_candiesRevealed >= _totalCandies)
            {
                Debug.Log($"Level {currentGridSize - 1} Complete!");
                StartCoroutine(NextLevel());
            }
        }
    }

    // --- Core Level Logic ---

    private void LoadLevel(int size)
    {
        // 1. Clear existing level
        CleanupLevel();
        
        // 2. Set new grid size and counts
        currentGridSize = size;
        int totalCells = size * size;
        _bombsCount = size - 1; // Example: 2x2=1 bomb, 3x3=2 bombs
        
        _totalCandies = totalCells - _bombsCount;
        _candiesRevealed = 0;

        levelLabel.text = "Level " + (size - 1);
        scoreLabel.text = "Score: " + _score;

        // 3. Setup and start the timer
        _currentTime = timeLimitPerLevel;
        _isTimerRunning = true;
        timerLabel.text = $"Time: {_currentTime:F1}";

        // 4. Create a list of content types
        List<BoxCell.ContentType> contentList = new List<BoxCell.ContentType>();
        for (int i = 0; i < _totalCandies; i++) contentList.Add(BoxCell.ContentType.Candy);
        for (int i = 0; i < _bombsCount; i++) contentList.Add(BoxCell.ContentType.Bomb);
        
        // 5. Shuffle the list
        ShuffleContentList(contentList);

        // 6. Place boxes manually
        Vector3 startPos = originalBox.transform.position;
        // Calculate offset to center the grid
        float centerOffset = (size - 1) * offsetX / 2f; 

        for (int row = 0; row < size; row++)
        {
            for (int col = 0; col < size; col++)
            {
                BoxCell box;
                int index = row * size + col;
                
                // Use the original for the first box, instantiate for the rest
                if (index == 0)
                {
                    box = originalBox;
                    box.ResetBox(); // Reset the original prefab's state
                }
                else
                {
                    box = Instantiate(originalBox);
                }

                // Set its position using manual calculation
                float posX = startPos.x + (col * offsetX) - centerOffset;
                float posY = startPos.y - (row * offsetY) + centerOffset;
                box.transform.position = new Vector3(posX, posY, startPos.z);
                
                // Set the box's content (bomb or candy)
                box.SetContent(contentList[index]);
            }
        }
    }

    // --- UI and Helper Methods ---

    private void PositionUI()
    {
        Camera cam = Camera.main;
        if (cam == null)
        {
            Debug.LogError("No Main Camera found in the scene!");
            return;
        }

        // Get viewport corners in world coordinates
        Vector3 topLeft = cam.ViewportToWorldPoint(new Vector3(0, 1, cam.nearClipPlane));
        Vector3 topRight = cam.ViewportToWorldPoint(new Vector3(1, 1, cam.nearClipPlane));
        Vector3 topCenter = cam.ViewportToWorldPoint(new Vector3(0.5f, 1, cam.nearClipPlane));
        
        float padding = 0.5f; // Adjust this to add space from the edge
        float uiZ = -1f; // Ensure UI is in front of the grid (at Z=0)

        // SCORE (Top Left)
        scoreLabel.transform.position = new Vector3(topLeft.x + padding, topLeft.y - padding, uiZ);
        
        // LEVEL (Top Center)
        levelLabel.transform.position = new Vector3(topCenter.x, topLeft.y - padding, uiZ);
        
        // TIMER (Top Right)
        timerLabel.transform.position = new Vector3(topRight.x - padding, topLeft.y - padding, uiZ);
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
        // Find all boxes except the original prefab template
        BoxCell[] oldBoxes = FindObjectsOfType<BoxCell>();
        foreach (BoxCell box in oldBoxes)
        {
            if (box != originalBox)
            {
                Destroy(box.gameObject);
            }
        }
    }

    // --- Coroutines ---

    private IEnumerator NextLevel()
    {
        _isTimerRunning = false; // Stop the timer on level completion
        
        yield return new WaitForSeconds(1.5f);

        if (currentGridSize + 1 <= totalLevels + 1)
        {
            LoadLevel(currentGridSize + 1);
        }
        else
        {
            Debug.Log("All Levels Complete! Game Won!");
            levelLabel.text = "YOU WIN!";
        }
    }

    private IEnumerator GameOver(bool timeOut)
    {
        _isTimerRunning = false; // Stop the timer

        if (timeOut)
        {
            Debug.Log("Game Over! Time ran out!");
            levelLabel.text = "TIME OVER!"; 
        }
        else
        {
            Debug.Log("Game Over! You hit a bomb!");
            levelLabel.text = "GAME OVER!"; 
        }
        
        // Wait 3 seconds before reloading (or showing a menu)
        yield return new WaitForSeconds(3.0f);
        
        // Reload the current scene to restart
        // Note: You need to add 'using UnityEngine.SceneManagement;' at the top
        // SceneManager.LoadScene(SceneManager.GetActiveScene().buildIndex);
    }
}