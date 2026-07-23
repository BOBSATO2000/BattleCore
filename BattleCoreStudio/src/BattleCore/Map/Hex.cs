namespace BattleCore.Map
{
    /// <summary>
    /// ヘックスマップ上の1マスを表す。
    /// Army は Hex を直接保持せず CurrentHexId で参照する設計（案A採用）。
    /// これにより城・建物・部隊配置・イベント発生地点など全てが Hex を参照できる。
    /// </summary>
    public class Hex
    {
        public int Id      { get; }
        public int X       { get; }
        public int Y       { get; }
        public TerrainType Terrain { get; }

        /// <summary>
        /// 高度（0〜3）。
        /// 0=平地・谷、1=丘陵、2=高台、3=山頂。
        /// MovementSystem: 登坂AP+1、下り無消費。
        /// VisionSystem: 高いほど視界+Height。
        /// BattleModifier: 高所攻撃+20%、高所防御+10%。
        /// </summary>
        public int Height { get; }

        public Hex(int id, int x, int y, TerrainType terrain = TerrainType.Plain, int height = 0)
        {
            Id      = id;
            X       = x;
            Y       = y;
            Terrain = terrain;
            Height  = System.Math.Clamp(height, 0, 3);
        }
    }
}
