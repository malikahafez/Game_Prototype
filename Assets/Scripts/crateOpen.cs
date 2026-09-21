using UnityEngine;

public class crateOpen : MonoBehaviour
{
    [SerializeField] bool latch = false;
    [SerializeField] bool crate = false;
    [SerializeField] float internalDistance;
    
    // Update is called once per frame
    void Update()
    {
        // if(latch){
        //     OpenLatch();
        // }
        // if(crate){
        //     OpenCrate();
        // }
        internalDistance = Raycasting.distanceFromTarget;
        if(latch == false && crate ==false && internalDistance < 3)
        {
            if (Input.GetKeyDown(KeyCode.O))
            {
                latch = true;
                OpenLatch();
            }
            
        }
        if(latch == true && crate == false && internalDistance < 3)
        {
            if (Input.GetKeyDown(KeyCode.O))
            {
                crate = true;
                OpenCrate();
            }
            
        }
    }

    public void OpenLatch(){
        this.GetComponent<Animator>().Play("crate_open", 0, 0f);
    }

     public void OpenCrate(){
        this.GetComponent<Animator>().Play("crate_open2", 1, 0f);
    }
}