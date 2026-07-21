using BattleCore.Entities;
using BattleCore.Map;
using BattleCore.Relations;
using BattleCore.World;
using System.Collections.Generic;
using System.IO;
using System.Text.Json;

namespace BattleCore.Scenario
{
    /// <summary>
    /// JSONシナリオファイルを読み込み WorldState を構築するスタティッククラス。
    /// frMain.InitSimulation() から呼び出され、シナリオ選択ダイアログで選んだファイルを渡す。
    /// </summary>
    public static class ScenarioLoader
    {
        private static readonly JsonSerializerOptions Options = new()
        {
            PropertyNameCaseInsensitive = true
        };

        /// <summary>
        /// JSONファイルを読み込み、WorldState・タイトル・イベントトリガーリストを返す。
        /// </summary>
        public static (WorldState world, string title, List<EventTriggerData> triggers) Load(string path)
        {
            var json = File.ReadAllText(path);
            var data = JsonSerializer.Deserialize<ScenarioData>(json, Options)
                       ?? throw new InvalidDataException("シナリオファイルの読み込みに失敗しました。");

            var world = new WorldState();

            foreach (var h in data.Map)
            {
                var terrain = h.Terrain switch
                {
                    "Mountain" => TerrainType.Mountain,
                    "Forest"   => TerrainType.Forest,
                    _          => TerrainType.Plain,
                };
                world.Map.AddHex(new Hex(h.Id, h.Q, h.R, terrain));
            }

            foreach (var c in data.Clans)
                world.Clans.Add(new Clan(c.Id)
                {
                    Name               = c.Name,
                    DaimyoOfficerId    = c.DaimyoOfficerId,
                    IsPlayerControlled = c.IsPlayerControlled,
                });

            foreach (var o in data.Officers)
                world.Officers.Add(new Officer(o.Id, o.Name)
                {
                    Leadership   = o.Leadership,
                    Strategy     = o.Strategy,
                    Courage      = o.Courage,
                    Loyalty      = o.Loyalty,
                    Intelligence = o.Intelligence,
                    Ambition     = o.Ambition,
                    Personality  = string.IsNullOrEmpty(o.Personality)
                        ? OfficerPersonality.Loyal
                        : System.Enum.Parse<OfficerPersonality>(o.Personality, ignoreCase: true),
                });

            foreach (var m in data.Memberships)
                world.Memberships.Add(new Membership(m.Id, m.OfficerId, m.ClanId) { Loyalty = m.Loyalty });

            foreach (var a in data.Armies)
            {
                var army = new Army(a.Id, 0, a.ClanId, a.CurrentHexId);
                if (a.Soldiers < 1000) army.LoseSoldiers(1000 - a.Soldiers);
                else if (a.Soldiers > 1000) army.Reinforce(a.Soldiers - 1000);
                if (a.OfficerId.HasValue) army.AssignOfficer(a.OfficerId.Value);
                world.Armies.Add(army);
            }

            foreach (var r in data.Relationships)
                world.Relationships.Add(new Relationship(r.Id, r.FromOfficerId, r.ToOfficerId)
                {
                    Trust   = r.Trust,
                    Respect = r.Respect,
                    Dislike = r.Dislike,
                });

            foreach (var a in data.Alliances)
                world.Alliances.Add(new Alliance(a.Id, a.ClanId1, a.ClanId2, a.DurationTicks));

            foreach (var c in data.Castles)
                world.Castles.Add(new Castle(c.Id, c.Name, c.HexId, c.OwnerClanId, c.ReinforcementPerTick));

            return (world, data.Title, data.EventTriggers);
        }
    }
}
