using System;
using System.Linq;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class CarSpawner : MonoBehaviour
{
    public List<GameObject> carPrefabs;
    public List<Material> carColors;
    // TODO: Dynamic
    public float carOffset = 0.5f;
    public float spawnTime = 2;
    public float verticalSpawnOffset = 1.2f;
    public int simulationSteps = 2;
    public float spawnChance;

	public Material carWarningMaterial;

    private List<Car> cars;
    private Neighborhood neighborhood;

    public void Awake()
    {
        neighborhood = GetComponent<Neighborhood>();
        cars = new List<Car>();
    }

    public void StartSpawning()
    {
        SimulateAdvancement(10);
    }

    private IEnumerator StartSpawnRoutines()
    {
        StartCoroutine(SpawnNewCars(3));
        StartCoroutine(SpawnNewCars(1));
        yield return new WaitForSeconds(verticalSpawnOffset);
        StartCoroutine(SpawnNewCars(0));
        StartCoroutine(SpawnNewCars(2));
        yield return null;
    }

    private IEnumerator SpawnNewCars(int direction)
    {
        // TODO: I thought extents x would = 10 but its only 5? Probably because origin is not at bottom-left
        while (true)
        {
            SpawnCarsForEdge(direction);
            yield return new WaitForSeconds(spawnTime);
        }
    }

    private void SpawnCarsForEdge(int direction)
    {
        Block[] edgeBlocks = neighborhood.GetEdgeBlocks(direction);
        for (int i = 0; i < edgeBlocks.Length; i++)
        {
            BlockType[] rural = new BlockType[2]{BlockType.forest, BlockType.mountain};
            int oppositeDirection = (direction + 2) % 4;
            Block leftBlock = edgeBlocks[i].left(oppositeDirection);
            if (
                Array.IndexOf(rural, edgeBlocks[i].type) > -1 
                && (!leftBlock || Array.IndexOf(rural, leftBlock.type) > -1)
            ) return;
            GameObject carGameObject = SpawnCar(edgeBlocks[i], direction);
            if (carGameObject)
                cars.Add(carGameObject.GetComponent<Car>());
        }
    }

    private GameObject SpawnCar(Block parentBlock, int direction)
    {
        if (UnityEngine.Random.value * 100 > spawnChance)
        {
            return null;
        }
        float blockRadius = parentBlock.basePlane.GetComponent<MeshFilter>().mesh.bounds.extents.x;
        int oppositeDirection = direction - 2;
        GameObject carPrefab = carPrefabs[UnityEngine.Random.Range(0, carPrefabs.Count - 1)];
        Material carColor = carColors[UnityEngine.Random.Range(0, carColors.Count - 1)];
        GameObject carGameObject = Instantiate(carPrefab, Vector3.zero, Quaternion.identity);
        Vector3 carPosition = Vector3.zero;
        if (direction == 0)
        {
            carPosition = new Vector3(blockRadius - carOffset, 0, blockRadius);
        }
        else if (direction == 1)
        {
            carPosition = new Vector3(blockRadius, 0, -blockRadius + carOffset);
        }
        else if (direction == 2)
        {
            carPosition = new Vector3(-blockRadius + carOffset, 0, -blockRadius);
        }
        else
        {
            carPosition = new Vector3(-blockRadius, 0, blockRadius - carOffset);
        }
        Quaternion rotation = Quaternion.Euler(0f, 90f * (oppositeDirection - 1), 0f);
        foreach (MeshRenderer renderer in carGameObject.GetComponentsInChildren<MeshRenderer>())
        {
            renderer.material = carColor;
        }
        carGameObject.transform.parent = parentBlock.transform;
        carGameObject.transform.localPosition = carPosition;
        carGameObject.transform.rotation = rotation;
        Car car = carGameObject.GetComponent<Car>();
        car.spawner = this;
        car.direction = ((Direction)direction).GetOpposite();  // Opposite direction
        return carGameObject;
    }

    private void SimulateAdvancement(int steps)
    {
        foreach(int step in Enumerable.Range(0, 10)){
            // 2 seconds of time total at 60 fps
            SpawnCarsForEdge(3);
            SpawnCarsForEdge(1);
            foreach (Car car in cars)
            {
                car.SimulateFrames(72);  // 1.2 seconds
            }
            SpawnCarsForEdge(0);
            SpawnCarsForEdge(2);
            foreach (Car car in cars)
            {
                car.SimulateFrames(48);  // 0.8 seconds
            }
        }
    }
}


