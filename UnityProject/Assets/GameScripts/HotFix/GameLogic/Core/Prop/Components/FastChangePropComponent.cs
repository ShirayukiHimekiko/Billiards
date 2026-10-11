using UnityEngine;

namespace GameLogic
{
    /// <summary>
    /// 加快球体尺寸变化的 Prefab 功能组件。
    /// </summary>
    public sealed class FastChangePropComponent : MonoBehaviour, IPropSimulationProvider
    {
        /// <inheritdoc />
        public PropEffectSpec CreateEffect(PropPlacementData data)
        {
            return FastChangePropEffect.Spec;
        }
    }
}

