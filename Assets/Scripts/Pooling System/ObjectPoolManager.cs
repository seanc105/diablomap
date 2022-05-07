using System.Collections.Generic;
using UnityEngine;

public sealed class ObjectPoolManager {
    private Dictionary<int, ObjectPoolList> _gameObjectPoolDictionary;

    public static ObjectPoolManager Instance { get; private set; } = new ObjectPoolManager();

    static ObjectPoolManager() {
        Instance._gameObjectPoolDictionary = new Dictionary<int, ObjectPoolList>();
    }

    /// <summary>
    /// Clears all the pooled lists
    /// </summary>
    public void ResetPools() {
        foreach (var item in _gameObjectPoolDictionary) {
            item.Value.ClearPoolLists();
        }
    }

    /// <summary>
    /// Disable all the pool objects of a given type
    /// </summary>
    /// <param name="poolObjectType">The Object in the pool list</param>
    public void DisableAllObjectsInPool(PoolObject poolObjectType) {
        ObjectPoolList poolListReference = GetSharedPoolList(poolObjectType.GetInstanceID(), poolObjectType);
        poolListReference.SetAllObjectsToInactive();
    }

    /// <summary>
    /// Get a pool list of a given instance id. If it doesn't exist, it'll be created and returned.
    /// </summary>
    /// <param name="instanceId_">The GameObject' or Prefab's instance id</param>
    /// <param name="poolObject_">The Object to put into the pool list</param>
    /// <returns></returns>
    public ObjectPoolList GetSharedPoolList(int instanceId_, PoolObject poolObject_) {
        if (!Instance._gameObjectPoolDictionary.TryGetValue(instanceId_, out ObjectPoolList sharedPoolList)) {
            sharedPoolList = new ObjectPoolList(poolObject_);
            Instance._gameObjectPoolDictionary.Add(instanceId_, sharedPoolList);
        }

        return sharedPoolList;
    }

    /// <summary>
    /// Gets a ready or new active object from the object pool
    /// </summary>
    /// <param name="poolObject_">The respective pool object type to retrieve</param>
    /// <param name="positionToStart_">The Vector3 position to start at when retrieved</param>
    /// <param name="rotationToStart_">The Quaternion rotation to start at when retrieved</param>
    /// <returns>The newly created or ready object</returns>
    public GameObject RetrieveNewlyActiveObject(PoolObject poolObject_, Vector3 positionToStart_, Quaternion rotationToStart_) {
        ObjectPoolList poolListReference = GetSharedPoolList(poolObject_.GetInstanceID(), poolObject_);

        return poolListReference.RetrieveNewlyActiveObject(positionToStart_, rotationToStart_);
    }
}