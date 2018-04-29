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

    // Testing
    public Neighborhood neighborhood;
    public List<Car> cars = new List<Car>();

    public void Awake()
    {
        neighborhood = GetComponent<Neighborhood>();
    }

    public void Begin()
    {
        SimulateAdvancement();
    }

    private void SpawnCarsForEdge(Direction direction)
    {
        Block[] edgeBlocks = neighborhood.GetEdgeBlocks(direction);
        Debug.Log(edgeBlocks);
        for (int i = 0; i < edgeBlocks.Length; i++){
            Block parentBlock = edgeBlocks[i];
            Block leftBlock = parentBlock.GetLeft(direction.GetOpposite());
            if (parentBlock.type.IsRural() && leftBlock.type.IsRural()){
                continue;
            }
            GameObject carGameObject = SpawnCar(parentBlock, direction);
            if (carGameObject)
                cars.Add(carGameObject.GetComponent<Car>());
        }
    }

    private GameObject SpawnCar(Block parentBlock, Direction direction)
    {
        if (UnityEngine.Random.value * 100 > spawnChance)
        {
            return null;
        }
        float blockRadius = parentBlock.basePlane.GetComponent<MeshFilter>().mesh.bounds.extents.x;
        GameObject carPrefab = carPrefabs[UnityEngine.Random.Range(0, carPrefabs.Count - 1)];
        Material carColor = carColors[UnityEngine.Random.Range(0, carColors.Count - 1)];

        GameObject carGameObject = Instantiate(carPrefab, Vector3.zero, Quaternion.identity);
        Vector3 carPosition = Vector3.zero;
        if (direction == Direction.North)
        {
            carPosition = new Vector3(blockRadius - carOffset, 0, blockRadius);
        }
        else if (direction == Direction.East)
        {
            carPosition = new Vector3(blockRadius, 0, -blockRadius + carOffset);
        }
        else if (direction == Direction.South)
        {
            carPosition = new Vector3(-blockRadius + carOffset, 0, -blockRadius);
        }
        else if (direction == Direction.West)
        {
            carPosition = new Vector3(-blockRadius, 0, blockRadius - carOffset);
        } else {
            Debug.Log("ERRRRRRORRR");
        }
        Quaternion rotation = Quaternion.Euler(0f, 90f * ((int)direction.GetOpposite() - 1), 0f);
        foreach (MeshRenderer renderer in carGameObject.GetComponentsInChildren<MeshRenderer>())
        {
            renderer.material = carColor;
        }
        carGameObject.transform.parent = parentBlock.transform;
        carGameObject.transform.localPosition = carPosition;
        carGameObject.transform.rotation = rotation;
        Car car = carGameObject.GetComponent<Car>();
        car.spawner = this;
        car.heading = ((Direction)direction).GetOpposite();
        Debug.Log("CAR");
        return carGameObject;
    }

    private void SimulateAdvancement(){
        // 2 seconds of time total at 60 fps
        SpawnCarsForEdge(Direction.West);
        // SpawnCarsForEdge(Direction.East);
        // foreach (Car car in cars)
        // {
        //     car.SimulateFrames(72);  // 1.2 seconds
        // }
        // SpawnCarsForEdge(Direction.North);
        // SpawnCarsForEdge(Direction.South);
        // foreach (Car car in cars)
        // {
        //     car.SimulateFrames(48);  // 0.8 seconds
        // }
    }

}


