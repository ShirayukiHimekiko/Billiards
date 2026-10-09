using System;
using GameConfig;
using UnityEngine;

namespace GameLogic
{
    /// <summary>
    /// 一个图形同时持有内切、外接两类目标及一个效果绑定。
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
        /// 内切目标圆心。
        /// </summary>
        public readonly Vector2 InscribedCenter;

        /// <summary>
        /// 外接目标圆心。
        /// </summary>
        public readonly Vector2 CircumscribedCenter;

        /// <summary>
        /// 配置中的内切目标半径。
        /// </summary>
        public readonly float InscribedRadius;

        /// <summary>
        /// 配置中的外接目标半径。
        /// </summary>
        public readonly float CircumscribedRadius;

        /// <summary>
        /// 几何条件判定允许的距离误差。
        /// </summary>
        public readonly float Tolerance;

        /// <summary>
        /// 历史配置中的提示优先条件，不参与双目标判定。
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
        /// 转换顶点并计算双目标圆。
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

            float a = Vector2.Distance(B, C);
            float b = Vector2.Distance(C, A);
            float c = Vector2.Distance(A, B);
            InscribedCenter = (a * A + b * B + c * C) / (a + b + c);
            float calculatedInscribedRadius = Mathf.Abs(cross) / (a + b + c);
            CircumscribedCenter = A + new Vector2(v.y * u.sqrMagnitude - u.y * v.sqrMagnitude, u.x * v.sqrMagnitude - v.x * u.sqrMagnitude) / (2 * cross);
            float calculatedCircumscribedRadius = Vector2.Distance(CircumscribedCenter, A);

            if (row.InscribedRadius <= 0 || row.CircumscribedRadius <= 0)
            {
                throw new ArgumentException($"图形 {Id} 的双目标半径必须为正数。");
            }

            if (Mathf.Abs(row.InscribedRadius - calculatedInscribedRadius) > .001f
                || Mathf.Abs(row.CircumscribedRadius - calculatedCircumscribedRadius) > .001f)
            {
                throw new ArgumentException($"图形 {Id} 的双目标半径与三角形几何数据不一致。");
            }

            InscribedRadius = row.InscribedRadius;
            CircumscribedRadius = row.CircumscribedRadius;

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

        /// <summary>
        /// 取得历史提示优先使用的目标圆心。
        /// </summary>
        public Vector2 Center
        {
            get
            {
                return Condition == ConditionKind.Inscribed ? InscribedCenter : CircumscribedCenter;
            }
        }

        /// <summary>
        /// 取得历史提示优先使用的目标半径。
        /// </summary>
        public float Radius
        {
            get
            {
                return Condition == ConditionKind.Inscribed ? InscribedRadius : CircumscribedRadius;
            }
        }

        /// <summary>
        /// 取得球心到三角形三条内法线边界的最小距离。
        /// </summary>
        public float MinimumBoundaryDistance(Vector2 position, out Vector2 normal)
        {
            float orientation = Mathf.Sign(LevelData.Cross(B - A, C - A));
            float minimum = float.PositiveInfinity;
            normal = Vector2.zero;

            for (int i = 0; i < 3; i++)
            {
                Vector2 vertex = Vertex(i);
                Vector2 edge = Vertex((i + 1) % 3) - vertex;
                Vector2 edgeNormal = orientation * new Vector2(-edge.y, edge.x).normalized;
                float distance = Vector2.Dot(edgeNormal, position - vertex);

                if (distance < minimum)
                {
                    minimum = distance;
                    normal = edgeNormal;
                }
            }

            return minimum;
        }
    }
}
