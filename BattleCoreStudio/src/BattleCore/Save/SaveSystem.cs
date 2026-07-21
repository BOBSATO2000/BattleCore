using BattleCore.Entities;
using BattleCore.Map;
using BattleCore.Relations;
using BattleCore.Simulation;
using BattleCore.World;
using System;
using System.IO;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace BattleCore.Save
{
    /// <summary>
    /// セーブ／ロードの責務を持つクラス。
    /// SimulationEngine を知らず、WinForms から直接呼ばれる。
    /// 
    /// 使い方:
    ///   SaveSystem.Save(context, "sengoku1560", path)
    ///   var (context, scenarioId) = SaveSystem.Load(path)
    /// </summary>
    public static class SaveSystem
    {
        /// <summary>現在のセーブデータフォーマットバージョン。</summary>
        public const int CurrentVersion = 1;

        private static readonly JsonSerializerOptions JsonOptions = new()
        {
            WriteIndented = true,
            Converters = { new JsonStringEnumConverter() },
        };

        /// <summary>
        /// 現在のシミュレーション状態をファイルに保存する。
        /// </summary>
        public static void Save(SimulationContext context, string scenarioId, string filePath)
        {
            var data = new SaveData
            {
                Metadata = new SaveMetadata
                {
                    Version    = CurrentVersion,
                    SavedAt    = DateTime.UtcNow,
                    ScenarioId = scenarioId,
                    Turn       = context.Time.Tick,
                    Phase      = context.CurrentPhase.ToString(),
                },
                Time = new GameTimeSnapshot
                {
                    Tick    = context.Time.Tick,
                    Year    = context.Time.Year,
                    Season  = context.Time.Season.ToString(),
                    Weather = context.Time.Weather.ToString(),
                },
                CurrentPhase = context.CurrentPhase,
                World        = TakeWorldSnapshot(context.World),
            };

            var json = JsonSerializer.Serialize(data, JsonOptions);
            File.WriteAllText(filePath, json);
        }

        /// <summary>
        /// ファイルからセーブデータを読み込み、SimulationContext を復元する。
        /// 戻り値の string はシナリオID。
        /// </summary>
        public static (SimulationContext context, string scenarioId) Load(string filePath)
        {
            var json = File.ReadAllText(filePath);
            var data = JsonSerializer.Deserialize<SaveData>(json, JsonOptions)
                ?? throw new InvalidDataException("セーブデータの読み込みに失敗しました。");

            if (data.Metadata.Version != CurrentVersion)
                throw new InvalidDataException(
                    $"セーブデータのバージョンが異なります。(保存:{data.Metadata.Version} / 現在:{CurrentVersion})");

            var world   = RestoreWorld(data.World);
            var context = RestoreContext(world, data);

            return (context, data.Metadata.ScenarioId);
        }

        /// <summary>メタデータのみ読み込む（セーブ一覧表示用）。</summary>
        public static SaveMetadata LoadMetadata(string filePath)
        {
            var json = File.ReadAllText(filePath);
            var data = JsonSerializer.Deserialize<SaveData>(json, JsonOptions)
                ?? throw new InvalidDataException("セーブデータの読み込みに失敗しました。");
            return data.Metadata;
        }

        // ── Snapshot 生成 ────────────────────────────────────────

        private static WorldSnapshot TakeWorldSnapshot(WorldState world)
        {
            var snap = new WorldSnapshot { Weather = world.Weather.ToString() };

            foreach (var c in world.Clans)
                snap.Clans.Add(new ClanSnapshot
                {
                    Id = c.Id, Name = c.Name, Gold = c.Gold,
                    DaimyoOfficerId = c.DaimyoOfficerId,
                    IsPlayerControlled = c.IsPlayerControlled,
                });

            foreach (var o in world.Officers)
                snap.Officers.Add(new OfficerSnapshot
                {
                    Id = o.Id, Name = o.Name, Loyalty = o.Loyalty,
                    Intelligence = o.Intelligence, Ambition = o.Ambition,
                    Leadership = o.Leadership, Strategy = o.Strategy,
                    Courage = o.Courage, BattleWins = o.BattleWins,
                });

            foreach (var a in world.Armies)
                snap.Armies.Add(new ArmySnapshot
                {
                    Id = a.Id, ClanId = a.ClanId, OfficerId = a.OfficerId,
                    CurrentHexId = a.CurrentHexId, DestinationHexId = a.DestinationHexId,
                    Soldiers = a.Soldiers, MoveCooldown = a.MoveCooldown,
                    ActionPoints = a.ActionPoints,
                });

            foreach (var c in world.Castles)
                snap.Castles.Add(new CastleSnapshot
                {
                    Id = c.Id, Name = c.Name, HexId = c.HexId,
                    OwnerClanId = c.OwnerClanId,
                    ReinforcementPerTick = c.ReinforcementPerTick,
                });

            foreach (var al in world.Alliances)
                snap.Alliances.Add(new AllianceSnapshot
                    { ClanId1 = al.ClanId1, ClanId2 = al.ClanId2 });

            foreach (var r in world.Relationships)
                snap.Relationships.Add(new RelationshipSnapshot
                {
                    Id = r.Id, FromOfficerId = r.FromOfficerId, ToOfficerId = r.ToOfficerId,
                    Trust = r.Trust, Respect = r.Respect, Dislike = r.Dislike,
                });

            foreach (var m in world.Memberships)
                snap.Memberships.Add(new MembershipSnapshot
                    { OfficerId = m.OfficerId, ClanId = m.ClanId });

            foreach (var h in world.Map.Hexes)
                snap.Hexes.Add(new HexSnapshot
                    { Id = h.Id, X = h.X, Y = h.Y, Terrain = h.Terrain.ToString() });

            return snap;
        }

        // ── 復元 ─────────────────────────────────────────────────

        private static WorldState RestoreWorld(WorldSnapshot snap)
        {
            var world = new WorldState();

            foreach (var c in snap.Clans)
            {
                var clan = new Clan(c.Id)
                {
                    Name = c.Name, Gold = c.Gold,
                    DaimyoOfficerId = c.DaimyoOfficerId,
                    IsPlayerControlled = c.IsPlayerControlled,
                };
                world.Clans.Add(clan);
            }

            foreach (var o in snap.Officers)
            {
                var officer = new Officer(o.Id, o.Name)
                {
                    Loyalty = o.Loyalty, Intelligence = o.Intelligence,
                    Ambition = o.Ambition, Leadership = o.Leadership,
                    Strategy = o.Strategy, Courage = o.Courage,
                    BattleWins = o.BattleWins,
                };
                world.Officers.Add(officer);
            }

            foreach (var a in snap.Armies)
            {
                var army = new Army(a.Id)
                {
                    ClanId = a.ClanId,
                    MoveCooldown = a.MoveCooldown,
                    ActionPoints = a.ActionPoints,
                };
                army.MoveTo(a.CurrentHexId);
                if (a.DestinationHexId.HasValue) army.OrderMove(a.DestinationHexId.Value);
                army.LoseSoldiers(1000 - a.Soldiers);
                if (a.OfficerId.HasValue) army.AssignOfficer(a.OfficerId.Value);
                world.Armies.Add(army);
            }

            foreach (var c in snap.Castles)
            {
                world.Castles.Add(new Castle(c.Id, c.Name, c.HexId, c.OwnerClanId, c.ReinforcementPerTick));
            }

            int allianceId = 1;
            foreach (var al in snap.Alliances)
                world.Alliances.Add(new Alliance(allianceId++, al.ClanId1, al.ClanId2, int.MaxValue));

            foreach (var r in snap.Relationships)
            {
                var rel = new Relationship(r.Id, r.FromOfficerId, r.ToOfficerId)
                    { Trust = r.Trust, Respect = r.Respect, Dislike = r.Dislike };
                world.Relationships.Add(rel);
            }

            int membershipId = 1;
            foreach (var m in snap.Memberships)
                world.Memberships.Add(new Membership(membershipId++, m.OfficerId, m.ClanId));

            foreach (var h in snap.Hexes)
            {
                var terrain = Enum.Parse<TerrainType>(h.Terrain);
                world.Map.AddHex(new Hex(h.Id, h.X, h.Y, terrain));
            }

            world.Weather = Enum.Parse<Weather>(snap.Weather);
            return world;
        }

        private static SimulationContext RestoreContext(WorldState world, SaveData data)
        {
            var time = new GameTime();
            // Tick・Year・Season・Weather を復元するため内部状態を合わせる
            RestoreGameTime(time, data.Time);

            var context = new SimulationContext(world, time)
            {
                CurrentPhase = data.CurrentPhase,
            };
            return context;
        }

        private static void RestoreGameTime(GameTime time, GameTimeSnapshot snap)
        {
            // GameTime は Advance() で状態が進むため、
            // 保存値を直接セットできるよう RestoreFrom() を呼ぶ
            time.RestoreFrom(snap.Tick, snap.Year,
                Enum.Parse<Season>(snap.Season),
                Enum.Parse<Weather>(snap.Weather));
        }
    }
}
