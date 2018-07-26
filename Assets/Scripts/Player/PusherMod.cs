using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class PusherMod : MonoBehaviour
{
    [SerializeField]
    Transform transform;

    bool goingOut = true;
    bool cycleFinished = false;

    void Start ()
    {
        Debug.Log("Pusher initialised");
        //transform.Translate(5, 0, 0);
        //transform.localPosition.x.Equals(transform.position.x + 0.1);

    }
	
	void FixedUpdate ()
    {
        if (Input.GetKey("space"))
        {
            if (goingOut)
            {
                if (transform.localPosition.x <= 0.7)
                {
                    transform.Translate(0.04f, 0, 0);
                    Debug.Log(transform.localPosition.x);
                }
                else
                {
                    goingOut = false;
                }
            }
            else
            {
                if (transform.localPosition.x >= 0)
                {
                    transform.Translate(-0.04f, 0, 0);
                }
                else
                {
                    goingOut = true;
                    cycleFinished = true;
                }
            }
        }
        
	}
}
