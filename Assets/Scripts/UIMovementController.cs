using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.EventSystems;

public class UIMovementController : MonoBehaviour {
	public EventTrigger breakTrigger;
	public EventTrigger jumpTrigger;
	public EventTrigger leftTrigger;
	public EventTrigger rightTrigger;

	public GameObject player;
	public MovementController movementController;

	void AddEventTriggerListener(
		EventTrigger trigger,
		EventTriggerType eventType,
		System.Action<BaseEventData> callback
	) {
		EventTrigger.Entry entry = new EventTrigger.Entry();
		entry.eventID = eventType;
		entry.callback = new EventTrigger.TriggerEvent();
		entry.callback.AddListener(new UnityEngine.Events.UnityAction<BaseEventData>(callback));
		trigger.triggers.Add(entry);
	}

	void onBreakDown(BaseEventData eventData) {
		movementController.Decelerate = true;
	}

	void onBreakUp(BaseEventData eventData) {
		movementController.Decelerate = false;
	}

	void onLeftDown(BaseEventData eventData) {
		movementController.Steer = MovementController.SteerDirection.Left;
	}

	void onLeftUp(BaseEventData eventData) {
		movementController.Steer = MovementController.SteerDirection.Neutral;
	}

	void onRightDown(BaseEventData eventData) {
		movementController.Steer = MovementController.SteerDirection.Right;
	}

	void onRightUp(BaseEventData eventData) {
		movementController.Steer = MovementController.SteerDirection.Neutral;
	}

	void onJumpDown(BaseEventData eventData) {
		movementController.Jump = MovementController.JumpStatus.Jumping;
		movementController.Jumps += 1;
	}

	void onJumpUp(BaseEventData eventData) {
		movementController.Jump = MovementController.JumpStatus.Falling;
	}

	void Start () {
//		breakTrigger = GameObject.Find("BreakButton").GetComponent<EventTrigger>();
//		leftTrigger = GameObject.Find("LeftButton").GetComponent<EventTrigger>();
//		rightTrigger = GameObject.Find("RightButton").GetComponent<EventTrigger>();
//		jumpTrigger = GameObject.Find("JumpButton").GetComponent<EventTrigger>();
//
//		movementController = GameObject.Find("Player").GetComponentInChildren<MovementController>();

		AddEventTriggerListener(
			breakTrigger,
			EventTriggerType.PointerDown,
			onBreakDown
		);

		AddEventTriggerListener(
			breakTrigger,
			EventTriggerType.PointerUp,
			onBreakUp
		);

		AddEventTriggerListener(
			leftTrigger,
			EventTriggerType.PointerDown,
			onLeftDown
		);

		AddEventTriggerListener(
			leftTrigger,
			EventTriggerType.PointerUp,
			onLeftUp
		);

		AddEventTriggerListener(
			rightTrigger,
			EventTriggerType.PointerDown,
			onRightDown
		);

		AddEventTriggerListener(
			rightTrigger,
			EventTriggerType.PointerUp,
			onRightUp
		);

		AddEventTriggerListener(
			jumpTrigger,
			EventTriggerType.PointerDown,
			onJumpDown
		);

		AddEventTriggerListener(
			jumpTrigger,
			EventTriggerType.PointerUp,
			onJumpUp
		);
	}
}
