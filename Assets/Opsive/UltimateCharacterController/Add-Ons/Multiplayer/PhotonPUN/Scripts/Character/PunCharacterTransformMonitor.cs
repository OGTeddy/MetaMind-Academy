/// ---------------------------------------------
/// Ultimate Character Controller
/// Copyright (c) Opsive. All Rights Reserved.
/// https://www.opsive.com
/// ---------------------------------------------

namespace Opsive.UltimateCharacterController.AddOns.Multiplayer.PhotonPun.Character
{
    using Opsive.Shared.Events;
    using Opsive.Shared.Game;
    using Opsive.UltimateCharacterController.AddOns.Multiplayer.PhotonPun.Utility;
    using Opsive.UltimateCharacterController.Character;
    using Opsive.UltimateCharacterController.Utility;
    using Photon.Pun;
    using UnityEngine;

    /// <summary>
    /// Synchronizes the character's transform values over the network.
    /// </summary>
    public class PunCharacterTransformMonitor : MonoBehaviour, IPunObservable
    {
        [Tooltip("Should the transform's scale be synchronized?")]
        [SerializeField] protected bool m_SynchronizeScale;
        [Tooltip("A multiplier to apply to the interpolation destination for remote players.")]
        [SerializeField] protected float m_RemoteInterpolationMultiplayer = 1.2f;

        private Transform m_Transform;
        private PhotonView m_PhotonView;
        private UltimateCharacterLocomotion m_CharacterLocomotion;
        private CharacterFootEffects m_CharacterFootEffects;

        private Vector3 m_NetworkPosition;
        private Quaternion m_NetworkRotation;
        private Vector3 m_NetworkScale;

        private int m_PlatformPhotonViewID;
        private Quaternion m_NetworkPlatformRotationOffset;
        private Quaternion m_NetworkPlatformPrevRotationOffset;
        private Vector3 m_NetworkPlatformRelativePosition;
        private Vector3 m_NetworkPlatformPrevRelativePosition;

        private float m_Distance;
        private float m_Angle;
        private bool m_InitialSync = true;

        /// <summary>
        /// Specifies which transform objects are dirty.
        /// </summary>
        private enum TransformDirtyFlags : byte
        {
            Position = 1,           // The position has changed.
            Rotation = 2,           // The rotation has changed.
            Platform = 4,           // The platform has changed.
            Scale = 8              // The scale has changed.
        }

        /// <summary>
        /// Initialize the default values.
        /// </summary>
        private void Awake()
        {
            m_Transform = transform;
            m_PhotonView = gameObject.GetCachedComponent<PhotonView>();
            m_CharacterLocomotion = gameObject.GetCachedComponent<UltimateCharacterLocomotion>();
            m_CharacterFootEffects = gameObject.GetCachedComponent<CharacterFootEffects>();

            m_NetworkPosition = m_Transform.position;
            m_NetworkRotation = m_Transform.rotation;
            m_NetworkScale = m_Transform.localScale;

            EventHandler.RegisterEvent(gameObject, "OnRespawn", OnRespawn);
            EventHandler.RegisterEvent<bool>(gameObject, "OnCharacterImmediateTransformChange", OnImmediateTransformChange);
        }

        /// <summary>
        /// Updates the remote character's transform values.
        /// </summary>
        private void Update()
        {
            // Local players will move using the regular UltimateCharacterLocomotion.Move method.
            if (m_PhotonView.IsMine) {
                return;
            }

            // When the character is on a moving platform the position and rotation is relative to that platform. This allows the character to stay on the platform
            // even though the platform will not be in the exact same location between any two instances.
            var serializationRate = (1f / PhotonNetwork.SerializationRate) * m_RemoteInterpolationMultiplayer;
            if (m_CharacterLocomotion.Platform != null) {
#if ULTIMATE_CHARACTER_CONTROLLER_MULTIPLAYER
                if (m_CharacterFootEffects != null && (m_NetworkPlatformPrevRelativePosition - m_NetworkPlatformRelativePosition).sqrMagnitude > 0.01f) {
                    m_CharacterFootEffects.CanPlaceFootstep = true;
                }
#endif
                m_NetworkPlatformPrevRelativePosition = Vector3.MoveTowards(m_NetworkPlatformPrevRelativePosition, m_NetworkPlatformRelativePosition, m_Distance * serializationRate);
                m_NetworkPlatformPrevRotationOffset = Quaternion.RotateTowards(m_NetworkPlatformPrevRotationOffset, m_NetworkPlatformRotationOffset, m_Angle * serializationRate);
                m_CharacterLocomotion.SetPositionAndRotation(m_CharacterLocomotion.Platform.TransformPoint(m_NetworkPlatformPrevRelativePosition), MathUtility.TransformQuaternion(m_CharacterLocomotion.Platform.rotation, m_NetworkPlatformPrevRotationOffset), false);
            } else {
#if ULTIMATE_CHARACTER_CONTROLLER_MULTIPLAYER
                if (m_CharacterFootEffects != null && (m_Transform.position - m_NetworkPosition).sqrMagnitude > 0.01f) {
                    m_CharacterFootEffects.CanPlaceFootstep = true;
                }
#endif
                m_Transform.position = Vector3.MoveTowards(m_Transform.position, m_NetworkPosition, m_Distance * serializationRate);
                m_Transform.rotation = Quaternion.RotateTowards(m_Transform.rotation, m_NetworkRotation, m_Angle * serializationRate);
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
                byte dirtyFlag = 0;
                if (m_SynchronizeScale && m_Transform.localScale != m_NetworkScale) {
                    dirtyFlag |= (byte)TransformDirtyFlags.Scale;
                }

                // When the character is on a platform the position and rotation is relative to that platform.
                if (m_CharacterLocomotion.Platform != null) {
                    var platformPhotonView = m_CharacterLocomotion.Platform.gameObject.GetCachedComponent<PhotonView>();
                    if (platformPhotonView == null) {
                        Debug.LogError($"Error: The platform {m_CharacterLocomotion.Platform} must have a PhotonView.");
                        return;
                    }
                    // Determine the changed objects before sending them.
                    dirtyFlag |= (byte)TransformDirtyFlags.Platform;
                    var position = m_CharacterLocomotion.Platform.InverseTransformPoint(m_Transform.position);
                    var rotation = MathUtility.InverseTransformQuaternion(m_CharacterLocomotion.Platform.rotation, m_Transform.rotation);
                    if (position != m_NetworkPosition) {
                        dirtyFlag |= (byte)TransformDirtyFlags.Position;
                        m_NetworkPosition = position;
                    }
                    if (rotation != m_NetworkRotation) {
                        dirtyFlag |= (byte)TransformDirtyFlags.Rotation;
                        m_NetworkRotation = rotation;
                    }

                    stream.SendNext(dirtyFlag);
                    stream.SendNext(platformPhotonView.ViewID);
                    if ((dirtyFlag & (byte)TransformDirtyFlags.Position) != 0) {
                        PunUtility.SendCompressedVector3(stream, position);
                    }
                    if ((dirtyFlag & (byte)TransformDirtyFlags.Rotation) != 0) {
                        PunUtility.SendCompressedVector3(stream, rotation.eulerAngles);
                    }
                } else {
                    // Determine the changed objects before sending them.
                    if (m_Transform.position != m_NetworkPosition) {
                        dirtyFlag |= (byte)TransformDirtyFlags.Position;
                    }
                    if (m_Transform.rotation != m_NetworkRotation) {
                        dirtyFlag |= (byte)TransformDirtyFlags.Rotation;
                    }

                    stream.SendNext(dirtyFlag);
                    if ((dirtyFlag & (byte)TransformDirtyFlags.Position) != 0) {
                        PunUtility.SendCompressedVector3(stream, m_Transform.position);
                        PunUtility.SendCompressedVector3(stream, m_Transform.position - m_NetworkPosition);
                        m_NetworkPosition = m_Transform.position;
                    }
                    if ((dirtyFlag & (byte)TransformDirtyFlags.Rotation) != 0) {
                        PunUtility.SendCompressedVector3(stream, m_Transform.eulerAngles);
                        m_NetworkRotation = m_Transform.rotation;
                    }
                }

                if ((dirtyFlag & (byte)TransformDirtyFlags.Scale) != 0) {
                    PunUtility.SendCompressedVector3(stream, m_Transform.localScale);
                    m_NetworkScale = m_Transform.localScale;
                }
            } else {
                var dirtyFlag = (byte)stream.ReceiveNext();
                if ((dirtyFlag & (byte)TransformDirtyFlags.Platform) != 0) {
                    var platformPhotonViewID = (int)stream.ReceiveNext();

                    // When the character is on a platform the position and rotation is relative to that platform.
                    if ((dirtyFlag & (byte)TransformDirtyFlags.Position) != 0) {
                        m_NetworkPlatformRelativePosition = PunUtility.ReceiveCompressedVector3(stream);
                    }
                    if ((dirtyFlag & (byte)TransformDirtyFlags.Rotation) != 0) {
                        m_NetworkPlatformRotationOffset = Quaternion.Euler(PunUtility.ReceiveCompressedVector3(stream));
                    }

                    // Do not do any sort of interpolation when the platform has changed.
                    if (platformPhotonViewID != m_PlatformPhotonViewID) {
                        var platform = PhotonNetwork.GetPhotonView(platformPhotonViewID).transform;
                        m_CharacterLocomotion.SetPlatform(platform, true);
                        m_NetworkPlatformRelativePosition = m_NetworkPlatformPrevRelativePosition = platform.InverseTransformPoint(m_Transform.position);
                        m_NetworkPlatformRotationOffset = m_NetworkPlatformPrevRotationOffset = MathUtility.InverseTransformQuaternion(platform.rotation, m_Transform.rotation);
                    }

                    m_Distance = Vector3.Distance(m_NetworkPlatformPrevRelativePosition, m_NetworkPlatformRelativePosition);
                    m_Angle = Quaternion.Angle(m_NetworkPlatformPrevRotationOffset, m_NetworkPlatformRotationOffset);
                    m_PlatformPhotonViewID = platformPhotonViewID;
                } else {
                    if (m_PlatformPhotonViewID != -1) {
                        m_CharacterLocomotion.SetPlatform(null, true);
                        m_PlatformPhotonViewID = -1;
                    }
                    if ((dirtyFlag & (byte)TransformDirtyFlags.Position) != 0) {
                        m_NetworkPosition = PunUtility.ReceiveCompressedVector3(stream);
                        var velocity = PunUtility.ReceiveCompressedVector3(stream);
                        if (!m_InitialSync) {
                            // Account for the lag.
                            var lag = Mathf.Abs((float)(PhotonNetwork.Time - info.SentServerTime));
                            m_NetworkPosition += velocity * lag;
                        }
                        m_InitialSync = false;
                    }
                    if ((dirtyFlag & (byte)TransformDirtyFlags.Rotation) != 0) {
                        m_NetworkRotation = Quaternion.Euler(PunUtility.ReceiveCompressedVector3(stream));
                    }

                    m_Distance = Vector3.Distance(m_Transform.position, m_NetworkPosition);
                    m_Angle = Quaternion.Angle(m_Transform.rotation, m_NetworkRotation);
                }

                if ((dirtyFlag & (byte)TransformDirtyFlags.Scale) != 0) {
                    m_Transform.localScale = PunUtility.ReceiveCompressedVector3(stream);
                }
            }
        }

        /// <summary>
        /// The character has respawned.
        /// </summary>
        private void OnRespawn()
        {
            m_NetworkPosition = m_Transform.position;
            m_NetworkRotation = m_Transform.rotation;
        }

        /// <summary>
        /// The character's position or rotation has been teleported.
        /// </summary>
        /// <param name="snapAnimator">Should the animator be snapped?</param>
        private void OnImmediateTransformChange(bool snapAnimator)
        {
            m_NetworkPosition = m_Transform.position;
            m_NetworkRotation = m_Transform.rotation;
        }

        /// <summary>
        /// The character has been destroyed.
        /// </summary>
        private void OnDestroy()
        {
            EventHandler.UnregisterEvent(gameObject, "OnRespawn", OnRespawn);
            EventHandler.UnregisterEvent<bool>(gameObject, "OnCharacterImmediateTransformChange", OnImmediateTransformChange);
        }
    }
}