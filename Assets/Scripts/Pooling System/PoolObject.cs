using Logger = LoggingUtils.Logger;
using UnityEngine;
using System;

[AddComponentMenu("Scripts/CoreGameScripts/PoolObject")]
public class PoolObject : MonoBehaviour {
    private ObjectPoolList _referencedPoolList;

    protected virtual void Start() {
        // If this is a scene object and not a prefab, _referendPoolList is null by default
        if (_referencedPoolList == null) {
            _referencedPoolList = ObjectPoolManager.Instance.GetSharedPoolList(GetInstanceID(), this);
        }
    }

    public void SetReferencedPoolList(ObjectPoolList referencedPoolList_) {
        _referencedPoolList = referencedPoolList_;
    }

    /// <summary>
    /// Disables the given pool object. Assumes this wants to be recycled by default.
    /// </summary>
    /// <param name="recycleObject">Should this be recycled into the pooled list? False will destroy the gameobject.</param>
    public void DisablePoolObject(bool recycleObject = true) {
        if (recycleObject) {
            gameObject.SetActive(false);

            // Shouldn't be called, but just in case, avoid errors.
            if (_referencedPoolList != null) {
                _referencedPoolList.SetObjectToInactive(gameObject);
            } else {
                Logger.Error($"PoolObject: {gameObject.name}, does not have _referencedPoolList set.");
            }
        } else {
            Destroy(gameObject);
        }
    }

    /// <summary>
    /// Resets the pool object to its default state by calling the action, and then sets the gameobject to active
    /// </summary>
    /// <param name="resetObjectToDefaultsAction">The action to call to reset the gameobject to defaults</param>
    public void EnablePoolObject(Action resetObjectToDefaultsAction) {
        resetObjectToDefaultsAction();
        gameObject.SetActive(true);
    }
}