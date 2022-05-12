/// ---------------------------------------------
/// Ultimate Character Controller
/// Copyright (c) Opsive. All Rights Reserved.
/// https://www.opsive.com
/// ---------------------------------------------

namespace Opsive.UltimateCharacterController.AddOns.Multiplayer.PhotonPun.Character
{
    using Opsive.Shared.Events;
    using Opsive.UltimateCharacterController.AddOns.Multiplayer.Utility;
    using Opsive.UltimateCharacterController.Character;
    using Photon.Pun;
    using Photon.Realtime;
    using UnityEngine;

    /// <summary>
    /// Synchronizes the Ultimate Character Controller animator across the network.
    /// </summary>
    [RequireComponent(typeof(PhotonView))]
    public class PunAnimatorMonitor : AnimatorMonitor, IPunObservable
    {
        /// <summary>
        /// Specifies which parameters are dirty.
        /// </summary>
        private enum ParameterDirtyFlags : short
        {
            HorizontalMovement = 1,     // The Horizontal Movement parameter has changed.
            ForwardMovement = 2,        // The Forward Movement parameter has changed.
            Pitch = 4,                  // The Pitch parameter has changed.
            Yaw = 8,                    // The Yaw parameter has changed.
            Speed = 16,                 // The Speed parameter has changed.
            Height = 32,                // The Height parameter has changed.
            Moving = 64,                // The Moving parameter has changed.
            Aiming = 128,               // The Aiming parameter has changed.
            MovementSetID = 256,        // The Movement Set ID parameter has changed.
            AbilityIndex = 512,         // The Ability Index parameter has changed.
            AbilityIntData = 1024,      // The Ability Int Data parameter has changed.
            AbilityFloatData = 2048     // The Ability Float Data parameter has changed.
        }

        private PhotonView m_PhotonView;
        private int m_SnappedAbilityIndex;
        private short m_DirtyFlag;
        private byte m_ItemDirtySlot;

        private float m_NetworkHorizontalMovement;
        private float m_NetworkForwardMovement;
        private float m_NetworkPitch;
        private float m_NetworkYaw;
        private float m_NetworkSpeed;
        private float m_NetworkAbilityFloatData;

        /// <summary>
        /// Initializes the default values.
        /// </summary>
        protected override void Awake()
        {
            base.Awake();

            m_PhotonView = GetComponent<PhotonView>();
        }

        /// <summary>
        /// Verify the update mode of the animator.
        /// </summary>
        protected override void Start()
        {
            base.Start();

            // Remote players do not move within the FixedUpdate loop.
            if (!m_PhotonView.IsMine) {
                var animators = GetComponentsInChildren<Animator>(true);
                for (int i = 0; i < animators.Length; ++i) {
                    animators[i].updateMode = AnimatorUpdateMode.Normal;
                }
            } else {
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
            m_PhotonView.RPC("SynchronizeParametersRPC", player, HorizontalMovement, ForwardMovement, Pitch, Yaw, Speed, 
                                    Height, Moving, Aiming, MovementSetID, AbilityIndex, AbilityIntData, AbilityFloatData);
            if (HasItemParameters) {
                for (int i = 0; i < ParameterSlotCount; ++i) {
                    m_PhotonView.RPC("SynchronizeItemParametersRPC", player, i, ItemSlotID[i], ItemSlotStateIndex[i], ItemSlotSubstateIndex[i]);
                }
            }
        }

        /// <summary>
        /// Sets the initial parameter values.
        /// </summary>
        [PunRPC]
        private void SynchronizeParametersRPC(float horizontalMovement, float forwardMovement, float pitch, float yaw, float speed, int height, bool moving, bool aiming, 
                                                int movementSetID, int abilityIndex, int abilityIntData, float abilityFloatData)
        {
            SetHorizontalMovementParameter(horizontalMovement, 1);
            SetForwardMovementParameter(forwardMovement, 1);
            SetPitchParameter(pitch, 1);
            SetYawParameter(yaw, 1);
            SetSpeedParameter(speed, 1);
            SetHeightParameter(height);
            SetMovingParameter(moving);
            SetAimingParameter(aiming);
            SetMovementSetIDParameter(movementSetID);
            SetAbilityIndexParameter(abilityIndex);
            SetAbilityIntDataParameter(abilityIntData);
            SetAbilityFloatDataParameter(abilityFloatData, 1);

            SnapAnimator();
        }

        /// <summary>
        /// Sets the initial item parameter values.
        /// </summary>
        [PunRPC]
        private void SynchronizeItemParametersRPC(int slotID, int itemID, int itemStateIndex, int itemSubstateIndex)
        {
            SetItemIDParameter(slotID, itemID);
            SetItemStateIndexParameter(slotID, itemStateIndex);
            SetItemSubstateIndexParameter(slotID, itemSubstateIndex);

            SnapAnimator();
        }

        /// <summary>
        /// Snaps the animator to the default values.
        /// </summary>
        protected override void SnapAnimator()
        {
            base.SnapAnimator();

            m_SnappedAbilityIndex = AbilityIndex;
        }

        /// <summary>
        /// Reads/writes the continuous animator parameters.
        /// </summary>
        private void Update()
        {
            // Local players will update the animator through the regular UltimateCharacterLocomotion.Move method.
            if (m_PhotonView.IsMine) {
                return;
            }

            SetHorizontalMovementParameter(m_NetworkHorizontalMovement, 1);
            SetForwardMovementParameter(m_NetworkForwardMovement, 1);
            SetPitchParameter(m_NetworkPitch, 1);
            SetYawParameter(m_NetworkYaw, 1);
            SetSpeedParameter(m_NetworkSpeed, 1);
            SetAbilityFloatDataParameter(m_NetworkAbilityFloatData, 1);
        }

        /// <summary>
        /// Called by PUN several times per second, so that your script can write and read synchronization data for the PhotonView.
        /// </summary>
        /// <param name="stream">The stream that is being written to/read from.</param>
        /// <param name="info">Contains information about the message.</param>
        public void OnPhotonSerializeView(PhotonStream stream, PhotonMessageInfo info)
        {
            if (stream.IsWriting) {
                stream.SendNext(m_DirtyFlag);
                if ((m_DirtyFlag & (short)ParameterDirtyFlags.HorizontalMovement) != 0) {
                    stream.SendNext(NetworkCompression.FloatToShortMovement(HorizontalMovement));
                }
                if ((m_DirtyFlag & (short)ParameterDirtyFlags.ForwardMovement) != 0) {
                    stream.SendNext(NetworkCompression.FloatToShortMovement(ForwardMovement));
                }
                if ((m_DirtyFlag & (short)ParameterDirtyFlags.Pitch) != 0) {
                    stream.SendNext(NetworkCompression.FloatToShort(Pitch));
                }
                if ((m_DirtyFlag & (short)ParameterDirtyFlags.Yaw) != 0) {
                    stream.SendNext(NetworkCompression.FloatToShort(Yaw));
                }
                if ((m_DirtyFlag & (short)ParameterDirtyFlags.Speed) != 0) {
                    stream.SendNext(NetworkCompression.FloatToShort(Speed));
                }
                if ((m_DirtyFlag & (short)ParameterDirtyFlags.Height) != 0) {
                    stream.SendNext(Height);
                }
                if ((m_DirtyFlag & (short)ParameterDirtyFlags.Moving) != 0) {
                    stream.SendNext(Moving);
                }
                if ((m_DirtyFlag & (short)ParameterDirtyFlags.Aiming) != 0) {
                    stream.SendNext(Aiming);
                }
                if ((m_DirtyFlag & (short)ParameterDirtyFlags.MovementSetID) != 0) {
                    stream.SendNext(MovementSetID);
                }
                if ((m_DirtyFlag & (short)ParameterDirtyFlags.AbilityIndex) != 0) {
                    stream.SendNext(AbilityIndex);
                }
                if ((m_DirtyFlag & (short)ParameterDirtyFlags.AbilityIntData) != 0) {
                    stream.SendNext(AbilityIntData);
                }
                if ((m_DirtyFlag & (short)ParameterDirtyFlags.AbilityFloatData) != 0) {
                    stream.SendNext(NetworkCompression.FloatToShort(AbilityFloatData));
                }
                if (HasItemParameters) {
                    stream.SendNext(m_ItemDirtySlot);
                    for (int i = 0; i < ParameterSlotCount; ++i) {
                        if ((m_ItemDirtySlot & (i + 1)) == 0) {
                            continue;
                        }
                        stream.SendNext(ItemSlotID[i]);
                        stream.SendNext(ItemSlotStateIndex[i]);
                        stream.SendNext(ItemSlotSubstateIndex[i]);
                    }
                }

                m_DirtyFlag = 0;
                m_ItemDirtySlot = 0;
            } else { // Reading.
                var dirtyFlag = (short)stream.ReceiveNext();
                if ((dirtyFlag & (short)ParameterDirtyFlags.HorizontalMovement) != 0) {
                    m_NetworkHorizontalMovement = NetworkCompression.ShortToFloatMovement(System.Convert.ToInt16(stream.ReceiveNext()));
                }
                if ((dirtyFlag & (short)ParameterDirtyFlags.ForwardMovement) != 0) {
                    m_NetworkForwardMovement = NetworkCompression.ShortToFloatMovement(System.Convert.ToInt16(stream.ReceiveNext()));
                }
                if ((dirtyFlag & (short)ParameterDirtyFlags.Pitch) != 0) {
                    m_NetworkPitch = NetworkCompression.ShortToFloat((short)stream.ReceiveNext());
                }
                if ((dirtyFlag & (short)ParameterDirtyFlags.Yaw) != 0) {
                    m_NetworkYaw = NetworkCompression.ShortToFloat((short)stream.ReceiveNext());
                }
                if ((dirtyFlag & (short)ParameterDirtyFlags.Speed) != 0) {
                    m_NetworkSpeed = NetworkCompression.ShortToFloat((short)stream.ReceiveNext());
                }
                if ((dirtyFlag & (short)ParameterDirtyFlags.Height) != 0) {
                    SetHeightParameter((int)stream.ReceiveNext());
                }
                if ((dirtyFlag & (short)ParameterDirtyFlags.Moving) != 0) {
                    SetMovingParameter((bool)stream.ReceiveNext());
                }
                if ((dirtyFlag & (short)ParameterDirtyFlags.Aiming) != 0) {
                    SetAimingParameter((bool)stream.ReceiveNext());
                }
                if ((dirtyFlag & (short)ParameterDirtyFlags.MovementSetID) != 0) {
                    SetMovementSetIDParameter((int)stream.ReceiveNext());
                }
                if ((dirtyFlag & (short)ParameterDirtyFlags.AbilityIndex) != 0) {
                    var abilityIndex = (int)stream.ReceiveNext();
                    // When the animator is snapped the ability index will be reset. It may take some time for that value to propagate across the network.
                    // Wait to set the ability index until it is the correct reset value.
                    if (m_SnappedAbilityIndex == 0 || abilityIndex == m_SnappedAbilityIndex) {
                        SetAbilityIndexParameter(abilityIndex);
                        m_SnappedAbilityIndex = 0;
                    }
                }
                if ((dirtyFlag & (short)ParameterDirtyFlags.AbilityIntData) != 0) {
                    SetAbilityIntDataParameter((int)stream.ReceiveNext());
                }
                if ((dirtyFlag & (short)ParameterDirtyFlags.AbilityFloatData) != 0) {
                    m_NetworkAbilityFloatData = NetworkCompression.ShortToFloat((short)stream.ReceiveNext());
                }
                if (HasItemParameters) {
                    var itemDirtySlot = (byte)stream.ReceiveNext();
                    for (int i = 0; i < ParameterSlotCount; ++i) {
                        if ((itemDirtySlot & (i + 1)) == 0) {
                            continue;
                        }
                        SetItemIDParameter(i, (int)stream.ReceiveNext());
                        SetItemStateIndexParameter(i, (int)stream.ReceiveNext());
                        SetItemSubstateIndexParameter(i, (int)stream.ReceiveNext());
                    }
                }
            }
        }

        /// <summary>
        /// Sets the Horizontal Movement parameter to the specified value.
        /// </summary>
        /// <param name="value">The new value.</param>
        /// <param name="timeScale">The time scale of the character.</param>
        /// <param name="dampingTime">The time allowed for the parameter to reach the value.</param>
        /// <returns>True if the parameter was changed.</returns>
        public override bool SetHorizontalMovementParameter(float value, float timeScale, float dampingTime)
        {
            // The animator may not be enabled. Return silently.
            if (!m_Animator.isActiveAndEnabled) {
                return false;
            }
            if (base.SetHorizontalMovementParameter(value, timeScale, dampingTime)) {
                m_DirtyFlag |= (short)ParameterDirtyFlags.HorizontalMovement;
                return true;
            }
            return false;
        }

        /// <summary>
        /// Sets the Forward Movement parameter to the specified value.
        /// </summary>
        /// <param name="value">The new value.</param>
        /// <param name="timeScale">The time scale of the character.</param>
        /// <param name="dampingTime">The time allowed for the parameter to reach the value.</param>
        /// <returns>True if the parameter was changed.</returns>
        public override bool SetForwardMovementParameter(float value, float timeScale, float dampingTime)
        {
            // The animator may not be enabled. Return silently.
            if (!m_Animator.isActiveAndEnabled) {
                return false;
            }
            if (base.SetForwardMovementParameter(value, timeScale, dampingTime)) {
                m_DirtyFlag |= (short)ParameterDirtyFlags.ForwardMovement;
                return true;
            }
            return false;
        }

        /// <summary>
        /// Sets the Pitch parameter to the specified value.
        /// </summary>
        /// <param name="value">The new value.</param>
        /// <param name="timeScale">The time scale of the character.</param>
        /// <param name="dampingTime">The time allowed for the parameter to reach the value.</param>
        /// <returns>True if the parameter was changed.</returns>
        public override bool SetPitchParameter(float value, float timeScale, float dampingTime)
        {
            // The animator may not be enabled. Return silently.
            if (!m_Animator.isActiveAndEnabled) {
                return false;
            }
            if (base.SetPitchParameter(value, timeScale, dampingTime)) {
                m_DirtyFlag |= (short)ParameterDirtyFlags.Pitch;
                return true;
            }
            return false;
        }

        /// <summary>
        /// Sets the Yaw parameter to the specified value.
        /// </summary>
        /// <param name="value">The new value.</param>
        /// <param name="timeScale">The time scale of the character.</param>
        /// <param name="dampingTime">The time allowed for the parameter to reach the value.</param>
        /// <returns>True if the parameter was changed.</returns>
        public override bool SetYawParameter(float value, float timeScale, float dampingTime)
        {
            // The animator may not be enabled. Return silently.
            if (!m_Animator.isActiveAndEnabled) {
                return false;
            }
            if (base.SetYawParameter(value, timeScale, dampingTime)) {
                m_DirtyFlag |= (short)ParameterDirtyFlags.Yaw;
                return true;
            }
            return false;
        }

        /// <summary>
        /// Sets the Speed parameter to the specified value.
        /// </summary>
        /// <param name="value">The new value.</param>
        /// <param name="timeScale">The time scale of the character.</param>
        /// <param name="dampingTime">The time allowed for the parameter to reach the value.</param>
        /// <returns>True if the parameter was changed.</returns>
        public override bool SetSpeedParameter(float value, float timeScale, float dampingTime)
        {
            // The animator may not be enabled. Return silently.
            if (!m_Animator.isActiveAndEnabled) {
                return false;
            }
            if (base.SetSpeedParameter(value, timeScale, dampingTime)) {
                m_DirtyFlag |= (short)ParameterDirtyFlags.Speed;
                return true;
            }
            return false;
        }

        /// <summary>
        /// Sets the Height parameter to the specified value.
        /// </summary>
        /// <param name="value">The new value.</param>
        /// <returns>True if the parameter was changed.</returns>
        public override bool SetHeightParameter(int value)
        {
            // The animator may not be enabled. Return silently.
            if (!m_Animator.isActiveAndEnabled) {
                return false;
            }
            if (base.SetHeightParameter(value)) {
                m_DirtyFlag |= (short)ParameterDirtyFlags.Height;
                return true;
            }
            return false;
        }

        /// <summary>
        /// Sets the Moving parameter to the specified value.
        /// </summary>
        /// <param name="value">The new value.</param>
        /// <returns>True if the parameter was changed.</returns>
        public override bool SetMovingParameter(bool value)
        {
            // The animator may not be enabled. Return silently.
            if (!m_Animator.isActiveAndEnabled) {
                return false;
            }
            if (base.SetMovingParameter(value)) {
                m_DirtyFlag |= (short)ParameterDirtyFlags.Moving;
                return true;
            }
            return false;
        }

        /// <summary>
        /// Sets the Aiming parameter to the specified value.
        /// </summary>
        /// <param name="value">The new value.</param>
        /// <returns>True if the parameter was changed.</returns>
        public override bool SetAimingParameter(bool value)
        {
            // The animator may not be enabled. Return silently.
            if (!m_Animator.isActiveAndEnabled) {
                return false;
            }
            if (base.SetAimingParameter(value)) {
                m_DirtyFlag |= (short)ParameterDirtyFlags.Aiming;
                return true;
            }
            return false;
        }

        /// <summary>
        /// Sets the Movement Set ID parameter to the specified value.
        /// </summary>
        /// <param name="value">The new value.</param>
        /// <returns>True if the parameter was changed.</returns>
        public override bool SetMovementSetIDParameter(int value)
        {
            // The animator may not be enabled. Return silently.
            if (!m_Animator.isActiveAndEnabled) {
                return false;
            }
            if (base.SetMovementSetIDParameter(value)) {
                m_DirtyFlag |= (short)ParameterDirtyFlags.MovementSetID;
                return true;
            }
            return false;
        }

        /// <summary>
        /// Sets the Ability Index parameter to the specified value.
        /// </summary>
        /// <param name="value">The new value.</param>
        /// <returns>True if the parameter was changed.</returns>
        public override bool SetAbilityIndexParameter(int value)
        {
            // The animator may not be enabled. Return silently.
            if (!m_Animator.isActiveAndEnabled) {
                return false;
            }
            if (base.SetAbilityIndexParameter(value)) {
                m_DirtyFlag |= (short)ParameterDirtyFlags.AbilityIndex;
                return true;
            }
            return false;
        }

        /// <summary>
        /// Sets the Int Data parameter to the specified value.
        /// </summary>
        /// <param name="value">The new value.</param>
        /// <returns>True if the parameter was changed.</returns>
        public override bool SetAbilityIntDataParameter(int value)
        {
            // The animator may not be enabled. Return silently.
            if (!m_Animator.isActiveAndEnabled) {
                return false;
            }
            if (base.SetAbilityIntDataParameter(value)) {
                m_DirtyFlag |= (short)ParameterDirtyFlags.AbilityIntData;
                return true;
            }
            return false;
        }

        /// <summary>
        /// Sets the Ability Float parameter to the specified value.
        /// </summary>
        /// <param name="value">The new value.</param>
        /// <param name="timeScale">The time scale of the character.</param>
        /// <param name="dampingTime">The time allowed for the parameter to reach the value.</param>
        /// <returns>True if the parameter was changed.</returns>
        public override bool SetAbilityFloatDataParameter(float value, float timeScale, float dampingTime)
        {
            // The animator may not be enabled. Return silently.
            if (!m_Animator.isActiveAndEnabled) {
                return false;
            }
            if (base.SetAbilityFloatDataParameter(value, timeScale, dampingTime)) {
                m_DirtyFlag |= (short)ParameterDirtyFlags.AbilityFloatData;
                return true;
            }
            return false;
        }

        /// <summary>
        /// Sets the Item ID parameter with the indicated slot to the specified value.
        /// </summary>
        /// <param name="slotID">The slot that the item occupies.</param>
        /// <param name="value">The new value.</param>
        public override bool SetItemIDParameter(int slotID, int value)
        {
            // The animator may not be enabled. Return silently.
            if (!m_Animator.isActiveAndEnabled) {
                return false;
            }
            if (base.SetItemIDParameter(slotID, value)) {
                m_ItemDirtySlot |= (byte)(slotID + 1);
                return true;
            }
            return false;
        }

        /// <summary>
        /// Sets the Primary Item State Index parameter with the indicated slot to the specified value.
        /// </summary>
        /// <param name="slotID">The slot that the item occupies.</param>
        /// <param name="value">The new value.</param>
        /// <returns>True if the parameter was changed.</returns>
        public override bool SetItemStateIndexParameter(int slotID, int value)
        {
            // The animator may not be enabled. Return silently.
            if (!m_Animator.isActiveAndEnabled) {
                return false;
            }
            if (base.SetItemStateIndexParameter(slotID, value)) {
                m_ItemDirtySlot |= (byte)(slotID + 1);
                return true;
            }
            return false;
        }

        /// <summary>
        /// Sets the Item Substate Index parameter with the indicated slot to the specified value.
        /// </summary>
        /// <param name="slotID">The slot that the item occupies.</param>
        /// <param name="value">The new value.</param>
        /// <returns>True if the parameter was changed.</returns>
        public override bool SetItemSubstateIndexParameter(int slotID, int value)
        {
            // The animator may not be enabled. Return silently.
            if (!m_Animator.isActiveAndEnabled) {
                return false;
            }
            if (base.SetItemSubstateIndexParameter(slotID, value)) {
                m_ItemDirtySlot |= (byte)(slotID + 1);
                return true;
            }
            return false;
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