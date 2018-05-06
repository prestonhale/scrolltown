using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class NeighborhoodShape {
	public Neighborhood center;
	public Neighborhood top;
	public Neighborhood left;
	public NeighborhoodShape previousShape;
	public NeighborhoodShape nextShape;

	public NeighborhoodShape(Neighborhood center, Neighborhood top, Neighborhood left){
		this.center = center;
		center.parentShape = this;
		this.top = top;
		top.parentShape = this;
		this.left = left;
		left.parentShape = this;
	}
	
	public void Build(){ 
		// TODO: Center is failing to capture its neighbors correctly
		center.Build();
		top.Build();
		left.Build();
	}

	public Neighborhood GetNeighborhoodInDirection(Direction direction, Neighborhood neighborhood){
		if (neighborhood == center){
			if (direction == Direction.West){
				return left;
			}
			if (direction == Direction.South){
				if (previousShape != null){
					return previousShape.left;
				}
			}
			if (direction == Direction.East){
				if (previousShape != null){
					return previousShape.top;
				}
			}
			else if (direction == Direction.North){
				return top;
			}
			return null;
		}
		else if (neighborhood == top){
			if (direction == Direction.South){
				return center;
			}
			else if (direction == Direction.West){
				if (nextShape != null) return nextShape.center;
			}
			return null;
		}
		else if (neighborhood == left){
			if (direction == Direction.North){
				if (nextShape != null) return nextShape.center;
			}
			if (direction == Direction.East){
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

	public void Start() {
		Physics.autoSimulation = false;
		camera = Camera.main;
		Screen.SetResolution(200, 200, false);
		Neighborhood neighborhood = neighborhoodPrefab.GetComponent<Neighborhood>();
		// We could create a separate height and width offset but lets keep it simple
		totalOffset = neighborhood.blockOffset * neighborhood.height;
		radius = totalOffset/2;
		currentShape = SpawnNeighborhoodShape(0f + neighborhood.blockOffset/2, 0f + neighborhood.blockOffset/2, null);
	}

	public void Update () {
		// TODO: There's probably something better to check here
		if (currentShape.center.centerPoint.x > camera.transform.position.x){
		// if (false) {
			if (mediumShape != null){
				oldShape = mediumShape;
			}
			mediumShape = currentShape;
			Neighborhood centerOfShape = currentShape.center;
			float spawnX = (centerOfShape.bottomLeft.x - totalOffset) + (centerOfShape.blockOffset/2);
			float spawnZ = (centerOfShape.bottomLeft.z + totalOffset) + (centerOfShape.blockOffset/2);
			currentShape = SpawnNeighborhoodShape(spawnX, spawnZ, mediumShape);
			if (oldShape != null){
				neighborhoods.Remove(oldShape.center.gameObject.GetComponent<Neighborhood>());
				Destroy(oldShape.center.gameObject);
				neighborhoods.Remove(oldShape.top.gameObject.GetComponent<Neighborhood>());
				Destroy(oldShape.top.gameObject);
				neighborhoods.Remove(oldShape.left.gameObject.GetComponent<Neighborhood>());
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
		if (prevShape != null) prevShape.nextShape = shape;
		
		center.transform.position = new Vector3(x, 0f, z);
		top.transform.position = new Vector3(x, 0f, z + totalOffset);
		left.transform.position = new Vector3(x - totalOffset, 0f, z);
		
		shape.Build();

		return shape;
	}
}
