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

	public Block GetNeighborInDirection(int direction){
		if (direction == 0){
			return north;
		} else if (direction == 1){
			return east;
		} else if (direction == 2){
			return south;
		} else if (direction == 3){
			return west;
		} else {
			return null;
		}
	}
}
