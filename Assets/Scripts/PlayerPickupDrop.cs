using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class PlayerPickupDrop : MonoBehaviour
{

    [SerializeField] private Transform playerCameraTransform;
    [SerializeField] private Transform objectGrabPointTransform;
    [SerializeField] private LayerMask pickupLayerMask;

    public float pickupDistance = 10f;
    private ObjectGrabbable objectGrabbable;

    private void Update()
    {
        if (Input.GetKeyDown(KeyCode.F))
        {
            if (objectGrabbable == null)
            {
                //Not carrying an object, try to grab
                bool cast = Physics.Raycast(playerCameraTransform.position, playerCameraTransform.forward, out RaycastHit raycasthit, pickupDistance, pickupLayerMask);
                if (cast)
                {

                    Debug.Log("Grabbed: " + raycasthit.transform);
                    Debug.Log(raycasthit.transform);
                    if (raycasthit.transform.TryGetComponent(out ObjectGrabbable objectGrabbable))
                    {
                        this.objectGrabbable = objectGrabbable;
                        objectGrabbable.Grab(objectGrabPointTransform);
                        Debug.Log(objectGrabbable);
                    }
                }
            }
            else
            {
                //currently carrying something, drop
                objectGrabbable.Drop();
                Debug.Log("Dropped: " + objectGrabbable);
                objectGrabbable = null;
            }

        }
    }
}
