using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class MovementController : MonoBehaviour {
	Rigidbody rb;
	BehaviourController behaviourController;
	GameObject level;
	
	bool freeze = true;
	public bool Freeze {
		get { return freeze; }
		set { freeze = value; }
	} 

	//=======================================================
	//                                                       
	//  ######  ##      ##  #####    ###    ##  ##   ####  
	//    ##    ##      ##  ##      ## ##   ## ##   ##     
	//    ##    ##  ##  ##  #####  ##   ##  ####     ###   
	//    ##    ##  ##  ##  ##     #######  ## ##      ##  
	//    ##     ###  ###   #####  ##   ##  ##  ##  ####   
	//                                                       
	//=======================================================

	//
	// ─── ACCELERATION ───────────────────────────────────────────────────────────────
	//

	bool accelerate = true;
	public bool Accelerate { 
		get { return accelerate; }
		set { accelerate = value; }
	}

	bool decelerate = false;
	public bool Decelerate {
		get { return decelerate; }
		set { decelerate = value; }
	}
	
	float initialVelocity = 0f;
	float currentVelocity = 0f;

	[Header("Acceleration Tweaks")]
	[Space]

	[SerializeField][Range(0, 100)]
	float maxVelocity = 20f;
	[SerializeField][Range(0, 10)]
	float accelerationRate = 1f;
	[SerializeField][Range(0, 10)]
	float decelerationRate = 0.5f;

	public float InitialVelocity { get; private set; }
	public float MaxVelocity { get; private set; }
	public float CurrentVelocity { get; private set;}

	// ────────────────────────────────────────────────────────────────────────────────


	//
	// ─── STEERING ───────────────────────────────────────────────────────────────────
	//
		
	public enum SteerDirection { Neutral, Left, Right }
	SteerDirection steer = SteerDirection.Neutral;
	public SteerDirection Steer {
		get { return steer; }
		set { steer = value; }
	}

	float initialSteering = 0f;
	float currentSteering = 0f;

	[Space]
	[Header("Steering Tweaks")]
	[Space]

	[SerializeField][Range(0, 10)]
	float maxSteering = 6f;
	[SerializeField][Range(0, 1)]
	float steeringRate = 0.6f;
	[SerializeField][Range(0, 1)]
	float steeringReturnRate = 0.3f;

	// public float InitialSteering { get; private set; }
	// public float MaxSteering { get; private set; }
	// public float CurrentSteering { get; private set;}

	// ────────────────────────────────────────────────────────────────────────────────


	//
	// ─── JUMP ───────────────────────────────────────────────────────────────────────
	//
		
	public enum JumpStatus { Idle, Jumping, Falling }
	JumpStatus jump = JumpStatus.Idle;
	public JumpStatus Jump {
		get { return jump; }
		set { jump = value; }
	}

	bool isGrounded;
	public bool IsGrounded {
		get { return isGrounded; }
	}

	int jumps = 0;
	public int Jumps {
		get { return jumps; }
		set { jumps = value; }
	}

	// float initialJumpSpeed = 0f;
	// float currentJumpSpeed = 0f;
	float groundHeight = 0f;
	float currentJumpHeight = 0f;

	[Space]
	[Header("Jump Tweaks")]
	[Space]

	[SerializeField][Range(0, 5)]
	int maxJumps = 1;
	// [SerializeField][Range(0, 100)]
	// float maxJumpSpeed = 30f;
	[SerializeField][Range(0, 1000)]
	float jumpStrength = 600f;
	[SerializeField][Range(-100, 0)]
	float gravity = -35f;
	[SerializeField][Range(0, 5)]
	float maxJumpHeight = 0.1f;
	
	// ────────────────────────────────────────────────────────────────────────────────


	//=============================================================================
	//                                                                             
	//  #####  ##   ##  ##     ##   ####  ######  ##   #####   ##     ##   ####  
	//  ##     ##   ##  ####   ##  ##       ##    ##  ##   ##  ####   ##  ##     
	//  #####  ##   ##  ##  ## ##  ##       ##    ##  ##   ##  ##  ## ##   ###   
	//  ##     ##   ##  ##    ###  ##       ##    ##  ##   ##  ##    ###     ##  
	//  ##      #####   ##     ##   ####    ##    ##   #####   ##     ##  ####   
	//                                                                             
	//=============================================================================
	
	bool checkGrounded() {
	    RaycastHit hit;
	    float distance = transform.localScale.y/2;
	    Vector3 dir = new Vector3(0, -1);

	    if (Physics.Raycast(transform.position, dir, out hit, distance)) {
	      return true;
	    } else {
	      return false;
	    }
	  }

	public float GetVelocity() {
		return currentVelocity;
	}

	//=============================================
	//                                             
	//   ####  ######    ###    #####    ######  
	//  ##       ##     ## ##   ##  ##     ##    
	//   ###     ##    ##   ##  #####      ##    
	//     ##    ##    #######  ##  ##     ##    
	//  ####     ##    ##   ##  ##   ##    ##    
	//                                             
	//=============================================

	void Start() {
		rb = GetComponent<Rigidbody>();
		behaviourController = GetComponent<BehaviourController>();
		level = GameObject.Find("Level");
	}


	//=====================================================
	//                                                     
	//  ##   ##  #####   ####      ###    ######  #####  
	//  ##   ##  ##  ##  ##  ##   ## ##     ##    ##     
	//  ##   ##  #####   ##  ##  ##   ##    ##    #####  
	//  ##   ##  ##      ##  ##  #######    ##    ##     
	//   #####   ##      ####    ##   ##    ##    #####  
	//                                                     
	//=====================================================

	void FixedUpdate() {
		if (freeze) { return; }

		isGrounded = checkGrounded();

		if (false) {
			decelerate = Input.GetKey("s") ? true : false;
			if (Input.GetKey("a")) {
				steer = SteerDirection.Left;
			} else if (Input.GetKey("d")) {
				steer = SteerDirection.Right;
			} else {
				steer = SteerDirection.Neutral;
			}
			if (Input.GetKey("space")) {
				jump = JumpStatus.Jumping;
			}
		}

		//
		// ─── ACCELERATION ────────────────────────────────────────────────
		//
			
		if (decelerate) {
      currentVelocity -= decelerationRate;
    } else if (accelerate) {
      currentVelocity += accelerationRate;
    }

		currentVelocity = Mathf.Clamp (currentVelocity, initialVelocity, maxVelocity);
		level.transform.Translate(new Vector3(0, 0, -currentVelocity*Time.deltaTime));

		// ─────────────────────────────────────────────────────────────────


		//
		// ─── STEERING ────────────────────────────────────────────────────
		//
			
		if (steer == SteerDirection.Left) {
			currentSteering -= steeringRate;
			currentSteering = Mathf.Clamp (currentSteering, -maxSteering, initialSteering);
		} else if (steer == SteerDirection.Right) {
			currentSteering += steeringRate;
			currentSteering = Mathf.Clamp (currentSteering, initialSteering, maxSteering);
		} else {
			if (currentSteering < 0) {
				currentSteering += steeringReturnRate;
				currentSteering = Mathf.Clamp (currentSteering, -maxSteering, initialSteering);
			} else if (currentSteering > 0) {
				currentSteering -= steeringReturnRate;
				currentSteering = Mathf.Clamp (currentSteering, initialSteering, maxSteering);
			}
		}

		transform.Translate(Vector3.right * currentSteering*Time.deltaTime);

		// ─────────────────────────────────────────────────────────────────


		//
		// ─── JUMP ────────────────────────────────────────────────────────
		//

		// if (jump == JumpStatus.Jumping) {
		// 	currentJumpHeight = transform.position.y - groundHeight;
		// 	if (currentJumpHeight+jumpStrength*Time.deltaTime <= maxJumpHeight*jumps && jumps <= maxJumps) {
		// 		currentJumpSpeed += jumpStrength;
		// 		currentJumpSpeed = Mathf.Clamp(currentJumpSpeed, initialJumpSpeed, maxJumpSpeed);
		// 	} else {
		// 		jump = JumpStatus.Falling;
		// 	}
		// } else if (jump == JumpStatus.Falling) {
		// 	currentJumpSpeed = 0;
		// 	if (isGrounded) {
		// 		jump = JumpStatus.Idle;
		// 	} else {
		// 		currentJumpSpeed -= gravity;
		// 	}
		// } else {
		// 	jumps = 0;
		// 	groundHeight = transform.position.y;
		// }

		if (jump == JumpStatus.Jumping) {
			currentJumpHeight = transform.position.y - groundHeight;
			if (currentJumpHeight+rb.velocity.y*Time.deltaTime <= maxJumpHeight*maxJumps && jumps <= maxJumps) {
				rb.velocity = Vector3.zero;
				rb.AddForce(0, jumpStrength*Time.deltaTime, 0, ForceMode.Impulse);
			} else {
				jump = JumpStatus.Falling;
			}
		} else if (jump == JumpStatus.Falling) {
			if (isGrounded) {
				jump = JumpStatus.Idle;
			} else {
				groundHeight = -1000;
			}
		} else {
			jumps = 0;
			groundHeight = transform.position.y;
		}

		if (!isGrounded) {
			if (jump != JumpStatus.Jumping) {
				// jump = JumpStatus.Falling;
				rb.AddForce(0, gravity, 0, ForceMode.Force);
			}
		}

		// ─────────────────────────────────────────────────────────────────
		
	}
}
