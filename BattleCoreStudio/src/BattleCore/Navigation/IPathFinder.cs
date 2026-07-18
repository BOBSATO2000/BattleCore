using BattleCore.Map;
using System.Collections.Generic;

namespace BattleCore.Navigation
{
    /// <summary>
    /// 経路探索インターフェース。
    /// SimpleArmyDecision など AI が「目的地までの経路」を求める際に使用する。
    /// 実装を差し替えることで A* など高度なアルゴリズムに変更できる。
    /// </summary>
    public interface IPathFinder
    {
        /// <summary>
        /// 開始HexIDから目標HexIDまでの経路をHex IDのリストで返す。
        /// リストの先頭が startHexId、末尾が targetHexId になる。
        /// 経路が見つからない場合は空リストを返す。
        /// </summary>
        List<int> FindPath(GameMap map, int startHexId, int targetHexId);
    }
}
