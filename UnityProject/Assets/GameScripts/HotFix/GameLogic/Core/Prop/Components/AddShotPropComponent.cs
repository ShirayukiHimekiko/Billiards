using UnityEngine;

namespace GameLogic
{
    /// <summary>
    /// 增加击球次数的 Prefab 功能组件。
    /// </summary>
    public sealed class AddShotPropComponent : MonoBehaviour, IPropSimulationProvider
    {
        /// <inheritdoc />
        public PropEffectSpec CreateEffect(PropPlacementData data)
        {
            return AddShotPropEffect.Spec;
        }
    }
}

