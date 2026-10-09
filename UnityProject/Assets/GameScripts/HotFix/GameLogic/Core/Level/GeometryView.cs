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
        /// 当前图形明细 ID。
        /// </summary>
        private int _geometryId;

        /// <summary>
        /// 原始条件圆的局部位置、半径和样式，用于反馈结束后恢复。
        /// </summary>
        private Vector2 _circleCenter;
        private float _circleRadius;
        private Color _circleColor;
        private float _lineWidth;

        /// <summary>
        /// 预亮目标样式与持续时间。
        /// </summary>
        private Color _previewColor;
        private float _previewLineWidth;
        private float _previewDuration;

        /// <summary>
        /// 失败反馈已显示的非缩放时间。
        /// </summary>
        private float _feedbackElapsed;

        /// <summary>
        /// 是否正在显示失败反馈。
        /// </summary>
        private bool _feedbackVisible;

        /// <summary>
        /// 失败反馈的目标半径和线宽。
        /// </summary>
        private float _feedbackRadius;
        private float _feedbackLineWidth;

        /// <summary>
        /// 预亮目标的独立生命周期状态。
        /// </summary>
        private float _previewElapsed;
        private bool _previewVisible;
        private Vector2 _previewCenter;
        private float _previewRadius;

        /// <summary>
        /// 图形明细 ID。
        /// </summary>
        public int GeometryId => _geometryId;

        /// <summary>
        /// 初始化图形及唯一判定圆。
        /// </summary>
        /// <param name="data">图形顶点与唯一条件圆数据。</param>
        /// <param name="style">轮廓、条件圆颜色及线宽配置。</param>
        public void Initialize(GeometryData data, GeometryStyle style)
        {
            _geometryId = data.Id;
            Vector4 a = style.FigureColor;
            Vector4 b = style.CircleColor;
            Vector4 c = style.PreviewColor;
            _circleColor = new Color(b.x, b.y, b.z, b.w);
            _lineWidth = style.LineWidth;
            _previewColor = new Color(c.x, c.y, c.z, c.w);
            _previewLineWidth = style.PreviewLineWidth;
            _previewDuration = style.PreviewDuration;
            _figure.SetStyle(new Color(a.x, a.y, a.z, a.w), _lineWidth);
            _circle.SetStyle(_circleColor, _lineWidth);
            _figure.SetTriangle(data.A, data.B, data.C);
            _circleCenter = data.Center;
            _circleRadius = data.Radius;
            RestoreCircle();
        }

        /// <summary>
        /// 显示一次失败反馈环，半径和位置完全来自物理层提交的数据。
        /// </summary>
        /// <param name="feedback">物理层生成的失败反馈。</param>
        public void ShowFailureFeedback(GoalFeedback feedback)
        {
            if (_completed)
            {
                return;
            }

            _feedbackElapsed = 0;
            _feedbackVisible = true;
            _feedbackRadius = feedback.TargetRadius;
            _feedbackLineWidth = Mathf.Clamp(_lineWidth + Mathf.Abs(feedback.RadiusDelta) * .5f, _lineWidth, .18f);
            _circle.transform.localPosition = feedback.Position;
            _circle.gameObject.SetActive(true);

            if (feedback.ShouldPreview)
            {
                _previewElapsed = 0;
                _previewVisible = true;
                _previewCenter = feedback.TargetCenter;
                _previewRadius = feedback.TargetRadius;
            }

            RenderCurrentState();
        }

        /// <summary>
        /// 效果完成后隐藏对应几何障碍。
        /// </summary>
        /// <param name="completed">是否已完成图形效果；完成后隐藏图形及条件圆。</param>
        public void SetCompleted(bool completed)
        {
            _completed = completed;

            if (completed)
            {
                _feedbackVisible = false;
                _previewVisible = false;
            }

            _figure.gameObject.SetActive(!completed);
            ApplyCircleVisibility();
        }

        /// <summary>
        /// 显示或隐藏条件提示。
        /// </summary>
        /// <param name="hints">是否显示尚未完成图形的条件圆。</param>
        public void SetHints(bool hints)
        {
            _hints = hints;
            ApplyCircleVisibility();
        }

        private void Update()
        {
            if (!_feedbackVisible && !_previewVisible)
            {
                return;
            }

            if (_feedbackVisible)
            {
                _feedbackElapsed += Time.unscaledDeltaTime;

                if (_feedbackElapsed >= .45f)
                {
                    _feedbackVisible = false;
                }
            }

            if (_previewVisible)
            {
                _previewElapsed += Time.unscaledDeltaTime;

                if (_previewElapsed >= _previewDuration)
                {
                    _previewVisible = false;
                }
            }

            RenderCurrentState();
            ApplyCircleVisibility();
        }

        /// <summary>
        /// 按预亮、失败反馈、静态条件圆的优先级绘制当前状态。
        /// </summary>
        private void RenderCurrentState()
        {
            if (_previewVisible)
            {
                _circle.transform.localPosition = _previewCenter;
                _circle.SetStyle(_previewColor, _previewLineWidth);
                _circle.SetCircle(_previewRadius);

                return;
            }

            if (_feedbackVisible)
            {
                float fade = Mathf.Clamp01((_feedbackElapsed - .05f) / .4f);
                RenderFeedback(1 - fade);

                return;
            }

            RestoreCircle();
        }

        private void ApplyCircleVisibility()
        {
            _circle.gameObject.SetActive(!_completed && (_hints || _feedbackVisible || _previewVisible));
        }

        private void RenderFeedback(float opacity)
        {
            Color color = _circleColor;
            color.a *= opacity;
            _circle.SetStyle(color, _feedbackLineWidth);
            _circle.SetCircle(_feedbackRadius);
        }

        private void RestoreCircle()
        {
            _circle.transform.localPosition = _circleCenter;
            _circle.SetStyle(_circleColor, _lineWidth);
            _circle.SetCircle(_circleRadius);
        }
    }
}
