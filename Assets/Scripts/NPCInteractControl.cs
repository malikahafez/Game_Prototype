using Unity.Cinemachine;
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
    [Header("Cameras")]
    [SerializeField] private CinemachineCamera mainCam;
    [SerializeField] private CinemachineCamera cutsceneCam;
    [Header("UI Overlay")]
    [SerializeField] private GameObject dialogUI;
    [SerializeField] private GameObject mainUI;






    void Update()
    {
        bool cast = Physics.Raycast(playerCameraTransform.position, playerCameraTransform.forward, out RaycastHit raycasthit, pickupDistance);
        if (cast && !isTalking)
        {
            // Debug.Log("raycast hit " + isTalking);

            if (raycasthit.collider.CompareTag("npc") && Input.GetKeyDown(KeyCode.E))
            {
                Debug.Log("talking mode " + isTalking);
                Talk();
                Debug.Log("after enable talking " + isTalking);

            }

        }
        else if (isTalking && Input.GetKeyDown(KeyCode.E))
        {
            Debug.Log("talking mode " + isTalking);
            StopTalking();
            Debug.Log("after disable talking " + isTalking);
        }

    }



    void Talk()
    {
        anim.SetBool("isTalking", true);
        isTalking = true;
        //switch camera to telescope camera
        cutsceneCam.Priority = 20;
        mainCam.Priority = 10;
        if (dialogUI != null)
        {//enable telescope UI
            dialogUI.SetActive(true);
        }
        if (mainUI != null)
        {//enable telescope UI
            mainUI.SetActive(false);
        }
    }

    void StopTalking()
    {
        anim.SetBool("isTalking", false);
        isTalking = false;
        //switch camera to telescope camera
        cutsceneCam.Priority = 10;
        mainCam.Priority = 20;
        if (dialogUI != null)
        {//enable telescope UI
            dialogUI.SetActive(false);
        }
        if (mainUI != null)
        {//enable telescope UI
            mainUI.SetActive(true);
        }
    }


}
