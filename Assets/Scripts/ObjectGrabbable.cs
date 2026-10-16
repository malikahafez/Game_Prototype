using UnityEngine;
using UnityEngine.UIElements;

public class ObjectGrabbable : MonoBehaviour
{
    // private Rigidbody objectRigidbody;
    private Transform objectGrabPointTransform;
    private Vector3 _initialPosition;
    private Quaternion _initialRotation;

    private float lerpSpeed = 15.0f;

    private void Awake()
    {
        // Cache the original spawning transform
        _initialPosition = transform.position;
        _initialRotation = transform.rotation;
    }
    public void Grab(Transform objectGrabPointTransform)
    {
        this.objectGrabPointTransform = objectGrabPointTransform;

    }

    public void Drop()
    {
        this.objectGrabPointTransform = null;
        transform.position = _initialPosition;
        transform.rotation  = _initialRotation;
    }

    private void FixedUpdate()
    {
        if(objectGrabPointTransform != null)
        {
            Vector3 newPos = Vector3.Lerp(transform.position, objectGrabPointTransform.position, Time.deltaTime * lerpSpeed);
            transform.position = newPos;
        }
    }
}
