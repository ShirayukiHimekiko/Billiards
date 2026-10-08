using System;
using GameConfig;
using UnityEngine;

namespace GameLogic
{
    /// <summary>
    /// 一个图形只持有一种条件及一个效果绑定。
    /// </summary>
    public sealed class GeometryData
    {
        /// <summary>
        /// 图形明细 ID。
        /// </summary>
        public readonly int Id;

        /// <summary>
        /// 唯一效果绑定的球洞或道具明细 ID。
        /// </summary>
        public readonly int TargetId;

        /// <summary>
        /// 变换后台面局部坐标中的第一个三角形顶点。
        /// </summary>
        public readonly Vector2 A;

        /// <summary>
        /// 变换后台面局部坐标中的第二个三角形顶点。
        /// </summary>
        public readonly Vector2 B;

        /// <summary>
        /// 变换后台面局部坐标中的第三个三角形顶点。
        /// </summary>
        public readonly Vector2 C;

        /// <summary>
        /// 配置条件对应的内切圆或外接圆圆心。
        /// </summary>
        public readonly Vector2 Center;

        /// <summary>
        /// 配置条件对应的内切圆或外接圆半径。
        /// </summary>
        public readonly float Radius;

        /// <summary>
        /// 几何条件判定允许的距离误差。
        /// </summary>
        public readonly float Tolerance;

        /// <summary>
        /// 本图形采用的唯一内切或外接条件。
        /// </summary>
        public readonly ConditionKind Condition;

        /// <summary>
        /// 允许触发条件及通过几何门的球种。
        /// </summary>
        public readonly BallKind AllowedBallKind;

        /// <summary>
        /// 效果是否为解锁球洞；否则触发绑定道具。
        /// </summary>
        public readonly bool UnlocksPocket;

        /// <summary>
        /// 转换顶点并计算唯一目标圆。
        /// </summary>
        /// <param name="row">包含三角形、变换、唯一条件及效果绑定的图形明细。</param>
        public GeometryData(LevelGeometry row)
        {
            if (!(row.Shape is Triangle triangle))
            {
                throw new ArgumentException($"图形 {row.Id} 暂不支持该形状。");
            }

            Id = row.Id;
            Condition = row.ConditionKind;
            Tolerance = row.Tolerance;
            AllowedBallKind = row.AllowedBallKind;

            Quaternion rotation = Quaternion.Euler(0, 0, row.Rotation);
            A = row.Position + (Vector2)(rotation * (triangle.A * row.UniformScale));
            B = row.Position + (Vector2)(rotation * (triangle.B * row.UniformScale));
            C = row.Position + (Vector2)(rotation * (triangle.C * row.UniformScale));

            Vector2 u = B - A;
            Vector2 v = C - A;
            float cross = LevelData.Cross(u, v);

            if (Mathf.Abs(cross) < .00001f || row.UniformScale <= 0)
            {
                throw new ArgumentException($"图形 {Id} 退化或缩放无效。");
            }

            if (Condition == ConditionKind.Inscribed)
            {
                float a = Vector2.Distance(B, C);
                float b = Vector2.Distance(C, A);
                float c = Vector2.Distance(A, B);
                Center = (a * A + b * B + c * C) / (a + b + c);
                Radius = Mathf.Abs(cross) / (a + b + c);
            }
            else
            {
                Center = A + new Vector2(v.y * u.sqrMagnitude - u.y * v.sqrMagnitude, u.x * v.sqrMagnitude - v.x * u.sqrMagnitude) / (2 * cross);
                Radius = Vector2.Distance(Center, A);
            }

            UnlocksPocket = row.EffectBinding is UnlockPocket;
            TargetId = UnlocksPocket
                ? ((UnlockPocket)row.EffectBinding).PocketPlacementId : ((TriggerProp)row.EffectBinding).PropPlacementId;
        }

        /// <summary>
        /// 取得三角形顶点。
        /// </summary>
        /// <param name="index">三角形顶点索引，调用方使用零到二。</param>
        /// <returns>台面局部坐标中的对应顶点。</returns>
        public Vector2 Vertex(int index)
        {
            return index == 0 ? A : index == 1 ? B : C;
        }
    }
}
