using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class PlayerPickupDrop : MonoBehaviour
{
    [Header("Pickup Settings")]
    [SerializeField] private Transform playerCameraTransform;
    [SerializeField] private Transform objectGrabPointTransform;
    [SerializeField] private LayerMask pickupLayerMask;
    public float pickupDistance = 10f;
    private ObjectGrabbable objectGrabbable;

    [Header("Quest Manager Reference")]
    public ObjectTrackerManager trackerManager;

    private void Update()
    {
        if (Input.GetKeyDown(KeyCode.F))
        {
            if (objectGrabbable == null)
            {
                // Not carrying an object, try to raycast
                bool cast = Physics.Raycast(playerCameraTransform.position, playerCameraTransform.forward, out RaycastHit raycasthit, pickupDistance, pickupLayerMask);

                if (cast)
                {
                    Transform hitTransform = raycasthit.transform;

                    // 1. Check if the object is a quest item
                    if (hitTransform.CompareTag("questitem"))
                    {
                        // Get its index using a small CollectibleItem component on the object
                        if (hitTransform.TryGetComponent(out QuestItem collectible))
                        {
                            if (trackerManager != null)
                            {
                                trackerManager.CollectItem(collectible.itemIndex);
                            }
                        }
                        else
                        {
                            Debug.LogWarning("Object tagged 'questitem' is missing the CollectibleItem script to hold its list index!");
                        }

                        // Destroy/disappear the object from the scene
                        Destroy(hitTransform.gameObject);
                        return; // Stop here so it doesn't run the normal grab logic
                    }

                    // 2. Otherwise, handle normal grabbable objects
                    if (hitTransform.TryGetComponent(out ObjectGrabbable grabbableComponent))
                    {
                        objectGrabbable = grabbableComponent;
                        objectGrabbable.Grab(objectGrabPointTransform);
                        Debug.Log("Grabbed: " + hitTransform);
                    }
                }
            }
            else
            {
                // Currently carrying something, drop
                objectGrabbable.Drop();
                Debug.Log("Dropped: " + objectGrabbable);
                objectGrabbable = null;
            }
        }
    }
}


// using System.Collections;
// using System.Collections.Generic;
// using UnityEngine;

// public class PlayerPickupDrop : MonoBehaviour
// {

//     [SerializeField] private Transform playerCameraTransform;
//     [SerializeField] private Transform objectGrabPointTransform;
//     [SerializeField] private LayerMask pickupLayerMask;

//     public float pickupDistance = 10f;
//     private ObjectGrabbable objectGrabbable;

//     [Header("Manager Reference")]
//     public ObjectTrackerManager trackerManager;

//     [Header("List Settings")]
//     public int itemIndex; // Must match the index in the Manager array (0 for 1st item, 1 for 2nd, etc.)

//     private void Update()
//     {
//         if (Input.GetKeyDown(KeyCode.F))
//         {
//             if (objectGrabbable == null)
//             {
//                 //Not carrying an object, try to grab
//                 bool cast = Physics.Raycast(playerCameraTransform.position, playerCameraTransform.forward, out RaycastHit raycasthit, pickupDistance, pickupLayerMask);
//                 if (cast)
//                 {

//                     Debug.Log("Grabbed: " + raycasthit.transform);
//                     Debug.Log(raycasthit.transform);
//                     if (raycasthit.transform.TryGetComponent(out ObjectGrabbable objectGrabbable))
//                     {
//                         this.objectGrabbable = objectGrabbable;
//                         objectGrabbable.Grab(objectGrabPointTransform);
//                         Debug.Log(objectGrabbable);
//                     }
//                 }
//             }
//             else
//             {
//                 //currently carrying something, drop
//                 objectGrabbable.Drop();
//                 Debug.Log("Dropped: " + objectGrabbable);
//                 objectGrabbable = null;
//             }

//         }
//     }
// }
