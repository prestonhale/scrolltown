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
                GridCoord coord = new GridCoord(x, z);
                if (UnityEngine.Random.value * 100 < residentialChance)
                {
                    CreateBlock(4, coord, i++);
                }
                else
                {
                    CreateBlock(1, coord, i++);
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
        CheckCornerBlocks();
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
                    GridCoord coord = new GridCoord(x, z);
                    int urbanCount = GetSurroundingUrbanCount(coord);
                    if (urbanCount > requiredSmoothingNeigbors)
                    {
                        ReplaceBlock(4, coord);
                    }
                    else if (urbanCount < requiredSmoothingNeigbors)
                    {
                        ReplaceBlock(1, coord);
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
                GridCoord coord = new GridCoord(x, z);
                int index = GetIndex(coord);
                Block block = blocks[index];
                if (block.type != BlockType.forest)
                {
                    continue;
                }
                // TODO: Track a "potential mountain range" and if there's already
                // one in this forest clump, kill it
                if (UnityEngine.Random.value * 100 <= mountainChance)
                {
                    ExtendMountain(coord, 0);
                }
            }
        }
    }

    public void ExtendMountain(GridCoord coord, int mtnLength)
    {
        if (mtnLength >= maxMtnLength)
        {
            return;
        }
        ReplaceBlock(2, coord);
        for (int i = 0; i < 4; i++)
        {
            Block neighbor = GetBlockInDirection((Direction)i, coord);
            if (!neighbor)
            {
                continue;
            }
            if (neighbor.type == BlockType.forest)
            {
                GridCoord neighborCoords = IndexToCoord(GetIndexInDirection(i, coord));
                // TODO: Extend in both directions
                ExtendMountain(coord, mtnLength + 1);
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
                GridCoord coord = new GridCoord(x, z);
                int index = GetIndex(coord);
                Block block = blocks[index];
                if (block.type != BlockType.residential)
                {
                    continue;
                }
                BlockType[] urban = new BlockType[2] { BlockType.residential, BlockType.city };
                // TODO: This will check tiles that are north or south in the NEXT col
                Block northBlock = GetBlockInDirection(Direction.North, coord);
                Block eastBlock = GetBlockInDirection(Direction.East, coord);
                Block southBlock = GetBlockInDirection(Direction.South, coord);
                Block westBlock = GetBlockInDirection(Direction.West, coord);
                bool northIsResidential = northBlock ? Array.IndexOf(urban, northBlock.type) > -1 : false;
                bool eastIsResidential = eastBlock ? Array.IndexOf(urban, eastBlock.type) > -1 : false;
                bool southIsResidential = southBlock ? Array.IndexOf(urban, southBlock.type) > -1 : false;
                bool westIsResidential = westBlock ? Array.IndexOf(urban, westBlock.type) > -1 : false;
                if (northIsResidential && eastIsResidential && southIsResidential && westIsResidential)
                {
                    ReplaceBlock(0, coord);
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
                GridCoord coord = new GridCoord(x, z);
                Block block = GetBlockAtCoords(coord);
                bool northIsCity = block.north ? block.north.type == BlockType.city : false;
                bool eastIsCity = block.east ? block.east.type == BlockType.city : false;
                bool southIsCity = block.south ? block.south.type == BlockType.city : false;
                bool westIsCity = block.west ? block.west.type == BlockType.city : false;
                if (northIsCity && eastIsCity && southIsCity && westIsCity && UnityEngine.Random.value * 100 < parkChance)
                {
                    ReplaceBlock(3, coord);
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
                GridCoord coord = new GridCoord(x, z);
                int index = GetIndex(coord);
                if (Array.IndexOf(new BlockType[2] { BlockType.residential, BlockType.city }, blocks[index].type) == -1)
                {
                    continue;
                }
                bool residentialSeen = false;
                bool citySeen = false;
                BlockType[] urban = new BlockType[2] { BlockType.residential, BlockType.city };

                Block northBlock = GetBlockInDirection(Direction.North, coord);
                Block eastBlock = GetBlockInDirection(Direction.East, coord);
                Block southBlock = GetBlockInDirection(Direction.South, coord);
                Block westBlock = GetBlockInDirection(Direction.West, coord);

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
                    ReplaceBlock(5, coord);
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


    public int GetIndexInDirection(int direction, GridCoord coord)
    {
        int curIndex = GetIndex(coord);
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

    public Block GetBlockInDirection(Direction direction, GridCoord coord)
    {
        int newX = 0;
        int newZ = 0;
        if (direction == Direction.North) {newX = coord.x; newZ = coord.z + 1;}
        else if (direction == Direction.East) {newX = coord.x + 1; newZ = coord.z;}
        else if (direction == Direction.South) {newX = coord.x; newZ = coord.z - 1;}
        else if (direction == Direction.West) {newX = coord.x - 1; newZ = coord.z;}
        return GetBlockAtCoords(coord);
    }

    public int GetIndex(GridCoord coord)
    {
        return (coord.x * height) + coord.z;
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
            CalculateNeighbors(coords);
        }
    }

    public bool ContainsIndex(int index){
        if (index >= 0 && index < blocks.Length)
            return true;
        return false;
    }

    public void CalculateNeighbors(GridCoord coord)
    {
        Block block = GetBlockAtCoords(coord);
        if (!block)
        {
            return;
        }
        Block northBlock = GetBlockInDirection(Direction.North, coord);
        Block eastBlock = GetBlockInDirection(Direction.East, coord);
        Block southBlock = GetBlockInDirection(Direction.South, coord);
        Block westBlock = GetBlockInDirection(Direction.West, coord);

        block.north = northBlock;
        block.east = eastBlock;
        block.south = southBlock;
        block.west = westBlock;
    }

    public Block GetBlockAtCoords(GridCoord coord)
    {
        if (parentShape != null){
            if (coord.x < 0){
                Neighborhood borderingNeighborhood = parentShape.GetNeighborhoodInDirection(3, this);
                if (!borderingNeighborhood) return null;
                return borderingNeighborhood.GetBlockAtCoords(new GridCoord(width - 1, coord.z));
            }
            if (coord.z < 0){
                Neighborhood borderingNeighborhood = parentShape.GetNeighborhoodInDirection(2, this);
                if (!borderingNeighborhood) return null;
                return borderingNeighborhood.GetBlockAtCoords(new GridCoord(coord.x, height -1));
            }
            if (coord.x > width - 1){
                Neighborhood borderingNeighborhood = parentShape.GetNeighborhoodInDirection(1, this);
                if (!borderingNeighborhood) return null;
                return borderingNeighborhood.GetBlockAtCoords(new GridCoord(0, coord.z));
            }
            if (coord.z > height - 1){
                Neighborhood borderingNeighborhood = parentShape.GetNeighborhoodInDirection(0, this);
                if (!borderingNeighborhood) return null;
                return borderingNeighborhood.GetBlockAtCoords(new GridCoord(coord.x, 0));
            }
        }
        int index = GetIndex(coord);
        if (ContainsIndex(index)){
            Block block = blocks[index];
            return block;
        }
        return null;
    }

    public void CreateBlock(int blockIndex, GridCoord coord, int i)
    {
        Vector3 newPosition = new Vector3(coord.x * blockOffset, 0f, coord.z * blockOffset);
        Block blockPrefab = blockPrefabs[blockIndex];
        Block block = blocks[i] = Instantiate<Block>(blockPrefab);
        block.transform.SetParent(transform, false);
        block.transform.localPosition = newPosition;
        block.neighborhood = this;
        block.index = GetIndex(coord);
    }

    public void ReplaceBlock(int blockIndex, GridCoord coord)
    {
        int index = GetIndex(coord);
        Block oldBlock = blocks[index];
        Vector3 oldBlockPosition = oldBlock.transform.position;
        CreateBlock(blockIndex, coord, index);

        Destroy(oldBlock.gameObject);

        CalculateNeighbors(coord);
        CalculateNeighbors(new GridCoord(coord.x, coord.z + 1));
        CalculateNeighbors(new GridCoord(coord.x + 1, coord.z));
        CalculateNeighbors(new GridCoord(coord.x, coord.z - 1));
        CalculateNeighbors(new GridCoord(coord.x - 1, coord.z));
    }

    public int GetSurroundingUrbanCount(GridCoord coord)
    {
        // The minus and plus ones in the loop here allow us to search
        // in a 3x3 sqr around the selected tile
        int urbanCount = 0;
        BlockType[] rural = new BlockType[2] { BlockType.forest, BlockType.mountain };
        for (int neighborX = coord.x - 1; neighborX <= coord.x + 1; neighborX++)
        {
            for (int neighborZ = coord.z - 1; neighborZ <= coord.z + 1; neighborZ++)
            {
                if (neighborX != coord.x || neighborX != coord.z)
                {
                    // Neighbor at coords is NOT rural, or is an edge
                    Block neighbor = GetBlockAtCoords(new GridCoord(neighborX, neighborZ));
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
                GridCoord coord = new GridCoord(x, z);
                Block thisBlock = GetBlockAtCoords(coord);
                // Residential blocks always get 4 edge roads
                if (Array.IndexOf(rural, thisBlock.type) == -1){
                    foreach (Direction direction in Enum.GetValues(typeof(Direction))){
                        thisBlock.AddEdgeRoad(direction);
                    }
                } else {  // Rural blocks only get edge roads if their neighbors are residential
                    for (int i = 0; i < 4; i++)
                    {
                        Block neighbor = GetBlockInDirection((Direction)i, coord);
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
        // If 'forward' is rural
        // and left with heading forward is rural
        // and left of forward with heading forward is rural
        // add a road corner
        for (int x = 0; x < width; x++){
            for (int z = 0; z < height; z++){
                GridCoord coord = new GridCoord(x, z);
                Block thisBlock = GetBlockAtCoords(coord);
                foreach (Direction direction in Enum.GetValues(typeof(Direction))){
                    Direction left = direction.GetLeft();
                    Block forwardNeighbor = thisBlock.GetNeighborInDirection(direction);

                    if (Array.IndexOf(BlockTypes.RuralTypes, thisBlock.type) > -1){
                        continue;
                    }
                    if (!forwardNeighbor || Array.IndexOf(BlockTypes.RuralTypes, forwardNeighbor.type) == -1){
                        continue;
                    }
                    Block leftNeighbor = thisBlock.GetNeighborInDirection(direction.GetLeft());
                    if (!leftNeighbor || Array.IndexOf(BlockTypes.RuralTypes, leftNeighbor.type) == -1){
                        continue;
                    }
                    Block forwardLeftNeighbor = forwardNeighbor.GetNeighborInDirection(direction.GetLeft());
                    if (!forwardLeftNeighbor || Array.IndexOf(BlockTypes.RuralTypes, forwardLeftNeighbor.type) == -1){
                        continue;
                    }

                    // Place corner block at block radius + road radius foward and left
                    Vector3 cornerPosition = (direction.ToIntVector3() + left.ToIntVector3()) * 5.25f;
                    GameObject cornerRoad = Instantiate(cornerRoadPrefab, Vector3.zero, Quaternion.identity);
                    cornerRoad.transform.parent = thisBlock.transform;
                    // Raise it just slightly out of ground
                    cornerRoad.transform.localPosition = cornerPosition + new Vector3(0f, 0.01f, 0f);
                }
            }
        }

    }

    public Block[] GetEdgeBlocks(Direction direction){
        Block[] edgeBlocks = new Block[width];
        if (direction == Direction.North){
            edgeBlocks = new Block[width];
            for (int i = 0; i < width; i++){
                edgeBlocks[i] = GetBlockAtCoords(new GridCoord(i, height-1));
            }
        }
        else if (direction == Direction.East){
            edgeBlocks = new Block[height];
            for (int i = 0; i < height; i++){
                edgeBlocks[i] = GetBlockAtCoords(new GridCoord(width-1, i));
            }
        }
        else if (direction == Direction.South){
            edgeBlocks = new Block[width];
            for (int i = 0; i < width; i++){
                edgeBlocks[i] = GetBlockAtCoords(new GridCoord(i, 0));
            }
        }
        else if (direction == Direction.West){
            edgeBlocks = new Block[height];
            for (int i = 0; i < height; i++){
                edgeBlocks[i] = GetBlockAtCoords(new GridCoord(0, i));
            }
        }
        return edgeBlocks;
    }
}
