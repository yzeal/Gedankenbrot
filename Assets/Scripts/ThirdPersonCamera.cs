using UnityEngine;
using System.Collections;

struct CameraPosition{
	
	private Vector3 position;
	
	private Transform xForm;
	
	public Vector3 Position{ get {return position;} set {position = value;} }
	public Transform XForm{ get {return xForm;} set{xForm = value;} }
	
	public void Init(string camName, Vector3 pos, Transform transform, Transform parent){
		position = pos;
		xForm = transform;
		xForm.name = camName;
		xForm.parent = parent;
		xForm.localPosition = Vector3.zero;
		xForm.localPosition = position;
	}
}

public class ThirdPersonCamera : MonoBehaviour {
	
	#region Variables (private)
	
	[SerializeField]
	private float distanceAway;
	[SerializeField]
	private Transform parentRig;
	[SerializeField]
	private float distanceAwayMultiplier = 1.5f;
	[SerializeField]
	private float distanceUp;
	[SerializeField]
	private float distanceUpMultiplier = 5f;
	[SerializeField]
	private float smooth;
	[SerializeField]
	private CharacterControllerLogic follow;
	[SerializeField]
	private Transform followXform;
	[SerializeField]
	private float targetingTime = 0.5f;
	[SerializeField]
	private float firstPersonTreshold = 0.5f;
	[SerializeField]
	private float freeTreshold = -0.1f;
	[SerializeField]
	private float firstPersonLookSpeed = 1.5f;
	[SerializeField]
	private Vector2 firstPersonXAxisClamp = new Vector2(-70.0f, 60.0f);
	[SerializeField]
	private float fPSRotationDegreesPerSecond = 120.0f;
	[SerializeField]
	private Vector2 camMinDistFromChar = new Vector2(1f, -0.5f);
	[SerializeField]
	private float rightStickThreshold = 0.1f;
	[SerializeField]
	private const float freeRotationDegreesPerSecond = -5f;
	
	private Vector3 lookDir;
	private Vector3 curLookDir;
	private Vector3 targetPosition;
	private CameraPosition firstPersonCamPos;
	private float xAxisRot = 0.0f;
	private float lookWeight;
	private const float TARGETING_TRESHOLD = 0.01f;
	private CamStates camState = CamStates.Behind;
	private Vector3 savedRigToGoal;
	private float distanceAwayFree;
	private float distanceUpFree;
	private Vector2 rightStickPrevFrame = Vector2.zero;
	
	//Smoothing and damping:
	private Vector3 velocityCamSmooth = Vector3.zero;
	[SerializeField]
	private float camSmoothDampTime = 0.1f;
	private Vector3 velocityLookDir = Vector3.zero;
	[SerializeField]
	private float lookDirDampTime = 0.1f;
	
	
	
	#endregion
	
	#region Properties (public)

	public Transform ParentRig {
		get {
			return this.parentRig;
		}
	}

	public Vector3 LookDir {
		get {
			return this.lookDir;
		}
	}	
	public CamStates CamState {
		get {
			return this.camState;
		}
	}

	
	public enum CamStates{
		Behind, FirstPerson, Target, Free	
	}
	
	#endregion

	// Use this for initialization
	void Start () {
		
		parentRig = this.transform.parent;
		
		if(parentRig == null){
			Debug.Log("Parent camera to empty Game Object.");
		}
		
		follow = GameObject.FindWithTag("Player").GetComponent<CharacterControllerLogic>();
		followXform = GameObject.FindWithTag("Player").transform;
		lookDir = followXform.forward;
		curLookDir = followXform.forward;
		lookWeight = 0.0f;
		
		firstPersonCamPos = new CameraPosition();
		firstPersonCamPos.Init(
			"First Person Camera",
			new Vector3(0.0f, 1.6f, 0.2f),
			new GameObject().transform,
			followXform
		);
	}
	
	// Update is called once per frame
	void Update () {
	
	}
	
	void LateUpdate(){
		
		float rightX = Input.GetAxis("RightStickX");
		float rightY = Input.GetAxis("RightStickY");
		float leftX = Input.GetAxis("Horizontal");
		float leftY = Input.GetAxis("Vertical");
		
		Vector3 characterOffset = followXform.position + new Vector3(0f, distanceUp, 0f);
		Vector3 lookAt = characterOffset;
		targetPosition = Vector3.zero;
		
		//Determine camera state:
		//Target
		if(Input.GetAxis("Target") > TARGETING_TRESHOLD){
			
			camState = CamStates.Target;
			
		}else{
			
			//First Person
			if(rightY > firstPersonTreshold && camState != CamStates.Free && !follow.IsInLocomotion()){
				
				xAxisRot = 0;
				lookWeight = 0.0f;
				if(!follow.IsInPivot()){
					camState = CamStates.FirstPerson;
				}
				
			}
			
			//Free Camera
			if(rightY < freeTreshold && System.Math.Round(follow.Speed, 2) == 0){
				camState = CamStates.Free;
				savedRigToGoal = Vector3.zero;
			}
			
			//Behind
			if((camState == CamStates.FirstPerson && Input.GetButton("ExitFPV")) || 
			   (camState == CamStates.Target && (Input.GetAxis("Target") <= TARGETING_TRESHOLD))){
			
				camState = CamStates.Behind;
				
			}
			
		}
		
		follow.Animator.SetLookAtWeight(lookWeight);
		
		//Execute camera state:
		switch(camState){
			
			case CamStates.Behind:
				ResetCamera();
				if(follow.Speed > follow.LocomotionThreshold && follow.IsInLocomotion() && !follow.IsInPivot()){
					lookDir = Vector3.Lerp(followXform.right * (leftX < 0 ? 1f : -1f), followXform.forward * (leftY < 0 ? -1f : 1f), Mathf.Abs(Vector3.Dot(this.transform.forward, followXform.forward)));
					curLookDir = Vector3.Normalize(characterOffset - this.transform.position);
					curLookDir.y = 0;
					curLookDir = Vector3.SmoothDamp(curLookDir, lookDir, ref velocityLookDir, lookDirDampTime);
				}
				targetPosition = characterOffset + followXform.up * distanceUp - Vector3.Normalize(curLookDir) * distanceAway;
				break;
			case CamStates.Target:
				ResetCamera();
				lookDir = followXform.forward;
				curLookDir = followXform.forward;
				targetPosition = characterOffset + followXform.up * distanceUp - lookDir * distanceAway;
				break;
			case CamStates.FirstPerson:
				//Looking up and down
				xAxisRot += leftY * firstPersonLookSpeed;
				xAxisRot = Mathf.Clamp(xAxisRot, firstPersonXAxisClamp.x, firstPersonXAxisClamp.y);
				firstPersonCamPos.XForm.localRotation = Quaternion.Euler(xAxisRot, 0, 0);
				Quaternion rotationShift = Quaternion.FromToRotation(this.transform.forward, firstPersonCamPos.XForm.forward);
				this.transform.rotation = rotationShift * this.transform.rotation;
				
				//Move character's head:
				follow.Animator.SetLookAtPosition(firstPersonCamPos.XForm.position + firstPersonCamPos.XForm.forward);
				lookWeight = Mathf.Lerp(lookWeight, 1.0f, Time.deltaTime * firstPersonLookSpeed);
				
				//Looking left and right:
				Vector3 rotationAmount = Vector3.Lerp (Vector3.zero, new Vector3(0f, fPSRotationDegreesPerSecond * (leftX < 0f ? -1f : 1f), 0f), Mathf.Abs(leftX));
				Quaternion deltaRotation = Quaternion.Euler(rotationAmount * Time.deltaTime);
				follow.transform.rotation = follow.transform.rotation * deltaRotation;
				targetPosition = firstPersonCamPos.XForm.position;
				lookAt = Vector3.Lerp(targetPosition + followXform.forward, this.transform.position + this.transform.forward, camSmoothDampTime * Time.deltaTime);
				lookAt = Vector3.Lerp(this.transform.position + this.transform.forward, lookAt, Vector3.Distance(this.transform.position, firstPersonCamPos.XForm.position));
				break;
			case CamStates.Free:
				lookWeight = Mathf.Lerp(lookWeight, 0.0f, Time.deltaTime * firstPersonLookSpeed);
				Vector3 rigToGoalDirection = Vector3.Normalize(characterOffset - this.transform.position);
				rigToGoalDirection.y = 0f;
				Vector3 rigToGoal = characterOffset - parentRig.position;
				rigToGoal.y = 0f;	
			
				//Moving camera in and out
				if(rightY < -1f * rightStickThreshold && rightY <= rightStickPrevFrame.y && Mathf.Abs(rightX) < rightStickThreshold){
					distanceUpFree = Mathf.Lerp(distanceUp, distanceUp * distanceUpMultiplier, Mathf.Abs (rightY));
					distanceAwayFree = Mathf.Lerp(distanceAway, distanceAway * distanceAwayMultiplier, Mathf.Abs(rightY));
					targetPosition = characterOffset + followXform.up * distanceUpFree - rigToGoalDirection * distanceAwayFree;
				}else if(rightY > rightStickThreshold && rightY >= rightStickPrevFrame.y && Mathf.Abs(rightX) < rightStickThreshold){
					distanceUpFree = Mathf.Lerp(Mathf.Abs(transform.position.y - characterOffset.y), camMinDistFromChar.y, rightY);
					distanceAwayFree = Mathf.Lerp(rigToGoal.magnitude, camMinDistFromChar.x, rightY);
					targetPosition = characterOffset + followXform.up * distanceUpFree - rigToGoalDirection * distanceAwayFree;
				}
			
				if(rightX != 0 || rightY != 0){
					savedRigToGoal = rigToGoalDirection;
				}
			
				parentRig.RotateAround(characterOffset, followXform.up, freeRotationDegreesPerSecond * (Mathf.Abs(rightX) > rightStickThreshold ? rightX : 0f));
			
				if(targetPosition == Vector3.zero){
					targetPosition = characterOffset + followXform.up * distanceUpFree - savedRigToGoal * distanceAwayFree;
				}
			
				//smoothPosition(transform.position, targetPosition);
				//transform.LookAt(lookAt);
			
				break;
			
		}
		
		
		//if(camState != CamStates.Free){
			CompensateForWalls (characterOffset, ref targetPosition);
			
			smoothPosition(this.transform.position, targetPosition);
			
			transform.LookAt(lookAt);
		//}
		
		rightStickPrevFrame = new Vector2(rightX, rightY);
	}
	
	#region Methods
	
	private void smoothPosition(Vector3 fromPos, Vector3 toPos){
		
		this.transform.position = Vector3.SmoothDamp(fromPos, toPos, ref velocityCamSmooth, camSmoothDampTime);
	}
	
	private void CompensateForWalls(Vector3 fromObject, ref Vector3 toTarget){
		
		RaycastHit wallHit = new RaycastHit();
		if(Physics.Linecast (fromObject, toTarget, out wallHit)){
			toTarget = new Vector3(wallHit.point.x, toTarget.y, wallHit.point.z);
		}
	}
	
	private void ResetCamera(){
		
		lookWeight = Mathf.Lerp(lookWeight, 0.0f, Time.deltaTime * firstPersonLookSpeed);
		transform.localRotation = Quaternion.Lerp(transform.localRotation, Quaternion.identity, Time.deltaTime);
	}
	
	#endregion
	
	
}
