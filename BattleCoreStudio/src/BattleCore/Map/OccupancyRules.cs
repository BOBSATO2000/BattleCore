using BattleCore.Entities;
using BattleCore.World;
using System.Linq;

namespace BattleCore.Map
{
    /// <summary>
    /// Hexの占有ルールを一元管理するクラス。
    /// WorldState に保持され、MovementSystem・BattleFinder が参照する。
    ///
    /// 基本ルール：
    ///   通常Hex = 1部隊まで
    ///   城Hex   = Castle.Capacity まで（デフォルト4）
    ///
    /// これにより「前線」が自然に形成され、ZOC・Facing・包囲が活きる。
    /// </summary>
    public class OccupancyRules
    {
        /// <summary>
        /// 指定Hexに指定勢力の軍が移動できるか判定する。
        /// - 空き容量がある → true
        /// - 満員 → false（移動ブロック）
        /// - 敵が占有 → false（MovementSystem が戦闘トリガーとして扱う）
        /// </summary>
        public static bool CanEnter(int hexId, int movingClanId, WorldState world)
        {
            int capacity = GetCapacity(hexId, world);
            int occupants = world.Armies.Count(a =>
                a.Soldiers > 0 && a.CurrentHexId == hexId);
            return occupants < capacity;
        }

        /// <summary>
        /// 指定Hexに敵軍がいるか判定する。
        /// MovementSystem が「移動先に敵がいる = 戦闘」として使用する。
        /// </summary>
        public static bool HasEnemy(int hexId, int movingClanId, WorldState world)
            => world.Armies.Any(a =>
                a.Soldiers > 0 &&
                a.CurrentHexId == hexId &&
                a.ClanId != movingClanId &&
                !world.AreAllied(movingClanId, a.ClanId));

        /// <summary>
        /// 指定Hexの最大収容部隊数を返す。
        /// 城Hexは Castle.Capacity、それ以外は1。
        /// </summary>
        public static int GetCapacity(int hexId, WorldState world)
        {
            var castle = world.Castles.FirstOrDefault(c => c.HexId == hexId);
            return castle?.Capacity ?? 1;
        }

        /// <summary>
        /// 指定Hexの現在の占有部隊数を返す。
        /// </summary>
        public static int GetOccupantCount(int hexId, WorldState world)
            => world.Armies.Count(a => a.Soldiers > 0 && a.CurrentHexId == hexId);
    }
}
