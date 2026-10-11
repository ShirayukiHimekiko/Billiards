using UnityEngine;

namespace GameLogic
{
    /// <summary>
    /// 翻转球体尺寸趋势的 Prefab 功能组件。
    /// </summary>
    public sealed class FlipPropComponent : MonoBehaviour, IPropSimulationProvider
    {
        /// <inheritdoc />
        public PropEffectSpec CreateEffect(PropPlacementData data)
        {
            return FlipPropEffect.Spec;
        }
    }
}

