﻿using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class NeighborhoodShape {
	public Neighborhood center;
	public Neighborhood top;
	public Neighborhood left;
	public NeighborhoodShape previousShape;

	public NeighborhoodShape(Neighborhood center, Neighborhood top, Neighborhood left){
		this.center = center;
		center.parentShape = this;
		this.top = top;
		top.parentShape = this;
		this.left = left;
		left.parentShape = this;
	}

	public void CreateAll(){
		center.CreateNeighborhood();
		top.CreateNeighborhood();
		left.CreateNeighborhood();
	}

	public Neighborhood GetNeighborhoodInDirection(int direction, Neighborhood neighborhood){
		if (neighborhood == center){
			if (direction == 3){  // West
				return left;
			}
			if (direction == 2){  // South
				if (previousShape != null){
					return previousShape.left;
				}
			}
			if (direction == 1){  // East
				if (previousShape != null){
					return previousShape.top;
				}
			}
			else if (direction == 0){  // North
				return top;
			}
			return null;
		}
		else if (neighborhood == top){
			if (direction == 2){  // South
				return center;
			}
			return null;
		}
		else if (neighborhood == left){
			if (direction == 1){  // East
				return center;
			}
		}
		return null;
	}
}

public class NeighborhoodCreator : MonoBehaviour {

	public GameObject neighborhoodPrefab;

	private Camera camera;
	private NeighborhoodShape currentShape;
	private NeighborhoodShape oldShape;
	private NeighborhoodShape mediumShape;
	private List<Neighborhood> neighborhoods = new List<Neighborhood>();
	private float totalOffset;
	private float radius;

	void Start() {
		Physics.autoSimulation = false;
		camera = Camera.main;
		Screen.SetResolution(200, 200, false);
		Neighborhood neighborhood = neighborhoodPrefab.GetComponent<Neighborhood>();
		// We could create a separate height and width offset but lets keep it simple
		totalOffset = neighborhood.blockOffset * neighborhood.height;
		radius = totalOffset/2;
		currentShape = SpawnNeighborhoodShape(0f + neighborhood.blockOffset/2, 0f + neighborhood.blockOffset/2, null);
	}

	void Update () {
		// TODO: There's probably something better to check here
		if (currentShape.center.centerPoint.x > camera.transform.position.x){
			if (mediumShape != null){
				oldShape = mediumShape;
			}
			mediumShape = currentShape;
			Neighborhood centerOfShape = currentShape.center;
			float spawnX = (centerOfShape.bottomLeft.x - totalOffset) + (centerOfShape.blockOffset/2);
			float spawnZ = (centerOfShape.bottomLeft.z + totalOffset) + (centerOfShape.blockOffset/2);
			// X and Z are the coords of the new BOTTOM LEFT!
			currentShape = SpawnNeighborhoodShape(spawnX, spawnZ, mediumShape);
			if (oldShape != null){
				Destroy(oldShape.center.gameObject);
				Destroy(oldShape.top.gameObject);
				Destroy(oldShape.left.gameObject);
			}
		}
		for (int i = 0; i < neighborhoods.Count; i++){
			neighborhoods[i].Move();
		}
	}

	public NeighborhoodShape SpawnNeighborhoodShape(float x, float z, NeighborhoodShape prevShape){
		Neighborhood center = Instantiate(neighborhoodPrefab, Vector3.zero, Quaternion.identity).GetComponent<Neighborhood>();
		Neighborhood top = Instantiate(neighborhoodPrefab, Vector3.zero, Quaternion.identity).GetComponent<Neighborhood>();
		Neighborhood left = Instantiate(neighborhoodPrefab, Vector3.zero, Quaternion.identity).GetComponent<Neighborhood>();

		neighborhoods.Add(center);
		neighborhoods.Add(top);
		neighborhoods.Add(left);

		NeighborhoodShape shape = new NeighborhoodShape(center, top, left);
		shape.previousShape = prevShape;
		
		center.transform.position = new Vector3(x, 0f, z);
		top.transform.position = new Vector3(x, 0f, z + totalOffset);
		left.transform.position = new Vector3(x - totalOffset, 0f, z);
		
		shape.CreateAll();

		return shape;
	}
}
