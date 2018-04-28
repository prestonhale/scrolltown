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
	public Block pubLeftSideNeighbor;

	private Vector3 raycastOffset;
	private float timeSinceLastChecked;

	public Car(Quaternion rotation){
		transform.rotation = rotation;
	}

	public void Awake(){
		raycastOffset = new Vector3(0f, 1f, 0f);
		ableToMove = true;
		timeSinceLastChecked = 1.0f;
	}

	public void Start(){
		UpdateParentBlock(0f);
	}

	public void Update () {
		SpecifiedUpdate(Time.deltaTime);
	}

	private void SpecifiedUpdate(float elapsedTime){
		float delta = speed * elapsedTime;
		UpdateParentBlock(elapsedTime);
		if (ableToMove){
			Move(delta);
		}
	}

	public void SimulateFrames(int frames){
		float defaultTimeScale = 0.01667f;   // 60 fps
		for (int i = 0; i <= frames; i++){
			SpecifiedUpdate(defaultTimeScale);
		}
	}

	public void Move(float delta){
		Vector3 translation = new Vector3(delta, 0f, 0f);
		transform.Translate(translation);
	}

	private void UpdateParentBlock(float elapsedTime){
		timeSinceLastChecked += elapsedTime;
		if (timeSinceLastChecked >= 1.0f){
			timeSinceLastChecked = 0f;
			RaycastHit hit;
			Vector3 rayStart = transform.position + raycastOffset;
			int mask = 1 << 8;
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
		BlockType[] rural = new BlockType[2]{BlockType.forest, BlockType.mountain}; 
		Block nextBlock = parentBlock.GetNeighborInDirection(direction);
		pubNextBlock = nextBlock;
		if (!nextBlock || Array.IndexOf(rural, nextBlock.type) == -1){
			return false;
		}
		Block leftSideNeighbor = nextBlock.GetNeighborInDirection(direction.GetLeft());
		pubLeftSideNeighbor = leftSideNeighbor;
		if (!leftSideNeighbor){ // In this case, its the edge of world aka no road
			return true;
		}
		if (Array.IndexOf(rural, leftSideNeighbor.type) == -1){
			return false;
		}
		return true;
	}

}
