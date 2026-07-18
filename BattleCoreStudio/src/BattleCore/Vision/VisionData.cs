using System.Collections.Generic;

namespace BattleCore.Vision
{
    /// <summary>
    /// 1つの Army の索敵情報。VisionSystem が毎Step生成し WorldState.Visions に格納する。
    /// AI は VisibleHexes を参照することで「見えている範囲」だけを認識できる。
    /// </summary>
    public class VisionData
    {
        /// <summary>この索敵情報を持つ Army の ID。</summary>
        public int ArmyId { get; }

        /// <summary>視界内にある Hex の ID セット。</summary>
        public HashSet<int> VisibleHexes { get; } = new();

        public VisionData(int armyId)
        {
            ArmyId = armyId;
        }
    }
}
