using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.iOS;

public class AdaptiveScreenController : MonoBehaviour {

	void Start () {
		RectTransform rt = GetComponent<RectTransform> ();

		if (Device.generation == DeviceGeneration.iPhoneUnknown) {
			rt.offsetMax = new Vector2 (0, -80);
			rt.offsetMin = new Vector2 (0, 100);
		}
	}
	
	// Updatead is called once per frame
	void Update () {
		
	}
}
