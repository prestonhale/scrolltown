﻿using System.Collections;
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

	public int index;
	public Block north;
	public Block east;
	public Block south;
	public Block west;

	public void Awake(){
		GetComponent<Renderer>().material.color=color;
	}

	public override string ToString(){
		return index.ToString() + ": " + type.ToString();
	}
}
