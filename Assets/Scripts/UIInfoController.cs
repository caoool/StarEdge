using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class UIInfoController : MonoBehaviour {

	float speed;
	int distance;

	Text speedDisplay;
	Text distanceDisplay;
	GameObject level;
	MovementController movementController;


	void Start () {
		speedDisplay = GameObject.Find ("SpeedDisplay").GetComponent<Text> ();
		distanceDisplay = GameObject.Find ("DistanceDisplay").GetComponent<Text> ();
		level = GameObject.Find ("Level");
		movementController = GameObject.Find ("Player").GetComponent<MovementController> ();
	}

	void Update () {
		speedDisplay.text = movementController.GetVelocity ().ToString("F2");
		distanceDisplay.text = Mathf.Abs(level.transform.position.z).ToString("F0");
	}
}
