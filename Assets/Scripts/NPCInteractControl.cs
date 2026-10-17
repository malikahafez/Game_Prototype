using UnityEngine;

public class NPCInteractControl : MonoBehaviour
{
    [Header("Animation")]
    [SerializeField] private Animator anim;
    [SerializeField] private Rigidbody rb;
    [Header("Toggle Talking")]
    [SerializeField] private bool isTalking = false;
    [Header("Player Raycasting")]
    [SerializeField] private Transform playerCameraTransform;
    public float pickupDistance = 5.0f;



    void Update()
    {
        bool cast = Physics.Raycast(playerCameraTransform.position, playerCameraTransform.forward, out RaycastHit raycasthit, pickupDistance);
        if (cast)
        {
            // Debug.Log("raycast hit " + isTalking);

            if (raycasthit.collider.CompareTag("npc") && Input.GetKeyDown(KeyCode.E))
            {
                Debug.Log("talking mode " + isTalking);
                Talk();
                Debug.Log("after enable talking " + isTalking);

            }

        }

    }



    void Talk()
    {
        anim.SetBool("isTalking", true);
        isTalking = true;
    }

    void StopTalking()
    {
        anim.SetBool("isTalking", false);
    }


}
