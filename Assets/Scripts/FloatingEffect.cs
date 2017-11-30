using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class FloatingEffect : MonoBehaviour {
  float x = 0f;
  float y = 0f;
  float z = 0f;

  void Start() {
    x = Random.Range(-0.3f, 0.3f);
    y = Random.Range(-0.3f, 0.3f);
    z = Random.Range(-0.1f, 0.1f);
  }

	void Update() {
		transform.Rotate(new Vector3(x, y, z) * Time.deltaTime);
	}
}
