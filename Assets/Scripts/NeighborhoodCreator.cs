using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class NeighborhoodShape {
	public Neighborhood center;
	public Neighborhood top;
	public Neighborhood left;

	public NeighborhoodShape(Neighborhood center, Neighborhood top, Neighborhood left){
		this.center = center;
		this.top = top;
		this.left = left;
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
		camera = Camera.main;
		Screen.SetResolution(200, 200, false);
		// We could create a separate height and width offset but lets keep it simple
		Neighborhood neighborhood = neighborhoodPrefab.GetComponent<Neighborhood>();
		totalOffset = neighborhood.blockOffset * neighborhood.height;
		radius = totalOffset/2;
		currentShape = SpawnNeighborhoodShape(0f + neighborhood.blockOffset/2, 0f + neighborhood.blockOffset/2);
	}

	void Update () {
		// TODO: There's probably something better to check here
		if (currentShape.center.centerPoint.x > camera.transform.position.x){
			if (mediumShape != null){
				oldShape = mediumShape;
			}
			mediumShape = currentShape;
			Vector3 currentCenter = currentShape.center.centerPoint;
			currentShape = SpawnNeighborhoodShape(currentCenter.x - totalOffset, currentCenter.z + totalOffset);
			if (oldShape != null){
				Destroy(oldShape.center.gameObject);
				Destroy(oldShape.top.gameObject);
				Destroy(oldShape.left.gameObject);
			}
		}
		
	}

	public NeighborhoodShape SpawnNeighborhoodShape(float x, float z){
		Neighborhood center = Instantiate(neighborhoodPrefab, Vector3.zero, Quaternion.identity).GetComponent<Neighborhood>();
		Neighborhood top = Instantiate(neighborhoodPrefab, Vector3.zero, Quaternion.identity).GetComponent<Neighborhood>();
		Neighborhood left = Instantiate(neighborhoodPrefab, Vector3.zero, Quaternion.identity).GetComponent<Neighborhood>();
		Debug.Log(radius);

		// TODO: These are both exactly 0.5 off, I have no idea why
		// Possibly the order that update is called in? Rounding floats?
		// Its really weird that they're EXACTLY .5 off.
		x = x + 0.5f;
		z = z - 0.5f;
		
		center.transform.position = new Vector3(x - radius, 0f, z - radius);
		top.transform.position = new Vector3(x - radius, 0f, z + totalOffset - radius);
		left.transform.position = new Vector3(x - totalOffset - radius, 0f, z -radius);

		NeighborhoodShape shape = new NeighborhoodShape(center, top, left);
		return shape;
	}
}
