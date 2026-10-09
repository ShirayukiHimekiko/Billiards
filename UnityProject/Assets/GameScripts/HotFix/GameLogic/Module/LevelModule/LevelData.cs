using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using GameConfig;
using UnityEngine;

namespace GameLogic
{
    /// <summary>
    /// 由九表聚合的本关快照，摆放数据不混入运行进度。
    /// </summary>
    public sealed class LevelData
    {
        /// <summary>
        /// 关卡主表 ID。
        /// </summary>
        public readonly int Id;

        /// <summary>
        /// 本关允许的击球次数上限。
        /// </summary>
        public readonly int MaxShots;

        /// <summary>
        /// 本关可使用的折射预测道具次数。
        /// </summary>
        public readonly int PredictionPropCount;

        /// <summary>
        /// 用于界面提示的关卡名称。
        /// </summary>
        public readonly string Name;

        /// <summary>
        /// 资源系统加载本关台面 Prefab 使用的地址。
        /// </summary>
        public readonly string BoardLocation;

        /// <summary>
        /// 本关的操作或解谜教学提示。
        /// </summary>
        public readonly string Objective;

        /// <summary>
        /// 本关初始是否显示图形条件圆提示。
        /// </summary>
        public readonly bool ShowHints;

        /// <summary>
        /// 台面局部坐标中的有效边界。
        /// </summary>
        public readonly Rect Bounds;

        /// <summary>
        /// 本关固定步长、接触与预测使用的物理参数。
        /// </summary>
        public readonly PhysicsConfigData Physics;

        /// <summary>
        /// 本关球体的出生摆放快照，顺序供模拟与表现共同使用。
        /// </summary>
        public readonly ReadOnlyCollection<BallPlacementData> Balls;

        /// <summary>
        /// 本关六个球洞的摆放及初始状态。
        /// </summary>
        public readonly ReadOnlyCollection<PocketPlacementData> Pockets;

        /// <summary>
        /// 本关道具的摆放及作用域参数。
        /// </summary>
        public readonly ReadOnlyCollection<PropPlacementData> Props;

        /// <summary>
        /// 本关图形的唯一条件及效果绑定。
        /// </summary>
        public readonly ReadOnlyCollection<GeometryData> Geometries;

        /// <summary>
        /// 三角形与条件圆的显示样式。
        /// </summary>
        public readonly GeometryStyle GeometryStyle;

        /// <summary>
        /// 预测虚线与终点圆点的显示样式。
        /// </summary>
        public readonly PredictionStyle PredictionStyle;

        /// <summary>
        /// 四种球洞状态对应的显示样式。
        /// </summary>
        public readonly ReadOnlyCollection<PocketStyle> PocketStyles;

        /// <summary>
        /// 本关唯一白球的出生配置。
        /// </summary>
        public readonly BallPlacementData White;

        /// <summary>
        /// 白球输入参数。
        /// </summary>
        public BallConfigData Ball => White.Config;

        /// <summary>
        /// 构造并检查关卡布局及绑定关系。
        /// </summary>
        /// <param name="row">关卡主表记录。</param>
        /// <param name="board">台面边界及表现配置。</param>
        /// <param name="physics">已转换的物理参数。</param>
        /// <param name="balls">本关球体出生数据。</param>
        /// <param name="pockets">本关六洞摆放数据。</param>
        /// <param name="props">本关道具摆放及作用域参数。</param>
        /// <param name="geometries">本关图形条件及效果绑定。</param>
        public LevelData(
            Level row,
            Board board,
            PhysicsConfigData physics,
            List<BallPlacementData> balls,
            List<PocketPlacementData> pockets,
            List<PropPlacementData> props,
            List<GeometryData> geometries)
        {
            Id = row.Id;
            Name = row.Name;
            MaxShots = row.MaxShots;
            PredictionPropCount = row.PredictionPropCount;
            ShowHints = row.ShowGeometryHintsByDefault;
            BoardLocation = board.PrefabLocation;
            Objective = row.Objective;
            Bounds = Rect.MinMaxRect(board.Min.x, board.Min.y, board.Max.x, board.Max.y);
            Physics = physics;
            Balls = balls.AsReadOnly();
            Pockets = pockets.AsReadOnly();
            Props = props.AsReadOnly();
            Geometries = geometries.AsReadOnly();
            GeometryStyle = board.GeometryStyle;
            PredictionStyle = board.PredictionStyle;
            PocketStyles = board.PocketStyles.AsReadOnly();

            int whiteCount = 0;
            int blackCount = 0;

            foreach (var ball in Balls)
            {
                if (ball.Config.Kind == BallKind.White)
                {
                    White = ball;
                    whiteCount++;
                }
                else
                {
                    blackCount++;
                }

                if (!ContainsCircle(ball.Position, ball.Radius)
                    || ball.Radius < ball.Config.RadiusMin
                    || ball.Radius > ball.Config.RadiusMax)
                {
                    throw new ArgumentException($"关卡 {Id} 球 {ball.Id} 出生配置无效。");
                }
            }

            if (whiteCount != 1
                || blackCount < 1
                || blackCount > 6
                || MaxShots <= 0
                || PredictionPropCount < 0
                || Bounds.width <= 0
                || Bounds.height <= 0
                || Pockets.Count != 6)
            {
                throw new ArgumentException($"关卡 {Id} 必须有一个白球、一至六个黑球及六洞。");
            }

            for (int i = 0; i < Balls.Count; i++)
            {
                for (int j = i + 1; j < Balls.Count; j++)
                {
                    if (Vector2.Distance(Balls[i].Position, Balls[j].Position) <= Balls[i].Radius + Balls[j].Radius)
                    {
                        throw new ArgumentException($"关卡 {Id} 出生球重叠。");
                    }
                }
            }

            var slots = new HashSet<PocketSlot>();
            var boundPockets = new HashSet<int>();
            var boundProps = new HashSet<int>();
            var keyPockets = new HashSet<int>();

            foreach (var pocket in Pockets)
            {
                if (!slots.Add(pocket.Slot)
                    || pocket.InitialState == PocketState.Occupied
                    || pocket.CaptureRadius <= 0
                    || pocket.MouthWidth < 2 * pocket.CaptureRadius)
                {
                    throw new ArgumentException($"关卡 {Id} 洞状态、槽位或尺寸无效。");
                }
            }

            foreach (var prop in Props)
            {
                if (prop.UseLimit <= 0 || prop.TriggerRadius <= 0 || prop.VisualDiameter <= 0 || !ContainsCircle(
                    prop.Position,
                    prop.TriggerRadius))
                {
                    throw new ArgumentException($"道具 {prop.Id} 配置无效。");
                }

                if (prop.Kind == PropEffectKind.Key
                    && (prop.TriggerMode != TriggerMode.Contact
                    || prop.UseScope != UseScope.Level
                    || prop.UseLimit != 1
                    || prop.TargetPocketId <= 0
                    || !keyPockets.Add(prop.TargetPocketId)
                    || !pockets.Exists(p => p.Id == prop.TargetPocketId
                    && p.InitialState == PocketState.Locked)))
                {
                    throw new ArgumentException($"钥匙道具 {prop.Id} 必须接触拾取、整关只使用一次，并独占绑定本关上锁洞。");
                }
            }

            foreach (var geometry in Geometries)
            {
                if (geometry.Tolerance < 0 || !Bounds.Contains(geometry.A) || !Bounds.Contains(geometry.B) || !Bounds.Contains(geometry.C))
                {
                    throw new ArgumentException($"图形 {geometry.Id} 位置或容差无效。");
                }

                if (geometry.UnlocksPocket)
                {
                    if (!boundPockets.Add(geometry.TargetId)
                        || !pockets.Exists(p => p.Id == geometry.TargetId
                        && p.InitialState == PocketState.Locked))
                    {
                        throw new ArgumentException($"图形 {geometry.Id} 应独占绑定本关上锁洞。");
                    }
                }
                else if (!boundProps.Add(geometry.TargetId)
                    || !props.Exists(p => p.Id == geometry.TargetId
                    && p.TriggerMode == TriggerMode.Geometry))
                {
                    throw new ArgumentException($"图形 {geometry.Id} 应独占绑定本关 Geometry 道具。");
                }
            }

            int usable = 0;

            foreach (var pocket in Pockets)
            {
                if (pocket.InitialState == PocketState.Locked
                    && !boundPockets.Contains(pocket.Id)
                    && !keyPockets.Contains(pocket.Id))
                {
                    throw new ArgumentException($"洞 {pocket.Id} 缺少解锁配置。");
                }

                if (pocket.InitialState != PocketState.Disabled)
                {
                    usable++;
                }
            }

            foreach (var prop in Props)
            {
                if (prop.TriggerMode == TriggerMode.Geometry && !boundProps.Contains(prop.Id))
                {
                    throw new ArgumentException($"道具 {prop.Id} 缺少触发图形。");
                }
            }

            if (blackCount > usable)
            {
                throw new ArgumentException($"关卡 {Id} 黑球数量超过可用洞数量。");
            }
        }

        /// <summary>
        /// 检查圆是否完整位于台面。
        /// </summary>
        /// <param name="p">台面局部坐标中的圆心。</param>
        /// <param name="r">圆半径。</param>
        /// <returns>圆的四个极值点是否都位于台面边界内。</returns>
        public bool ContainsCircle(Vector2 p, float r)
        {
            return p.x - r >= Bounds.xMin
                && p.x + r <= Bounds.xMax
                && p.y - r >= Bounds.yMin
                && p.y + r <= Bounds.yMax;
        }

        /// <summary>
        /// 二维叉积。
        /// </summary>
        /// <param name="a">第一个二维向量。</param>
        /// <param name="b">第二个二维向量。</param>
        /// <returns>二维叉积的有符号标量值。</returns>
        public static float Cross(Vector2 a, Vector2 b)
        {
            return a.x * b.y - a.y * b.x;
        }
    }
}
