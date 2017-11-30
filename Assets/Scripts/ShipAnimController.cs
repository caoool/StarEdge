using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class ShipAnimController : MonoBehaviour {

	Animator animator;
	MovementController movementController;

	void Start () {
		animator = GetComponent<Animator>();
		movementController = GetComponent<MovementController>();
	}
	
	void Update () {

		bool decelerate = movementController.Decelerate;

		switch (decelerate) {
			case true:
				animator.SetBool("Break", true);
				break;
			case false:
				animator.SetBool("Break", false);
				break;
		}

		MovementController.JumpStatus jump = movementController.Jump;

		switch (jump) {
			case MovementController.JumpStatus.Jumping:
				animator.SetBool("Jump", true);
				animator.SetBool("Fall", false);
				break;
			case MovementController.JumpStatus.Falling:
				animator.SetBool("Jump", false);
				animator.SetBool("Fall", true);
				break;
			case MovementController.JumpStatus.Idle:
				animator.SetBool("Jump", false);
				animator.SetBool("Fall", false);
				break;
		}


		MovementController.SteerDirection steer = movementController.Steer;

		switch (steer) {
			case MovementController.SteerDirection.Left:
				animator.SetBool("Left", true);
				animator.SetBool("Right", false);
				break;
			case MovementController.SteerDirection.Right:
				animator.SetBool("Left", false);
				animator.SetBool("Right", true);
				break;
			case MovementController.SteerDirection.Neutral:
				animator.SetBool("Left", false);
				animator.SetBool("Right", false);
				break;
		}
	}
}
