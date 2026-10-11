using System;
using System.Collections.Generic;
using UnityEngine;

namespace GameLogic
{
    /// <summary>
    /// 地形规则可用的受控模拟上下文；只暴露模拟所需数据与操作，不接触表现与全局模块。
    /// </summary>
    public sealed class TerrainContext
    {
        /// <summary>
        /// 由世界注入的候选位置可用性查询。
        /// </summary>
        private readonly Func<Vector2, float, int, bool> _canPlace;

        /// <summary>
        /// 本关全部球体。
        /// </summary>
        public readonly SimulatedBall[] Balls;

        /// <summary>
        /// 本关全部地形摆放。
        /// </summary>
        public readonly IReadOnlyList<TerrainPlacementData> Terrains;

        /// <summary>
        /// 每球每地形的接触记录。
        /// </summary>
        public readonly TerrainContactBook Contacts;

        /// <summary>
        /// 轨迹接触时间求解器。
        /// </summary>
        public readonly ContactSolver Solver;

        /// <summary>
        /// 接触分离使用的距离容差。
        /// </summary>
        public readonly float ContactTolerance;

        /// <summary>
        /// 球体停止判定的速度阈值。
        /// </summary>
        public readonly float StopSpeed;

        /// <summary>
        /// 创建地形规则上下文。
        /// </summary>
        /// <param name="balls">本关全部球体。</param>
        /// <param name="terrains">本关全部地形摆放。</param>
        /// <param name="contacts">每球每地形的接触记录。</param>
        /// <param name="solver">轨迹接触时间求解器。</param>
        /// <param name="physics">本关物理参数。</param>
        /// <param name="canPlace">候选位置可用性查询。</param>
        public TerrainContext(
            SimulatedBall[] balls,
            IReadOnlyList<TerrainPlacementData> terrains,
            TerrainContactBook contacts,
            ContactSolver solver,
            PhysicsConfigData physics,
            Func<Vector2, float, int, bool> canPlace)
        {
            Balls = balls;
            Terrains = terrains;
            Contacts = contacts;
            Solver = solver;
            ContactTolerance = physics.ContactTolerance;
            StopSpeed = physics.StopSpeed;
            _canPlace = canPlace;
        }

        /// <summary>
        /// 查询候选位置与半径是否可放置，排除指定球自身。
        /// </summary>
        /// <param name="position">待放置的台面局部圆心坐标。</param>
        /// <param name="radius">待放置的球半径。</param>
        /// <param name="exceptBall">重叠检查需要排除的球索引。</param>
        /// <returns>候选球是否完整位于台面且不与其他活动球重叠。</returns>
        public bool CanPlaceAt(Vector2 position, float radius, int exceptBall)
        {
            return _canPlace(position, radius, exceptBall);
        }

        /// <summary>
        /// 按关卡数组索引取地形。
        /// </summary>
        /// <param name="index">地形在关卡数组中的索引。</param>
        /// <returns>对应的地形摆放。</returns>
        public TerrainPlacementData GetTerrain(int index)
        {
            return Terrains[index];
        }

        /// <summary>
        /// 按地形 ID 取地形。
        /// </summary>
        /// <param name="id">地形明细 ID。</param>
        /// <returns>对应地形；不存在时抛出参数异常。</returns>
        public TerrainPlacementData TerrainById(int id)
        {
            int index = TerrainIndex(id);

            if (index < 0)
            {
                throw new ArgumentException("不存在地形 " + id);
            }

            return Terrains[index];
        }

        /// <summary>
        /// 按地形 ID 取关卡数组索引。
        /// </summary>
        /// <param name="id">地形明细 ID。</param>
        /// <returns>关卡数组索引；不存在时返回负值。</returns>
        public int TerrainIndex(int id)
        {
            for (int i = 0; i < Terrains.Count; i++)
            {
                if (Terrains[i].Id == id)
                {
                    return i;
                }
            }

            return -1;
        }

        /// <summary>
        /// 按入口面法线反射入射速度，用于球径过大时弹开。
        /// </summary>
        /// <param name="ball">需要修改速度的模拟球。</param>
        /// <param name="normal">入口面的单位法线。</param>
        public static void ReflectAtEntrance(SimulatedBall ball, Vector2 normal)
        {
            ball.State.Velocity = Vector2.Reflect(ball.State.Velocity, normal);
        }
    }
}
