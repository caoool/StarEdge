using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class LevelGenerator : MonoBehaviour {

	[SerializeField]
	GameObject Plane; 

	float initPosition = 400;
	float currentPosition = 0;
	bool building = false;

	public void init() {
		destructor();
		transform.position = Vector3.zero;
		currentPosition = 0;
		building = false;
	}

	void destructor() {
		GameObject[] generated = GameObject.FindGameObjectsWithTag("Generated");
		foreach (GameObject item in generated) {
			GameObject.Destroy(item);
		}
	}

	void Start() {
		// init();
	}
	
	void Update () {

		if (building) { return; }

		if (Mathf.Abs(transform.position.z) > currentPosition) {

			building = true;

			GameObject plane = Instantiate(
				Plane,
				Vector3.zero,
				Quaternion.identity
			) as GameObject;
			
			plane.gameObject.tag = "Generated";
			plane.transform.parent = transform;
			plane.transform.localScale = new Vector3(
				UnityEngine.Random.Range(1, 5),
				UnityEngine.Random.Range(1, 2),
				UnityEngine.Random.Range(15, 35)
			);

			plane.transform.Rotate(
				UnityEngine.Random.Range(-3f, 3f),
				UnityEngine.Random.Range(-8f, 8f),
				UnityEngine.Random.Range(-5f, 5f)
			);

			float gap = UnityEngine.Random.Range(3f, 5f);
			plane.transform.position = new Vector3(
				UnityEngine.Random.Range(-3, 3),
				UnityEngine.Random.Range(-2, 1),
				initPosition + currentPosition + gap + plane.transform.localScale.z / 2
			);

			currentPosition += gap + plane.transform.localScale.z / 2;
			building = false;
		}
	}
}
