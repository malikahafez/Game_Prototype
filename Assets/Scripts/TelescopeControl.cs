using UnityEngine;
using Unity.Cinemachine;

public class TelescopeControl : MonoBehaviour
{
    [Header("Cameras")]
    [SerializeField] private CinemachineCamera telescopeCam;
    [SerializeField] private CinemachineCamera mainCam;

    [Header("UI Overlay")]
    [SerializeField] private GameObject telescopeUI;

    [Header("Raycast Interaction")]
    [SerializeField] private Transform playerCameraTransform;
    public float pickupDistance = 5.0f;


    private bool telescopeMode = false;

    void Update()
    {
        bool cast = Physics.Raycast(playerCameraTransform.position, playerCameraTransform.forward, out RaycastHit raycasthit, pickupDistance);
        if (cast)
        {
            if (raycasthit.collider.CompareTag("telescope") && Input.GetKeyDown(KeyCode.E))
            {
                Debug.Log("telescope " + telescopeMode);
                enableTelescopeMode();
            }
            else if (Input.GetKeyDown(KeyCode.E) && telescopeMode)
            {
                disableTelescopeMode();
            }
        }

    }



    private void enableTelescopeMode()
    {
        telescopeMode = true;

        //switch camera to telescope camera
        telescopeCam.Priority = 20;
        mainCam.Priority = 10;

        if (telescopeUI != null)
        {//enable telescope UI
            telescopeUI.SetActive(true);
        }
    }

    private void disableTelescopeMode()
    {
        telescopeMode = false;

        //switch camera to main camera
        telescopeCam.Priority = 10;
        mainCam.Priority = 20;
        if (telescopeUI != null)
        {//disable telescope UI
            telescopeUI.SetActive(false);
        }
    }



}
