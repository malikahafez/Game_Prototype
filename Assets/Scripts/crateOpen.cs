using UnityEngine;

public class crateOpen : MonoBehaviour
{
    [SerializeField] bool latch = false;
    [SerializeField] bool crate = false;
    
    // Update is called once per frame
    void Update()
    {
        if(latch){
            OpenLatch();
        }
        if(crate){
            OpenCrate();
        }
    }

    public void OpenLatch(){
        this.GetComponent<Animator>().Play("crate_open", -1, 0f);
    }

     public void OpenCrate(){
        this.GetComponent<Animator>().Play("crate_open2", 0, 0f);
    }
}
