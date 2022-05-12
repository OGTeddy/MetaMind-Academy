/// ---------------------------------------------
/// Ultimate Character Controller
/// Copyright (c) Opsive. All Rights Reserved.
/// https://www.opsive.com
/// ---------------------------------------------

using UnityEngine;
using Opsive.UltimateCharacterController.Character;
using Opsive.UltimateCharacterController.Inventory;
using Opsive.UltimateCharacterController.Utility.Builders;
using UMA;
using UMA.CharacterSystem;

namespace Opsive.UltimateCharacterController.Integrations.UMA
{
    /// <summary>
    /// Dynamically builds an Ultimate Character Controller character after UMA creates the character. See the documentation for steps to setup your character:
    /// https://opsive.com/?post_type=documentation&p=3433&preview=true
    /// </summary>
    public class UMACharacterBuilder : MonoBehaviour
    {
        [Tooltip("Should the first person perspective be added?")]
        [SerializeField] protected bool m_AddFirstPersonPerspective;
        [Tooltip("The first person movement type that should be added.")]
        [SerializeField] protected string m_FirstPersonMovementType = "Opsive.UltimateCharacterController.FirstPersonController.Character.MovementTypes.Combat";
        [Tooltip("Any objects that should be hidden on the character while in a first person perspective.")]
        [SerializeField] protected string[] m_FirstPersonHiddenObjectNames;
        [Tooltip("A reference to the shadow caster material that is applied to the first person objects.")]
        [SerializeField] protected Material m_InvisibleShadowCastorMaterial;
        [Tooltip("A list of dynamically generated first person base objects.")]
        [SerializeField] protected UMAFirstPersonBaseObject[] m_FirstPersonBaseObjects;
        [Tooltip("Should the third person perspective be added?")]
        [SerializeField] protected bool m_AddThirdPersonPerspective = true;
        [Tooltip("The third person movement type that should be added.")]
        [SerializeField] protected string m_ThirdPersonMovementType = "Opsive.UltimateCharacterController.ThirdPersonController.Character.MovementTypes.Adventure";
        [Tooltip("Should the character start in a first person perspective?")]
        [SerializeField] protected bool m_StartFirstPersonPerspective;
        [Tooltip("A reference to the animator controller used by the character.")]
        [SerializeField] protected RuntimeAnimatorController m_AnimatorController;
        [Tooltip("Can the character hold items?")]
        [SerializeField] protected bool m_AddItems = true;
        [Tooltip("A reference to the ItemCollection used by the ItemSetManager and Inventory.")]
        [SerializeField] protected ItemCollection m_ItemCollection;
        [Tooltip("Does the character have health?")]
        [SerializeField] protected bool m_AddHealth = true;
        [Tooltip("Should Unity's IK system be added?")]
        [SerializeField] protected bool m_AddUnityIK = true;
        [Tooltip("Should footstep effects play when moving?")]
        [SerializeField] protected bool m_AddFootEffects = true;
        [Tooltip("Should the standard set of abilities be added to the character?")]
        [SerializeField] protected bool m_AddStandardAbilities = true;
        [Tooltip("Is the character an AI agent?")]
        [SerializeField] protected bool m_AIAgent;
        [Tooltip("Should the NavMeshAgent be added to the AI agent?")]
        [SerializeField] protected bool m_AddNavMeshAgent;
        [Tooltip("Should the camera be assigned to the character after the character has been created?")]
        [SerializeField] protected bool m_AssignCamera = true;
        [Tooltip("Events that are executed after the character has been created.")]
        [SerializeField] protected UMADataEvent m_CharacterCreated;

        /// <summary>
        /// Initialize the default values.
        /// </summary>
        private void Awake()
        {
            var dynamicCharacterAvatar = GetComponent<DynamicCharacterAvatar>();
            dynamicCharacterAvatar.CharacterCreated.AddListener(OnCharacterCreated);
        }

        /// <summary>
        /// UMA has created the character. Add the Ultimate Character Controller components.
        /// </summary>
        /// <param name="data">The UMAData for the character.</param>
        public void OnCharacterCreated(UMAData data)
        {
            var characterLocomotion = data.gameObject.GetComponent<UltimateCharacterLocomotion>();
            if (characterLocomotion != null) {
                return;
            }
            // The Ultimate Character Controller will add the capsule collider.
            var capsuleCollider = data.gameObject.GetComponent<CapsuleCollider>();
            if (capsuleCollider != null) {
                DestroyImmediate(capsuleCollider);
            }

            GameObject[] firstPersonHiddenObjects = null;
#if FIRST_PERSON_CONTROLLER
            if (m_AddFirstPersonPerspective && m_FirstPersonHiddenObjectNames != null && m_FirstPersonHiddenObjectNames.Length > 0) {
                firstPersonHiddenObjects = new GameObject[m_FirstPersonHiddenObjectNames.Length];
                for (int i = 0; i < firstPersonHiddenObjects.Length; ++i) {
                    var child = data.gameObject.transform.Find(m_FirstPersonHiddenObjectNames[i]);
                    firstPersonHiddenObjects[i] = child != null ? child.gameObject : null;
                }
            }
#else
            m_AddFirstPersonPerspective = false;
#endif
            // Use the Character Builder to add the Ultimate Character Controller components.
            CharacterBuilder.BuildCharacter(data.gameObject, true, m_AnimatorController, m_AddFirstPersonPerspective ? m_FirstPersonMovementType : string.Empty, 
                                            m_AddThirdPersonPerspective ? m_ThirdPersonMovementType : string.Empty, m_AddFirstPersonPerspective &&  m_StartFirstPersonPerspective, 
                                            firstPersonHiddenObjects, m_InvisibleShadowCastorMaterial, m_AIAgent);
            CharacterBuilder.BuildCharacterComponents(data.gameObject, m_AIAgent, m_AddItems, m_ItemCollection, !string.IsNullOrEmpty(m_FirstPersonMovementType), m_AddHealth, m_AddUnityIK, m_AddFootEffects, m_AddStandardAbilities, m_AddNavMeshAgent);
            // Ensure the smoothed bones have been added to the character.
            characterLocomotion = data.gameObject.GetComponent<UltimateCharacterLocomotion>();
            characterLocomotion.AddDefaultSmoothedBones();

            // The Animator Monitor is one of the first components added and the item system hasn't been added to the character yet. Initialize the Item Parameters after the item system has been setup.
            if (m_AddItems) {
                var animatorMonitor = data.gameObject.GetComponent<AnimatorMonitor>();
                if (animatorMonitor != null) {
                    animatorMonitor.InitializeItemParameters();
                }
            }

#if FIRST_PERSON_CONTROLLER
            // Add any dynamically created base objects.
            if (m_FirstPersonBaseObjects != null) {
                var firstPersonObjects = gameObject.GetComponentInChildren<FirstPersonController.Character.FirstPersonObjects>();
                if (firstPersonObjects != null) {
                    for (int i = 0; i < m_FirstPersonBaseObjects.Length; ++i) {
                        m_FirstPersonBaseObjects[i].AddFirstPersonObject(firstPersonObjects.gameObject);
                    }
                }
            }
#endif
            
            // The camera can automatically be assigned ot the new character.
            if (m_AssignCamera && !m_AIAgent) {
                var camera = Opsive.Shared.Camera.CameraUtility.FindCamera(data.gameObject);
                if (camera != null) {
                    var cameraController = camera.GetComponent<Camera.CameraController>();
                    if (cameraController != null) {
                        cameraController.Character = data.gameObject;
                    }
                }
            }

            // Additional abilities can be added.
            var abilityBuilder = GetComponent<UMAAbilityBuilder>();
            if (abilityBuilder != null) {
                abilityBuilder.OnCharacterCreated(data);
            }

            // Items can automatically be added.
            var itemBuilder = GetComponent<UMAItemPickup>();
            if (itemBuilder != null) {
                itemBuilder.OnCharacterCreated(data);
            }

            if (m_CharacterCreated != null) {
                m_CharacterCreated.Invoke(data);
            }

            // The script is no longer required.
            Destroy(this);
        }
    }
}