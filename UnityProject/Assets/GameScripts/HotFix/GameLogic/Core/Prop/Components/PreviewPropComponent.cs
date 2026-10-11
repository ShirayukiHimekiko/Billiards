using UnityEngine;

namespace GameLogic
{
    /// <summary>
    /// 启用下一杆预演的 Prefab 功能组件。
    /// </summary>
    public sealed class PreviewPropComponent : MonoBehaviour, IPropSimulationProvider
    {
        /// <inheritdoc />
        public PropEffectSpec CreateEffect(PropPlacementData data)
        {
            return PreviewPropEffect.Spec;
        }
    }
}

