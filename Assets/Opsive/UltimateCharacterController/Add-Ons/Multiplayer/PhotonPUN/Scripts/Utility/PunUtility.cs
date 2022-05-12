/// ---------------------------------------------
/// Ultimate Character Controller
/// Copyright (c) Opsive. All Rights Reserved.
/// https://www.opsive.com
/// ---------------------------------------------

namespace Opsive.UltimateCharacterController.AddOns.Multiplayer.PhotonPun.Utility
{
    using Opsive.Shared.Game;
    using Opsive.UltimateCharacterController.AddOns.Multiplayer.Utility;
    using Opsive.UltimateCharacterController.Inventory;
    using Opsive.UltimateCharacterController.Objects;
    using Photon.Pun;
    using System.Collections.Generic;
    using UnityEngine;

    /// <summary>
    /// Small utility methods that interact with PUN.
    /// </summary>
    public static class PunUtility
    {
        private static Dictionary<GameObject, Dictionary<uint, ObjectIdentifier>> s_IDObjectIDMap = new Dictionary<GameObject, Dictionary<uint, ObjectIdentifier>>();
        private static Dictionary<uint, ObjectIdentifier> s_SceneIDMap = new Dictionary<uint, ObjectIdentifier>();

        /// <summary>
        /// Registers the Object Identifier within the scene ID map.
        /// </summary>
        /// <param name="sceneObjectIdentifier">The Object Identifier that should be registered.</param>
        public static void RegisterSceneObjectIdentifier(ObjectIdentifier sceneObjectIdentifier)
        {
            if (s_SceneIDMap.ContainsKey(sceneObjectIdentifier.ID)) {
                Debug.LogError($"Error: The scene object ID {sceneObjectIdentifier.ID} already exists. This can be corrected by running Scene Setup again on this scene.", sceneObjectIdentifier);
                return;
            }
            s_SceneIDMap.Add(sceneObjectIdentifier.ID, sceneObjectIdentifier);
        }

        /// <summary>
        /// Returns the PUN-friendly ID for the specified GameObject.
        /// </summary>
        /// <param name="gameObject">The GameObject to get the ID of.</param>
        /// <param name="itemSlotID">If the object is an item then return the slot ID of the item.</param>
        /// <returns>The ID for the specified GameObject.</returns>
        public static uint GetID(GameObject gameObject, out int itemSlotID)
        {
            itemSlotID = -1;
            if (gameObject == null) {
                return 0;
            }

            uint id = 0;
            var hasID = false;
            var photonView = gameObject.GetCachedComponent<PhotonView>();
            if (photonView != null) {
                id = (uint)photonView.ViewID;
                hasID = true;
            } else {
                // Try to get the ObjectIdentifier.
                var objectIdentifier = gameObject.GetCachedComponent<ObjectIdentifier>();
                if (objectIdentifier != null) {
                    id = objectIdentifier.ID;
                    hasID = true;
                } else {
                    // The object may be an item.
                    var inventory = gameObject.GetCachedParentComponent<InventoryBase>();
                    if (inventory != null) {
                        for (int i = 0; i < inventory.SlotCount; ++i) {
                            var item = inventory.GetActiveItem(i);
                            if (item == null) {
                                continue;
                            }
                            var visibleObject = item.ActivePerspectiveItem.GetVisibleObject();
                            if (gameObject == visibleObject) {
                                id = item.ItemIdentifier.ID;
                                itemSlotID = item.SlotID;
                                hasID = true;
                                break;
                            }
                        }

                        // The item may be a holstered item.
                        if (!hasID) {
                            var allItems = inventory.GetAllItems();
                            for (int i = 0; i < allItems.Count; ++i) {
                                var visibleObject = allItems[i].ActivePerspectiveItem.GetVisibleObject();
                                if (gameObject == visibleObject) {
                                    id = allItems[i].ItemIdentifier.ID;
                                    itemSlotID = allItems[i].SlotID;
                                    hasID = true;
                                    break;
                                }
                            }
                        }
                    }
                }
            }
            if (!hasID) {
                Debug.LogWarning($"Error: The object {gameObject.name} does not contain a PhotonView or ObjectIdentifier. It will not be able to be sent over the network.");
            }

            return id;
        }

        /// <summary>
        /// Retrieves the GameObject with the specified ID.
        /// </summary>
        /// <param name="parent">The parent GameObject to the object with the specified ID.</param>
        /// <param name="id">The ID to search for.</param>
        /// <param name="itemSlotID">If the object is an item then the slot ID will specify which slot the item is from.</param>
        /// <returns>The GameObject with the specified ID. Can be null.</returns>
        public static GameObject RetrieveGameObject(GameObject parent, uint id, int itemSlotID)
        {
            if (id == 0) {
                return null;
            }

            // The ID can be a PhotonView, ObjectIdentifier, or Item ID. Search for the ObjectIdentifier first and then the PhotonView.
            GameObject gameObject = null;
            if (itemSlotID == -1) {
                Dictionary<uint, ObjectIdentifier> idObjectIDMap;
                if (parent == null) {
                    idObjectIDMap = s_SceneIDMap;
                } else {
                    if (!s_IDObjectIDMap.TryGetValue(parent, out idObjectIDMap)) {
                        idObjectIDMap = new Dictionary<uint, ObjectIdentifier>();
                        s_IDObjectIDMap.Add(parent, idObjectIDMap);
                    }
                }
                ObjectIdentifier objectIdentifier = null;
                if (!idObjectIDMap.TryGetValue(id, out objectIdentifier)) {
                    // The ID doesn't exist in the cache. Try to find the object.
                    var objectIdentifiers = parent == null ? GameObject.FindObjectsOfType<ObjectIdentifier>() : parent.GetComponentsInChildren<ObjectIdentifier>(true);
                    if (objectIdentifiers != null) {
                        for (int i = 0; i < objectIdentifiers.Length; ++i) {
                            if (objectIdentifiers[i].ID == id) {
                                objectIdentifier = objectIdentifiers[i];
                                break;
                            }
                        }
                    }
                    idObjectIDMap.Add(id, objectIdentifier);
                }
                if (objectIdentifier == null) {
                    // The object isn't identified with an ObjectIdentifier. Search the PhotonViews.
                    var hitPhotonView = PhotonNetwork.GetPhotonView((int)id);
                    if (hitPhotonView != null) {
                        gameObject = hitPhotonView.gameObject;
                    }
                } else {
                    gameObject = objectIdentifier.gameObject;
                }
            } else { // The ID is an item.
                if (parent == null) {
                    Debug.LogError("Error: The parent must exist in order to retrieve the item ID.");
                    return null;
                }

                var itemIdentifier = Game.ItemIdentifierTracker.GetItemIdentifier(id);
                if (itemIdentifier == null) {
                    Debug.LogError($"Error: The ItemIdentifier with id {id} does not exist.");
                    return null;
                }

                var inventory = parent.GetCachedParentComponent<InventoryBase>();
                if (inventory == null) {
                    Debug.LogError("Error: The parent does not contain an inventory.");
                    return null;
                }

                var item = inventory.GetItem(itemIdentifier, itemSlotID);
                // The item may not exist if it was removed shortly after it was hit on sending client.
                if (item == null) {
                    return null;
                }

                return item.ActivePerspectiveItem.GetVisibleObject();
            }

            return gameObject;
        }

        /// <summary>
        /// Unregisters the Object Identifier within the scene ID map.
        /// </summary>
        /// <param name="sceneObjectIdentifier">The Object Identifier that should be unregistered.</param>
        public static void UnregisterSceneObjectIdentifier(ObjectIdentifier sceneObjectIdentifier)
        {
            s_SceneIDMap.Remove(sceneObjectIdentifier.ID);
        }

        /// <summary>
        /// Receives a compressed Vector3 over the PhotonStream.
        /// </summary>
        /// <param name="stream">The stream to write.</param>
        /// <param name="vector3">The Vector3 value.</param>
        public static void SendCompressedVector3(PhotonStream stream, Vector3 vector3)
        {
            stream.SendNext(NetworkCompression.FloatToShort(vector3.x));
            stream.SendNext(NetworkCompression.FloatToShort(vector3.y));
            stream.SendNext(NetworkCompression.FloatToShort(vector3.z));
        }

        /// <summary>
        /// Receives a compressed Vector3 from the PhotonStream.
        /// </summary>
        /// <param name="stream">The stream to read.</param>
        /// <returns>The Vector3 value.</returns>
        public static Vector3 ReceiveCompressedVector3(PhotonStream stream)
        {
            return new Vector3(NetworkCompression.ShortToFloat((short)stream.ReceiveNext()), 
                               NetworkCompression.ShortToFloat((short)stream.ReceiveNext()),
                               NetworkCompression.ShortToFloat((short)stream.ReceiveNext()));
        }
    }
}