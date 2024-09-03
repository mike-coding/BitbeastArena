using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class GameObjectPool: ScriptableObject
{
    public int toSpawnID;
    private GameObject _pooledObject;
    public int Size;
    private Queue<GameObject> _queue;

    public void Init(GameObject toPool, int size = 100) 
    {
        _pooledObject = toPool;
        Size = size;
        _queue = new Queue<GameObject>();

        for (int i = 0; i < Size; i++)
        {
            GameObject pooledObjectInstance = Instantiate(_pooledObject, new Vector3(-150f, -150f, 0f), Quaternion.identity);
            pooledObjectInstance.SetActive(false);
            _queue.Enqueue(pooledObjectInstance);
        }
    }

    public GameObject GetPooledObject()
    {
        if (_queue.Count < 1) return null;
        GameObject pooledObjectToReturn = _queue.Dequeue();
        return pooledObjectToReturn;
    }

    public void RequeuePooledObject(GameObject enemy)
    {
        _queue.Enqueue(enemy);
    }

    public void Clear()
    {
        _queue.Clear();
    }
}
