namespace BattleCore.Entities
{
    /// <summary>
    /// ゲーム内全エンティティの基底クラス。
    /// Army / Officer / Clan / Relationship など全てのゲームオブジェクトが継承する。
    /// </summary>
    public abstract class Entity
    {
        /// <summary>エンティティを一意に識別するID。</summary>
        public int Id { get; init; }

        protected Entity(int id) => Id = id;

        protected Entity() { }
    }
}
