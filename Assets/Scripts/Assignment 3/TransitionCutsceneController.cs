using UnityEngine;
using UnityEngine.Playables;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

/// <summary>
/// Plays the appropriate Timeline & Cinemachine cutscene in 04_TransitionCutscene,
/// then transitions to either the next level or 00_MainMenu.
/// </summary>
public class TransitionCutsceneController : MonoBehaviour
{
    [Header("Timeline Playable Directors")]
    public PlayableDirector cutscene1Director; // After Level 1 (U-33 backstory)
    public PlayableDirector cutscene2Director; // After Level 2 (Storm & Caprona arrival)
    public PlayableDirector cutscene3Director; // After Level 3 (Ending)

    [Header("Optional Skip Button")]
    public Button skipCutsceneButton;

    private PlayableDirector activeDirector;
    private string destinationScene = "00_MainMenu";

    private void Start()
    {
        Cursor.lockState = CursorLockMode.None;
        Cursor.visible = true;

        int cutsceneIndex = 1;
        if (StoryStateManager.Instance != null)
        {
            cutsceneIndex = StoryStateManager.Instance.cutsceneToPlay;
            destinationScene = StoryStateManager.Instance.postCutsceneDestinationScene;
        }

        // Disable all directors first
        if (cutscene1Director != null) cutscene1Director.gameObject.SetActive(false);
        if (cutscene2Director != null) cutscene2Director.gameObject.SetActive(false);
        if (cutscene3Director != null) cutscene3Director.gameObject.SetActive(false);

        // Select the matching Timeline
        switch (cutsceneIndex)
        {
            case 1: activeDirector = cutscene1Director; break;
            case 2: activeDirector = cutscene2Director; break;
            case 3: activeDirector = cutscene3Director; break;
        }

        if (skipCutsceneButton != null)
            skipCutsceneButton.onClick.AddListener(FinishCutscene);

        if (activeDirector != null)
        {
            activeDirector.gameObject.SetActive(true);
            activeDirector.stopped += OnTimelineFinished;
            activeDirector.Play();
        }
        else
        {
            FinishCutscene();
        }
    }

    private void OnTimelineFinished(PlayableDirector director)
    {
        FinishCutscene();
    }

    public void FinishCutscene()
    {
        if (activeDirector != null)
            activeDirector.stopped -= OnTimelineFinished;

        SceneManager.LoadScene(destinationScene);
    }
}