/// ---------------------------------------------
/// Ultimate Character Controller
/// Copyright (c) Opsive. All Rights Reserved.
/// https://www.opsive.com
/// ---------------------------------------------

using UnityEngine;
using UMA;
using UMA.CharacterSystem;
using Opsive.UltimateCharacterController.Utility;

namespace Opsive.UltimateCharacterController.Integrations.UMA
{
    /// <summary>
    /// Adds the First Person Base Object components to the UMA created object.
    /// </summary>
    public class UMAFirstPersonBaseObject : MonoBehaviour
    {
#if FIRST_PERSON_CONTROLLER
        [Tooltip("The local position of the first person object.")]
        [SerializeField] protected Vector3 m_LocalPosition;
        [Tooltip("The local rotation of the first person object.")]
        [SerializeField] protected Quaternion m_LocalRotation;
        [Tooltip("Specifies any child names that should be where the Item Slot item is located.")]
        [SerializeField] protected string[] m_ItemSlotLocations;
        [Tooltip("The animator controller used by the First Person Object.")]
        [SerializeField] protected RuntimeAnimatorController m_AnimatorController;

        private GameObject m_FirstPersonObjects;
        private bool m_ObjectCreated = false;

        /// <summary>
        /// Initialize the default values.
        /// </summary>
        private void Awake()
        {
            var dynamicCharacterAvatar = GetComponent<DynamicCharacterAvatar>();
            dynamicCharacterAvatar.CharacterCreated.AddListener(FirstPersonObjectCreated);
        }

        /// <summary>
        /// The DynamicCharacterAvatar component has finished creating the object.
        /// </summary>
        /// <param name="data"></param>
        private void FirstPersonObjectCreated(UMAData data)
        {
            m_ObjectCreated = true;

            // Add the first person components if the main character has been created.
            if (m_FirstPersonObjects != null) {
                AddFirstPersonObject(m_FirstPersonObjects);
            }
        }

        /// <summary>
        /// Adds the First Person Base Object components to the object.
        /// </summary>
        /// <param name="firstPersonObjects">A reference to the GameObject that contains the First Person Objects component.</param>
        public void AddFirstPersonObject(GameObject firstPersonObjects)
        {
            m_FirstPersonObjects = firstPersonObjects;
            if (!m_ObjectCreated) {
                return;
            }

            // Move the object to be a child of the character.
            transform.parent = firstPersonObjects.transform;
            transform.localPosition = m_LocalPosition;
            transform.localRotation = m_LocalRotation;
            transform.SetLayerRecursively(Game.LayerManager.Overlay);

            // The base object ID must be unique.
            var maxID = -1;
            var baseObjects = firstPersonObjects.GetComponentsInChildren<FirstPersonController.Character.Identifiers.FirstPersonBaseObject>();
            for (int i = 0; i < baseObjects.Length; ++i) {
                if (baseObjects[i].ID > maxID) {
                    maxID = baseObjects[i].ID;
                }
            }
            var baseObject = gameObject.AddComponent<FirstPersonController.Character.Identifiers.FirstPersonBaseObject>();
            baseObject.ID = maxID + 1;

            // Setup the animator controller.
            if (m_AnimatorController != null) {
                Animator animator;
                if ((animator = baseObject.gameObject.GetComponent<Animator>()) == null) {
                    animator = baseObject.gameObject.AddComponent<Animator>();
                }
                animator.updateMode = AnimatorUpdateMode.AnimatePhysics;
                animator.cullingMode = AnimatorCullingMode.AlwaysAnimate;
                animator.runtimeAnimatorController = m_AnimatorController;
                if (baseObject.gameObject.GetComponent<Character.ChildAnimatorMonitor>() == null) {
                    baseObject.gameObject.AddComponent<Character.ChildAnimatorMonitor>();
                }
            }

            // Add any Item Slots.
            var addedItemSlotLocation = false;
            if (m_ItemSlotLocations != null) {
                for (int i = 0; i < m_ItemSlotLocations.Length; ++i) {
                    var itemTransform = transform.Find(m_ItemSlotLocations[i]);
                    if (itemTransform != null) {
                        var items = new GameObject("Items");
                        items.transform.SetParentOrigin(itemTransform);
                        var itemSlot = items.AddComponent<Items.ItemSlot>();
                        itemSlot.ID = i;
                        addedItemSlotLocation = true;
                    }
                }
            }
            if (!addedItemSlotLocation) {
                gameObject.AddComponent<Items.ItemSlot>();
            }

            // The component is no longer needed.
            Destroy(this);
        }

#else
        /// <summary>
        /// This component can only be used by the assets with a first person perspective.
        /// </summary>
        private void Awake()
        {
            Debug.LogError("Error: A first person perpsective controller is required in order to add a first person object.");
        }
#endif
    }
}