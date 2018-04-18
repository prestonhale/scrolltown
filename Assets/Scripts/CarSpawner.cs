using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class CarSpawner : MonoBehaviour {
    public List<GameObject> carPrefabs;
    public List<Material> carColors;
    // TODO: Dynamic
    public float carOffset = 0.5f;
    public float spawnTime = 2;
    public float verticalSpawnOffset=1.2f;
    public float simulationSteps=2;
    public float spawnChance;
    
    private List<Car> cars;
    private Neighborhood neighborhood;

    public void Awake(){
        neighborhood = GetComponent<Neighborhood>();
        cars = new List<Car>();
    }

    public void StartSpawning(){
        SimulateAdvancement();
        StartCoroutine(StartSpawnRoutines());
    }

    private IEnumerator StartSpawnRoutines(){
        StartCoroutine(SpawnNewCars(3));
        StartCoroutine(SpawnNewCars(1));
        yield return new WaitForSeconds(verticalSpawnOffset);
        StartCoroutine(SpawnNewCars(0));
        StartCoroutine(SpawnNewCars(2));
        yield return null;
    }

    private IEnumerator SpawnNewCars(int direction){
        Block[] edgeBlocks = neighborhood.GetEdgeBlocks(direction);
        // TODO: I thought extents x would = 10 but its only 5? Probably because origin is not at bottom-left
        float blockRadius = edgeBlocks[0].basePlane.GetComponent<MeshFilter>().mesh.bounds.extents.x;
        while(true){
            for (int i = 0; i < edgeBlocks.Length; i++){
                if (UnityEngine.Random.value * 100 > spawnChance){
                    continue;
                }
                int oppositeDirection = direction - 2;
                GameObject carPrefab = carPrefabs[UnityEngine.Random.Range(0, carPrefabs.Count - 1)];
                Material carColor = carColors[UnityEngine.Random.Range(0, carColors.Count - 1)];
                GameObject carGameObject = Instantiate(carPrefab, Vector3.zero, Quaternion.identity);
                Vector3 carPosition = Vector3.zero;
                if (direction==0){
                    carPosition = new Vector3(blockRadius - carOffset, 0, blockRadius);
                } else if (direction==1) {
                    carPosition = new Vector3(blockRadius, 0, -blockRadius + carOffset);
                } else if (direction==2) {
                    carPosition = new Vector3(-blockRadius + carOffset, 0, -blockRadius);
                } else {
                    carPosition = new Vector3(-blockRadius, 0, blockRadius - carOffset);
                }
                Quaternion rotation = Quaternion.Euler(0f, 90f * (oppositeDirection-1), 0f);
                foreach (MeshRenderer renderer in carGameObject.GetComponentsInChildren<MeshRenderer>()){
                    renderer.material = carColor;
                }
                carGameObject.transform.parent = edgeBlocks[i].transform;
                carGameObject.transform.localPosition = carPosition;
                carGameObject.transform.rotation = rotation;
                cars.Add(carGameObject.GetComponent<Car>());
            }
            yield return new WaitForSeconds(spawnTime);
        }
    }
    
    private void SimulateAdvancement(){
        for (int i = 0; i < simulationSteps; i++){
            StartCoroutine(SpawnNewCars(3));
            StartCoroutine(SpawnNewCars(1));
            foreach(Car car in cars){
                car.SimulateFrames(38);
            }
            StartCoroutine(SpawnNewCars(0));
            StartCoroutine(SpawnNewCars(2));
            foreach(Car car in cars){
                car.SimulateFrames(38);
            }
        }
    }
}


