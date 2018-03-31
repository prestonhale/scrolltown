using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class NeighborhoodCreator : MonoBehaviour {

	public float translationSpeed;
	public Vector3 translationDirection;
	public GameObject neighborhoodPrefab;
	public float tempTimeToNewNeighborhood;
	public List<GameObject> neighborhoods;

	private float lastSpawnTime;
	private GameObject mostRecentNeighborhood;
	private GameObject bottomBound;

	void Start() {
		lastSpawnTime = Time.time + tempTimeToNewNeighborhood;
		mostRecentNeighborhood = neighborhoods[neighborhoods.Count-1];
		bottomBound = transform.GetChild(0).gameObject;
	}

	void Update () {
		bool spawnNewNeighborhood = false;
		// TODO: A lot of this could be functions on a neighbrohood object
		for (int i = 0; i < neighborhoods.Count; i++){
			GameObject neighborhood = neighborhoods[i];
            neighborhood.transform.Translate(translationDirection * translationSpeed);

			if (neighborhood.transform.position.x < bottomBound.transform.position.x && neighborhood.transform.position.z > bottomBound.transform.position.z){
				spawnNewNeighborhood = true;
				neighborhoods.Remove(neighborhood);
				Destroy(neighborhood);
			}
		}

		if (spawnNewNeighborhood){
			Transform nextSpawnPoint = mostRecentNeighborhood.transform.GetChild(0);
			GameObject newNeighborhood = (GameObject)Instantiate(neighborhoodPrefab, nextSpawnPoint.position, Quaternion.identity);
			neighborhoods.Add(newNeighborhood);
			mostRecentNeighborhood = newNeighborhood;
			lastSpawnTime = Time.time;
		}
	}

}
