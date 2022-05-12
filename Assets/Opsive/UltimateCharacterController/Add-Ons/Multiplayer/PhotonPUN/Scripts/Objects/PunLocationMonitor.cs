/// ---------------------------------------------
/// Ultimate Character Controller
/// Copyright (c) Opsive. All Rights Reserved.
/// https://www.opsive.com
/// ---------------------------------------------

namespace Opsive.UltimateCharacterController.AddOns.Multiplayer.PhotonPun.Objects
{
    using Opsive.Shared.Events;
    using Opsive.UltimateCharacterController.AddOns.Multiplayer.PhotonPun.Utility;
    using Opsive.UltimateCharacterController.Networking.Game;
    using Photon.Pun;
    using Photon.Realtime;
    using UnityEngine;

    /// <summary>
    /// Synchronizes the object's GameObject, Transform or Rigidbody values over the network.
    /// </summary>
    [RequireComponent(typeof(PhotonView))]
    public class PunLocationMonitor : MonoBehaviour, IPunObservable
    {
        [Tooltip("Should the GameObject's active state be syncornized?")]
        [SerializeField] protected bool m_SynchronizeActiveState = true;
        [Tooltip("Should the transform's position be synchronized?")]
        [SerializeField] protected bool m_SynchronizePosition = true;
        [Tooltip("Should the transform's rotation be synchronized?")]
        [SerializeField] protected bool m_SynchronizeRotation = true;
        [Tooltip("Should the transform's scale be synchronized?")]
        [SerializeField] protected bool m_SynchronizeScale;

        public bool SynchronizeActiveState { get { return m_SynchronizeActiveState; } set { m_SynchronizeActiveState = value; } }
        public bool SynchronizePosition { get { return m_SynchronizePosition; } set { m_SynchronizePosition = value; } }
        public bool SynchronizeRotation { get { return m_SynchronizeRotation; } set { m_SynchronizeRotation = value; } }
        public bool SynchronizeScale { get { return m_SynchronizeScale; } set { m_SynchronizeScale = value; } }

        private GameObject m_GameObject;
        private Transform m_Transform;
        private Rigidbody m_Rigidbody;
        private PhotonView m_PhotonView;

        private Vector3 m_NetworkPosition;
        private Vector3 m_NetworkRigidbodyVelocity;
        private Quaternion m_NetworkRotation;
        private Vector3 m_NetworkRigidbodyAngularVelocity;
        private Vector3 m_NetworkScale;

        private float m_Distance;
        private float m_Angle;
        private bool m_InitialSync = true;

        /// <summary>
        /// Specifies which transform objects are dirty.
        /// </summary>
        private enum TransformDirtyFlags : byte
        {
            Position = 1,                   // The position has changed.
            RigidbodyVelocity = 2,          // The Rigidbody velocity has changed.
            Rotation = 4,                   // The rotation has changed.
            RigidbodyAngularVelocity = 8,   // The Rigidbody angular velocity has changed.
            Scale = 16                      // The scale has changed.
        }

        /// <summary>
        /// Initialize the default values.
        /// </summary>
        private void Awake()
        {
            m_GameObject = gameObject;
            m_Transform = transform;
            m_Rigidbody = GetComponent<Rigidbody>();
            m_PhotonView = GetComponent<PhotonView>();

            m_NetworkPosition = m_Transform.position;
            m_NetworkRotation = m_Transform.rotation;
        }

        /// <summary>
        /// The object has been enabled.
        /// </summary>
        private void OnEnable()
        {
            m_InitialSync = true;

            // If the object is pooled then the network object pool will manage the active state.
            if (m_SynchronizeActiveState && NetworkObjectPool.SpawnedWithPool(m_GameObject)) {
                m_SynchronizeActiveState = false;
            }

            if (m_SynchronizeActiveState && m_PhotonView.ViewID != 0 && m_PhotonView.IsMine) {
                m_PhotonView.RPC("SetActiveRPC", RpcTarget.Others, true);
            }
        }

        /// <summary>
        /// Registers for any interested events.
        /// </summary>
        private void Start()
        {
            if (m_PhotonView.IsMine) {
                EventHandler.RegisterEvent<Player, GameObject>("OnPlayerEnteredRoom", OnPlayerEnteredRoom);
            }
        }

        /// <summary>
        /// A player has entered the room. Ensure the joining player is in sync with the current game state.
        /// </summary>
        /// <param name="player">The Photon Player that entered the room.</param>
        /// <param name="character">The character that the player controls.</param>
        private void OnPlayerEnteredRoom(Player player, GameObject character)
        {
            if (!m_SynchronizeActiveState || m_PhotonView.ViewID == 0 || NetworkObjectPool.SpawnedWithPool(m_GameObject)) {
                return;
            }

            m_PhotonView.RPC("SetActiveRPC", player, m_GameObject.activeSelf);
        }

        /// <summary>
        /// Activates or deactivates the GameObject on the network.
        /// </summary>
        /// <param name="active">Should the GameObject be activated?</param>
        [PunRPC]
        private void SetActiveRPC(bool active)
        {
            m_GameObject.SetActive(active);
        }

        /// <summary>
        /// Updates the remote object's transform values.
        /// </summary>
        private void Update()
        {
            if (m_PhotonView.IsMine || m_Rigidbody == null) {
                return;
            }

            Synchronize();
        }

        /// <summary>
        /// Updates the remote object's transform values.
        /// </summary>
        private void FixedUpdate()
        {
            if (m_PhotonView.IsMine || m_Rigidbody != null) {
                return;
            }

            Synchronize();
        }

        /// <summary>
        /// Synchronizes the transform.
        /// </summary>
        private void Synchronize()
        {
            // The position and rotation should be applied immediately if it is the first sync.
            if (m_InitialSync) {
                if (m_SynchronizePosition) {
                    m_Transform.position = m_NetworkPosition;
                }
                if (m_SynchronizeRotation) {
                    m_Transform.rotation = m_NetworkRotation;
                }
                m_InitialSync = false;
                return;
            }

            if (m_SynchronizePosition) {
                m_Transform.position = Vector3.MoveTowards(transform.position, m_NetworkPosition, m_Distance * (1.0f / PhotonNetwork.SerializationRate));
            }
            if (m_SynchronizeRotation) {
                m_Transform.rotation = Quaternion.RotateTowards(transform.rotation, m_NetworkRotation, m_Angle * (1.0f / PhotonNetwork.SerializationRate));
            }
        }

        /// <summary>
        /// Called by PUN several times per second, so that your script can write and read synchronization data for the PhotonView.
        /// </summary>
        /// <param name="stream">The stream that is being written to/read from.</param>
        /// <param name="info">Contains information about the message.</param>
        public void OnPhotonSerializeView(PhotonStream stream, PhotonMessageInfo info)
        {
            if (stream.IsWriting) {
                // Determine the dirty objects before sending the value.
                byte dirtyFlag = 0;
                if (m_SynchronizePosition) {
                    if (m_NetworkPosition != m_Transform.position) {
                        dirtyFlag |= (byte)TransformDirtyFlags.Position;
                        m_NetworkPosition = m_Transform.position;
                    }
                    if (m_Rigidbody != null && m_NetworkRigidbodyVelocity != m_Rigidbody.velocity) {
                        dirtyFlag |= (byte)TransformDirtyFlags.RigidbodyVelocity;
                        m_NetworkRigidbodyVelocity = m_Rigidbody.velocity;
                    }
                }
                if (m_SynchronizeRotation) {
                    if (m_NetworkRotation != m_Transform.rotation) {
                        dirtyFlag |= (byte)TransformDirtyFlags.Rotation;
                        m_NetworkRotation = m_Transform.rotation;
                    }
                    if (m_Rigidbody != null && m_NetworkRigidbodyAngularVelocity != m_Rigidbody.angularVelocity) {
                        dirtyFlag |= (byte)TransformDirtyFlags.RigidbodyAngularVelocity;
                        m_NetworkRigidbodyAngularVelocity = m_Rigidbody.angularVelocity;
                    }
                }
                if (m_SynchronizeScale) {
                    if (m_NetworkScale != m_Transform.localScale) {
                        dirtyFlag |= (byte)TransformDirtyFlags.Scale;
                        m_NetworkScale = m_Transform.localScale;
                    }
                }

                // Send the current GameObject and Transform values to all remote players.
                if (dirtyFlag != 0) {
                    stream.SendNext(dirtyFlag);
                }
                if (m_SynchronizePosition) {
                    if ((dirtyFlag & (byte)TransformDirtyFlags.Position) != 0) {
                        PunUtility.SendCompressedVector3(stream, m_Transform.position);
                        PunUtility.SendCompressedVector3(stream, m_Transform.position - m_NetworkPosition);
                        m_NetworkPosition = m_Transform.position;
                    }
                    if ((dirtyFlag & (byte)TransformDirtyFlags.RigidbodyVelocity) != 0 && m_Rigidbody != null) {
                        PunUtility.SendCompressedVector3(stream, m_Rigidbody.velocity);
                    }
                }

                if (m_SynchronizeRotation) {
                    if ((dirtyFlag & (byte)TransformDirtyFlags.Rotation) != 0) {
                        PunUtility.SendCompressedVector3(stream, m_Transform.eulerAngles);
                    }
                    if ((dirtyFlag & (byte)TransformDirtyFlags.RigidbodyAngularVelocity) != 0 && m_Rigidbody != null) {
                        PunUtility.SendCompressedVector3(stream, m_Rigidbody.angularVelocity);
                    }
                }

                if (m_SynchronizeScale) {
                    if ((dirtyFlag & (byte)TransformDirtyFlags.Scale) != 0) {
                        PunUtility.SendCompressedVector3(stream, m_Transform.localScale);
                    }
                }
            } else {
                // Receive the GameObject and Transform values.
                // The position and rotation will then be used within the Update method to actually move the character.
                var dirtyFlag = (byte)stream.ReceiveNext();
                if (m_SynchronizePosition) {
                    if ((dirtyFlag & (byte)TransformDirtyFlags.Position) != 0) {
                        m_NetworkPosition = PunUtility.ReceiveCompressedVector3(stream);
                        var velocity = PunUtility.ReceiveCompressedVector3(stream);
                        if (!m_InitialSync) {
                            // Compensate for the lag.
                            var lag = Mathf.Abs((float)(PhotonNetwork.Time - info.SentServerTime));
                            m_NetworkPosition += velocity * lag;
                        }
                        m_Distance = Vector3.Distance(m_Transform.position, m_NetworkPosition);
                    }

                    if ((dirtyFlag & (byte)TransformDirtyFlags.RigidbodyVelocity) != 0 && m_Rigidbody != null) {
                        m_Rigidbody.velocity = PunUtility.ReceiveCompressedVector3(stream);
                    }
                }

                if (m_SynchronizeRotation) {
                    if ((dirtyFlag & (byte)TransformDirtyFlags.Rotation) != 0) {
                        m_NetworkRotation = Quaternion.Euler(PunUtility.ReceiveCompressedVector3(stream));
                        m_Angle = Quaternion.Angle(m_Transform.rotation, m_NetworkRotation);
                    }

                    if ((dirtyFlag & (byte)TransformDirtyFlags.RigidbodyAngularVelocity) != 0 && m_Rigidbody != null) {
                        m_Rigidbody.angularVelocity = PunUtility.ReceiveCompressedVector3(stream);
                    }
                }

                if (m_SynchronizeScale) {
                    if ((dirtyFlag & (byte)TransformDirtyFlags.Scale) != 0) {
                        m_Transform.localScale = PunUtility.ReceiveCompressedVector3(stream);
                    }
                }
            }
        }

        /// <summary>
        /// The object has been deactivated.
        /// </summary>
        private void OnDisable()
        {
            if (m_SynchronizeActiveState && m_PhotonView.ViewID != 0 && PhotonNetwork.IsConnected) {
                m_PhotonView.RPC("SetActiveRPC", RpcTarget.Others, false);
            }
        }

        /// <summary>
        /// The object has been destroyed.
        /// </summary>
        private void OnDestroy()
        {
            EventHandler.UnregisterEvent<Player, GameObject>("OnPlayerEnteredRoom", OnPlayerEnteredRoom);
        }
    }
}