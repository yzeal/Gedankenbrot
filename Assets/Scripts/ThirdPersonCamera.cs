using UnityEngine;
using System.Collections;

public class ThirdPersonCamera : MonoBehaviour {
	
	#region Variables (private)
	
	[SerializeField]
	private float distanceAway;
	[SerializeField]
	private float distanceUp;
	[SerializeField]
	private float smooth;
	[SerializeField]
	private Transform followXform;
	[SerializeField]
	private Vector3 offset = new Vector3(0f, 1.5f, 0f);
	
	private Vector3 lookDir;
	private Vector3 targetPosition;
	
	//Smoothing and damping:
	private Vector3 velocityCamSmooth = Vector3.zero;
	[SerializeField]
	private float camSmoothDampTime = 0.1f;
	
	#endregion

	// Use this for initialization
	void Start () {
		followXform = GameObject.FindWithTag("Player").transform;
	}
	
	// Update is called once per frame
	void Update () {
	
	}
	
	void LateUpdate(){
		
		Vector3 characterOffset = followXform.position + offset;
		
		lookDir = characterOffset - this.transform.position;
		lookDir.y = 0;
		lookDir.Normalize();
		
		
		
		targetPosition = characterOffset + followXform.up * distanceUp - lookDir * distanceAway;
		Debug.DrawRay(followXform.position, Vector3.up * distanceUp, Color.red);
		Debug.DrawRay (followXform.position, -1f * followXform.forward * distanceAway, Color.blue);
		Debug.DrawLine (followXform.position, targetPosition, Color.magenta);
		
		
		
		smoothPosition(this.transform.position, targetPosition);
		
		transform.LookAt(followXform);
	}
	
	#region Methods
	
	private void smoothPosition(Vector3 fromPos, Vector3 toPos){
		
		this.transform.position = Vector3.SmoothDamp(fromPos, toPos, ref velocityCamSmooth, camSmoothDampTime);
	}
	
	#endregion
	
	
}
