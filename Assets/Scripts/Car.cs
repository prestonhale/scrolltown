using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Car : MonoBehaviour {
	
	[Range(0, 3)]
	public float speed;
	public int direction;

	public Car(Quaternion rotation){
		transform.rotation = rotation;
	}

	void Update () {
		float delta = speed * Time.deltaTime;
		Move(delta);
	}

	public void SimulateFrames(int frames){
		float defaultTimeScale = 0.05f;
		float distancePerFrame = speed * defaultTimeScale;
		float movement = frames * distancePerFrame; 
		Move(movement);
	}

	public void Move(float delta){
		Vector3 translation = new Vector3(delta, 0f, 0f);
		transform.Translate(translation);
	}

}
