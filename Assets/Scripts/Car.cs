using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Car : MonoBehaviour {
	
	[Range(0, 3)]
	public float speed;
	public Direction direction;
	public Block parentBlock;
	public CarSpawner spawner;
	public bool ableToMove;

	// Testing
	public Block pubNextBlock;

	private Vector3 raycastOffset;
	private float timeSinceLastChecked;

	public Car(Quaternion rotation){
		transform.rotation = rotation;
	}

	public void Awake(){
		ableToMove = true;
		timeSinceLastChecked = 1.5f;
	}

	public void Advance () {
		SpecifiedUpdate(Time.deltaTime);
	}
	
	public void SimulateFrames(int frames){
		float defaultTimeScale = 0.01667f;   // 60 fps
		for (int i = 0; i <= frames; i++){
			SpecifiedUpdate(defaultTimeScale);
		}
	}

	private void SpecifiedUpdate(float elapsedTime){
		float delta = speed * elapsedTime;
		UpdateParentBlock(elapsedTime);
		if (ableToMove){
			Move(delta);
		}
	}

	public void Move(float delta){
		Vector3 translation = new Vector3(delta, 0f, 0f);
		transform.Translate(translation);
	}

	private void UpdateParentBlock(float elapsedTime){
		timeSinceLastChecked += elapsedTime;
		if (timeSinceLastChecked < 1.0f)
			return;
		timeSinceLastChecked = 0.0f;
		RaycastHit hit;
		Vector3 raycastOffset = new Vector3(0f, 1f, 0f) + direction.ToIntVector3();
		Vector3 rayStart = transform.position + raycastOffset;
		int mask = 1 << 8;
		if (Physics.Raycast(rayStart, -Vector3.up, out hit, Mathf.Infinity, mask)){
			Debug.DrawRay(rayStart, -Vector3.up * hit.distance, Color.yellow, 3f);
			parentBlock = hit.transform.gameObject.GetComponentInParent<Block>();
			if (!parentBlock)
				return;
			if (RoadIsEnding()){
				MeshRenderer[] renderers = GetComponentsInChildren<MeshRenderer>();
				foreach(MeshRenderer renderer in renderers){
					renderer.material = spawner.carWarningMaterial;
				}
				ableToMove = false;
			} else {
				ableToMove = true;
			}
		}
	} 

	private bool RoadIsEnding(){
		Block nextBlock = parentBlock.GetNeighborInDirection(direction);
		pubNextBlock = nextBlock;
		Direction drivingSide = direction.GetLeft();
		if (!nextBlock){
			return false;
		} else if (nextBlock.edgeRoads[(int)drivingSide]){
			return false;
		}
		return true;
	}

}
