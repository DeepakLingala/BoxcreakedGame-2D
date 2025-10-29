using UnityEngine;
using System.Collections;

public class BoxCell : MonoBehaviour
{
    [Header("Object References")]
    [SerializeField] private GameObject boxCover; // The sprite that hides the content
    [SerializeField] private GameObject candyObject; // The "Candy" sprite
    [SerializeField] private GameObject bombObject;  // The "Bomb" sprite
    
    // The LevelManager is found automatically
    private LevelManager manager; 

    public enum ContentType { Candy, Bomb };
    private ContentType _contentType;
    public ContentType contentType => _contentType; // Read-only property

    private bool isRevealed = false;

    // Awake is called when the object is instantiated
    void Awake()
    {
        // Find the LevelManager in the scene
        manager = FindObjectOfType<LevelManager>();
        if (manager == null)
        {
            Debug.LogError("BoxCell cannot find the LevelManager in the scene!");
        }
        
        // Ensure pop-up items are hidden by scale (or being inactive)
        candyObject.transform.localScale = Vector3.zero;
        bombObject.transform.localScale = Vector3.zero;
        candyObject.SetActive(false);
        bombObject.SetActive(false);
    }

    // Called by LevelManager to assign content
    public void SetContent(ContentType type)
    {
        _contentType = type;
        
        // Set the correct visual active *before* the pop-up
        candyObject.SetActive(type == ContentType.Candy);
        bombObject.SetActive(type == ContentType.Bomb);
    }

    // Called by Unity when the 2D collider is clicked
    public void OnMouseDown()
    {
        // Only allow clicking if the cover is active and the game is running
        if (boxCover.activeSelf && !isRevealed && manager.IsTimerRunning())
        {
            boxCover.SetActive(false);
            isRevealed = true;

            // Trigger the pop-up animation for the correct content
            StartCoroutine(PopUpAnimation(_contentType == ContentType.Candy ? candyObject : bombObject));

            // Tell the manager this box was clicked
            manager.BoxClicked(this);
        }
    }
    
    // Resets the box to its starting state (used for the original prefab)
    public void ResetBox()
    {
        boxCover.SetActive(true);
        isRevealed = false;
        candyObject.transform.localScale = Vector3.zero;
        bombObject.transform.localScale = Vector3.zero;
        candyObject.SetActive(false);
        bombObject.SetActive(false);
    }

    // Coroutine for the pop-up animation
    private IEnumerator PopUpAnimation(GameObject target)
    {
        target.SetActive(true);
        float duration = 0.2f; // Animation speed
        Vector3 startScale = Vector3.zero;
        Vector3 endScale = Vector3.one;
        
        target.transform.localScale = startScale;
        float startTime = Time.time;
        
        while (Time.time < startTime + duration)
        {
            float t = (Time.time - startTime) / duration;
            // Simple bounce effect using sine
            float bounce = Mathf.Sin(t * Mathf.PI); 
            target.transform.localScale = Vector3.Lerp(startScale, endScale, t) * (1f + bounce * 0.2f);
            yield return null;
        }
        // Ensure it ends at exactly scale 1
        target.transform.localScale = endScale;
    }
}