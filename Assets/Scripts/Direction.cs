using System.Collections;
using System.Collections.Generic;
using UnityEngine;


public enum Direction {
	North,
	East,
	South,
	West
}

public static class Directions {

	public const int Count = 4;

	public static Direction RandomValue {
		get {
			return (Direction)Random.Range(0, Count);
		}
	}

	private static Vector3[] vector3s = {
		new Vector3 (0, 0, 1),
		new Vector3 (1, 0, 0),
		new Vector3 (0, 0, -1),
		new Vector3 (-1, 0, 0),
	};

	public static Vector3 ToIntVector3 (this Direction direction){
		return vector3s[(int)direction];
	}

	private static Quaternion[] rotations = {
		Quaternion.identity,
		Quaternion.Euler(0f, 90f, 0),
		Quaternion.Euler(0f, 180f, 0),
		Quaternion.Euler(0f, 270f, 0)
	};

	public static Quaternion ToRotation (this Direction direction){
		return rotations[(int)direction];
	}


	private static Direction[] opposites = {
		Direction.South ,
		Direction.West ,
		Direction.North ,
		Direction.East ,
	};

	public static Direction GetOpposite (this Direction direction){
		return opposites[(int)direction];
	}
	
    private static Direction[] left = {
		Direction.West ,
		Direction.North ,
		Direction.East ,
		Direction.South ,
	};

	public static Direction GetLeft (this Direction direction){
		return left[(int)direction];
	}

}