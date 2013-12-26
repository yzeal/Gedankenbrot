using UnityEngine;
using System.Collections;

public class CharacterControllerLogic : MonoBehaviour {
	
	#region Variables (private)
	[SerializeField]
	private Animator animator;
	[SerializeField]
	private float directionDampTime = 0.25f;
	[SerializeField]
	private float speedDampTime = 0.05f;
	[SerializeField]
	private float directionSpeed = 3.0f;
	[SerializeField]
	private float rotationDegreesPerSecond = 120.0f;
	[SerializeField]
	private ThirdPersonCamera gamecam;
	
	private float speed = 0.0f;
	private float direction = 0.0f;
	private float charAngle = 0.0f;
	private float horizontal = 0.0f;
	private float vertical = 0.0f;
	private AnimatorStateInfo stateInfo;
	private AnimatorTransitionInfo transInfo;
	
	//Hashes
	private int m_LocomotionId = 0;
	private int m_LocomotionPivotLId = 0;
	private int m_LocomotionPivotRId = 0;
	private int m_LocomotionPivotLTransId = 0;
	private int m_LocomotionPivotRTransId = 0;
	
	#endregion
	
	#region Properties (public)
	
	public Animator Animator{
		get{
			return this.animator;	
		}
	}

	public float Speed {
		get {
			return this.speed;
		}
	}
	
	public float LocomotionThreshold { get { return 0.2f; } }
	
	#endregion

	// Use this for initialization
	void Start () {
		animator = GetComponent<Animator>();
		
		if(animator.layerCount >= 2){
			animator.SetLayerWeight(1, 1);
		}
		
		m_LocomotionId = Animator.StringToHash("Base Layer.Locomotion");
		m_LocomotionPivotLId = Animator.StringToHash("Base Layer.LocomotionPivotL");
		m_LocomotionPivotRId = Animator.StringToHash("Base Layer.LocomotionPivotR");
		m_LocomotionPivotLTransId = Animator.StringToHash("Base Layer.Locomotion -> Base Layer.LocomotionPivotL");
		m_LocomotionPivotRTransId = Animator.StringToHash("Base Layer.Locomotion -> Base Layer.LocomotionPivotR");
	}
	
	// Update is called once per frame
	void Update () {
		
		if(animator && gamecam.CamState != ThirdPersonCamera.CamStates.FirstPerson){
			
			stateInfo = animator.GetCurrentAnimatorStateInfo(0);
			transInfo = animator.GetAnimatorTransitionInfo(0);
			
			horizontal = Input.GetAxis("Horizontal");
			vertical = Input.GetAxis ("Vertical");
			
			charAngle = 0f;
			direction = 0f;
			
			StickToWorldspace(this.transform, gamecam.transform, ref direction, ref speed, ref charAngle, IsInPivot());
			
			animator.SetFloat("Speed", speed, speedDampTime, Time.deltaTime);
			animator.SetFloat("Direction", direction, directionDampTime, Time.deltaTime);
		
			if(speed > LocomotionThreshold){
				if(!IsInPivot()){
					animator.SetFloat("Angle", charAngle);
				}
			}
			
			if(speed < LocomotionThreshold && Mathf.Abs(horizontal) < 0.5f){
				animator.SetFloat("Direction", 0f);	
				animator.SetFloat("Angle", 0f);
			}
		}
	
	}
	
	void FixedUpdate(){
		//Rotate character model if stick is tilted right or left, but only if character is moving in that direction
		if(IsInLocomotion() && ((direction >= 0 && horizontal >= 0) || (direction < 0 && horizontal < 0))){
			
			Vector3 rotationAmount = Vector3.Lerp(Vector3.zero, new Vector3(0f, rotationDegreesPerSecond * (horizontal < 0f ? -1f : 1f), 0f), Mathf.Abs (horizontal));
			Quaternion deltaRotation = Quaternion.Euler(rotationAmount * Time.deltaTime);
			this.transform.rotation = (this.transform.rotation * deltaRotation);
		}
		
	}
	
	#region Methods
	
	public void StickToWorldspace(Transform root, Transform camera, ref float directionOut, ref float speedOut, ref float angleOut, bool isPivoting){
		
		Vector3 rootDirection = root.forward;
		
		Vector3 stickDirection = new Vector3(horizontal, 0, vertical);
		
		speedOut = stickDirection.sqrMagnitude;
		
		//Get camera rotation:
		Vector3 cameraDirection = camera.forward;
		cameraDirection.y = 0.0f;
		Quaternion referentialShift = Quaternion.FromToRotation(Vector3.forward, cameraDirection);
	
		//convert joystick input to worldspace coordinates
		Vector3 moveDirection = referentialShift * stickDirection;
		Vector3 axisSign = Vector3.Cross (moveDirection, rootDirection);
		
		float angleRootToMove = Vector3.Angle (rootDirection, moveDirection) * (axisSign.y >= 0 ? -1f : 1f);
		
		if(!isPivoting){
			angleOut = angleRootToMove;
		}
		
		angleRootToMove /= 180f;
		
		directionOut = angleRootToMove * directionSpeed;
	
	}
	
	public bool IsInLocomotion(){
		return stateInfo.nameHash == m_LocomotionId;	
	}
	
	public bool IsInPivot(){
		return (stateInfo.nameHash == m_LocomotionPivotLId) || 
			(stateInfo.nameHash == m_LocomotionPivotRId) ||
			(stateInfo.nameHash == m_LocomotionPivotLTransId) ||
			(stateInfo.nameHash == m_LocomotionPivotRTransId);	
	}
	
	#endregion
}
