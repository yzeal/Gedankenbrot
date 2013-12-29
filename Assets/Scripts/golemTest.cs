using UnityEngine;
using System.Collections;

public class golemTest : MonoBehaviour {

	[SerializeField]
	private GameObject player;
	[SerializeField]
	private AudioClip golemSound;	
	
	private NavMeshAgent agent;
	private bool followCharacter;
	private AudioSource soundTest;
	
	
    void Start() {
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
		//agent.Move();
            
    }
	
	void OnTriggerEnter(Collider other){
		if(other.CompareTag("Player")){
			followCharacter = true;
			Debug.Log("triggered");
		}
		
		if(soundTest != null){
			soundTest.Play();
		}
	}
	
	void OnTriggerExit(Collider other){
		
	}
	
}
