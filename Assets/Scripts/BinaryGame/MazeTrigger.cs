using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class MazeTrigger : MonoBehaviour {
    
    BinaryGame binaryGame;
	// Use this for initialization
	void Start () 
    {
        binaryGame = GameObject.Find("Main").GetComponent<BinaryGame>();
	}
	
	// Update is called once per frame
	void Update () {
		
	}

	void OnControllerColliderHit(ControllerColliderHit hit)
	{
        ColliderData colliderData = hit.collider.GetComponent<ColliderData>();
        if (colliderData != null)
        {
            Debug.Log("Mission Status : " + hit.collider.GetComponent<ColliderData>().Correct);
            if(hit.collider.GetComponent<ColliderData>().Correct){
                binaryGame.GiveReward(2, true);
            }
            else
            {
                binaryGame.WitholdReward();
            }
        }
	}
}
