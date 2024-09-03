using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Tilemaps;
using System.Linq;

public class ProceduralGenerator : MonoBehaviour
{
    public static MapNode[,] PopulateMapNodes_NewStyle(MapData workingMapData)
    {
        //main map obstacles
        workingMapData = GenerateElongatedGridObstacleMap(workingMapData);
        workingMapData = ConnectObstacleColumns(workingMapData);
        workingMapData = ConnectObstacleColumnsHorizontally(workingMapData);
        workingMapData = EliminateDeadEnds(workingMapData);

        //events, entities
        workingMapData = PlaceEvents(workingMapData);

        //boundary additions- remain unchanged from old processing style
        workingMapData = AddMapBoundary(workingMapData);
        workingMapData = AddEntryAndExit(workingMapData, workingMapData.EntryExitDepth);
        workingMapData = AddBumpers(workingMapData, 4);

        //storing additional mapData 
        workingMapData = FlagPathNodes(workingMapData);
        workingMapData = StoreStartAndEndNodes(workingMapData);
        workingMapData.CurrentPlayerCoords = workingMapData.StartEndNodes.start;

        return workingMapData.MainMap;
    }

    #region Axillary Map Data Processing
    public static MapData PlaceEvents(MapData map)
    {
        int width = map.Width;
        int height = map.Height;

        for (int y = 2; y < height; y += 3) // Start from y = 1 and increment by 3
        {
            for (int x = 0; x < width; x++)
            {
                if (map.MainMap[x, y] != null && map.MainMap[x, y].Style == MapNodeStyle.Open)
                {
                    map.MainMap[x, y].Event = EventStyle.PreEvent;
                }
            }
        }

        return map;
    }

    private static MapData FlagPathNodes(MapData map)
    {
        for (int x = 0; x < map.Width; x++)
        {
            for (int y = 0; y < map.Height; y++)
            {
                MapNodeStyle nodeStyle = map.MainMap[x, y].Style;
                if (map.IsWithinExpandedBounds(x, y, map.InnerMapBounds, 2) & nodeStyle == MapNodeStyle.Open)
                {
                    map.MainMap[x, y].IsPath = true;
                }
            }
        }
        return map;
    }

    public static MapData StoreStartAndEndNodes(MapData map)
    {
        Vector2Int startNode = new Vector2Int(-1, -1); // Initialize to an invalid position
        Vector2Int endNode = new Vector2Int(-1, -1);   // Initialize to an invalid position

        // Iterate through the LoadedPathNodePositions to find the start and end nodes
        for (int x = 0; x < map.Width; x++)
        {
            for (int y = 0; y < map.Height; y++)
            {
                if (map.MainMap[x, y].IsPath) // Check if a path node object exists
                {
                    // Check for the lowest y-value for the start node
                    if (startNode.y == -1 || y < startNode.y)
                    {
                        startNode = new Vector2Int(x, y);
                    }

                    // Check for the highest y-value for the end node
                    if (endNode.y == -1 || y > endNode.y)
                    {
                        endNode = new Vector2Int(x, y);
                    }
                }
            }
        }

        // Store the found start and end nodes
        map.StartEndNodes = (startNode, endNode);
        return map;
    }

    #endregion


    #region Adding Borders and Boundaries
    private static MapData AddMapBoundary(MapData inputMap)
    {
        int expandedWidth = inputMap.Width + 2; // Expand width by 2 (one on each side)
        int expandedHeight = inputMap.Height + 2; // Expand height by 2 (one on each side)

        // Create an expanded map
        MapNode[,] expandedMap = new MapNode[expandedWidth, expandedHeight];

        // Initialize expanded map with default values or copy from input map
        for (int y = 0; y < expandedHeight; y++)
        {
            for (int x = 0; x < expandedWidth; x++)
            {
                if (x == 0 || y == 0 || x == expandedWidth - 1 || y == expandedHeight - 1) expandedMap[x, y] = new MapNode { Style = MapNodeStyle.Obstacle };

                else expandedMap[x, y] = inputMap.MainMap[x - 1, y - 1];
            }
        }
        inputMap.ReplaceMainMap(expandedMap);
        return inputMap;
    }

    private static MapData AddBumpers(MapData map, int bumperSize)
    {
        int additionalColumns = bumperSize * 2; // Bumper size on each side
        int width = map.Width + additionalColumns;
        int height = map.Height;

        MapNode[,] expandedMap = new MapNode[width, height];

        // Initialize expanded map
        for (int x = 0; x < width; x++)
        {
            for (int y = 0; y < height; y++)
            {
                if (x < bumperSize || x >= width - bumperSize) // Check for bumper regions
                {
                    expandedMap[x, y] = new MapNode { Style = MapNodeStyle.Obstacle, level = 2 }; // Bumper node
                }
                else
                {
                    // Copy existing map node
                    expandedMap[x, y] = map.MainMap[x - bumperSize, y];
                }
            }
        }

        map.ReplaceMainMap(expandedMap); // Replace the old map with the new expanded map
        return map;
    }

    #region Add Entry and Exit
    private static MapData AddEntryAndExit(MapData inputMap, int rowsToAddEachSide)
    {
        int width = inputMap.Width;
        int additionalRows = rowsToAddEachSide * 2;
        int height = inputMap.Height + additionalRows;

        MapNode[,] expandedMap = new MapNode[width, height];

        // Copy the existing map into the center of the expanded map
        for (int x = 0; x < width; x++)
        {
            for (int y = rowsToAddEachSide; y < height - rowsToAddEachSide; y++)
            {
                expandedMap[x, y] = inputMap.MainMap[x, y - rowsToAddEachSide];
            }
        }

        // Add rows of obstacles at the top and bottom
        for (int y = 0; y < rowsToAddEachSide; y++) // iterate for each top row
        {
            for (int x = 0; x < width; x++)
            {
                expandedMap[x, y] = new MapNode { Style = MapNodeStyle.Obstacle };
                expandedMap[x, height - 1 - y] = new MapNode { Style = MapNodeStyle.Obstacle };
            }
        }

        // Carve paths through the new rows to connect to the existing paths
        List<int> pathPositions = FindPathPositions(expandedMap, rowsToAddEachSide);
        if (pathPositions.Count > 0)
        {
            int connectionPointTop = pathPositions[UnityEngine.Random.Range(0, pathPositions.Count)];
            int connectionPointBottom = pathPositions[UnityEngine.Random.Range(0, pathPositions.Count)];

            // Iterate for each row where the path will be carved, including the boundary row
            for (int y = 0; y <= rowsToAddEachSide + 1; y++) // include the boundary row by using <=
            {
                expandedMap[connectionPointTop, y].Style = MapNodeStyle.Open;
                expandedMap[connectionPointTop, rowsToAddEachSide].Style = MapNodeStyle.Open;

                expandedMap[connectionPointBottom, height - y - 1].Style = MapNodeStyle.Open;
                expandedMap[connectionPointBottom, height - rowsToAddEachSide - 1].Style = MapNodeStyle.Open;
            }
        }
        else Debug.LogError("No path positions found. Cannot carve path.");

        //DebugCorners(inputMap, "AddEntryExit PreReplacement");
        inputMap.ReplaceMainMap(expandedMap);
        //DebugCorners(inputMap, "AddEntryExit input map after Replacemap");
        return inputMap;
    }

    private static List<int> FindPathPositions(MapNode[,] map, int searchDistance)
    {
        List<int> pathPositions = new List<int>();
        int width = map.GetLength(0);
        int height = map.GetLength(1);

        // Look for open nodes in the second row from the top
        for (int x = 0; x < width; x++)
        {
            if (map[x, searchDistance + 1].Style == MapNodeStyle.Open) // Second row from the top
            {
                pathPositions.Add(x);
            }
        }

        // Look for open nodes in the second-to-last row from the bottom
        for (int x = 0; x < width; x++)
        {
            if (map[x, height - (searchDistance + 2)].Style == MapNodeStyle.Open) // Second-to-last row from the bottom
            {
                // Avoid adding the same position twice if both the second and second-to-last are open
                if (!pathPositions.Contains(x))
                {
                    pathPositions.Add(x);
                }
            }
        }

        //Debug.Log("Path positions count: " + pathPositions.Count);
        return pathPositions;
    }
    #endregion
    #endregion

    #region Obstacle Population
    private static MapData GenerateGridObstacleMap(MapData inputMap)
    {
        // Assuming that the top-left corner of the image corresponds to the first element of the array (0,0)
        // and the sandy tiles represent the path (0), and the grassy tiles represent obstacles (1)
        for (int y = 0; y < inputMap.Height; y++)
        {
            for (int x = 0; x < inputMap.Width; x++)
            {
                // Initialize the node
                inputMap.MainMap[x, y] = new MapNode { Position = new int[] { x, y } };
                if ((x == 0 && y == 0) || (x == inputMap.Width - 1 && y == inputMap.Height - 1)) inputMap.MainMap[x, y].IsCorner = true;

                // Determine if the position should be an obstacle or open based on the grid pattern
                bool isObstacle = x % 2 == 1 & y % 2 == 1;
                inputMap.MainMap[x, y].Style = isObstacle ? MapNodeStyle.Obstacle : MapNodeStyle.Open;
            }
        }
        inputMap.RecalculateBounds();
        return inputMap;
    }

    private static MapData GenerateElongatedGridObstacleMap(MapData inputMap)
    {
        // Initialize every node in the map
        for (int y = 0; y < inputMap.Height; y++)
        {
            for (int x = 0; x < inputMap.Width; x++)
            {
                inputMap.MainMap[x, y] = new MapNode { Position = new int[] { x, y } };
                if ((x == 0 && y == 0) || (x == inputMap.Width - 1 && y == inputMap.Height - 1)) inputMap.MainMap[x, y].IsCorner = true;

                // Set the node style based on the elongated grid pattern
                // Every even column is empty, odd columns have a pattern of one empty followed by two obstacles
                bool isObstacle = (x % 2 == 1) && ((y % 3 == 1) || (y % 3 == 2));
                inputMap.MainMap[x, y].Style = isObstacle ? MapNodeStyle.Obstacle : MapNodeStyle.Open;
            }
        }
        inputMap.RecalculateBounds();
        return inputMap;
    }

    private static MapData ConnectObstacleColumns(MapData map)
    {
        System.Random rand = new System.Random();
        for (int x = 0; x < map.Width; x++)
        {
            for (int y = 1; y < map.Height - 1; y++) // Skip the first and last rows
            {
                // Check if the current tile is empty and has obstacles above and below
                if (map.MainMap[x, y].Style == MapNodeStyle.Open &&
                    map.MainMap[x, y - 1].Style == MapNodeStyle.Obstacle &&
                    map.MainMap[x, y + 1].Style == MapNodeStyle.Obstacle)
                {
                    // 1/3 chance to turn the tile into an obstacle
                    if (rand.NextDouble() < 0.333) map.MainMap[x, y].Style = MapNodeStyle.Obstacle;
                }
            }
        }
        return map;
    }

    private static MapData ConnectObstacleColumnsHorizontally(MapData map)
    {
        int width = map.Width;
        int height = map.Height;
        System.Random random = new System.Random(); // Random number generator

        for (int x = 0; x < width; x++)
        {
            for (int y = 0; y < height-1; y++) // height - 2 to avoid checking beyond the second-to-last row
            {
                // Check if current and below node are empty
                if (map.MainMap[x, y].Style == MapNodeStyle.Open && map.MainMap[x, y + 1].Style == MapNodeStyle.Open)
                {
                    // Check left and right boundaries and obstacles
                    if (x > 0 && x < width - 1 &&
                        map.MainMap[x - 1, y].Style == MapNodeStyle.Obstacle &&
                        map.MainMap[x + 1, y].Style == MapNodeStyle.Obstacle &&
                        map.MainMap[x - 1, y - 1].Style == MapNodeStyle.Obstacle &&
                        map.MainMap[x + 1, y - 1].Style == MapNodeStyle.Obstacle)
                    {
                        // Check the tile two steps below
                        if (y >1 &&CountAdjacentEmptyTiles(map, x, y -2) >= 3)
                        {
                            if (random.NextDouble() < 0.6)
                            {
                                map.MainMap[x, y].Style = MapNodeStyle.Obstacle;
                                map.MainMap[x, y - 1].Style = MapNodeStyle.Obstacle;
                            }
                            
                        }
                    }
                }
            }
        }

        return map;
    }

    private static int CountAdjacentEmptyTiles(MapData map, int x, int y)
    {
        int count = 0;
        Vector2Int[] directions = { new Vector2Int(0, 1), new Vector2Int(0, -1), new Vector2Int(-1, 0), new Vector2Int(1, 0) };

        foreach (var dir in directions)
        {
            int adjacentX = x + dir.x;
            int adjacentY = y + dir.y;

            if (adjacentX >= 0 && adjacentX < map.Width && adjacentY >= 0 && adjacentY < map.Height)
            {
                if (map.MainMap[adjacentX, adjacentY].Style == MapNodeStyle.Open)
                {
                    count++;
                }
            }
        }

        return count;
    }

    private static MapData GrowGridObstacleMapWalls(MapData inputMap, int supressionLevel)
    {
        MapNode[,] outputMap = new MapNode[inputMap.Width, inputMap.Height];

        // Initialize each element of outputMap
        for (int y = 0; y < inputMap.Height; y++)
        {
            for (int x = 0; x < inputMap.Width; x++)
            {
                outputMap[x, y] = new MapNode
                {
                    Style = inputMap.MainMap[x, y].Style,
                    IsCorner = inputMap.MainMap[x, y].IsCorner
                };
            }
        }
        // Define directions as tuples (dx, dy)
        List<(int, int)> directions = new List<(int, int)>
        {
        (1, 0), // Right
        (-1, 0), // Left
        (0, 1), // Up
        (0, -1) // Down
        };

        // Iterate over the map
        for (int y = 0; y < inputMap.Height; y++)
        {
            for (int x = 0; x < inputMap.Width; x++)
            {
                // Only grow obstacles
                if (inputMap.MainMap[x, y].Style == MapNodeStyle.Obstacle)
                {
                    // Randomly select a direction to grow
                    var (dx, dy) = directions[UnityEngine.Random.Range(0, directions.Count)];

                    // Calculate new position
                    int newX = x + dx;
                    int newY = y + dy;

                    // Check bounds and grow obstacle if the new position is within the map
                    if (newX >= 0 && newX < inputMap.Width && newY >= 0 && newY < inputMap.Height)
                    {
                        int supressionCheck = UnityEngine.Random.Range(0, 9);
                        Debug.Log($"GrowGridObstacleMapWalls() called.\n" +
                                  $"Input map MainMap null: {outputMap == null}\n" +
                                  $"Map Width: {outputMap.GetLength(0)}\n" +
                                  $"Map Height: {outputMap.GetLength(1)}\n" +
                                  $"Current x,y in outputMap is null: {outputMap[newX, newY]==null}\n" +
                                  $"Current x,y in inputMap is null: {inputMap.MainMap[newX, newY]==null}");
                        if (supressionCheck > supressionLevel-1) outputMap[newX, newY].Style = MapNodeStyle.Obstacle;
                    }
                }
            }
        }
        inputMap.MainMap = outputMap;
        return inputMap;
    }

    #region Obstacle Pruning
    #region EnsureContiguousPaths
    private static MapData EnsureContiguousPaths(MapData map, int passes)
    {
        for (int i =1; i < passes;i++)
        {
            bool[,] visited = new bool[map.Width, map.Height]; // Tracks which tiles have been visited for each pass

            // Find the first open tile to start the flood fill
            bool startFound = false;
            for (int y = 0; y < map.Height && !startFound; y++)
            {
                for (int x = 0; x < map.Width && !startFound; x++)
                {
                    if (map.MainMap[x, y].Style == MapNodeStyle.Open)
                    {
                        FloodFill(map.MainMap, visited, x, y);
                        startFound = true; //break out of loop
                    }
                }
            }

            // Identify isolated regions and connect them
            for (int y = 0; y < map.Height; y++)
            {
                for (int x = 0; x < map.Width; x++)
                {
                    if (map.MainMap[x, y].Style == MapNodeStyle.Open && !visited[x, y])
                    {
                        // Perform a flood fill for the isolated region and store the tiles
                        List<(int, int)> isolatedTiles = new List<(int, int)>();
                        FloodFillIsolated(map.MainMap, visited, x, y, isolatedTiles);

                        // Find the nearest tile in the contiguous region
                        (int, int) nearestTile = FindContiguityBlockingTile(map.MainMap, visited, isolatedTiles);
                        // Create a path by opening up the nearest obstacle
                        map.MainMap[nearestTile.Item1, nearestTile.Item2].Style = MapNodeStyle.Open;

                        // Connect the isolated region with the main path by revisiting
                        FloodFill(map.MainMap, visited, nearestTile.Item1, nearestTile.Item2);
                    }
                }
            }
        }
        return map;
    }

    private static void FloodFill(MapNode[,] map, bool[,] visited, int x, int y)
    {
        int width = map.GetLength(0);
        int height = map.GetLength(1);

        // If the tile is out of bounds or already visited, return
        if (x < 0 || x >= width || y < 0 || y >= height || visited[x, y] || map[x, y].Style == MapNodeStyle.Obstacle)
        {
            return;
        }

        // Mark the tile as visited
        visited[x, y] = true;

        // Recursively visit all connecting tiles
        FloodFill(map, visited, x + 1, y);
        FloodFill(map, visited, x - 1, y);
        FloodFill(map, visited, x, y + 1);
        FloodFill(map, visited, x, y - 1);
    }

    // Modified FloodFill to store isolated tiles
    private static void FloodFillIsolated(MapNode[,] map, bool[,] visited, int x, int y, List<(int, int)> isolatedTiles)
    {
        // Boundary check
        if (x < 0 || x >= map.GetLength(0) || y < 0 || y >= map.GetLength(1))
        {
            return; // Out of bounds
        }
        if (visited[x, y] || map[x, y].Style != MapNodeStyle.Open)
        {
            return; // Already visited or not an open tile
        }

        // Mark this tile as visited and add it to the isolated tiles list
        visited[x, y] = true;
        isolatedTiles.Add((x, y));

        // Recursively visit all neighboring tiles
        FloodFillIsolated(map, visited, x - 1, y, isolatedTiles); // Left
        FloodFillIsolated(map, visited, x + 1, y, isolatedTiles); // Right
        FloodFillIsolated(map, visited, x, y - 1, isolatedTiles); // Down
        FloodFillIsolated(map, visited, x, y + 1, isolatedTiles); // Up
    }

    // Finds the nearest tile in the contiguous path to the isolated tiles
    private static (int, int) FindContiguityBlockingTile(MapNode[,] map, bool[,] visited, List<(int, int)> isolatedTiles)
    {
        int minDistance = int.MaxValue;
        (int, int) nearestTile = (-1, -1);

        foreach (var isolatedTile in isolatedTiles)
        {
            for (int y = 0; y < map.GetLength(1); y++)
            {
                for (int x = 0; x < map.GetLength(0); x++)
                {
                    if (visited[x, y] && map[x, y].Style == MapNodeStyle.Open)
                    {
                        // Check adjacent tiles for obstacles
                        foreach (var dir in new[] { (-1, 0), (1, 0), (0, -1), (0, 1) })
                        {
                            int newX = x + dir.Item1;
                            int newY = y + dir.Item2;

                            // Ensure the new coordinates are within bounds and are obstacles
                            if (newX >= 0 && newX < map.GetLength(0) && newY >= 0 && newY < map.GetLength(1) && map[newX, newY].Style == MapNodeStyle.Obstacle)
                            {
                                int distance = Math.Abs(isolatedTile.Item1 - newX) + Math.Abs(isolatedTile.Item2 - newY);
                                if (distance < minDistance)
                                {
                                    minDistance = distance;
                                    nearestTile = (newX, newY);
                                }
                            }
                        }
                    }
                }
            }
        }

        return nearestTile;
    }
    private static bool IsValidPosition(Vector2Int position, int width, int height)
    {
        return position.x >= 0 && position.x < width && position.y >= 0 && position.y < height;
    }

    private static void DebugCorners(MapData DebugMap, string functionOrigin)
    {
        int cornerCounter = 0;
        for (int x=0; x< DebugMap.Width; x++)
        {
            for (int y = 0; y < DebugMap.Height; y++)
            {
                if (DebugMap.MainMap[x, y].IsCorner == true) cornerCounter += 1;
            }
        }
        bool cornerCountPass = cornerCounter == 2;
        Debug.Log($"Corner check passed in: {functionOrigin}: {cornerCountPass}");
    }

    private static void DebugBumpers(MapData DebugMap, string functionOrigin)
    {
        int bumperCounter = 0;
        for (int x = 0; x < DebugMap.Width; x++)
        {
            for (int y = 0; y < DebugMap.Height; y++)
            {
                if (DebugMap.MainMap[x, y].level==2) bumperCounter += 1;
            }
        }
        bool bumperCountPass = bumperCounter > 10;
        Debug.Log($"Bumper check passed: {bumperCountPass}\n" +
                  $"Bumper count: {bumperCounter}");
    }


    #endregion

    #region EliminateDeadEnds
    private static MapData EliminateDeadEnds(MapData map)
    {
        int width = map.Width;
        int height = map.Height;

        for (int y = 0; y < height; y++)
        {
            int consecutiveOpenStart = -1;
            int consecutiveOpenEnd = -1;

            for (int x = 0; x < width; x++)
            {
                if (map.MainMap[x, y].Style == MapNodeStyle.Open)
                {
                    if (consecutiveOpenStart == -1)
                        consecutiveOpenStart = x;
                    consecutiveOpenEnd = x;
                }
                else
                {
                    if (consecutiveOpenStart != -1)
                    {
                        ProcessConsecutiveOpenNodes(map, consecutiveOpenStart, consecutiveOpenEnd, y, width, height);
                        consecutiveOpenStart = -1; // Reset for next group of consecutive open nodes
                    }
                }
            }

            // Check if the row ends with open nodes
            if (consecutiveOpenStart != -1)
                ProcessConsecutiveOpenNodes(map, consecutiveOpenStart, consecutiveOpenEnd, y, width, height);
        }

        return map;
    }

    private static void ProcessConsecutiveOpenNodes(MapData map, int start, int end, int y, int width, int height)
    {
        bool hasOpenPathUp = false;
        bool hasOpenPathDown = false;
        System.Random random = new System.Random();

        for (int x = start; x <= end; x++)
        {
            if (y > 0 && map.MainMap[x, y - 1].Style == MapNodeStyle.Open)
                hasOpenPathUp = true;
            if (y < height - 1 && map.MainMap[x, y + 1].Style == MapNodeStyle.Open)
                hasOpenPathDown = true;
        }

        if (!hasOpenPathUp || !hasOpenPathDown)
        {
            int direction = hasOpenPathDown ? -1 : 1; // Determine digging direction
            int targetY = hasOpenPathDown ? y - 1 : y + 1;

            // Choose either the start or the end as selectX
            int selectX = random.Next(2) == 0 ? start : end;

            // Dig in the determined direction
            while (targetY >= 0 && targetY < height && map.MainMap[selectX, targetY].Style != MapNodeStyle.Open)
            {
                map.MainMap[selectX, targetY].Style = MapNodeStyle.Open;
                targetY += direction;
            }
        }
    }
    #endregion

    #endregion

    #endregion



}
