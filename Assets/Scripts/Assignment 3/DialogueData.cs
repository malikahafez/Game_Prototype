using UnityEngine;

[System.Serializable]
public class DialogueLine
{
    public string speakerName;
    public Sprite speakerPortrait;
    [TextArea(2, 5)]
    public string text;
    public AudioClip voiceOrFoleyClip;
}


// ScriptableObject storing a complete NPC conversation, hint, or internal monologue.

[CreateAssetMenu(fileName = "NewDialogue", menuName = "Caprona/Dialogue Data")]
public class DialogueData : ScriptableObject
{
    [Header("Preconditions")]
    [Tooltip("Optional flag required before this dialogue can play.")]
    public string requiredFlag;

    [Header("Dialogue Content")]
    public DialogueLine[] lines;

    [Header("Reactive Choice / Penalty Override")]
    [Tooltip("Optional alternate lines if player has 4+ misclick penalties.")]
    public DialogueLine[] highPenaltyLines;

    [Header("Post-Dialogue State Change")]
    [Tooltip("Flag set in StoryStateManager when this dialogue finishes.")]
    public string flagToSetWhenFinished;

    [Header("Optional Player Choice (At End of Dialogue)")]
    public bool hasPlayerChoice = false;
    [TextArea(1, 2)] public string choicePrompt;        // e.g., "Are you enjoying your vacation in Greenland?"
    public string yesOptionLabel = "Yes";
    public string noOptionLabel = "No";

    [Header("Choice Consequences")]
    public string yesFlagToSet;                         // e.g., "enjoyedGreenland" or "pettedNobs"
    public string noFlagToSet;                          // e.g., "dislikedGreenland"
    public int yesCoinReward = 0;                       // Set to 25 or 50 for Nobs ("Yes" -> coins!)
    public DialogueData yesFollowUpDialogue;            // Plays immediately when player clicks Yes
    public DialogueData noFollowUpDialogue;             // Plays immediately when player clicks No
}