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
}