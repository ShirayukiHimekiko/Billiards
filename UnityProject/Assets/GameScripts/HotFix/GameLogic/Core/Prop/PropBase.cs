using System;
using UnityEngine;

namespace GameLogic
{
    /// <summary>
    /// 道具表现基类；实例生命周期由 PropManager 管理。
    /// </summary>
    public abstract class PropBase : MonoBehaviour
    {
        /// <summary>
        /// 显示道具可用状态的精灵渲染组件。
        /// </summary>
        [SerializeField]
        private SpriteRenderer _renderer;

        /// <summary>
        /// 应用摆放及视觉尺寸。
        /// </summary>
        /// <param name="data">台面局部位置及显示尺寸配置。</param>
        public void Initialize(PropPlacementData data)
        {
            if (_renderer == null)
            {
                throw new InvalidOperationException($"道具 {data.Id} 缺少 SpriteRenderer 绑定。");
            }

            transform.localPosition = data.Position;
            transform.localScale = Vector3.one * (data.VisualDiameter / _renderer.sprite.bounds.size.x);
            SetAvailable(true);
        }

        /// <summary>
        /// 显示道具是否还可触发。
        /// </summary>
        /// <param name="available">道具是否仍有可用次数；不可用时降低颜色与透明度。</param>
        public void SetAvailable(bool available)
        {
            _renderer.color = available ? Color.white : new Color(.5f, .5f, .5f, .35f);
        }
    }
}
