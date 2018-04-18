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
		Vector3 translation = Vector3.zero;
		float delta = speed * Time.deltaTime;
		translation = new Vector3(delta, 0f, 0f);
		transform.Translate(translation);
	}

}
