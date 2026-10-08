using GameConfig;
using UnityEngine;

namespace GameLogic
{
    /// <summary>
    /// 图形表现同时只绘制配置选择的一种条件圆。
    /// </summary>
    public sealed class GeometryView : MonoBehaviour
    {
        /// <summary>
        /// 绘制三角形轮廓和填充的组件。
        /// </summary>
        [SerializeField]
        private GeometryGraphic _figure;

        /// <summary>
        /// 绘制唯一条件圆的提示组件。
        /// </summary>
        [SerializeField]
        private GeometryGraphic _circle;

        /// <summary>
        /// 当前图形的效果是否已完成。
        /// </summary>
        private bool _completed;

        /// <summary>
        /// 条件圆提示是否启用。
        /// </summary>
        private bool _hints = true;

        /// <summary>
        /// 初始化图形及唯一判定圆。
        /// </summary>
        /// <param name="data">图形顶点与唯一条件圆数据。</param>
        /// <param name="style">轮廓、条件圆颜色及线宽配置。</param>
        public void Initialize(GeometryData data, GeometryStyle style)
        {
            Vector4 a = style.FigureColor;
            Vector4 b = style.CircleColor;
            _figure.SetStyle(new Color(a.x, a.y, a.z, a.w), style.LineWidth);
            _circle.SetStyle(new Color(b.x, b.y, b.z, b.w), style.LineWidth);
            _figure.SetTriangle(data.A, data.B, data.C);
            _circle.transform.localPosition = data.Center;
            _circle.SetCircle(data.Radius);
        }

        /// <summary>
        /// 效果完成后隐藏对应几何障碍。
        /// </summary>
        /// <param name="completed">是否已完成图形效果；完成后隐藏图形及条件圆。</param>
        public void SetCompleted(bool completed)
        {
            _completed = completed;
            _figure.gameObject.SetActive(!completed);
            _circle.gameObject.SetActive(!completed && _hints);
        }

        /// <summary>
        /// 显示或隐藏条件提示。
        /// </summary>
        /// <param name="hints">是否显示尚未完成图形的条件圆。</param>
        public void SetHints(bool hints)
        {
            _hints = hints;
            _circle.gameObject.SetActive(!_completed && hints);
        }
    }
}
