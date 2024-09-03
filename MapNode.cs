using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public enum MapNodeStyle
{
    Open,
    Obstacle,
    Exit,
    Boss,
    Debug_Obstacle,
    Debug_Open
}

public enum EventStyle
{
    None,
    PreEvent,
    StarterLoot,
    Loot,
    Enemy,
    WeakEnemy,
    DisguisedEnemy,
    StrongEnemy,
    VendingMachine,
    Egg,
    Picnic,
    BoobyTrap,
    WishingWell,
    Shrine,
    GumballMachine
}

public class MapNode
{
    public MapNodeStyle Style;
    public int[] Position = new int[2];
    public int level = 1;
    public EventStyle Event = EventStyle.None;
    public EventController SpawnedEventController;
    public bool IsCorner = false;
    public bool IsPath = false;
    public bool WasEventTile = false;
    public List<GameObject> Obstacles= new List<GameObject>();    

    public void ClearObstacles()
    {
        foreach (GameObject obstacle in Obstacles) GameObject.Destroy(obstacle);
        Obstacles = new List<GameObject>();
    }

    public void SwitchObstacleActiveStatuses(bool TurnOn)
    {
        if (TurnOn && Obstacles.Count>0 && Obstacles[0].activeInHierarchy==false)
        {
            foreach(GameObject obstacle in Obstacles) obstacle.SetActive(true);
        }
        else if (!TurnOn && Obstacles.Count>0 && Obstacles[0].activeInHierarchy == true)
        {
            foreach (GameObject obstacle in Obstacles) obstacle.SetActive(false);
        }
    }

    public void ClearObstacleList(List<GameObject> obstaclesToClear)
    {
        foreach (GameObject obstacle in obstaclesToClear)
        {
            Obstacles.Remove(obstacle);
            GameObject.Destroy(obstacle);
        }
    }
}

