using UnityEngine;
using System.Collections;

public class golemTest : MonoBehaviour {

	[SerializeField]
	private GameObject player;
	
	private NavMeshAgent agent;
	private SphereCollider sphere;
	private bool followCharacter;
	
    void Start() {
        agent = GetComponent<NavMeshAgent>();
		sphere = GetComponentInChildren<SphereCollider>();
		
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
	}
	
	void OnTriggerExit(Collider other){
		
	}
	
}
