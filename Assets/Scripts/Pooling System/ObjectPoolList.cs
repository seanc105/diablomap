using System.Collections.Generic;
using UnityEngine;

public class ObjectPoolList {
    private HashSet<GameObject> _activeObjects;
    private Stack<GameObject> _inactiveObjects;

    private PoolObject _poolObjectPrefab;

    public ObjectPoolList(PoolObject poolObjectPrefab_) {
        _activeObjects = new HashSet<GameObject>();
        _inactiveObjects = new Stack<GameObject>();

        _poolObjectPrefab = poolObjectPrefab_;

        // Add scene objects to the list since it's not a prefab
        if (poolObjectPrefab_.gameObject.scene.rootCount != 0) {
            _inactiveObjects.Push(poolObjectPrefab_.gameObject);
        }
    }

    /// <summary>
    /// Moves an object from inactive to active (or adds a new one if there are none available) and returns the ready object.
    /// </summary>
    /// <param name="positionToStart_">The Vector3 position to start at when retrieved</param>
    /// <param name="rotationToStart_">The Quaternion rotation to start at when retrieved</param>
    /// <returns>The newly active object</returns>
    public GameObject RetrieveNewlyActiveObject(Vector3 positionToStart_, Quaternion rotationToStart_) {
        GameObject readyObject;

        if (_inactiveObjects.Count == 0) {
            readyObject = Object.Instantiate(_poolObjectPrefab.gameObject, positionToStart_, rotationToStart_);
            readyObject.GetComponent<PoolObject>().SetReferencedPoolList(this);
            _activeObjects.Add(readyObject);
        } else {
            readyObject = _inactiveObjects.Pop();
            readyObject.transform.position = positionToStart_;
            readyObject.transform.rotation = rotationToStart_;
            readyObject.SetActive(true);
            _activeObjects.Add(readyObject);
        }

        return readyObject;
    }

    /// <summary>
    /// Sets the given object to inactive (if it's active) in the pool list so it can be ready for the next round of use
    /// </summary>
    /// <param name="poolObject_">The object to set to inactive</param>
    public void SetObjectToInactive(GameObject poolObject_) {
        if (_activeObjects.Contains(poolObject_)) {
            _activeObjects.Remove(poolObject_);
            _inactiveObjects.Push(poolObject_);
        }
    }

    /// <summary>
    /// Simply moves all the active objects to the inactive list
    /// </summary>
    public void SetAllObjectsToInactive() {
        foreach (GameObject gameObject in _activeObjects) {
            if (!_inactiveObjects.Contains(gameObject)) {
                gameObject.SetActive(false);
                _inactiveObjects.Push(gameObject);
            }
        }
        _activeObjects.Clear();
    }

    /// <summary>
    /// Clear all active and inactive objects from the pool list
    /// </summary>
    public void ClearPoolLists() {
        _activeObjects.Clear();
        _inactiveObjects.Clear();
    }
}