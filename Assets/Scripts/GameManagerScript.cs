using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class GameManagerScript : MonoBehaviour {

	public enum GameStatus { Idle, Play, Pause, Restart }
	GameStatus status = GameStatus.Idle;
	public GameStatus Status {
		get { return status; }
		set { status = value; }
	}

	GameObject player;
	GameObject level;
	BehaviourController playerBehaviorController;
	LevelGenerator levelGenerator;
	GameObject uiMenu;
	Text title;
	Text score;
	GameObject uiControl;
	GameObject uiInfo;

	bool waitActive = false;

	public void startGame () {
		levelGenerator.init();
		playerBehaviorController.init();
		status = GameStatus.Play;
		uiManager ();
	}

	void uiManager() {
		switch (status) {
		case GameStatus.Idle:
			uiMenu.SetActive (true);
			uiControl.SetActive (false);
//			uiInfo.SetActive (false);
			break;
		case GameStatus.Play:
			uiMenu.SetActive (false);
			uiControl.SetActive (true);
//			uiInfo.SetActive (true);
			break;
		case GameStatus.Pause:
			uiMenu.SetActive (true);
			uiControl.SetActive (false);
//			uiInfo.SetActive (true);
			break;
		case GameStatus.Restart:
			uiMenu.SetActive (true);
			uiControl.SetActive (false);
//			uiInfo.SetActive (false);
			title.text = "";
			score.text = "SCORE\n" + Mathf.Abs (level.transform.position.z).ToString ("F0");
			break;
		default:
			break;
		}
	}

	void Awake() {
		Application.targetFrameRate = 300;
	}

	void Start () {
		player = GameObject.Find("Player");
		level = GameObject.Find ("Level");
		playerBehaviorController = player.GetComponent<BehaviourController>();
		levelGenerator = GameObject.Find("Level").GetComponent<LevelGenerator>();
		uiMenu = GameObject.Find("Menu");
		title = GameObject.Find ("Title").GetComponent<Text> ();
		score = GameObject.Find ("Score").GetComponent<Text> ();
		uiControl = GameObject.Find("Control");
		uiInfo = GameObject.Find ("Info");
		uiManager ();
	}
	
	void Update () {
		if (status == GameStatus.Play &&
			playerBehaviorController.Lost &&
			!waitActive) {
			StartCoroutine (RestartDelay ());
		}
	}

	IEnumerator RestartDelay () {
		waitActive = true;
		yield return new WaitForSeconds (3.0f);
		waitActive = false;
		status = GameStatus.Restart;
		uiManager ();
	}
}
