namespace BattleCore.Map
{
    /// <summary>
    /// ヘックスマップ上の1マスを表す。
    /// Army は Hex を直接保持せず CurrentHexId で参照する設計（案A採用）。
    /// これにより城・建物・部隊配置・イベント発生地点など全てが Hex を参照できる。
    /// </summary>
    public class Hex
    {
        /// <summary>Hexを一意に識別するID。GameMap.GetHexById() で検索する。</summary>
        public int Id { get; }

        /// <summary>X座標（東西方向）。</summary>
        public int X { get; }

        /// <summary>Y座標（南北方向）。</summary>
        public int Y { get; }

        /// <summary>
        /// 地形種別。MovementSystem が移動可否の判定に使用する。
        /// Mountain は移動不可。
        /// </summary>
        public TerrainType Terrain { get; }

        public Hex(int id, int x, int y, TerrainType terrain = TerrainType.Plain)
        {
            Id = id;
            X = x;
            Y = y;
            Terrain = terrain;
        }
    }
}
