using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class CameraFollowPlayer : MonoBehaviour {

	GameObject player;
	Vector3 offset;
	float speed = 5f;

	void Start() {
		player = GameObject.Find("Player");
		offset = transform.position - player.transform.position;
	}

	void LateUpdate() {
		float interpolation = speed * Time.deltaTime;

		Vector3 newPosition = new Vector3(
			// Mathf.Lerp(transform.position.x,
			// 	player.transform.position.x,
			// 	interpolation),
			transform.position.x,
			transform.position.y,
			player.transform.position.z + offset.z
		);
		transform.position = newPosition;
	}
}
