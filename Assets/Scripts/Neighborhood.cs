using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;


public struct GridCoord
{
    public int x;
    public int z;

    public GridCoord(int x, int z)
    {
        this.x = x;
        this.z = z;
    }

    public override String ToString()
    {
        return "Coord: " + this.x.ToString() + "," + this.z.ToString();
    }
}


public class Neighborhood : MonoBehaviour
{
    public float moveSpeed;
    public Vector3 moveDirection;
    public int height;
    public int width;
    public float blockOffset;

    [Header("Prefabs")]
    public Text labelPrefab;
    public GameObject cornerRoadPrefab;
    public Block[] blockPrefabs = new Block[7];

    [Header("Generation Chances")]
    [Range(0, 100)]
    public float residentialChance;
    [Range(0, 100)]
    public float cityUpgradeChance;
    [Range(0, 100)]
    public float mountainChance;
    [Range(0, 100)]
    public float parkChance;
    [Range(0, 100)]
    public float shoppingChance;
    [Range(0, 9)]
    public int requiredSmoothingNeigbors;
    [Range(0, 7)]
    public int smoothCount;
    [Range(0, 10)]
    public int maxMtnLength;
    public NeighborhoodShape parentShape;

    private Block[] blocks;
    private Canvas canvas;
    private System.Random random;
    private float radius;

    public Vector3 bottomLeft {
        get { return new Vector3(transform.position.x - (blockOffset/2), 0, transform.position.z - (blockOffset/2)); }
    }

    public Vector3 centerPoint
    {
        get { return new Vector3(bottomLeft.x + radius, 0, bottomLeft.z + radius); }
    }

    public void Awake()
    {
        random = new System.Random();
        blocks = new Block[height * width];
        radius = height / 2 * blockOffset;
    }

    public void Start(){
        CreateNeighborhood();
    }


    public void Update()
    {
        if (Input.GetKeyDown(KeyCode.R))
        {
            DestroyNeighborhood();
            CreateNeighborhood();
        }
    }

    public void Move(){
        if (!UnityEditor.EditorApplication.isPaused){
            transform.Translate(moveDirection * moveSpeed);
        }
    }

    public void CreateNeighborhood()
    {
        for (int x = 0, i = 0; x < width; x++)
        {
            for (int z = 0; z < height; z++)
            {
                if (UnityEngine.Random.value * 100 < residentialChance)
                {
                    CreateBlock(4, x, z, i++);
                }
                else
                {
                    CreateBlock(1, x, z, i++);
                }
            }
        }
        IterativelySmooth(smoothCount);
        CalculateAllNeighbors();

        LayMountainRanges();
        AttemptGrowCity();
        PlaceParks();
        PlaceShopping();
        SetEdgeRoads();
        CarSpawner carSpawner = GetComponent<CarSpawner>();
        if (carSpawner){
            carSpawner.Begin();
        }
    }

    public void IterativelySmooth(int smoothCount)
    {
        for (int i = 0; i < smoothCount; i++)
        {
            for (int x = 0; x < width; x++)
            {
                for (int z = 0; z < height; z++)
                {
                    int urbanCount = GetSurroundingUrbanCount(x, z);
                    if (urbanCount > requiredSmoothingNeigbors)
                    {
                        ReplaceBlock(4, x, z);
                    }
                    else if (urbanCount < requiredSmoothingNeigbors)
                    {
                        ReplaceBlock(1, x, z);
                    }
                }
            }
        }
    }

    public void LayMountainRanges()
    {
        for (int x = 0; x < width; x++)
        {
            for (int z = 0; z < height; z++)
            {
                int index = GetIndex(x, z);
                Block block = blocks[index];
                if (block.type != BlockType.forest)
                {
                    continue;
                }
                // TODO: Track a "potential mountain range" and if there's already
                // one in this forest clump, kill it
                if (UnityEngine.Random.value * 100 <= mountainChance)
                {
                    ExtendMountain(x, z, 0);
                }
            }
        }
    }

    public void ExtendMountain(int x, int z, int mtnLength)
    {
        if (mtnLength >= maxMtnLength)
        {
            return;
        }
        ReplaceBlock(2, x, z);
        for (int i = 0; i < 4; i++)
        {
            Block neighbor = GetBlockInDirection(i, x, z);
            if (!neighbor)
            {
                continue;
            }
            if (neighbor.type == BlockType.forest)
            {
                GridCoord neighborCoords = IndexToCoord(GetIndexInDirection(i, x, z));
                // TODO: Extend in both directions
                ExtendMountain(neighborCoords.x, neighborCoords.z, mtnLength + 1);
                break;
            }
        }
    }

    public void AttemptGrowCity()
    {
        for (int x = 0; x < width; x++)
        {
            for (int z = 0; z < height; z++)
            {
                int index = GetIndex(x, z);
                Block block = blocks[index];
                if (block.type != BlockType.residential)
                {
                    continue;
                }
                BlockType[] urban = new BlockType[2] { BlockType.residential, BlockType.city };
                // TODO: This will check tiles that are north or south in the NEXT col
                Block northBlock = GetBlockInDirection(0, x, z);
                Block eastBlock = GetBlockInDirection(1, x, z);
                Block southBlock = GetBlockInDirection(2, x, z);
                Block westBlock = GetBlockInDirection(3, x, z);
                bool northIsResidential = northBlock ? Array.IndexOf(urban, northBlock.type) > -1 : false;
                bool eastIsResidential = eastBlock ? Array.IndexOf(urban, eastBlock.type) > -1 : false;
                bool southIsResidential = southBlock ? Array.IndexOf(urban, southBlock.type) > -1 : false;
                bool westIsResidential = westBlock ? Array.IndexOf(urban, westBlock.type) > -1 : false;
                if (northIsResidential && eastIsResidential && southIsResidential && westIsResidential)
                {
                    ReplaceBlock(0, x, z);
                }
            }
        }
    }

    public void PlaceParks()
    {
        // 1. Place parks in areas completely surrounded by city
        // 2. Ensure no parks are with a 5x5 grid (centered on the park) of each other
        for (int x = 0; x < width; x++)
        {
            for (int z = 0; z < height; z++)
            {
                Block block = GetBlockAtCoords(x, z);
                bool northIsCity = block.north ? block.north.type == BlockType.city : false;
                bool eastIsCity = block.east ? block.east.type == BlockType.city : false;
                bool southIsCity = block.south ? block.south.type == BlockType.city : false;
                bool westIsCity = block.west ? block.west.type == BlockType.city : false;
                if (northIsCity && eastIsCity && southIsCity && westIsCity && UnityEngine.Random.value * 100 < parkChance)
                {
                    ReplaceBlock(3, x, z);
                }
            }
        }
    }

    public void PlaceShopping()
    {
        for (int x = 0; x < width; x++)
        {
            for (int z = 0; z < height; z++)
            {
                int index = GetIndex(x, z);
                if (Array.IndexOf(new BlockType[2] { BlockType.residential, BlockType.city }, blocks[index].type) == -1)
                {
                    continue;
                }
                bool residentialSeen = false;
                bool citySeen = false;
                BlockType[] urban = new BlockType[2] { BlockType.residential, BlockType.city };

                Block northBlock = GetBlockInDirection(0, x, z);
                Block eastBlock = GetBlockInDirection(1, x, z);
                Block southBlock = GetBlockInDirection(2, x, z);
                Block westBlock = GetBlockInDirection(3, x, z);

                bool northIsResidential = northBlock ? Array.IndexOf(urban, northBlock.type) > -1 : false;
                bool eastIsResidential = eastBlock ? Array.IndexOf(urban, eastBlock.type) > -1 : false;
                bool southIsResidential = southBlock ? Array.IndexOf(urban, southBlock.type) > -1 : false;
                bool westIsResidential = westBlock ? Array.IndexOf(urban, westBlock.type) > -1 : false;

                Block[] neighbors = new Block[4] { northBlock, eastBlock, southBlock, westBlock };
                foreach (Block neighbor in neighbors)
                {
                    if (neighbor && neighbor.type == BlockType.residential)
                    {
                        residentialSeen = true;
                    }
                    if (neighbor && neighbor.type == BlockType.city)
                    {
                        citySeen = true;
                    }
                }
                if (
                        northIsResidential && eastIsResidential && southIsResidential && westIsResidential
                        && residentialSeen && citySeen
                        && UnityEngine.Random.value * 100 < shoppingChance)
                {
                    ReplaceBlock(5, x, z);
                }
            }
        }
    }

    public GridCoord IndexToCoord(int index)
    {
        int x = index / height;
        int z = index - (x * height);
        GridCoord coord = new GridCoord(x, z);
        return coord;
    }


    public int GetIndexInDirection(int direction, int x, int z)
    {
        int curIndex = GetIndex(x, z);
        int newIndex;
        if (direction == 0)
        {
            newIndex = curIndex + 1;
        }
        else if (direction == 1)
        {
            newIndex = curIndex + height;
        }
        else if (direction == 2)
        {
            newIndex = curIndex - 1;
        }
        else
        {
            newIndex = curIndex - height;
        }
        return newIndex;
    }

    public Block GetBlockInDirection(int direction, int x, int z)
    {
        int newX = 0;
        int newZ = 0;
        if (direction == 0) {newX = x; newZ = z + 1;}
        else if (direction == 1) {newX = x + 1; newZ = z;}
        else if (direction == 2) {newX = x; newZ = z - 1;}
        else if (direction == 3) {newX = x - 1; newZ = z;}
        return GetBlockAtCoords(newX, newZ);
    }

    public int GetIndex(int x, int z)
    {
        return (x * height) + z;
    }

    public void DestroyNeighborhood()
    {
        foreach (var block in blocks)
        {
            Destroy(block.gameObject);
        }
    }

    public void CalculateAllNeighbors()
    {
        foreach (Block block in blocks)
        {
            int index = Array.IndexOf(blocks, block);
            GridCoord coords = IndexToCoord(index);
            CalculateNeighbors(coords.x, coords.z);
        }
    }

    public bool ContainsIndex(int index){
        if (index >= 0 && index < blocks.Length)
            return true;
        return false;
    }

    public void CalculateNeighbors(int x, int z)
    {
        Block block = GetBlockAtCoords(x, z);
        if (!block)
        {
            return;
        }
        Block northBlock = GetBlockInDirection(0, x, z);
        Block eastBlock = GetBlockInDirection(1, x, z);
        Block southBlock = GetBlockInDirection(2, x, z);
        Block westBlock = GetBlockInDirection(3, x, z);

        block.north = northBlock;
        block.east = eastBlock;
        block.south = southBlock;
        block.west = westBlock;
    }

    public Block GetBlockAtCoords(int x, int z)
    {
        if (parentShape != null){
            if (x < 0){
                Neighborhood borderingNeighborhood = parentShape.GetNeighborhoodInDirection(3, this);
                if (!borderingNeighborhood) return null;
                return borderingNeighborhood.GetBlockAtCoords(width - 1, z);
            }
            if (z < 0){
                Neighborhood borderingNeighborhood = parentShape.GetNeighborhoodInDirection(2, this);
                if (!borderingNeighborhood) return null;
                return borderingNeighborhood.GetBlockAtCoords(x, height -1);
            }
            if (x > width - 1){
                Neighborhood borderingNeighborhood = parentShape.GetNeighborhoodInDirection(1, this);
                if (!borderingNeighborhood) return null;
                return borderingNeighborhood.GetBlockAtCoords(0, z);
            }
            if (z > height - 1){
                Neighborhood borderingNeighborhood = parentShape.GetNeighborhoodInDirection(0, this);
                if (!borderingNeighborhood) return null;
                return borderingNeighborhood.GetBlockAtCoords(x, 0);
            }
        }
        int index = GetIndex(x, z);
        if (ContainsIndex(index)){
            Block block = blocks[index];
            return block;
        }
        return null;
    }

    public void CreateBlock(int blockIndex, int x, int z, int i)
    {
        Vector3 newPosition = new Vector3(x * blockOffset, 0f, z * blockOffset);
        Block blockPrefab = blockPrefabs[blockIndex];
        Block block = blocks[i] = Instantiate<Block>(blockPrefab);
        block.transform.SetParent(transform, false);
        block.transform.localPosition = newPosition;
        block.neighborhood = this;
        block.index = GetIndex(x, z);
    }

    public void ReplaceBlock(int blockIndex, int x, int z)
    {
        int index = GetIndex(x, z);
        Block oldBlock = blocks[index];
        Vector3 oldBlockPosition = oldBlock.transform.position;
        CreateBlock(blockIndex, x, z, index);

        Destroy(oldBlock.gameObject);

        CalculateNeighbors(x, z);
        CalculateNeighbors(x, z + 1);
        CalculateNeighbors(x + 1, z);
        CalculateNeighbors(x, z - 1);
        CalculateNeighbors(x - 1, z);
    }

    public int GetSurroundingUrbanCount(int x, int z)
    {
        // The minus and plus ones in the loop here allow us to search
        // in a 3x3 sqr around the selected tile
        int urbanCount = 0;
        BlockType[] rural = new BlockType[2] { BlockType.forest, BlockType.mountain };
        for (int neighborX = x - 1; neighborX <= x + 1; neighborX++)
        {
            for (int neighborZ = z - 1; neighborZ <= z + 1; neighborZ++)
            {
                if (neighborX != x || neighborX != z)
                {
                    // Neighbor at coords is NOT rural, or is an edge
                    Block neighbor = GetBlockAtCoords(neighborX, neighborZ);
                    if (!neighbor)
                    {
                        urbanCount += 1;
                        continue;
                    }
                    if (Array.IndexOf(rural, neighbor.type) == -1)
                    {
                        urbanCount += 1;
                    }
                }
            }
        }
        return urbanCount;
    }

    public void SetEdgeRoads()
    {
        BlockType[] rural = new BlockType[2] { BlockType.forest, BlockType.mountain };
        for (int x = 0; x < width; x++)
        {
            for (int z = 0; z < height; z++)
            {
                Block thisBlock = GetBlockAtCoords(x, z);
                // Residential blocks always get 4 edge roads
                if (Array.IndexOf(rural, thisBlock.type) == -1){
                    foreach (Direction direction in Enum.GetValues(typeof(Direction))){
                        thisBlock.AddEdgeRoad(direction);
                    }
                } else {  // Rural blocks only get edge roads if their neighbors are residential
                    for (int i = 0; i < 4; i++)
                    {
                        Block neighbor = GetBlockInDirection(i, x, z);
                        if (!neighbor) continue;
                        if (Array.IndexOf(rural, neighbor.type) == -1)
                        {
                            Direction direction = (Direction)i;
                            thisBlock.AddEdgeRoad(direction);
                        }
                    }
                }
            }
        }
    }

    public void CheckCornerBlocks(){
        // Check residential
        bool northRural = false;
        bool eastRural = false;
        bool southRural = false;
        bool westRural = false;
        BlockType[] rural = new BlockType[2] { BlockType.forest, BlockType.mountain };
        for (int x = 0; x < width; x++)
        {
            for (int z = 0; z < height; z++)
            {
                Block thisBlock = GetBlockAtCoords(x, z);
                for (int i = 0; i < 4; i++)
                {
                    Block neighbor = GetBlockInDirection(i, x, z);
                    if (!neighbor || Array.IndexOf(rural, neighbor.type) > -1)
                    {
                        if (i == 0)
                        {
                            northRural = true;
                        }
                        else if (i == 1)
                        {
                            eastRural = true;
                        }
                        else if (i == 2)
                        {
                            southRural = true;
                        }
                        else if (i == 3)
                        {
                            westRural = true;
                        }
                    }
                }

                // Assign corner blocks to avoid jagged corner edge roads
                if (northRural && eastRural)
                {
                    GameObject cornerRoad = Instantiate(cornerRoadPrefab, Vector3.zero, Quaternion.identity);
                    cornerRoad.transform.parent = thisBlock.transform;
                    cornerRoad.transform.localPosition = new Vector3(5.25f, 0.01f, 5.25f);
                }
                if (eastRural && southRural)
                {
                    GameObject cornerRoad = Instantiate(cornerRoadPrefab, Vector3.zero, Quaternion.identity);
                    cornerRoad.transform.parent = thisBlock.transform;
                    cornerRoad.transform.localPosition = new Vector3(5.25f, 0.01f, -5.25f);
                }
                if (southRural && westRural)
                {
                    GameObject cornerRoad = Instantiate(cornerRoadPrefab, Vector3.zero, Quaternion.identity);
                    cornerRoad.transform.parent = thisBlock.transform;
                    cornerRoad.transform.localPosition = new Vector3(-5.25f, 0.01f, -5.25f);
                }
                if (westRural && northRural)
                {
                    GameObject cornerRoad = Instantiate(cornerRoadPrefab, Vector3.zero, Quaternion.identity);
                    cornerRoad.transform.parent = thisBlock.transform;
                    cornerRoad.transform.localPosition = new Vector3(-5.25f, 0.01f, 5.25f);
                }
            }
        }
    }

    public Block[] GetEdgeBlocks(Direction direction){
        Block[] edgeBlocks = new Block[width];
        if (direction == Direction.North){
            edgeBlocks = new Block[width];
            for (int i = 0; i < width; i++){
                edgeBlocks[i] = GetBlockAtCoords(i, height-1);
            }
        }
        else if (direction == Direction.East){
            edgeBlocks = new Block[height];
            for (int i = 0; i < height; i++){
                edgeBlocks[i] = GetBlockAtCoords(width-1, i);
            }
        }
        else if (direction == Direction.South){
            edgeBlocks = new Block[width];
            for (int i = 0; i < width; i++){
                edgeBlocks[i] = GetBlockAtCoords(i, 0);
            }
        }
        else if (direction == Direction.West){
            edgeBlocks = new Block[height];
            for (int i = 0; i < height; i++){
                edgeBlocks[i] = GetBlockAtCoords(0, i);
            }
        }
        return edgeBlocks;
    }
}
