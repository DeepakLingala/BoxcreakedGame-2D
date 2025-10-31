using UnityEngine;
using System.Collections;

public class BoxCell : MonoBehaviour
{
    [Header("Object References")]
    [SerializeField] private GameObject boxCover; 
    [SerializeField] private GameObject candyObject;
    [SerializeField] private GameObject bombObject; 
    
    // --- NEW: Sound Effect References ---
    [Header("Sound Effects")]
    [SerializeField] private AudioClip candySound; // Drag your candy audio clip here
    [SerializeField] private AudioClip bombSound;  // Drag your bomb audio clip here

    private LevelManager manager; 
    private AudioSource audioSource; // NEW: Reference to the AudioSource component

    public enum ContentType { Candy, Bomb };
    private ContentType _contentType;
    public ContentType contentType => _contentType;

    private bool isRevealed = false;

    void Awake()
    {
        manager = FindObjectOfType<LevelManager>();
        if (manager == null)
        {
            Debug.LogError("BoxCell cannot find the LevelManager in the scene!");
        }
        
        // --- NEW: Get the AudioSource component ---
        audioSource = GetComponent<AudioSource>();
        if (audioSource == null)
        {
            Debug.LogWarning($"BoxCell on {gameObject.name} is missing an AudioSource component. Add one to play sounds!");
            // Add a default one if not found, useful for quick setup
            audioSource = gameObject.AddComponent<AudioSource>();
        }
        // Basic AudioSource settings (can be changed in Inspector)
        audioSource.playOnAwake = false; // Don't play sound automatically
        audioSource.spatialBlend = 0;   // 2D sound (doesn't depend on position)

        candyObject.transform.localScale = Vector3.zero;
        bombObject.transform.localScale = Vector3.zero;
        candyObject.SetActive(false);
        bombObject.SetActive(false);
    }

    public void SetContent(ContentType type)
    {
        _contentType = type;
        
        candyObject.SetActive(type == ContentType.Candy);
        bombObject.SetActive(type == ContentType.Bomb);
    }

    public void OnMouseDown()
    {
        if (manager == null) return; 
        
        if (boxCover.activeSelf && !isRevealed && manager.IsTimerRunning())
        {
            boxCover.SetActive(false);
            isRevealed = true;

            // --- NEW: Play the appropriate sound ---
            if (audioSource != null)
            {
                if (_contentType == ContentType.Candy && candySound != null)
                {
                    audioSource.PlayOneShot(candySound);
                }
                else if (_contentType == ContentType.Bomb && bombSound != null)
                {
                    audioSource.PlayOneShot(bombSound);
                }
            }

            StartCoroutine(PopUpAnimation(_contentType == ContentType.Candy ? candyObject : bombObject));
            
            manager.BoxClicked(this);
        }
    }
    
    public void ResetBox()
    {
        boxCover.SetActive(true);
        isRevealed = false;
        candyObject.transform.localScale = Vector3.zero;
        bombObject.transform.localScale = Vector3.zero;
        candyObject.SetActive(false);
        bombObject.SetActive(false);
    }

    private IEnumerator PopUpAnimation(GameObject target)
    {
        target.SetActive(true);
        float duration = 0.2f; 
        Vector3 startScale = Vector3.zero;
        Vector3 endScale = Vector3.one;
        
        target.transform.localScale = startScale;
        float startTime = Time.time;
        
        while (Time.time < startTime + duration)
        {
            float t = (Time.time - startTime) / duration;
            float bounce = Mathf.Sin(t * Mathf.PI); 
            target.transform.localScale = Vector3.Lerp(startScale, endScale, t) * (1f + bounce * 0.2f);
            yield return null;
        }
        target.transform.localScale = endScale;
    }
}