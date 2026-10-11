using UnityEngine;

namespace GameLogic
{
    /// <summary>
    /// 显示隐藏信息的 Prefab 功能组件。
    /// </summary>
    public sealed class RevealPropComponent : MonoBehaviour, IPropSimulationProvider
    {
        /// <inheritdoc />
        public PropEffectSpec CreateEffect(PropPlacementData data)
        {
            return RevealPropEffect.Spec;
        }
    }
}

