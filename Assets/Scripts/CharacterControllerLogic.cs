/// <summary>
/// UnityTutorials - A Unity Game Design Prototyping Sandbox
/// <copyright>(c) John McElmurray and Julian Adams 2013</copyright>
/// 
/// UnityTutorials homepage: https://github.com/jm991/UnityTutorials
/// 
/// This software is provided 'as-is', without any express or implied
/// warranty.  In no event will the authors be held liable for any damages
/// arising from the use of this software.
///
/// Permission is granted to anyone to use this software for any purpose,
/// and to alter it and redistribute it freely, subject to the following restrictions:
///
/// 1. The origin of this software must not be misrepresented; you must not
/// claim that you wrote the original software. If you use this software
/// in a product, an acknowledgment in the product documentation would be
/// appreciated but is not required.
/// 2. Altered source versions must be plainly marked as such, and must not be
/// misrepresented as being the original software.
/// 3. This notice may not be removed or altered from any source distribution.
/// </summary>

using UnityEngine;
using System.Collections;

/// <summary>
/// #DESCRIPTION OF CLASS#
/// </summary>
public class CharacterControllerLogic : MonoBehaviour 
{
	#region Variables (private)
	
	// Inspector serialized
	[SerializeField]
	private Animator animator;
	[SerializeField]
	private ThirdPersonCamera gamecam;
	[SerializeField]
	private float rotationDegreePerSecond = 120f;
	[SerializeField]
	private float directionSpeed = 1.5f;
	[SerializeField]
	private float directionDampTime = 0.25f;
	[SerializeField]
	private float speedDampTime = 0.05f;
	[SerializeField]
	private float fovDampTime = 3f;
	[SerializeField]
	private float jumpMultiplier = 1f;
	[SerializeField]
	private CapsuleCollider capCollider;
	[SerializeField]
	private float jumpDist = 1f;
	
	
	// Private global only
	private float leftX = 0f;
	private float leftY = 0f;
	private AnimatorStateInfo stateInfo;
	private AnimatorTransitionInfo transInfo;
	private float speed = 0f;
	private float direction = 0f;
	private float charAngle = 0f;
	private const float SPRINT_SPEED = 2.0f;	
	private const float SPRINT_FOV = 75.0f;
	private const float NORMAL_FOV = 60.0f;
	private float capsuleHeight;	
	private bool swim;
	private float waterHeight = 0f;
	private float fallHeight = 1f;
	
	
	// Hashes
    private int m_LocomotionId = 0;
	private int m_LocomotionPivotLId = 0;
	private int m_LocomotionPivotRId = 0;	
	private int m_SwimIdleId = 0;
	private int m_SwimLocomotionId = 0;
	private int m_LocomotionPivotLTransId = 0;	
	private int m_LocomotionPivotRTransId = 0;
	private int m_FallId = 0;
	
	#endregion
		
	
	#region Properties (public)

	public Animator Animator
	{
		get
		{
			return this.animator;
		}
	}

	public float Speed
	{
		get
		{
			return this.speed;
		}
	}
	
	public float LocomotionThreshold { get { return 0.2f; } }
	
	#endregion
	
	
	#region Unity event functions
	
	/// <summary>
	/// Use this for initialization.
	/// </summary>
	void Start() 
	{
		animator = GetComponent<Animator>();
		capCollider = GetComponent<CapsuleCollider>();
		capsuleHeight = capCollider.height;

		if(animator.layerCount >= 2)
		{
			animator.SetLayerWeight(1, 1);
		}		
		
		// Hash all animation names for performance
        m_LocomotionId = Animator.StringToHash("Base Layer.Locomotion");
		m_LocomotionPivotLId = Animator.StringToHash("Base Layer.LocomotionPivotL");
		m_LocomotionPivotRId = Animator.StringToHash("Base Layer.LocomotionPivotR");
		m_SwimIdleId = Animator.StringToHash("Base Layer.SwimIdle");
		m_SwimLocomotionId = Animator.StringToHash("Base Layer.SwimLocomotion");
		m_LocomotionPivotLTransId = Animator.StringToHash("Base Layer.Locomotion -> Base Layer.LocomotionPivotL");
		m_LocomotionPivotRTransId = Animator.StringToHash("Base Layer.Locomotion -> Base Layer.LocomotionPivotR");
		m_FallId = Animator.StringToHash("Base Layer.Fall");
	}
	
	/// <summary>
	/// Update is called once per frame.
	/// </summary>
	void Update() 
	{
		if (animator && gamecam.CamState != ThirdPersonCamera.CamStates.FirstPerson)
		{
			stateInfo = animator.GetCurrentAnimatorStateInfo(0);
			transInfo = animator.GetAnimatorTransitionInfo(0);

			// Enter water to swim
			if(swim){
				animator.SetBool("Swim", true);

			}else{
				animator.SetBool("Swim", false);
			}
			
			// Press A to jump
			if (Input.GetButton("Jump"))
			{
				animator.SetBool("Jump", true);
			}
			else
			{
				animator.SetBool("Jump", false);
			}	

			//if(!IsInJump () && !swim && !IsInSwimming()){
			if(!swim && !IsInSwimming()){
				//Cast 9 Rays in a diamond to check if the character is grounded:
				for(int i = 0; i < 9; i++){
					RaycastHit hit = new RaycastHit();
					if(i < 5){
						Physics.Raycast(new Vector3(transform.position.x + ((i == 1) ? 0.36f : ((i == 2) ? -0.36f : 0f)), transform.position.y + ((i == 0) ? 0.5f : 0.86f), transform.position.z + ((i == 3) ? 0.36f : ((i == 4) ? -0.36f : 0f))), Vector3.down, out hit, fallHeight);
					}else{
						Physics.Raycast(new Vector3(transform.position.x + ((i == 5 || i == 7) ? -0.25f : 0.25f), transform.position.y + ((i == 0) ? 0.5f : 0.86f), transform.position.z + ((i == 7 || i == 8) ? -0.25f : 0.25f)), Vector3.down, out hit, fallHeight);
					}
					//Debug.Log(transform.position + " " + hit.collider);
					if(hit.collider == null || (hit.collider.CompareTag("Water") && (IsInFall() || IsInJump ())) || hit.collider.CompareTag("FallThrough")){
						animator.SetBool("Fall", true);
					} else{
						animator.SetBool("Fall", false);
						break;
					}
				}

			} else{
				animator.SetBool("Fall", false);
			}
			
			// Pull values from controller/keyboard
			leftX = Input.GetAxis("Horizontal");
			leftY = Input.GetAxis("Vertical");			
			
			charAngle = 0f;
			direction = 0f;	
			float charSpeed = 0f;
		
			// Translate controls stick coordinates into world/cam/character space
            StickToWorldspace(this.transform, gamecam.transform, ref direction, ref charSpeed, ref charAngle, IsInPivot());		
			
			// Press B to sprint
			if (Input.GetButton("Sprint"))
			{
				speed = Mathf.Lerp(speed, SPRINT_SPEED, Time.deltaTime);
				gamecam.camera.fieldOfView = Mathf.Lerp(gamecam.camera.fieldOfView, SPRINT_FOV, fovDampTime * Time.deltaTime);
			}
			else
			{
				speed = charSpeed;
				gamecam.camera.fieldOfView = Mathf.Lerp(gamecam.camera.fieldOfView, NORMAL_FOV, fovDampTime * Time.deltaTime);		
			}
			
			animator.SetFloat("Speed", speed, speedDampTime, Time.deltaTime);
			animator.SetFloat("Direction", direction, directionDampTime, Time.deltaTime);
			
			if (speed > LocomotionThreshold)	// Dead zone
			{
				if (!IsInPivot())
				{
					Animator.SetFloat("Angle", charAngle);
				}
			}
			if (speed < LocomotionThreshold && Mathf.Abs(leftX) < 0.05f)    // Dead zone
			{
				animator.SetFloat("Direction", 0f);
				animator.SetFloat("Angle", 0f);
			}		
		} 

	}
	
	/// <summary>
	/// Any code that moves the character needs to be checked against physics
	/// </summary>
	void FixedUpdate()
	{							
		// Rotate character model if stick is tilted right or left, but only if character is moving in that direction
		if (IsInLocomotion() && gamecam.CamState != ThirdPersonCamera.CamStates.Free && !IsInPivot() && ((direction >= 0 && leftX >= 0) || (direction < 0 && leftX < 0)))
		//if ((IsInLocomotion() || IsInJump() || IsInSwimming()) && gamecam.CamState != ThirdPersonCamera.CamStates.Free && !IsInPivot() && ((direction >= 0 && leftX >= 0) || (direction < 0 && leftX < 0)))
		{
			Vector3 rotationAmount = Vector3.Lerp(Vector3.zero, new Vector3(0f, rotationDegreePerSecond * (leftX < 0f ? -1f : 1f), 0f), Mathf.Abs(leftX));
			Quaternion deltaRotation = Quaternion.Euler(rotationAmount * Time.deltaTime);
        	this.transform.rotation = (this.transform.rotation * deltaRotation);
		}		
		
		if (IsInJump() || IsInFall())
		{
			float oldY = transform.position.y;
			transform.Translate(Vector3.up * jumpMultiplier * animator.GetFloat("JumpCurve"));
			
			//TESTI
			//float sp = Mathf.Sqrt(Input.GetAxis("Horizontal")*Input.GetAxis("Horizontal") + Input.GetAxis("Vertical")*Input.GetAxis("Vertical")) * jumpDist;
			//transform.Translate(Vector3.forward * Time.deltaTime * sp);
			transform.Translate(Vector3.forward * Time.deltaTime * speed * jumpDist);
			if(speed > 0.1){
				transform.Rotate(new Vector3(0f,charAngle * Time.deltaTime, 0f));				
			}
			//transform.Rotate(0, Input.GetAxis("Horizontal") * -3f, 0);
			//float charA = Input.GetAxis("Horizontal");
			//float charSpeed = 0f;
			//StickToWorldspace(this.transform, gamecam.transform, ref direction, ref charSpeed, ref charA, false);	

			/*if (IsInLocomotionJump())
			{
				transform.Translate(Vector3.forward * Time.deltaTime * jumpDist);
			}*/
			//Ende TESTI
			capCollider.height = capsuleHeight + (animator.GetFloat("CapsuleCurve") * 0.5f);
			if (gamecam.CamState != ThirdPersonCamera.CamStates.Free)
			{
				//gamecam.ParentRig.Translate(Vector3.up * (transform.position.y - oldY));
			}
		}

		if(IsInSwimming()){
			if(speed > 0.1){
				float sp = Mathf.Sqrt(Input.GetAxis("Horizontal")*Input.GetAxis("Horizontal") + Input.GetAxis("Vertical")*Input.GetAxis("Vertical")) * jumpDist;
				//transform.Translate(Vector3.forward * Time.deltaTime * speed * 3f);
				//transform.Translate(new Vector3(Vector3.forward.x * Time.deltaTime * speed * 3f, 0f, Vector3.forward.z * Time.deltaTime * speed * 3f) );
				transform.Translate(0f, 0f, Time.deltaTime * speed * 3f);
				//transform.Rotate(new Vector3(0f,charAngle * Time.deltaTime, 0f));
			}
			if (gamecam.CamState != ThirdPersonCamera.CamStates.Free && !IsInPivot() && ((direction >= 0 && leftX >= 0) || (direction < 0 && leftX < 0)))
				//if ((IsInLocomotion() || IsInJump() || IsInSwimming()) && gamecam.CamState != ThirdPersonCamera.CamStates.Free && !IsInPivot() && ((direction >= 0 && leftX >= 0) || (direction < 0 && leftX < 0)))
			{
				Vector3 rotationAmount = Vector3.Lerp(Vector3.zero, new Vector3(0f, rotationDegreePerSecond * (leftX < 0f ? -1f : 1f), 0f), Mathf.Abs(leftX));
				Quaternion deltaRotation = Quaternion.Euler(rotationAmount * Time.deltaTime);
				this.transform.rotation = (this.transform.rotation * deltaRotation);
			}
		}


	}
	
	/// <summary>
	/// Debugging information should be put here.
	/// </summary>
	void OnDrawGizmos()
	{	
	
	}

	void OnTriggerEnter(Collider other){
		if(other.CompareTag("Water")){
//			swim = true;
			animator.SetBool("Fall", false);
			rigidbody.useGravity = false;
			waterHeight = other.transform.position.y;
			//if not grounded:
			RaycastHit hit = new RaycastHit();
			Physics.Raycast(transform.position, Vector3.down,out hit, 1.31f);
			if(hit.collider == null){
				transform.position = new Vector3(transform.position.x, other.transform.position.y-1.3f, transform.position.z);
			}
		}
	}

	void OnTriggerStay(Collider other){
		if(other.CompareTag("Water")){
			swim = true;
			animator.SetBool("Fall", false);
			//if not grounded:
			RaycastHit hit = new RaycastHit();
			Physics.Raycast(transform.position, Vector3.down,out hit, 1.31f);
			if(hit.collider == null){
				transform.position = new Vector3(transform.position.x, other.transform.position.y-1.3f, transform.position.z);
			}else{
				swim = false;
			}
		}
	}

	void OnTriggerExit(Collider other){
		if(other.CompareTag("Water")){
			swim = false;
			rigidbody.useGravity = true;
		}
	}

	
	#endregion
	
	
	#region Methods
	
	public bool IsInJump()
	{
		return (IsInIdleJump() || IsInLocomotionJump());
	}

	public bool IsInIdleJump()
	{
		return animator.GetCurrentAnimatorStateInfo(0).IsName("Base Layer.IdleJump");
	}
	
	public bool IsInLocomotionJump()
	{
		return animator.GetCurrentAnimatorStateInfo(0).IsName("Base Layer.LocomotionJump");
	}
	
	public bool IsInPivot()
	{
		return stateInfo.nameHash == m_LocomotionPivotLId || 
			stateInfo.nameHash == m_LocomotionPivotRId || 
			transInfo.nameHash == m_LocomotionPivotLTransId || 
			transInfo.nameHash == m_LocomotionPivotRTransId;
	}

    public bool IsInLocomotion()
    {
        return stateInfo.nameHash == m_LocomotionId;
    }

	public bool IsInFall()
	{
		return stateInfo.nameHash == m_FallId;
	}
	
	public bool IsInSwimming()
	{
		return stateInfo.nameHash == m_SwimIdleId || stateInfo.nameHash == m_SwimLocomotionId;
	}
	
	public void StickToWorldspace(Transform root, Transform camera, ref float directionOut, ref float speedOut, ref float angleOut, bool isPivoting)
    {
        Vector3 rootDirection = root.forward;
				
        Vector3 stickDirection = new Vector3(leftX, 0, leftY);
		
		speedOut = stickDirection.sqrMagnitude;		

        // Get camera rotation
        Vector3 CameraDirection = camera.forward;
        CameraDirection.y = 0.0f; // kill Y
        Quaternion referentialShift = Quaternion.FromToRotation(Vector3.forward, Vector3.Normalize(CameraDirection));

        // Convert joystick input in Worldspace coordinates
        Vector3 moveDirection = referentialShift * stickDirection;
		Vector3 axisSign = Vector3.Cross(moveDirection, rootDirection);
		
		Debug.DrawRay(new Vector3(root.position.x, root.position.y + 2f, root.position.z), moveDirection, Color.green);
		Debug.DrawRay(new Vector3(root.position.x, root.position.y + 2f, root.position.z), rootDirection, Color.magenta);
		Debug.DrawRay(new Vector3(root.position.x, root.position.y + 2f, root.position.z), stickDirection, Color.blue);
		Debug.DrawRay(new Vector3(root.position.x, root.position.y + 2.5f, root.position.z), axisSign, Color.red);
		
		float angleRootToMove = Vector3.Angle(rootDirection, moveDirection) * (axisSign.y >= 0 ? -1f : 1f);
		if (!isPivoting)
		{
			angleOut = angleRootToMove;
		}
		angleRootToMove /= 180f;
		
		directionOut = angleRootToMove * directionSpeed;
	}	
	
	#endregion Methods
}
