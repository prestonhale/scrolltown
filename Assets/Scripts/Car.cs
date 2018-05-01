using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Car : MonoBehaviour {
	
	[Range(0, 3)]
	public float speed = 2;
	public Block parentBlock;
	public CarSpawner spawner;
	public bool ableToMove;
	public bool mustTurn;
	public Direction heading;
	public float frontDistanceToCheck;

	// Testing
	public Block pubNextBlock;
	public List<GameObject> pubHit;
	public List<Block> parentBlocks;

	private Vector3 raycastOffset;
	private float timeSinceLastChecked = 0f;

	public void Awake(){
		pubHit = new List<GameObject>();
		parentBlocks = new List<Block>();
		ableToMove = true;
		mustTurn = false;
		frontDistanceToCheck = 2f;
	}

	public void Update () {
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
		if (ableToMove){
			Move(delta);
		}
		if (mustTurn)
			TurnIfNecessary();
		timeSinceLastChecked += elapsedTime;
		if (timeSinceLastChecked < 0.5f)
			return;
		UpdateParentBlock(elapsedTime);
		CheckFront();
		timeSinceLastChecked = 0.0f;
	}

	public void Move(float delta){
		Vector3 translation = new Vector3(delta, 0f, 0f);
		transform.Translate(translation);
	}

	private void UpdateParentBlock(float elapsedTime){
		RaycastHit hit;
		Vector3 raycastOffset = new Vector3(0f, 1f, 0f);
		Vector3 rayStart = transform.position + raycastOffset;
		int mask = ~(1 << 10);
		if (Physics.Raycast(rayStart, -Vector3.up, out hit, Mathf.Infinity, mask)){
			parentBlock = hit.transform.gameObject.GetComponentInParent<Block>();
			parentBlocks.Add(parentBlock);
			CheckRoadIsEnding();
		}
	} 
	
	private void CheckFront(){
		RaycastHit hit;
		Vector3 rayStart = transform.position + transform.right/2 + new Vector3(0f, .2f, 0f);
		Debug.DrawRay(rayStart, transform.right * frontDistanceToCheck, Color.yellow, .5f);
		if (Physics.Raycast(rayStart, transform.right, out hit, frontDistanceToCheck)){
			pubHit.Add(hit.transform.gameObject);
			if (hit.transform.gameObject.GetComponentInParent<Car>()){
				ableToMove = false;
				return;
			}
		}
		ableToMove = true;
	} 

	private void CheckRoadIsEnding(){
		Block nextBlock = parentBlock.GetNeighborInDirection(heading);
		Direction drivingSide = heading.GetLeft();
		if (!nextBlock){
			SetRoadEnding(false);
		} else if (nextBlock.edgeRoads[(int)drivingSide]){
			SetRoadEnding(false);
		} else {
			SetRoadEnding(true);
		}
	}


	public void TurnIfNecessary(){
		// TODO: Make dynamic
		float offsetAdjuster = 5f - 0.25f; // 5 for half of block .25 for half of road width
		if (parentBlock == null){
			return;
		}
		if (heading == Direction.North){
			if (transform.position.z >= parentBlock.transform.position.z + offsetAdjuster){
				SelectTurnDirection();
			}
		} else if (heading == Direction.East){
			if (transform.position.x >= parentBlock.transform.position.x + offsetAdjuster){
				SelectTurnDirection();
			}
		} else if (heading == Direction.South){
			if (transform.position.z <= parentBlock.transform.position.z - offsetAdjuster){
				SelectTurnDirection();
			}
		} else if (heading == Direction.West){
			if (transform.position.x <= parentBlock.transform.position.x - offsetAdjuster){
				SelectTurnDirection();
			}
		} else {
			throw new Exception("Unknown direction.");
		}
	}

	public void SelectTurnDirection(){
		// TODO: Clarify this quite a bit
		// If we're going north and turn right:
		// We'll be on the north edge of our same block
		Block rightBlock = parentBlock;
		Direction rightEdgeRoad = heading; 
		if (rightBlock && rightBlock.edgeRoads[(int)rightEdgeRoad] != null){
			TurnRight();
			SetRoadEnding(false);
			return;
		}

		// If we're going north and turn left
		// We'll be on the south edge of the block one north and one west of us
		Block forwardBlock = parentBlock.GetNeighborInDirection(heading);
		if (forwardBlock == null){
			return;
		}
		Block leftBlock = forwardBlock.GetNeighborInDirection(heading.GetLeft());
		Direction leftEdgeRoad = heading.GetOpposite();
		if (leftBlock && leftBlock.edgeRoads[(int)leftEdgeRoad] != null){
			TurnLeft();
			SetRoadEnding(false);
			return;
		}
	}

	public void TurnRight(){
		SetRoadEnding(false);
		float roadWidth = .5f;
		float blockWidth = 10f;
		Vector3 topLeftIntVector = heading.ToIntVector3() + heading.GetLeft().ToIntVector3();
		Vector3 offset = topLeftIntVector * ((blockWidth/2) - (roadWidth/2));
		transform.position = parentBlock.transform.position + offset;
		transform.Rotate(new Vector3(0f, 90f, 0f));
		heading = heading.GetRight();

		ChangeColor(spawner.rightTurnColor);
	}
	
	public void TurnLeft(){
		SetRoadEnding(false);
		// TODO: Dynamic
		float roadWidth = .5f;
		float blockWidth = 10f;
		Vector3 topLeftIntVector = heading.ToIntVector3() + heading.GetLeft().ToIntVector3();
		Vector3 offset = topLeftIntVector * ((blockWidth/2) + (roadWidth/2));
		transform.position = parentBlock.transform.position + offset;
		transform.Rotate(new Vector3(0f, -90f, 0f));
		heading = heading.GetLeft();

		ChangeColor(spawner.leftTurnColor);
	}

	private void SetRoadEnding(bool roadEnding){
		mustTurn = roadEnding;
		if (roadEnding){
			ChangeColor(spawner.carWarningMaterial);
		}
	}

	private void ChangeColor(Material material){
        MeshRenderer[] renderers = GetComponentsInChildren<MeshRenderer>();
        foreach(MeshRenderer renderer in renderers){
			renderer.material = material;
		}
	}
		
}
