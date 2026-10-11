using UnityEngine;

namespace GameLogic
{
    /// <summary>
    /// 暂停球体尺寸变化的 Prefab 功能组件。
    /// </summary>
    public sealed class FreezePropComponent : MonoBehaviour, IPropSimulationProvider
    {
        /// <inheritdoc />
        public PropEffectSpec CreateEffect(PropPlacementData data)
        {
            return FreezePropEffect.Spec;
        }
    }
}

