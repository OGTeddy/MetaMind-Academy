/// ---------------------------------------------
/// Ultimate Character Controller
/// Copyright (c) Opsive. All Rights Reserved.
/// https://www.opsive.com
/// ---------------------------------------------

using UnityEngine;
using Opsive.UltimateCharacterController.Character;
using Opsive.UltimateCharacterController.Utility;
using Opsive.UltimateCharacterController.Utility.Builders;
using UMA;

namespace Opsive.UltimateCharacterController.Integrations.UMA
{
    /// <summary>
    /// Adds abilities to the character. This class may need to be extended to allow for custom setup of the newly created abilities.
    /// </summary>
    public class UMAAbilityBuilder : MonoBehaviour
    {
        [Tooltip("A list of ability types that should be added.")]
        [SerializeField] protected string[] m_AbilityTypes = new string[] { "Opsive.UltimateCharacterController.Character.Abilities.Jump" };

        /// <summary>
        /// UMA has created the character.
        /// </summary>
        /// <param name="data">The UMAData for the character.</param>
        public void OnCharacterCreated(UMAData data)
        {
            var characterLocomotion = data.gameObject.GetComponent<UltimateCharacterLocomotion>();
            if (characterLocomotion == null) {
                return;
            }

            for (int i = 0; i < m_AbilityTypes.Length; ++i) {
                var abilityType = Shared.Utility.TypeUtility.GetType(m_AbilityTypes[i]);
                if (abilityType == null) {
                    Debug.LogWarning("Warning: Unable to add ability of type " + m_AbilityTypes[i] + " because the type cannot be found.");
                    continue;
                }

                // A reference to the ability is returned. Your script may need to customize the ability parameters.
                AbilityBuilder.AddAbility(characterLocomotion, abilityType);
            }

            // The script is no longer required.
            Destroy(this);
        }
    }
}