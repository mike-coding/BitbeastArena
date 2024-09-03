using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Tilemaps;

public class MapData
{
    //main MapData vars
    public MapNode[,] MainMap;
    public int Level;
    public int Height;
    public int Width;
    public int EntryExitDepth;
    public Bounds InnerMapBounds;
    public (Vector2Int start, Vector2Int end) StartEndNodes;
    public Vector2Int CurrentPlayerCoords;
    public int RandomSeed;

    //object references
    public GameObject bitBeastPrefab;
    public GameObject _treeObject;
    public GameObject _grassObject;
    public GameObject _mapNodeObject;
    public GameObject _ledgeObject;
    public GameObject _fogEmitterObject;
    public GameObject _eventObject;

    //tilemaps and tiles
    public Tilemap _level2Tilemap;
    public Tilemap _level3Tilemap;
    public Tilemap _level2ShadowMap;

    public Dictionary<string, List<Tile>> TileDict = new Dictionary<string, List<Tile>>();

    public MapData Init(int level, int width, int height, int entryExitDepth = 5)
    {
        Level = level;
        MainMap = new MapNode[width, height];
        Height = height;
        Width = width;
        EntryExitDepth = entryExitDepth;
        RandomSeed = Guid.NewGuid().GetHashCode();
        ReplaceMainMap(ProceduralGenerator.PopulateMapNodes_NewStyle(this));
        LoadObjectAndComponentReferences();
        PrepareMapToPush();
        PushMapToScene();

        return this;
    }

    public void ReplaceMainMap(MapNode[,] toReplaceWith)
    {
        int newWidth = toReplaceWith.GetLength(0);
        int newHeight = toReplaceWith.GetLength(1);

        MainMap = new MapNode[newWidth, newHeight];
        Width = newWidth;
        Height = newHeight;

        for (int x = 0; x < newWidth; x++)
        {
            for (int y = 0; y < newHeight; y++)
            {
                MainMap[x, y] = toReplaceWith[x, y];
            }
        }
        RecalculateBounds();
    }

    #region setup
    public void LoadObjectAndComponentReferences()
    {
        if (!bitBeastPrefab) bitBeastPrefab = Resources.Load<GameObject>("gameObjects/BeastEntity");
        _treeObject = Resources.Load<GameObject>("gameObjects/obstacles/Tree_OG");
        _grassObject = Resources.Load<GameObject>("gameObjects/obstacles/Grass_Forest");
        _mapNodeObject = Resources.Load<GameObject>("3D Objects/overworldMapNode");
        _ledgeObject = Resources.Load<GameObject>("3D Objects/UberLedge2");
        _fogEmitterObject = Resources.Load<GameObject>("gameObjects/FogEmitter");
        _eventObject = Resources.Load<GameObject>("gameObjects/EventEntity");

        _level2Tilemap = GameObject.Find("Grid/level2Ground").gameObject.GetComponent<Tilemap>();
        _level3Tilemap = GameObject.Find("Grid/level3Ground").gameObject.GetComponent<Tilemap>();
        _level2ShadowMap = GameObject.Find("Grid/level2GroundTiling").gameObject.GetComponent<Tilemap>();

        LoadGrassTiles();
        LoadSandTiles();
        LoadDirtTiles();
        LoadMiscTiles();
    }
    private void LoadGrassTiles()
    {
        List<int> flowerTiles = new List<int>() { 9, 10, 16, 22, 26, 27, 28 };
        List<int> grassTiles = new List<int>() { 7, 8, 13, 19, 25 };
        List<int> grassTileBordersRight = new List<int>() { 11, 17, 23, 29 };
        List<int> grassTileBordersLeft = new List<int>() { 6, 12, 18, 24 };

        TileDict["GRASS"] = new List<Tile>();
        TileDict["FLOWER"] = new List<Tile>();
        TileDict["BORDER_RIGHT"] = new List<Tile>();
        TileDict["BORDER_LEFT"] = new List<Tile>();

        Tile[] tiles = Resources.LoadAll<Tile>("palettes/newPalette/grassTiles");

        foreach (Tile tile in tiles)
        {
            string[] nameParts = tile.name.Split('_');
            if (int.TryParse(nameParts[nameParts.Length - 1], out int tileNumber))
            {
                if (grassTiles.Contains(tileNumber))
                {
                    TileDict["GRASS"].Add(tile);
                }
                else if (flowerTiles.Contains(tileNumber))
                {
                    TileDict["FLOWER"].Add(tile);
                }
                else if (grassTileBordersRight.Contains(tileNumber))
                {
                    TileDict["BORDER_RIGHT"].Add(tile);
                }
                else if (grassTileBordersLeft.Contains(tileNumber))
                {
                    TileDict["BORDER_LEFT"].Add(tile);
                }
            }
        }
    }
    private void LoadSandTiles()
    {
        TileDict["SAND"] = new List<Tile> { Resources.Load<Tile>("palettes/newPalette/sandTiles/sandTile_NG_5") };
    }
    private void LoadDirtTiles()
    {
        TileDict["DIRT"] = new List<Tile> { Resources.Load<Tile>("palettes/newPalette/dirtTiles/dirtTile_NG_5") };
    }
    private void LoadMiscTiles()
    {
        TileDict["SHADOW"] = new List<Tile> { Resources.Load<Tile>("palettes/newPalette/miscTiles/shadow") };
    }
    #endregion

    #region Store / modify Map Data


    public void RecalculateBounds()
    {
        Vector3 min = Vector3.one * float.MaxValue;
        Vector3 max = Vector3.one * float.MinValue;

        for (int y = 0; y < Height; y++)
        {
            for (int x = 0; x < Width; x++)
            {
                if (MainMap[x, y].IsCorner)
                {
                    Vector3 position = new Vector3(x, y, 0);

                    min = Vector3.Min(min, position);
                    max = Vector3.Max(max, position);
                }
            }
        }

        if (min != Vector3.one * float.MaxValue && max != Vector3.one * float.MinValue)
        {
            InnerMapBounds = new Bounds((min + max) / 2, max - min);
        }
    }

    public void UpdateMapNodeVisibilities()
    {
        Plane[] frustumPlanes = GeometryUtility.CalculateFrustumPlanes(GameManager.MainCamera);
        for (int x = 0; x < Width; x++)
        {
            for (int y = 0; y < Height; y++)
            {
                if (MainMap[x, y].Style == MapNodeStyle.Obstacle)
                {
                    Vector3 nodeWorldPosition = GetWorldPosition(new Vector2Int(x, y));
                    Vector3 shiftedNodeWorldPosition = nodeWorldPosition;
                    shiftedNodeWorldPosition.y += 2;
                    Bounds nodeBounds = new Bounds(shiftedNodeWorldPosition, new Vector3(2,5,3)); // Assuming nodeSize is the size of your MapNode in world units

                    bool nodeOnScreen = GeometryUtility.TestPlanesAABB(frustumPlanes, nodeBounds);

                    // Call a method on MapNode to switch obstacle active status
                    // This method must be defined in your MapNode class
                    MainMap[x, y].SwitchObstacleActiveStatuses(nodeOnScreen);
                }
            }
        }
    }

    public void ClearPassedEvents()
    {
        int yLevel = CurrentPlayerCoords.y;
        for (int x = 0; x<Width;x++)
        {
            for (int y = 0; y<=yLevel;y++)
            {
                if (MainMap[x,y].Event != EventStyle.None)
                {
                    MainMap[x, y].SpawnedEventController.DestroySelf();
                    MainMap[x, y].Event = EventStyle.None;
                    MainMap[x, y].WasEventTile = true;
                }
            }
        }
    }

    public void ScrubMapNodeSpawnedObstacleLists()
    {
        for (int x=0; x<Width;x++)
        {
            for (int y=0; y<Height;y++)
            {
                MainMap[x, y].Obstacles.Clear();
            }
        }
    }

    #endregion

    #region spawning / pushing to scene

    public void PrepareMapToPush()
    {
        RollEvents();
    }

    public void PushMapToScene()
    {
        ScrubMapNodeSpawnedObstacleLists();
        PushMapTilesToScene();
        SpawnMapObstacles(_treeObject, 0.25f, 0.6f);
        SpawnMapObstacles(_grassObject, 0.25f, 0.8f);
        PruneLedgeObstacles();
        SpawnEvents();
        SpawnEdgeLedges();
        //SpawnFogEmitters();
    }

    private void PushMapTilesToScene()
    {
        for (int x = 0; x < Width; x++)
        {
            for (int y = 0; y < Height; y++)
            {
                MapNodeStyle nodeStyle = MainMap[x, y].Style;
                EventStyle eventStyle = MainMap[x, y].Event;
                int nodeLevel = MainMap[x, y].level;
                bool isLeftSide = x < Width / 2;

                // Loop to create a 3x3 grid for each map node
                for (int i = 0; i < 3; i++)
                {
                    for (int j = 0; j < 3; j++)
                    {
                        Vector3Int tilePosition = new Vector3Int(x * 3 + i, y * 3 + j, 0);
                        Tilemap targetTilemap = nodeLevel == 1 ? _level2Tilemap : _level3Tilemap;

                        // Always set the regular tile first
                        targetTilemap.SetTile(tilePosition, GetRandomGrassTile());

                        // Additional shadow tile for level 1 in a 3x3 checkerboard pattern
                        if (nodeLevel == 1)
                        {
                            // Calculate the larger block index for the checkerboard pattern
                            int blockX = (x * 3 + i) / 3;
                            int blockY = (y * 3 + j) / 3;

                            if ((blockX + blockY) % 2 == 0) _level2ShadowMap.SetTile(tilePosition, TileDict["SHADOW"][0]);
                        }

                        // Set border tiles if necessary
                        if (nodeLevel == 2 && ((x > 0 && MainMap[x - 1, y].level == 1) || (x < Width - 1 && MainMap[x + 1, y].level == 1)))
                        {
                            if ((x > 0 && MainMap[x - 1, y].level == 1 && i == 0) || (x < Width - 1 && MainMap[x + 1, y].level == 1 && i == 2))
                            {
                                char borderDirection = isLeftSide ? 'R' : 'L';
                                targetTilemap.SetTile(tilePosition, GetRandomGrassTile(borderDirection));
                            }
                        }
                    }
                }
            }
        }
    }

    private void SpawnMapObstacles(GameObject objectToSpawn, float minimumObstacleDistance, float randomOffsetRange)
    {
        for (int x = 0; x < Width; x++)
        {
            for (int y = 0; y < Height; y++)
            {
                MapNode currentNode = MainMap[x, y];
                MapNodeStyle nodeStyle = currentNode.Style;

                if (nodeStyle == MapNodeStyle.Obstacle)//|| nodeStyle == MapNodeStyle.Debug_Obstacle
                {
                    for (int i = 0; i < 3; i++)
                    {
                        for (int j = 0; j < 3; j++)
                        {
                            Vector3Int tilePosition = new Vector3Int(x * 3 + i, y * 3 + j, 0);
                            Vector3 baseWorldPosition = _level2Tilemap.CellToWorld(tilePosition) + new Vector3(0.5f, 0.5f, 0);

                            Vector3 worldPosition = Vector3.zero;
                            bool positionFound = false;
                            float zPos = _level2Tilemap.transform.position.z-1.1f*(currentNode.level-1);

                            // Try to find a suitable position for the new obstacle
                            while (!positionFound)
                            {
                                // Randomize the position within the tile bounds
                                float randomX = UnityEngine.Random.Range(-randomOffsetRange, randomOffsetRange);
                                float randomY = UnityEngine.Random.Range(-randomOffsetRange, randomOffsetRange);
                                Vector3 randomOffset = new Vector3(randomX, randomY, 0);
                                worldPosition = baseWorldPosition + randomOffset;

                                // Adjust the z-position
                                worldPosition = new Vector3(worldPosition.x, worldPosition.y, zPos);

                                // Check the distance from existing obstacles
                                positionFound = true;
                                foreach (GameObject existingObstacle in currentNode.Obstacles)
                                {
                                    if (Vector3.Distance(existingObstacle.transform.position, worldPosition) < minimumObstacleDistance)
                                    {
                                        positionFound = false;
                                        break;
                                    }
                                }
                            }

                            // Instantiate the tree and add it to the MapNode's Obstacles list
                            GameObject newTree = GameObject.Instantiate(objectToSpawn, worldPosition, Quaternion.identity);
                            currentNode.Obstacles.Add(newTree);
                        }
                    }
                }
            }
        }
    }

    public bool IsWithinExpandedBounds(int x, int y, Bounds bounds, int expansion)
    {
        return (x >= bounds.min.x && x <= bounds.max.x && y >= bounds.min.y - expansion && y <= bounds.max.y + expansion);
    }


    public void SpawnEdgeLedges()
    {
        // Iterate over each column to find transition from level 1 to level 2
        for (int x = 0; x < Width - 1; x++)
        {
            for (int y = 0; y < Height; y++)
            {
                if (MainMap[x, y].level == 1 && MainMap[x + 1, y].level == 2)
                {
                    // Spawn ledges on the three innermost tiles of the level 2 MapNode
                    for (int i = 0; i < 3; i++)
                    {
                        Vector3Int ledgePosition = new Vector3Int((x + 1) * 3, y * 3 + i, 0); // Right side
                        SpawnLedgeAtPosition(ledgePosition);
                    }
                }
                else if (MainMap[x, y].level == 2 && MainMap[x + 1, y].level == 1)
                {
                    // Spawn ledges on the three innermost tiles of the level 2 MapNode
                    for (int i = 0; i < 3; i++)
                    {
                        Vector3Int ledgePosition = new Vector3Int(x * 3 + 2, y * 3 + i, 0); // Left side
                        SpawnLedgeAtPosition(ledgePosition);
                    }
                }
            }
        }
    }

    private void SpawnLedgeAtPosition(Vector3Int position)
    {
        if (_level3Tilemap != null && _ledgeObject != null)
        {
            Vector3 worldPos = _level3Tilemap.CellToWorld(position) + new Vector3(0.5f, 0.5f, 0); // Center of the cell
            GameObject newLedge = GameObject.Instantiate(_ledgeObject, worldPos, Quaternion.identity);

            // Adjustments
            newLedge.transform.Rotate(-90, 0, 0); // Rotate -90 degrees on X axis
            newLedge.transform.position += new Vector3(-0.238f, -0.425f, 1.26f); // Adjust position
            newLedge.transform.localScale = new Vector3(0.7f, newLedge.transform.localScale.y, 0.7f); // Adjust x and z scale
        }
    }

    public void RollEvents(int difficultyLevel = 1)
    {
        // Get threshold rows
        int[] thresholdRows = GetThresholdRows();

        System.Random random = new System.Random();

        foreach (int y in thresholdRows)
        {
            for (int x = 0; x < Width; x++)
            {
                if (MainMap[x, y].Event == EventStyle.PreEvent)
                {
                    // Determine the threshold level based on y
                    int thresholdLevel = Array.IndexOf(thresholdRows, y);

                    switch (difficultyLevel)
                    {
                        case 1: // Level 1 difficulty
                            switch (thresholdLevel)
                            {
                                //switch this to all picnic later for level 1
                                case 0: // 50/50 between loot and picnic
                                    if (OverworldManager.ProgressionLevel==1) MainMap[x, y].Event = EventStyle.Picnic;
                                    else
                                    {
                                        int row1Roll = random.Next(3); // Splitting the chance equally between 3 options
                                        switch (row1Roll)
                                        {
                                            case 0: MainMap[x, y].Event = EventStyle.Loot; break;
                                            case 1: MainMap[x, y].Event = EventStyle.Picnic; break;
                                            case 2: MainMap[x, y].Event = EventStyle.GumballMachine; break;
                                        }
                                    }
                                    break;

                                case 1: // All enemies
                                    if (OverworldManager.ProgressionLevel==1) MainMap[x, y].Event = EventStyle.WeakEnemy;
                                    else MainMap[x, y].Event = EventStyle.Enemy;
                                    break;

                                case 2: // Levels 3-4
                                case 3:
                                    int roll = random.Next(4); // Splitting the chance
                                    if (roll == 0) // 25% chance for disguised enemy
                                        MainMap[x, y].Event = EventStyle.DisguisedEnemy;
                                    else if (roll == 1) // 25% chance for regular enemy
                                        MainMap[x, y].Event = EventStyle.Enemy;
                                    else
                                    {
                                        // Equal chance between picnic, loot, vending machine, and wishing well
                                        roll = random.Next(5);
                                        switch (roll)
                                        {
                                            case 0: MainMap[x, y].Event = EventStyle.Picnic; break;
                                            case 1: MainMap[x, y].Event = EventStyle.Loot; break;
                                            case 2: MainMap[x, y].Event = EventStyle.GumballMachine; break;
                                            case 3: MainMap[x, y].Event = EventStyle.GumballMachine; break;
                                            case 4: MainMap[x, y].Event = EventStyle.Shrine; break;
                                        }
                                    }
                                    break;
                            }
                            break;

                            // Additional cases for different difficulty levels can be added here
                    }
                }
                //this can eventually get moved into case 1 when we tie difficulty level to overworld progression level
                /*
                if (Level == 1)
                {
                    int starterBagX = StartEndNodes.start[0];
                    int starterBagY = StartEndNodes.start[1] + 2;
                    MainMap[starterBagX, starterBagY].Event = EventStyle.StarterLoot;
                }
                */
            }
        }
    }
    
    private void SpawnEvents()
    {
        for (int x = 0; x < Width; x++)
        {
            for (int y = 0; y < Height; y++)
            {
                if (MainMap[x, y].Event != EventStyle.None)
                {
                    Vector3 worldPosition = GetWorldPosition(new Vector2Int(x, y)) + new Vector3(0, 0, 0);
                    MainMap[x, y].SpawnedEventController = GameObject.Instantiate(_eventObject, worldPosition, Quaternion.identity).GetComponent<EventController>();
                    MainMap[x, y].SpawnedEventController.SetEvent(MainMap[x, y].Event, OverworldManager.ProgressionLevel);
                }

            }
        }
    }

    private int[] GetThresholdRows()
    {
        int[] thresholdRows = new int[4];
        int foundRows = 0;
        for (int y=0;y < Height;y++)
        {
            for (int x=0;x < Width;x++)
            {
                if (MainMap[x,y].Event==EventStyle.PreEvent)
                {
                    thresholdRows[foundRows] = y;
                    foundRows += 1;
                    break;
                }
            }
        }
        return thresholdRows;
    }

    private void SpawnFogEmitters()
    {
        int centerColumnIndex = Width / 2; // Find the center column index
        int topBottomOffset = 6;
        // Instantiate the bottom fog emitter with -90 degrees rotation on the X axis and z position set to -2
        Vector3 bottomEmitterPosition = GetWorldPosition(new Vector2Int(centerColumnIndex, topBottomOffset-1)) + new Vector3(0, 0, -2);
        GameObject bottomEmitter = GameObject.Instantiate(_fogEmitterObject, bottomEmitterPosition, Quaternion.Euler(90, 0, 0));

        // Instantiate the top fog emitter with +90 degrees rotation on the X axis and z position set to -2
        Vector3 topEmitterPosition = GetWorldPosition(new Vector2Int(centerColumnIndex, Height - topBottomOffset)) + new Vector3(0, 0, -2);
        GameObject topEmitter = GameObject.Instantiate(_fogEmitterObject, topEmitterPosition, Quaternion.Euler(-90, 0, 0));
    }

    private void PruneLedgeObstacles()
    {
        for (int x = 0; x < Width - 1; x++)
        {
            for (int y = 0; y < Height; y++)
            {
                List<GameObject> obstaclesToDestroy = new List<GameObject>();

                // Right side border
                if (MainMap[x, y].level == 1 && MainMap[x + 1, y].level == 2)
                {
                    float mapNode1Cutoff = GetWorldPosition(new Vector2Int(x, 0)).x+0.5f;
                    float mapNode2Cutoff = GetWorldPosition(new Vector2Int((x + 1), 0)).x-1f;

                    // Check node1
                    foreach (GameObject obstacle in MainMap[x, y].Obstacles)
                    {
                        if (obstacle.transform.position.x > mapNode1Cutoff)
                        {
                            obstaclesToDestroy.Add(obstacle);
                        }
                    }
                    MainMap[x, y].ClearObstacleList(obstaclesToDestroy);
                    obstaclesToDestroy.Clear();

                    // Check node2
                    foreach (GameObject obstacle in MainMap[x + 1, y].Obstacles)
                    {
                        if (obstacle.transform.position.x < mapNode2Cutoff)
                        {
                            obstaclesToDestroy.Add(obstacle);
                        }
                    }
                    MainMap[x + 1, y].ClearObstacleList(obstaclesToDestroy);
                    obstaclesToDestroy.Clear();
                }
                // Left side border
                else if (MainMap[x, y].level == 2 && MainMap[x + 1, y].level == 1)
                {
                    float mapNode1Cutoff = GetWorldPosition(new Vector2Int(x, 0)).x + 1f;
                    float mapNode2Cutoff = GetWorldPosition(new Vector2Int((x + 1), 0)).x - 0.5f;

                    // Check node1 (the rightmost tile of the left side node)
                    foreach (GameObject obstacle in MainMap[x, y].Obstacles)
                    {
                        if (obstacle.transform.position.x > mapNode1Cutoff)
                        {
                            obstaclesToDestroy.Add(obstacle);
                        }
                    }
                    MainMap[x, y].ClearObstacleList(obstaclesToDestroy);
                    obstaclesToDestroy.Clear();

                    // Check node2 (the leftmost tile of the right side node)
                    foreach (GameObject obstacle in MainMap[x + 1, y].Obstacles)
                    {
                        if (obstacle.transform.position.x < mapNode2Cutoff)
                        {
                            obstaclesToDestroy.Add(obstacle);
                        }
                    }
                    MainMap[x + 1, y].ClearObstacleList(obstaclesToDestroy);
                    obstaclesToDestroy.Clear();
                }
            }
        }
    }
    #endregion

    #region Getters
    public Tile GetRandomGrassTile(char direction = ' ')
    {
        string TileType;

        switch (direction)
        {
            case 'L': // Left border tiles
                TileType = "BORDER_LEFT";
                break;
            case 'R': // Right border tiles
                TileType = "BORDER_RIGHT";
                break;
            case 'U': // Up (no implementation yet)
            case 'F': // Forward (no implementation yet)
            default:  // Default case for GRASS or FLOWER tiles
                TileType = UnityEngine.Random.Range(0, 5) == 0 ? "FLOWER" : "GRASS";
                break;
        }

        int randomIndex = UnityEngine.Random.Range(0, TileDict[TileType].Count);
        Tile randomTile = TileDict[TileType][randomIndex];
        return randomTile;
    }

    public Vector3 GetWorldPosition(Vector2Int gridPosition)
    {
        Vector3Int scaledPosition = new Vector3Int(gridPosition.x * 3 + 1, gridPosition.y * 3 + 1, 0); // Center of 3x3 grid
        return _level2Tilemap.CellToWorld(scaledPosition) + new Vector3(0.5f, 0.5f, 0);
    }

    public Vector2Int GetObjectCoordsInMap(GameObject obj)
    {
        // Get the world position of the GameObject
        Vector3 objPosition = obj.transform.position;

        // Convert world position to cell position on the Tilemap
        Vector3Int cellPosition = _level2Tilemap.WorldToCell(objPosition);

        // Convert the Tilemap cell position to MainMap grid coordinates
        // Assuming that each MapNode corresponds to a 3x3 grid on the Tilemap
        int mainMapX = cellPosition.x / 3;
        int mainMapY = cellPosition.y / 3;

        // Return the coordinates as a new Vector2Int
        return new Vector2Int(mainMapX, mainMapY);
    }
    #endregion

}
