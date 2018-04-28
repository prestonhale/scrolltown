using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public enum BlockType {
	residential,
	city,
	shoppingCenter,
	park,
	mountain,
	forest,
	none
}


// TODO: Make generation of these programmtic rather than prefab based
public class Block : MonoBehaviour {
	public BlockType type;
	public Color color;

	public bool useColor;
	public int index;
	public Block north;
	public Block east;
	public Block south;
	public Block west;
	public GameObject basePlane;
	public Neighborhood neighborhood;

	public void Awake(){
		if (useColor){
			GetComponent<Renderer>().material.color=color;
		}
	}

	public override string ToString(){
		return index.ToString() + ": " + type.ToString();
	}

	public Block left(int incomingDirection){
		if (incomingDirection == 0){
			return west;
		} else if (incomingDirection == 1){
			return north;
		} else if (incomingDirection == 2){
			return east;
		} else {
			return south;
		}
	}

	public Block GetNeighborInDirection(Direction direction){
		if (direction == Direction.North){
			return north;
		} else if (direction == Direction.East){
			return east;
		} else if (direction == Direction.South){
			return south;
		} else if (direction == Direction.West){
			return west;
		} else {
			return null;
		}
	}
}
