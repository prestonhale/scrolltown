using System.Collections;
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
	private float totalOffset;
	private float radius;

	void Start() {
		Physics.autoSimulation = false;
		camera = Camera.main;
		Screen.SetResolution(200, 200, false);
		// We could create a separate height and width offset but lets keep it simple
		Neighborhood neighborhood = neighborhoodPrefab.GetComponent<Neighborhood>();
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
			Vector3 currentCenter = currentShape.center.centerPoint;
			float spawnX = currentCenter.x - totalOffset;
			float spawnZ = currentCenter.z + totalOffset;
			currentShape = SpawnNeighborhoodShape(spawnX, spawnZ, mediumShape);
			if (oldShape != null){
				Destroy(oldShape.center.gameObject);
				Destroy(oldShape.top.gameObject);
				Destroy(oldShape.left.gameObject);
			}
		}
		
	}

	public NeighborhoodShape SpawnNeighborhoodShape(float x, float z, NeighborhoodShape prevShape){
		Neighborhood center = Instantiate(neighborhoodPrefab, Vector3.zero, Quaternion.identity).GetComponent<Neighborhood>();
		Neighborhood top = Instantiate(neighborhoodPrefab, Vector3.zero, Quaternion.identity).GetComponent<Neighborhood>();
		Neighborhood left = Instantiate(neighborhoodPrefab, Vector3.zero, Quaternion.identity).GetComponent<Neighborhood>();

		// TODO: These are both exactly 0.5 off, I have no idea why
		// Possibly the order that update is called in? Rounding floats?
		// Its really weird that they're EXACTLY .5 off.
		x = x + 0.5f;
		z = z - 0.5f;
		
		NeighborhoodShape shape = new NeighborhoodShape(center, top, left);
		shape.previousShape = prevShape;
		shape.CreateAll();
		
		center.transform.position = new Vector3(x - radius, 0f, z - radius);
		top.transform.position = new Vector3(x - radius, 0f, z + totalOffset - radius);
		left.transform.position = new Vector3(x - totalOffset - radius, 0f, z -radius);

		return shape;
	}
}
