using UnityEngine;
using System.Collections;

[RequireComponent (typeof (AudioSource))]
public class golemTest : MonoBehaviour {

	//[SerializeField]
	private GameObject player;	
	
	private NavMeshAgent agent;
	private bool followCharacter;
	private AudioSource soundTest;

    void Start() {
		player = GameObject.FindWithTag("Player");
        agent = GetComponent<NavMeshAgent>();
		soundTest = GetComponent<AudioSource>();
		
		followCharacter = false;
    }
	
    void Update() {
       
		if(followCharacter){
        	agent.SetDestination(player.transform.position);
		}else{
			agent.SetDestination(transform.position);	
		}
            
    }
	
	void OnTriggerEnter(Collider other){

		if(other.CompareTag("Player")){
			if(soundTest != null && !followCharacter){
				soundTest.Play();
			}
			followCharacter = true;
			Debug.Log("triggered");
		}
	}
	
	void OnTriggerExit(Collider other){
		
	}
	
}
