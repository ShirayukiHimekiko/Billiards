using UnityEngine;

namespace GameLogic
{
    /// <summary>
    /// 恢复球体半径的 Prefab 功能组件。
    /// </summary>
    public sealed class RestorePropComponent : MonoBehaviour, IPropSimulationProvider
    {
        /// <inheritdoc />
        public PropEffectSpec CreateEffect(PropPlacementData data)
        {
            return RestorePropEffect.Spec;
        }
    }
}

