using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.Tilemaps;

public class OverworldManager : MonoBehaviour
{

    public static MapData LoadedMapData;
    private static bool monsCurrentlyMoving = false;
    public static bool usePreLoadedMapOnInit = false;
    public static int ProgressionLevel = 1;

    public static Vector3? PartyTargetPoint=null;

    public static List<GameObject> PartyGameObjects = new List<GameObject>();
    public static List<BeastController> PartyControllers = new List<BeastController>();

    private static List<EventStyle> enemyEventStyles = new List<EventStyle>() { EventStyle.Enemy, EventStyle.DisguisedEnemy, EventStyle.WeakEnemy };

    public static void Init()
    {
        ResetPartyObjectVars();
        if (!usePreLoadedMapOnInit) LoadedMapData = new MapData().Init(ProgressionLevel, 9, 13, 5);
        else
        {
            LoadedMapData.LoadObjectAndComponentReferences();
            LoadedMapData.PushMapToScene();

        }
        SpawnPartyMons();
        LoadedMapData.UpdateMapNodeVisibilities();
        GameManager.UImanager.RefreshProfilePanels();
        GameManager.UImanager.UpdateProgressionDisplay();
        if (GameManager.Morale < 1) GameManager.UImanager.EndTheGame();
    }

    #region Move mons

    public enum Direction
    {
        Up,
        Down,
        Left,
        Right
    }

    public static void MoveMons(Direction direction)
    {
        if (PartyTargetPoint!=null || GameManager.UImanager.UIOpen)
        {
            return;
        }
        Vector2Int moveDelta = Vector2Int.zero;

        switch (direction)
        {
            case Direction.Up:
                moveDelta = new Vector2Int(0, 1);
                break;
            case Direction.Down:
                if (LoadedMapData.MainMap[LoadedMapData.CurrentPlayerCoords[0], LoadedMapData.CurrentPlayerCoords[1]].WasEventTile) return;
                moveDelta = new Vector2Int(0, -1);
                break;
            case Direction.Left:
                moveDelta = new Vector2Int(-1, 0);
                break;
            case Direction.Right:
                moveDelta = new Vector2Int(1, 0);
                break;
        }

        Vector2Int targetCoords = LoadedMapData.CurrentPlayerCoords + moveDelta;

        if (LoadedMapData.MainMap[targetCoords.x,targetCoords.y].IsPath)
        {

            Vector3 targetPoint = LoadedMapData.GetWorldPosition(new Vector2Int(targetCoords.x, targetCoords.y));
            targetPoint.z = -0.37f; // Set Z to 0 to keep the party on the ground
            PartyTargetPoint = targetPoint;
            LoadedMapData.CurrentPlayerCoords = targetCoords;
            //PointPartyMons(targetPoint);
        }
        
    }

    private static void PointPartyMons(Vector3 targetPoint)
    {
        if (PartyControllers.Count == 0) Debug.Log("attempted to point empty party!!");
        foreach (BeastController monController in PartyControllers) monController.PointTowards(targetPoint);
    }

    public static void CancelPartyMonPointing()
    {
        foreach (BeastController monController in PartyControllers) monController.CancelMovement();
    }

    public static void RecordMonsArrivalAtTarget(Vector3 targetReached)
    {
        if (targetReached==PartyTargetPoint)
        {
            monsCurrentlyMoving = false;
            PartyTargetPoint = null;
            LoadedMapData.UpdateMapNodeVisibilities();
            CheckPlayerPositionForInteractions();
        }
    }

    private static void CheckPlayerPositionForInteractions()
    {
        Vector2Int currentPlayerCoords = LoadedMapData.CurrentPlayerCoords;
        if (currentPlayerCoords == LoadedMapData.StartEndNodes.end)
        {
            ProgressionLevel += 1;
            GameManager.GetInstance().UpdateScene(GameManager.CurrentScene.name);
            usePreLoadedMapOnInit = false;
        }
        EventStyle eventAtPosition = LoadedMapData.MainMap[currentPlayerCoords[0], currentPlayerCoords[1]].Event;
        if (eventAtPosition!=EventStyle.None & !enemyEventStyles.Contains(eventAtPosition))
        {
            GameManager.EventManager.ActivateEvent(eventAtPosition);
        }
        else if (enemyEventStyles.Contains(eventAtPosition))
        {
            //hand this over to EventSystemController
            List<BeastState> enemyTeam = LoadedMapData.MainMap[currentPlayerCoords[0], currentPlayerCoords[1]].SpawnedEventController.enemyStates;
            BattleManager.PrepareBattle(GameManager.PartyBeastStates, enemyTeam);
            usePreLoadedMapOnInit = true;
        }
    }
    #endregion

    public static void PrintOutMapDebuggingInfo()
    {
        /*
        Debug.Log($"Stored Main Map Dimensions: x {LoadedMapData.Width}, y {LoadedMapData.Height}\n" +
                  $"Stored Main Map Dimensions Scaled To 1/3: x {LoadedMapData.Width / 3}, y {LoadedMapData.Height / 3}\n" +
                  $"Actual Main Map Dimensions: x {LoadedMapData.MainMap.GetLength(0)}, y {LoadedMapData.MainMap.GetLength(1)}\n" +
                  $"Actual Main Map Dimensions Scaled To 1/3: x {LoadedMapData.SpawnedPathNodeObjects.GetLength(0)/3}, y {LoadedMapData.SpawnedPathNodeObjects.GetLength(1) / 3}\n" +
                  $"SpawnedPathNodes Dimensions: x {LoadedMapData.SpawnedPathNodeObjects.GetLength(0)}, y {LoadedMapData.SpawnedPathNodeObjects.GetLength(1)}");
        */
    }

    private static void SpawnPartyMons()
    {
        Vector3 startingPosition = LoadedMapData.GetWorldPosition(LoadedMapData.CurrentPlayerCoords);
        startingPosition = new Vector3(startingPosition.x, startingPosition.y, -0.37f);
        Debug.Log($"PartyBeastStates.Count: {GameManager.PartyBeastStates.Count}");

        float jitterAmount = 0.5f; // Set the jitter range (0.5 units in this case)
        System.Random random = new System.Random();

        foreach (BeastState mon in GameManager.PartyBeastStates)
        {
            // Add a random jitter to the starting position
            Vector3 jitter = new Vector3(
                (float)(random.NextDouble() * 2 - 1) * jitterAmount,
                (float)(random.NextDouble() * 2 - 1) * jitterAmount,
                0);

            GameObject monObject = GameObject.Instantiate(LoadedMapData.bitBeastPrefab, startingPosition + jitter, Quaternion.identity);
            PartyGameObjects.Add(monObject);
            BeastController monController = monObject.GetComponent<BeastController>();
            PartyControllers.Add(monController);

            monController.LoadBeastState(mon);
        }

        GameManager.SetCameraFollowGameObject(PartyGameObjects[0]);
        GameManager.MainCamera.transform.position = PartyGameObjects[0].transform.position;
        GameManager.MainCamera.transform.position += new Vector3(0, -0.38f, -6f);
        PartyControllers[0].IsOWLead = true;
        LoadedMapData.ClearPassedEvents();
    }

    private static void ResetPartyObjectVars()
    {
        foreach (GameObject monObject in PartyGameObjects) GameObject.Destroy(monObject);
        PartyGameObjects = new List<GameObject>();
        PartyControllers = new List<BeastController>();
    }
}
