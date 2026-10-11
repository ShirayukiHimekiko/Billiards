using UnityEngine;

namespace GameLogic
{
    /// <summary>
    /// 反转球体尺寸变化方向的 Prefab 功能组件。
    /// </summary>
    public sealed class ReversePropComponent : MonoBehaviour, IPropSimulationProvider
    {
        /// <inheritdoc />
        public PropEffectSpec CreateEffect(PropPlacementData data)
        {
            return ReversePropEffect.Spec;
        }
    }
}

