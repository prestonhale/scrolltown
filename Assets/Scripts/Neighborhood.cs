﻿using System;
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
    public bool displayCoords;
    public float moveSpeed;
    public Vector3 moveDirection;
    public int height;
    public int width;
    public float blockOffset;
    public Block[] blockPrefabs = new Block[7];
    public float residentialChance;
    public float cityUpgradeChance;
    public float mountainChance;
    public float parkChance;
    public float shoppingChance;
    public GameObject edgeRoadPrefab;
    public Text labelPrefab;
    public GameObject cornerRoadPrefab;

    private Block[] blocks;
    private Canvas canvas;
    private System.Random random;
    private float radius;

    public Vector3 centerPoint {
        get { return new Vector3(transform.position.x + radius, 0, transform.position.z + radius);}
    }

    public void Awake()
    {
        random = new System.Random();
        blocks = new Block[height * width];
        radius = height/2 * blockOffset;

        if (displayCoords){
            canvas = GetComponentInChildren<Canvas>();
            for (int x = 0, i = 0; x < width; x++)
            {
                for (int z = 0; z < height; z++)
                {
                    CreateLabel(x, z, i++);
                }
            }
        }
        CreateNeighborhood();
    }

    public void Update()
    {
        if (Input.GetKeyDown(KeyCode.R))
        {
            DestroyNeighborhood();
            CreateNeighborhood();
        }
        transform.Translate(moveDirection * moveSpeed);
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
        CalculateAllNeighbors();

        LayMountainRanges();
        AttemptGrowCity();
        PlaceParks();
        PlaceShopping();
        SetEdgeRoads();
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
                    ExtendMountain(x, z);
                }
            }
        }
    }

    public void ExtendMountain(int x, int z)
    {
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
				ExtendMountain(neighborCoords.x, neighborCoords.z);
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

    public void PlaceParks(){
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
                if (northIsCity && eastIsCity && southIsCity && westIsCity && UnityEngine.Random.value * 100 < parkChance){
                    ReplaceBlock(3, x, z);
                }
            }
        }
    }

    public void PlaceShopping(){
        for (int x = 0; x < width; x++)
        {
            for (int z = 0; z < height; z++)
            {
                int index = GetIndex(x, z);
                if (Array.IndexOf(new BlockType[2]{BlockType.residential, BlockType.city}, blocks[index].type) == -1){
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

                Block[] neighbors = new Block[4]{northBlock, eastBlock, southBlock, westBlock};
                foreach (Block neighbor in neighbors){
                    if (neighbor && neighbor.type == BlockType.residential){
                        residentialSeen = true;
                    }
                    if (neighbor && neighbor.type == BlockType.city){
                        citySeen = true;
                    }
                }
                if (
                        northIsResidential && eastIsResidential && southIsResidential && westIsResidential
                        && residentialSeen && citySeen
                        && UnityEngine.Random.value * 100 < shoppingChance){
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
            newIndex = curIndex -1;
        }
        else
        {
            newIndex = curIndex - height;
        }
        return newIndex;
    }

    public Block GetBlockInDirection(int direction, int x, int z)
    {
        int newIndex = GetIndexInDirection(direction, x, z);
        if (newIndex >= 0 && newIndex < blocks.Length)
        {
            return blocks[newIndex];
        }
        else
        {
            return null;
        }

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

    public void CalculateAllNeighbors(){
        foreach (Block block in blocks){
            int index = Array.IndexOf(blocks, block);
            GridCoord coords = IndexToCoord(index);
            CalculateNeighbors(coords.x, coords.z);
        }
    }

    public void CalculateNeighbors(int x, int z){
        Block block = GetBlockAtCoords(x, z);
        if (!block){
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

    public Block GetBlockAtCoords(int x, int z){
        int index = GetIndex(x, z);
        if (index > blocks.Length - 1 || index < 0){
            return null;
        }
        Block block = blocks[index];
        return block;
    }

    public void CreateBlock(int blockIndex, int x, int z, int i)
    {
        Vector3 newPosition = new Vector3(x * blockOffset, 0f, z * blockOffset);
        Block blockPrefab = blockPrefabs[blockIndex];
        Block block = blocks[i] = Instantiate<Block>(blockPrefab);
        block.transform.SetParent(transform, false);
        block.transform.position = newPosition;
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
        CalculateNeighbors(x, z+1);
        CalculateNeighbors(x+1, z);
        CalculateNeighbors(x, z-1);
        CalculateNeighbors(x-1, z);
    }


    public void CreateLabel(int x, int z, int i)
    {
        Text text = Instantiate<Text>(labelPrefab);
        text.transform.SetParent(canvas.transform, false);
        text.name = x.ToString() + "," + z.ToString();
        text.rectTransform.localPosition = new Vector2(x * blockOffset, z * blockOffset);
        text.text = x.ToString() + "," + z.ToString();
    }

    public void SetEdgeRoads(){
        float blockOffset = 4.75f;
        BlockType[] rural = new BlockType[2]{BlockType.forest, BlockType.mountain};
        for (int x = 0; x < width; x++)
        {
            for (int z = 0; z < height; z++)
            {   
                bool northRural = false;
                bool eastRural = false;
                bool southRural = false;
                bool westRural = false;
                Block thisBlock = GetBlockAtCoords(x, z);

                // Check rural
                if (Array.IndexOf(rural, thisBlock.type) > -1){
                    for (int i = 0; i < 4; i++){
                        Block neighbor = GetBlockInDirection(i, x, z);
                        if (!neighbor || Array.IndexOf(rural, neighbor.type) == -1){
                            GameObject edgeRoad = Instantiate(edgeRoadPrefab, Vector3.zero, Quaternion.identity);
                            Vector3 newPosition = Vector3.zero;

                            // Check if neighbors are rural blocks, if they are add edge road
                            if (i == 0){
                                newPosition = new Vector3(0, 0.01f, 1 * blockOffset);
                            }
                            else if (i == 1){
                                newPosition = new Vector3(1 * blockOffset, 0.01f, 0);
                            }
                            else if (i == 2){
                                newPosition = new Vector3(0, 0.01f, -1 * blockOffset);
                            }
                            else if (i == 3){
                                newPosition = new Vector3(-1 * blockOffset, 0.01f, 0);
                            }
                            edgeRoad.transform.SetParent(thisBlock.transform);
                            edgeRoad.transform.localPosition = newPosition;
                            if (Array.IndexOf(new int[2]{0, 2}, i) > -1){
                                edgeRoad.transform.Rotate(new Vector3(0, 90, 0));
                            }
                        }
                    }
                }

                // Check residential
                if (Array.IndexOf(rural, thisBlock.type) == -1){
                    for (int i = 0; i < 4; i++){
                        Block neighbor = GetBlockInDirection(i, x, z);
                        if (!neighbor || Array.IndexOf(rural, neighbor.type) > -1){

                            // Check if neighbors are rural blocks, if they are add edge road
                            if (i == 0){
                                northRural = true;
                            }
                            else if (i == 1){
                                eastRural = true;
                            }
                            else if (i == 2){
                                southRural = true;
                            }
                            else if (i == 3){
                                westRural = true;
                            }
                        }
                    }

                    // Assign corner blocks to avoid jagged corner edge roads
                    if (northRural && eastRural){
                        GameObject cornerRoad = Instantiate(cornerRoadPrefab, Vector3.zero, Quaternion.identity);
                        cornerRoad.transform.parent = thisBlock.transform;
                        cornerRoad.transform.localPosition = new Vector3(5.25f, 0.01f, 5.25f);
                    }
                    if (eastRural && southRural){
                        GameObject cornerRoad = Instantiate(cornerRoadPrefab, Vector3.zero, Quaternion.identity);
                        cornerRoad.transform.parent = thisBlock.transform;
                        cornerRoad.transform.localPosition = new Vector3(5.25f, 0.01f, -5.25f);
                    }
                    if (southRural && westRural){
                        GameObject cornerRoad = Instantiate(cornerRoadPrefab, Vector3.zero, Quaternion.identity);
                        cornerRoad.transform.parent = thisBlock.transform;
                        cornerRoad.transform.localPosition = new Vector3(-5.25f, 0.01f, -5.25f);
                    }
                    if (westRural && northRural){
                        GameObject cornerRoad = Instantiate(cornerRoadPrefab, Vector3.zero, Quaternion.identity);
                        cornerRoad.transform.parent = thisBlock.transform;
                        cornerRoad.transform.localPosition = new Vector3(-5.25f, 0.01f, 5.25f);
                    }
                }
            }
        }
    }
}
