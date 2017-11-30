using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class LevelDestructor : MonoBehaviour {
	void OnTriggerEnter(Collider other) {
		if (other.gameObject.tag != "Base") {
			Destroy(other.gameObject);
		}
	}
}
