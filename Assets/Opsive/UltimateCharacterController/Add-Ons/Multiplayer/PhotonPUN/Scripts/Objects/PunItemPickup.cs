/// ---------------------------------------------
/// Ultimate Character Controller
/// Copyright (c) Opsive. All Rights Reserved.
/// https://www.opsive.com
/// ---------------------------------------------

namespace Opsive.UltimateCharacterController.AddOns.Multiplayer.PhotonPun.Objects
{
    using Opsive.Shared.Game;
    using Opsive.Shared.Utility;
    using Opsive.UltimateCharacterController.Inventory;
    using Opsive.UltimateCharacterController.AddOns.Multiplayer.PhotonPun.Game;
    using Opsive.UltimateCharacterController.Objects;
    using Opsive.UltimateCharacterController.Objects.CharacterAssist;
    using Photon.Pun;
    using UnityEngine;

    /// <summary>
    /// Initializes the item pickup over the network.
    /// </summary>
    public class PunItemPickup : ItemPickup, ISpawnDataObject
    {
        private object[] m_SpawnData;
        private object[] m_InstantiationData;
        public object[] InstantiationData { get => m_InstantiationData; set => m_InstantiationData = value; }

        private TrajectoryObject m_TrajectoryObject;

        /// <summary>
        /// Initialize the default values.
        /// </summary>
        protected override void Awake()
        {
            base.Awake();

            m_TrajectoryObject = gameObject.GetCachedComponent<TrajectoryObject>();
        }

        /// <summary>
        /// Returns the initialization data that is required when the object spawns. This allows the remote players to initialize the object correctly.
        /// </summary>
        /// <returns>The initialization data that is required when the object spawns.</returns>
        public object[] SpawnData()
        {
            var objLength = m_ItemDefinitionAmounts.Length * 2 + (m_TrajectoryObject != null ? 3 : 0);
            if (m_SpawnData == null) {
                m_SpawnData = new object[objLength];
            } else if (m_SpawnData.Length != objLength) {
                System.Array.Resize(ref m_SpawnData, objLength);
            }

            for (int i = 0; i < m_ItemDefinitionAmounts.Length; ++i) {
                m_SpawnData[i * 2] = (m_ItemDefinitionAmounts[i].ItemIdentifier as ItemType).ID;
                m_SpawnData[i * 2 + 1] = m_ItemDefinitionAmounts[i].Amount;
            }

            // The trajectory data needs to also be sent.
            if (m_TrajectoryObject != null) {
                m_SpawnData[m_SpawnData.Length - 3] = m_TrajectoryObject.Velocity;
                m_SpawnData[m_SpawnData.Length - 2] = m_TrajectoryObject.Torque;
                var originatorID = -1;
                if (m_TrajectoryObject.Originator != null) {
                    var originatorView = m_TrajectoryObject.Originator.GetCachedComponent<PhotonView>();
                    if (originatorView != null) {
                        originatorID = originatorView.ViewID;
                    }
                }
                m_SpawnData[m_SpawnData.Length - 1] = originatorID;
            }

            return m_SpawnData;
        }

        /// <summary>
        /// The object has been spawned. Initialize the item pickup.
        /// </summary>
        public void ObjectSpawned()
        {
            if (m_InstantiationData == null) {
                return;
            }

            // Return the old.
            for (int i = 0; i < m_ItemDefinitionAmounts.Length; ++i) {
                GenericObjectPool.Return(m_ItemDefinitionAmounts[i]);
            }

            // Setup the item counts.
            var itemDefinitionAmountLength = (m_InstantiationData.Length - (m_TrajectoryObject != null ? 3 : 0)) / 2;
            if (m_ItemDefinitionAmounts.Length != itemDefinitionAmountLength) {
                m_ItemDefinitionAmounts = new ItemDefinitionAmount[itemDefinitionAmountLength];
            }
            for (int i = 0; i < itemDefinitionAmountLength; ++i) {
                m_ItemDefinitionAmounts[i] = new ItemDefinitionAmount(ItemIdentifierTracker.GetItemIdentifier((uint)m_InstantiationData[i * 2]).GetItemDefinition(), 
                                                            (int)m_InstantiationData[i * 2 + 1]);
            }
            Initialize(true);

            // Setup the trajectory object.
            if (m_TrajectoryObject != null) {
                var velocity = (Vector3)m_InstantiationData[m_InstantiationData.Length - 3];
                var torque = (Vector3)m_InstantiationData[m_InstantiationData.Length - 2];
                var originatorID = (int)m_InstantiationData[m_InstantiationData.Length - 1];
                GameObject originator = null;
                if (originatorID != -1) {
                    var originatorView = PhotonNetwork.GetPhotonView(originatorID);
                    if (originatorView != null) {
                        originator = originatorView.gameObject;
                    }
                }
                m_TrajectoryObject.Initialize(velocity, torque, originator);
            }
        }
    }
}