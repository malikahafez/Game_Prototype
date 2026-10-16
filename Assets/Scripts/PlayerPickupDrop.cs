using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class PlayerPickupDrop : MonoBehaviour
{

    [SerializeField] private Transform playerCameraTransform;
    [SerializeField] private LayerMask pickupLayerMask;

    public float pickupDistance = 10f;

    private void Update()
    {
        if (Input.GetKeyDown(KeyCode.F))
        {
            bool cast = Physics.Raycast(playerCameraTransform.position, playerCameraTransform.forward, out RaycastHit raycasthit, pickupDistance, pickupLayerMask);
            if (cast)
            {
                Debug.Log("Grabbed: " + raycasthit.transform);
                // Debug.Log(raycasthit.transform);
                // if(raycasthit.transform.TryGetComponent(out ObjectGrabbable objectGrabbable))
                // {
                    // Debug.Log(objectGrabbable);
                // }
            }
        }
    }
}
