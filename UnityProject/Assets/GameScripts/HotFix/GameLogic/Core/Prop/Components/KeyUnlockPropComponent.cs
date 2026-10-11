using UnityEngine;

namespace GameLogic
{
    /// <summary>
    /// 解锁球洞并在拾取后隐藏道具的 Prefab 功能组件。
    /// </summary>
    public sealed class KeyUnlockPropComponent : MonoBehaviour, IPropSimulationProvider, IPropAvailabilityPresentation
    {
        /// <inheritdoc />
        public PropEffectSpec CreateEffect(PropPlacementData data)
        {
            return KeyPropEffect.Spec;
        }

        /// <inheritdoc />
        public void SetAvailable(bool available)
        {
            gameObject.SetActive(available);
        }
    }
}

