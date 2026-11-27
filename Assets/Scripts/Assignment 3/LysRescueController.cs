using System.Collections;
using UnityEngine;

/// <summary>
/// Starts Lys in Kneeling Idle. Once 'rescuedLys' is set and her rescue dialogue closes,
/// transitions her to Walking along the cave exit waypoints, then to standing Idle outside.
/// </summary>
public class LysRescueController : MonoBehaviour
{
    [Header("Story Flag")]
    public string rescueFlag = "rescuedLys";

    [Header("Animator & Parameters")]
    public Animator lysAnimator;
    public string walkBoolParameter = "IsWalking";
    public string standBoolParameter = "IsStanding";

    [Header("Escape Movement")]
    public Transform[] escapeWaypoints;     // Waypoints leading from inside the cave to outside
    public float walkSpeed = 3.2f;
    public float rotationSpeed = 6f;
    [Tooltip("Keep false so she remains standing Idle outside the cave.")]
    public bool hideAfterReachingExit = false;

    private bool hasStartedEscape = false;

    private void Start()
    {
        // Ensure she starts in default Kneeling Idle
        if (lysAnimator != null)
        {
            if (!string.IsNullOrEmpty(walkBoolParameter))
                lysAnimator.SetBool(walkBoolParameter, false);
            if (!string.IsNullOrEmpty(standBoolParameter))
                lysAnimator.SetBool(standBoolParameter, false);
        }

        if (StoryStateManager.Instance != null)
        {
            StoryStateManager.Instance.OnFlagSet += OnStoryFlagSet;
        }
    }

    private void OnDestroy()
    {
        if (StoryStateManager.Instance != null)
        {
            StoryStateManager.Instance.OnFlagSet -= OnStoryFlagSet;
        }
    }

    private void OnStoryFlagSet(string flag)
    {
        if (flag == rescueFlag && !hasStartedEscape)
        {
            hasStartedEscape = true;
            StartCoroutine(WaitForDialogueThenWalkOut());
        }
    }

    private IEnumerator WaitForDialogueThenWalkOut()
    {
        // Wait until the player closes Lys's rescue dialogue
        while (DialogueManager.Instance != null && DialogueManager.Instance.IsDialogueActive)
        {
            yield return null;
        }

        // Disable collider while walking so she doesn't snag on the player
        Collider lysCollider = GetComponent<Collider>();
        if (lysCollider != null)
            lysCollider.enabled = false;

        // 1. Transition from Kneeling Idle -> Walking
        if (lysAnimator != null && !string.IsNullOrEmpty(walkBoolParameter))
        {
            lysAnimator.SetBool(walkBoolParameter, true);
        }

        // Move through each waypoint leading out of the cave
        foreach (Transform waypoint in escapeWaypoints)
        {
            if (waypoint == null) continue;

            while (Vector3.Distance(transform.position, waypoint.position) > 0.2f)
            {
                Vector3 dir = (waypoint.position - transform.position).normalized;
                dir.y = 0f;
                if (dir.sqrMagnitude > 0.001f)
                {
                    Quaternion targetRot = Quaternion.LookRotation(dir);
                    transform.rotation = Quaternion.Slerp(transform.rotation, targetRot, rotationSpeed * Time.deltaTime);
                }

                transform.position = Vector3.MoveTowards(transform.position, waypoint.position, walkSpeed * Time.deltaTime);
                yield return null;
            }
        }

        // 2. Arrived outside: Transition from Walking -> Standing Idle
        if (lysAnimator != null)
        {
            if (!string.IsNullOrEmpty(walkBoolParameter))
                lysAnimator.SetBool(walkBoolParameter, false);

            if (!string.IsNullOrEmpty(standBoolParameter))
                lysAnimator.SetBool(standBoolParameter, true);
        }

        if (hideAfterReachingExit)
        {
            gameObject.SetActive(false);
        }
        else if (lysCollider != null)
        {
            // Re-enable collider outside if you want her physically present in the exterior area
            lysCollider.enabled = true;
        }
    }
}