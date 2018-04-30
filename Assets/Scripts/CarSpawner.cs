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
    public float carOffset;

    public int simulationSteps = 10;
    public float spawnChance;
	public Material carWarningMaterial;
	public Material rightTurnColor;
	public Material leftTurnColor;

    // Private
    private Neighborhood neighborhood;
    private List<Car> cars = new List<Car>();

    public void Awake()
    {
        neighborhood = GetComponent<Neighborhood>();
    }

    public void Begin()
    {
        for (int i = 0; i < simulationSteps; i ++){
            SimulateAdvancement();
        }
    }

    private void SpawnCarsForEdge(Direction direction)
    {
        Block[] edgeBlocks = neighborhood.GetEdgeBlocks(direction);
        for (int i = 0; i < edgeBlocks.Length; i++){
            Block parentBlock = edgeBlocks[i];
            Block leftBlock = parentBlock.GetLeft(direction.GetOpposite());
            if (parentBlock.type.IsRural() && leftBlock && leftBlock.type.IsRural()){
                continue;
            }
            GameObject carGameObject = SpawnCar(parentBlock, direction);
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
        cars.Add(car);
        return carGameObject;
    }

    private void SimulateAdvancement(){
        SpawnCarsForEdge(Direction.West);
        // SpawnCarsForEdge(Direction.East);
        foreach (Car car in cars)
        {
            car.SimulateFrames(120);  // 2 seconds at 60 fps
        }
        SpawnCarsForEdge(Direction.North);
        SpawnCarsForEdge(Direction.South);
        foreach (Car car in cars)
        {
            car.SimulateFrames(120);  // 2 seconds at 60 fps
        }
    }

}


