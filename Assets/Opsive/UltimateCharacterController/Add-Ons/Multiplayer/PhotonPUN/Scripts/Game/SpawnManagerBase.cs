/// ---------------------------------------------
/// Ultimate Character Controller
/// Copyright (c) Opsive. All Rights Reserved.
/// https://www.opsive.com
/// ---------------------------------------------

namespace Opsive.UltimateCharacterController.AddOns.Multiplayer.PhotonPun.Game
{
    using ExitGames.Client.Photon;
    using Opsive.Shared.Events;
    using Opsive.Shared.Game;
    using Opsive.UltimateCharacterController.Game;
    using Photon.Pun;
    using Photon.Realtime;
    using System.Collections.Generic;
    using UnityEngine;

    /// <summary>
    /// Manages the character instantiation within a PUN room.
    /// </summary>
    public abstract class SpawnManagerBase : MonoBehaviourPunCallbacks, IOnEventCallback
    {
        // Specifies the location that the character should spawn.
        public enum SpawnMode
        {
            FixedLocation,  // Always spawns the character in a fixed location.
            SpawnPoint      // Uses the Spawn Point system.
        }

        [Tooltip("Specifies the location that the character should spawn.")]
        [SerializeField] protected SpawnMode m_Mode;
        [Tooltip("The position the character should spawn if the SpawnMode is set to FixedLocation.")]
        [SerializeField] protected Transform m_SpawnLocation;
        [Tooltip("The offset to apply to the spawn position multiplied by the number of characters within the room.")]
        [SerializeField] protected Vector3 m_SpawnLocationOffset = new Vector3(2, 0, 0);
        [Tooltip("The grouping index to use when spawning to a spawn point. A value of -1 will ignore the grouping value.")]
        [SerializeField] protected int m_SpawnPointGrouping = -1;
        [Tooltip("The amount of time it takes until an inactive player is removed from the room.")]
        [SerializeField] protected float m_InactiveTimeout = 60;

        public SpawnMode Mode { get { return m_Mode; } set { m_Mode = value; } }
        public Transform SpawnLocation { get { return m_SpawnLocation; } set { m_SpawnLocation = value; } }
        public Vector3 SpawnLocationOffset { get { return m_SpawnLocationOffset; } set { m_SpawnLocationOffset = value; } }
        public int SpawnPointGrouping { get { return m_SpawnPointGrouping; } set { m_SpawnPointGrouping = value; } }

        private PhotonView[] m_Players;
        private int m_PlayerCount;

        private SendOptions m_ReliableSendOption;
        private RaiseEventOptions m_RaiseEventOptions;
        private Dictionary<int, int> m_ActorNumberByPhotonViewIndex;
        private Dictionary<Player, InactivePlayer> m_InactivePlayers;

        /// <summary>
        /// Stores the data about the player that became inactive.
        /// </summary>
        private struct InactivePlayer
        {
            public int PlayerIndex;
            public Vector3 Position;
            public Quaternion Rotation;
            public ScheduledEventBase RemoveEvent;

            /// <summary>
            /// Constructor for the InactivePlayer struct.
            /// </summary>
            /// <param name="index">The index within the Player array.</param>
            /// <param name="position">The last position of the player.</param>
            /// <param name="rotation">The last rotation of the player.</param>
            /// <param name="removeEvent">The event that will remove the player from the room.</param>
            public InactivePlayer(int index, Vector3 position, Quaternion rotation, ScheduledEventBase removeEvent)
            {
                PlayerIndex = index;
                Position = position;
                Rotation = rotation;
                RemoveEvent = removeEvent;
            }
        }

        /// <summary>
        /// Initialize the default values.
        /// </summary>
        private void Start()
        {
            var kinematicObjectManager = GameObject.FindObjectOfType<KinematicObjectManager>();
            m_Players = new PhotonView[kinematicObjectManager.StartCharacterCount];
            m_ActorNumberByPhotonViewIndex = new Dictionary<int, int>();

            // Cache the raise event options.
            m_ReliableSendOption = new SendOptions { Reliability = true };
            m_RaiseEventOptions = new RaiseEventOptions();
            m_RaiseEventOptions.CachingOption = EventCaching.DoNotCache;
            m_RaiseEventOptions.Receivers = ReceiverGroup.Others;

            SpawnPlayer(PhotonNetwork.LocalPlayer);
        }

        /// <summary>
        /// Spawns the character within the room. A manual spawn method is used to have complete control over the spawn location.
        /// </summary>
        /// <param name="newPlayer">The player that entered the room.</param>
        public void SpawnPlayer(Player newPlayer)
        {
            // Only the master client can spawn new players.
            if (!PhotonNetwork.IsMasterClient) {
                return;
            }

            var determineSpawnLocation = true;
            var spawnPosition = Vector3.zero;
            var spawnRotation = Quaternion.identity;

            InactivePlayer inactivePlayer;
            if (m_InactivePlayers != null && m_InactivePlayers.TryGetValue(newPlayer, out inactivePlayer)) {
                // The player has rejoined the game. The character does not need to go through the full spawn procedure.
                Scheduler.Cancel(inactivePlayer.RemoveEvent);
                m_InactivePlayers.Remove(newPlayer);

                // The spawn location is determined by the last disconnected location.
                spawnPosition = inactivePlayer.Position;
                spawnRotation = inactivePlayer.Rotation;
                determineSpawnLocation = false;
            }

            // Spawn the new player based on the spawn mode.
            if (determineSpawnLocation) {
                if (m_Mode == SpawnMode.SpawnPoint) {
                    if (!SpawnPointManager.GetPlacement(null, m_SpawnPointGrouping, ref spawnPosition, ref spawnRotation)) {
                        Debug.LogWarning($"Warning: The Spawn Point Manager is unable to determine a spawn location for grouping {m_SpawnPointGrouping}. " +
                                         "Consider adding more spawn points.");
                    }
                } else {
                    if (m_SpawnLocation != null) {
                        spawnPosition = m_SpawnLocation.position;
                        spawnRotation = m_SpawnLocation.rotation;
                    }
                    spawnPosition += m_PlayerCount * m_SpawnLocationOffset;
                }
            }

            // Instantiate the player and let the PhotonNetwork know of the new character.
            var player = GameObject.Instantiate(GetCharacterPrefab(newPlayer), spawnPosition, spawnRotation);
            var photonView = player.GetComponent<PhotonView>();
            photonView.ViewID = PhotonNetwork.AllocateViewID(newPlayer.ActorNumber);
            if (photonView.ViewID > 0) {
                // As of PUN 2.19, when the ViewID is allocated the Owner is not set. Set the owner to null and then to the player so the owner will correctly be assigned.
                photonView.TransferOwnership(null);
                photonView.TransferOwnership(newPlayer);

                // The character has been created. All other clients need to instantiate the character as well.
                var data = new object[]
                {
                    player.transform.position, player.transform.rotation, photonView.ViewID, newPlayer.ActorNumber
                };
                m_RaiseEventOptions.TargetActors = null;
                PhotonNetwork.RaiseEvent(PhotonEventIDs.PlayerInstantiation, data, m_RaiseEventOptions, m_ReliableSendOption);

                // The new player should instantiate all existing characters in addition to their character.
                if (newPlayer != PhotonNetwork.LocalPlayer) {
                    // Deactivate the character until the remote machine has the chance to create it. This will prevent the character from
                    // being active on the Master Client without being able to be controlled.
                    player.SetActive(false);

                    data = new object[m_PlayerCount * 4];
                    for (int i = 0; i < m_PlayerCount; ++i) {
                        data[i * 4] = m_Players[i].transform.position;
                        data[i * 4 + 1] = m_Players[i].transform.rotation;
                        data[i * 4 + 2] = m_Players[i].ViewID;
                        data[i * 4 + 3] = m_Players[i].Owner.ActorNumber;
                    }
                    m_RaiseEventOptions.TargetActors = new int[] { newPlayer.ActorNumber };
                    PhotonNetwork.RaiseEvent(PhotonEventIDs.PlayerInstantiation, data, m_RaiseEventOptions, m_ReliableSendOption);
                }

                AddPhotonView(photonView);
                EventHandler.ExecuteEvent("OnPlayerEnteredRoom", photonView.Owner, photonView.gameObject);
            } else {
                Debug.LogError("Failed to allocate a ViewId.");
                Destroy(player);
            }
        }

        /// <summary>
        /// Abstract method that allows for a character to be spawned based on the game logic.
        /// </summary>
        /// <param name="newPlayer">The player that entered the room.</param>
        /// <returns>The character prefab that should spawn.</returns>
        protected abstract GameObject GetCharacterPrefab(Player newPlayer);

        /// <summary>
        /// Adds the PhotonView to the player list.
        /// </summary>
        /// <param name="photonView">The PhotonView that should be added.</param>
        private void AddPhotonView(PhotonView photonView)
        {
            if (m_PlayerCount == m_Players.Length) {
                System.Array.Resize(ref m_Players, m_PlayerCount + 1);
            }
            m_Players[m_PlayerCount] = photonView;
            m_ActorNumberByPhotonViewIndex.Add(photonView.OwnerActorNr, m_PlayerCount);
            m_PlayerCount++;
        }

        /// <summary>
        /// Called when a remote player entered the room. This Player is already added to the playerlist.
        /// </summary>
        /// <param name="newPlayer">The player that entered the room.</param>
        public override void OnPlayerEnteredRoom(Player newPlayer)
        {
            base.OnPlayerEnteredRoom(newPlayer);

            SpawnPlayer(newPlayer);
        }

        /// <summary>
        /// Called when a local player leaves the room.
        /// </summary>
        public override void OnLeftRoom()
        {
            base.OnLeftRoom();

            // The local player has left the room. Cleanup like the player has permanently disconnected. If they rejoin at a later time the normal initialization process will run.
            for (int i = 0; i < m_PlayerCount; ++i) {
                if (m_Players[i] == null) {
                    continue;
                }
                EventHandler.ExecuteEvent("OnPlayerLeftRoom", m_Players[i].Owner, m_Players[i].gameObject);
                GameObject.Destroy(m_Players[i].gameObject);
            }
            m_PlayerCount = 0;
        }

        /// <summary>
        /// Called when a remote player left the room or became inactive. Check otherPlayer.IsInactive.
        /// </summary>
        /// <param name="otherPlayer">The player that left the room.</param>
        public override void OnPlayerLeftRoom(Player otherPlayer)
        {
            base.OnPlayerLeftRoom(otherPlayer);

            // Notify others that the player has left the room.
            if (m_ActorNumberByPhotonViewIndex.TryGetValue(otherPlayer.ActorNumber, out int index)) {
                var photonView = m_Players[index];
                // Inactive players may rejoin. Remember the last location of the inactive player.
                if (otherPlayer != null && otherPlayer.IsInactive) {
                    if (m_InactivePlayers == null) {
                        m_InactivePlayers = new Dictionary<Player, InactivePlayer>();
                    }
                    var removeEvent = Scheduler.Schedule<Player>(m_InactiveTimeout, (Player player) => { m_InactivePlayers.Remove(player); }, otherPlayer);
                    m_InactivePlayers.Add(otherPlayer, new InactivePlayer(index, photonView.transform.position, photonView.transform.rotation, removeEvent));
                }

                EventHandler.ExecuteEvent("OnPlayerLeftRoom", otherPlayer, photonView.gameObject);
                GameObject.Destroy(photonView.gameObject);
                m_ActorNumberByPhotonViewIndex.Remove(otherPlayer.ActorNumber);
                for (int j = index; j < m_PlayerCount - 1; ++j) {
                    m_Players[j] = m_Players[j + 1];
                    m_ActorNumberByPhotonViewIndex[m_Players[j + 1].Owner.ActorNumber] = j;
                }
                m_PlayerCount--;
            }
        }

        /// <summary>
        /// A event from Photon has been sent.
        /// </summary>
        /// <param name="photonEvent">The Photon event.</param>
        public void OnEvent(EventData photonEvent)
        {
            if (photonEvent.Code == PhotonEventIDs.PlayerInstantiation) {
                // The Master Client has instantiated a character. Create that character on the local client as well.
                var data = (object[])photonEvent.CustomData;
                for (int i = 0; i < data.Length / 4; ++i) {
                    var viewID = (int)data[i * 4 + 2];
                    if (PhotonNetwork.GetPhotonView(viewID) != null) {
                        continue;
                    }

                    var player = PhotonNetwork.CurrentRoom.GetPlayer((int)data[i * 4 + 3]);
                    var character = Instantiate(GetCharacterPrefab(player), (Vector3)data[i * 4], (Quaternion)data[i * 4 + 1]);
                    var photonView = character.GetCachedComponent<PhotonView>();
                    photonView.ViewID = viewID;
                    // As of PUN 2.19, when the ViewID is set the Owner is not set. Set the owner to null and then to the player so the owner will correctly be assigned.
                    photonView.TransferOwnership(null);
                    photonView.TransferOwnership(player);
                    AddPhotonView(photonView);

                    // If the instantiated character is a local player then the Master Client is waiting for it to be created on the client. Notify the Master Client
                    // that the character has been created so it can be activated.
                    if (photonView.IsMine) {
                        m_RaiseEventOptions.TargetActors = new int[] { PhotonNetwork.MasterClient.ActorNumber };
                        PhotonNetwork.RaiseEvent(PhotonEventIDs.RemotePlayerInstantiationComplete, photonView.Owner.ActorNumber, m_RaiseEventOptions, m_ReliableSendOption);
                    } else {
                        // Call start manually before any events are received. This ensures the remote character has been initialized.
                        var characterLocomotion = character.GetCachedComponent<UltimateCharacterController.Character.UltimateCharacterLocomotion>();
                        characterLocomotion.Start();
                    }
                    EventHandler.ExecuteEvent("OnPlayerEnteredRoom", photonView.Owner, photonView.gameObject);
                }
            } else if (photonEvent.Code == PhotonEventIDs.RemotePlayerInstantiationComplete) {
                // The remote player has instantiated the character. It can now be enabled (on the Master Client).
                var ownerActor = (int)photonEvent.CustomData;
                for (int i = 0; i < m_PlayerCount; ++i) {
                    if (m_Players[i].Owner.ActorNumber == ownerActor) {
                        m_Players[i].gameObject.SetActive(true);
                        break;
                    }
                }
            }
        }
    }
}