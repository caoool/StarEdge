using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.iOS;

public class AdaptiveScreenController : MonoBehaviour {

	public RectTransform menu;
	public RectTransform control;
	public RectTransform info;

	void Start () {
		if (Device.generation == DeviceGeneration.iPhoneUnknown) {
			menu.offsetMax = new Vector2 (0, -64);
			menu.offsetMin = new Vector2 (0, 64);
			control.offsetMax = new Vector2 (0, -64);
			control.offsetMin = new Vector2 (0, 64);
			info.offsetMax = new Vector2 (0, -64);
			info.offsetMin = new Vector2 (0, 64);
		}
	}
	
	// Updatead is called once per frame
	void Update () {
		
	}
}
