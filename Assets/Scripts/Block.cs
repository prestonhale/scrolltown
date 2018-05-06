using System;
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

public static class BlockTypes {

	public static BlockType[] RuralTypes = new BlockType[2]{
		BlockType.forest,
		BlockType.mountain
	};

	public static bool IsRural(this BlockType type){
		return Array.IndexOf(RuralTypes, type) > -1;
	}

}


public class Block : MonoBehaviour {
	public BlockType type;
	public Color color;

	public bool useColor;
	public GridCoord coord;
	public int index;
	public GameObject basePlane;
	public Neighborhood neighborhood;
    public GameObject edgeRoadPrefab;
	public GameObject[] edgeRoads = new GameObject[4];

	[SerializeField]
	private Block _north;
	[SerializeField]
	private Block _east;
	[SerializeField]
	private Block _south;
	[SerializeField]
	private Block _west;

	public Block north {
		get {
			_north = neighborhood.GetBlockInDirection(Direction.North, coord); 
			return _north;
		}
	} 

	public Block east {
		get {
			_east = neighborhood.GetBlockInDirection(Direction.East, coord);
			return _east;
		}
	}
 
	public Block south {
		get {
			_south = neighborhood.GetBlockInDirection(Direction.South, coord);
			return _south;
		}
	} 

	public Block west {
		get {
			_west = neighborhood.GetBlockInDirection(Direction.West, coord);
			return _west;
		}
	} 

	public void Awake(){
		if (useColor){
			GetComponent<Renderer>().material.color=color;
		}
	}

	public override string ToString(){
		return index.ToString() + ": " + type.ToString();
	}

	public Block GetLeft(Direction incomingDirection){
		if (incomingDirection == Direction.North){
			return west;
		} else if (incomingDirection == Direction.East){
			return north;
		} else if (incomingDirection == Direction.South){
			return east;
		} else if (incomingDirection == Direction.West){
			return south;
		} else {
			throw new Exception("Unknown direction.");
		}
	}
	
	public Block GetRight(Direction incomingDirection){
		if (incomingDirection == Direction.North){
			return east;
		} else if (incomingDirection == Direction.East){
			return south;
		} else if (incomingDirection == Direction.South){
			return west;
		} else if (incomingDirection == Direction.West){
			return north;
		} else {
			throw new Exception("Unknown direction.");
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

	public GameObject AddEdgeRoad(Direction direction){
		float blockOffset = 4.75f;
		GameObject edgeRoad = Instantiate(edgeRoadPrefab, Vector3.zero, Quaternion.identity);
		Vector3 yOffset = new Vector3(0f, 0.01f, 0f);
		Vector3 newPosition = (direction.ToIntVector3() * blockOffset) + yOffset;
		edgeRoad.transform.parent = this.transform;
		edgeRoad.transform.localPosition = newPosition;
		
		if (Array.IndexOf(new Direction[2]{Direction.North, Direction.South}, direction) > -1)
		{
			edgeRoad.transform.Rotate(new Vector3(0, 90, 0));
		}

		edgeRoads[(int)direction] = edgeRoad;

		return edgeRoad;
	}
}
