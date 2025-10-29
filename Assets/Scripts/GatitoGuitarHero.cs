using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.SceneManagement;

[Serializable] public class PromptEvent : UnityEvent<int, string> { } // (lane, lyric)

public class GatitoGuitarHero : MonoBehaviour
{
    [Header("Stars / Lives")]
    public GameObject starPrefab;
    [SerializeField] int numStars = 5;
    public float starRightX = -7f;
    public float starSpacingX = 1f;
    public List<GameObject> starList;

    [Header("UI / Popups")]
    public GameObject startPopUp;
    public GameObject nowPlayingText;
    public GameObject gameOverPopUp;
    public GameObject winPopUp;

    [Header("Audio")]
    public AudioSource audioSource;
    public AudioClip reowClip;
    public AudioClip yippyClip;

    [Header("Cats")]
    public GameObject yippyGatito;
    public GameObject angyGatito;

    [Header("Prompts JSON")]
    public TextAsset promptsJson;                 
    public float earlyShow = 0.8f;
    public float lateWindow = 0.8f;
    public float globalOffsetMs = 0f;
    bool answeredThisPrompt = false;

    [Header("Events")]
    public PromptEvent OnPrompt;       // lane, lyric
    public UnityEvent OnPromptExpired;

    bool hasStarted = false;
    double dspSongStart;
    bool songScheduled = false;

    PromptFile promptFile;
    int promptIndex = 0;
    Prompt currentPrompt = null;

    float OffsetSec => globalOffsetMs / 1000f;

    void Start()
    {
        // Instantiate stars on the screen
        starList = new List<GameObject>();
        for (int i = 0; i < numStars; i++)
        {
            GameObject star = Instantiate(starPrefab);
            Vector2 pos = Vector2.zero;
            pos.x = starRightX + (starSpacingX * i);
            pos.y = 4.18f;
            star.transform.position = pos;
            starList.Add(star);
        }

        // Load JSON prompts
        LoadPrompts();

        // Double-check pop-up states
        if (startPopUp) startPopUp.SetActive(true);
        if (nowPlayingText) nowPlayingText.SetActive(false);
        if (gameOverPopUp) gameOverPopUp.SetActive(false);
        if (winPopUp) winPopUp.SetActive(false);
    }

    void Update()
    {
        // Start pop-up: hit enter to start
        if (!hasStarted)
        {
            if (startPopUp && startPopUp.activeInHierarchy && Input.GetKeyDown(KeyCode.Return))
            {
                hasStarted = true;
                if (startPopUp) startPopUp.SetActive(false);
                StartSongScheduled();
                if (nowPlayingText) nowPlayingText.SetActive(true);
            }
            return;
        }

        // If not scheduled OR no prompts, return
        if (!songScheduled || promptFile == null || promptFile.prompts == null || promptFile.prompts.Count == 0)
            return;

        float songTime = GetSongTime() + OffsetSec;

        // Start next prompt
        if (promptIndex < promptFile.prompts.Count &&
            promptFile.prompts[promptIndex].time - songTime <= earlyShow)
        {
            currentPrompt = promptFile.prompts[promptIndex++];
            answeredThisPrompt = false;
            OnPrompt?.Invoke(currentPrompt.lane, currentPrompt.lyric);
        }

        // If prompt window passed, auto-expire
        if (currentPrompt != null && songTime > currentPrompt.time + lateWindow)
        {
            // No input --> Missed Note
            if (!answeredThisPrompt)
                missedNote();

            currentPrompt = null;
            answeredThisPrompt = false;            
            OnPromptExpired?.Invoke();
        }


        // Keyboard input handling
        if (Input.GetKeyDown(KeyCode.W)) TrySubmit(0);
        if (Input.GetKeyDown(KeyCode.A)) TrySubmit(1);
        if (Input.GetKeyDown(KeyCode.S)) TrySubmit(2);
        if (Input.GetKeyDown(KeyCode.D)) TrySubmit(3);

        // End of song --> Win!
        if (songScheduled && audioSource && !audioSource.isPlaying && promptIndex >= promptFile.prompts.Count)
        {
            audioSource.Stop();
            if (winPopUp) winPopUp.SetActive(true);
            audioSource.clip = yippyClip;
            audioSource.Play();
            hasStarted = false;
            StartCoroutine(DelayedActionCoroutine()); // delay 3 sec.
        }
    }

    // Correct input --> yippy cat
    void hitNote()
    {
        if (angyGatito) angyGatito.SetActive(false);
        if (yippyGatito) yippyGatito.SetActive(true);
    }

    // Incorrect input --> angy cat & remove star
    void missedNote()
    {
        int starIndex = starList.Count - 1;
        GameObject star = starList[starIndex];
        starList.RemoveAt(starIndex);
        Destroy(star);
        if (yippyGatito) yippyGatito.SetActive(false);
        if (angyGatito) angyGatito.SetActive(true);

        // If no stars left, GAME OVER
        if (starList.Count == 0)
        {
            audioSource.Stop();
            if (gameOverPopUp) gameOverPopUp.SetActive(true);
            audioSource.clip = reowClip;
            audioSource.Play();
            StartDelayedAction(); // delay 3 sec.
        }
    }

    // Check input against current prompt lane
    void TrySubmit(int lane)
    {
        if (currentPrompt == null) return; // potentially penalize

        if (lane == currentPrompt.lane)
        {
            answeredThisPrompt = true;
            currentPrompt = null;
            OnPromptExpired?.Invoke(); // turn off UI/highlights
            hitNote();
        }
        else
        {
            missedNote();
        }
    }

    // Load JSON prompts
    void LoadPrompts()
    {
        if (promptsJson != null)
        {
            promptFile = JsonUtility.FromJson<PromptFile>(promptsJson.text);
        }

        if (promptFile == null || promptFile.prompts == null || promptFile.prompts.Count == 0) return;

        // Double check time sorting
        promptFile.prompts.Sort((a, b) => a.time.CompareTo(b.time));
        promptIndex = 0;
        currentPrompt = null;
    }

    // Schedule song
    void StartSongScheduled()
    {
        if (!audioSource || !audioSource.clip) return;
        dspSongStart = AudioSettings.dspTime + 0.1f; 
        audioSource.PlayScheduled(dspSongStart);
        songScheduled = true;
    }

    float GetSongTime()
    {
        if (!songScheduled) return 0f;
        return (float)(AudioSettings.dspTime - dspSongStart);
    }

    // Call to wait 3 seconds
     void StartDelayedAction()
    {
        StartCoroutine(DelayedActionCoroutine());
    }

    // Wait & exit function 
    private IEnumerator DelayedActionCoroutine()
    {
        yield return new WaitForSeconds(3.0f);
        SceneManager.LoadScene("SampleScene");
    }
}
