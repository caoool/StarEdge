using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class BehaviourController : MonoBehaviour {
	MovementController movementController;

	bool started = false;
	public bool Started {
		get { return started; }
		set { started = value; }
	}

	bool lost = false;
	public bool Lost {
		get { return lost; }
		set { lost = value; }
	}

	public void init() {
		lost = false;
		transform.position = new Vector3(0, 1, 1);
		transform.Find("ShipMesh").gameObject.SetActive(true);
		// transform.Find("ParticleFollow").gameObject.SetActive(true);
		movementController.Freeze = false;
	}

	void lose() {
		if (!lost) {
			lost = true;
			BroadcastMessage("Explode");
			transform.Find("ShipMesh").gameObject.SetActive(false);
			// transform.Find("ParticleFollow").gameObject.SetActive(false);
			movementController.Freeze = true;
		}
	}

	void Start() {
		movementController = GetComponent<MovementController>();
	}

	void LateUpdate() {
		if (transform.position.y < -10) {
			lose();
		}
	}

	void OnTriggerEnter(Collider other) {
		lose();
	}
}
