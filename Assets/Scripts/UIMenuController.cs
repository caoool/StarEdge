using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.EventSystems;

public class UIMenuController : MonoBehaviour {
	EventTrigger startTrigger;
	GameManagerScript gameManagerScript;

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

	void onStartTrigger(BaseEventData eventData) {
		gameManagerScript.startGame ();
	}

	void Start () {
		startTrigger = GameObject.Find("StartButton").GetComponent<EventTrigger>();
		gameManagerScript = GameObject.Find("GameManager").GetComponent<GameManagerScript>();

		AddEventTriggerListener(
			startTrigger,
			EventTriggerType.PointerClick,
			onStartTrigger
		);
	}
}
