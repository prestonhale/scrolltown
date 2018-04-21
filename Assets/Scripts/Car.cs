using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Car : MonoBehaviour {
	
	[Range(0, 3)]
	public float speed;
	public int direction;
	public Block parentBlock;
	public CarSpawner spawner;
	public bool ableToMove;

	private Vector3 raycastOffset;
	private float timeSinceLastChecked = 0;

	public Car(Quaternion rotation){
		transform.rotation = rotation;
	}

	public void Awake(){
		raycastOffset = new Vector3(0f, 1f, 0f);
		ableToMove = true;
	}

	public void Update () {
		float delta = speed * Time.deltaTime;
		UpdateParentBlock();
		if (ableToMove){
			Move(delta);
		}
	}

	public void SimulateFrames(int frames){
		float defaultTimeScale = 0.05f;   // 60 fps
		float distancePerFrame = speed * defaultTimeScale;
		float movement = frames * distancePerFrame; 
		Move(movement);
	}

	public void Move(float delta){
		Vector3 translation = new Vector3(delta, 0f, 0f);
		transform.Translate(translation);
	}

	private void UpdateParentBlock(){
		timeSinceLastChecked += Time.deltaTime;
		if (timeSinceLastChecked >= 1.0f){
			timeSinceLastChecked = 0f;
			RaycastHit hit;
			Vector3 rayStart = transform.position + raycastOffset;
			int mask = 1 << spawner.groundLayer;
			if (Physics.Raycast(rayStart, -Vector3.up, out hit, Mathf.Infinity, mask)){
				Debug.DrawRay(rayStart, -Vector3.up * hit.distance, Color.yellow, 3f);
				Block blockUnderCar = hit.transform.gameObject.GetComponentInParent<Block>();
				parentBlock = blockUnderCar;
				if (!parentBlock)
					return;
				if (RoadIsEnding()){
					MeshRenderer[] renderers = GetComponentsInChildren<MeshRenderer>();
					foreach(MeshRenderer renderer in renderers){
						renderer.material = spawner.carWarningMaterial;
					}
					ableToMove = false;
				}
			}
		}
	} 

	private bool RoadIsEnding(){
		// The road is ending if the next block in the Car's current direction
		// as well as the block to the left of the next block, are both rural
		// X | O O
		//    ----
		// X  X  X
		Block nextBlock = parentBlock.GetNeighborInDirection(direction);
		if (!nextBlock)
			return false;
		BlockType[] rural = new BlockType[2]{BlockType.forest, BlockType.mountain}; 
		if (Array.IndexOf(rural, nextBlock.type) > -1){
			Block leftSideNeighbor = nextBlock.GetNeighborInDirection((direction + 3)%4);
			if (!leftSideNeighbor)
				return false;
			if (Array.IndexOf(rural, leftSideNeighbor.type) > -1){
				return true;
			}
		}
		return false;
	}

}
