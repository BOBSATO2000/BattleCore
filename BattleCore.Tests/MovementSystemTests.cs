using BattleCore.Entities;
using BattleCore.Map;
using BattleCore.Simulation;
using BattleCore.Systems;
using BattleCore.World;

namespace BattleCore.Tests;

[TestClass]
public class MovementSystemTests
{
    [TestMethod]
    public void ArmyMovesAfterStep()
    {
        // Arrange
        var world = new WorldState();

        var hex1 = new Hex(1, 0, 0);
        var hex2 = new Hex(2, 1, 0);

        world.Map.AddHex(hex1);
        world.Map.AddHex(hex2);

        var army = new Army(1, 1, 1, hex1.Id);
        world.Armies.Add(army);

        army.OrderMove(hex2.Id);

        var engine = new SimulationEngine(world);
        engine.RegisterSystem(new MovementSystem());

        // Act
        engine.Step();

        // Assert
        Assert.AreEqual(hex2.Id, army.CurrentHexId);
        Assert.IsNull(army.DestinationHexId);
    }

    [TestMethod]
    public void ArmyMovesOneHexPerStep()
    {
        // Arrange
        var world = new WorldState();

        var hex1 = new Hex(1, 0, 0);
        var hex2 = new Hex(2, 1, 0);
        var hex3 = new Hex(3, 2, 0);

        world.Map.AddHex(hex1);
        world.Map.AddHex(hex2);
        world.Map.AddHex(hex3);

        var army = new Army(1, 1, 1, hex1.Id);
        world.Armies.Add(army);

        army.OrderMove(hex3.Id);

        var engine = new SimulationEngine(world);
        engine.RegisterSystem(new MovementSystem());

        // Act
        engine.Step();

        // Assert
        Assert.AreEqual(hex2.Id, army.CurrentHexId);
        Assert.AreEqual(hex3.Id, army.DestinationHexId);
    }
    [TestMethod]
    public void ArmyDoesNotMoveWithoutOrder()
    {
        // Arrange
        var world = new WorldState();

        var hex1 = new Hex(1, 0, 0);
        var hex2 = new Hex(2, 1, 0);

        world.Map.AddHex(hex1);
        world.Map.AddHex(hex2);

        var army = new Army(1, 1, 1, hex1.Id);
        world.Armies.Add(army);

        var engine = new SimulationEngine(world);
        engine.RegisterSystem(new MovementSystem());

        // Act
        engine.Step();

        // Assert
        Assert.AreEqual(hex1.Id, army.CurrentHexId);
    }
    [TestMethod]
    public void ArmyStopsAtDestination()
    {
        // Arrange
        var world = new WorldState();

        var hex1 = new Hex(1, 0, 0);
        var hex2 = new Hex(2, 1, 0);

        world.Map.AddHex(hex1);
        world.Map.AddHex(hex2);

        var army = new Army(1, 1, 1, hex1.Id);
        world.Armies.Add(army);

        army.OrderMove(hex2.Id);

        var engine = new SimulationEngine(world);
        engine.RegisterSystem(new MovementSystem());


        // Act 1回目
        engine.Step();


        // Assert 到着確認
        Assert.AreEqual(hex2.Id, army.CurrentHexId);
        Assert.IsNull(army.DestinationHexId);


        // Act 2回目
        engine.Step();


        // Assert その場にいる
        Assert.AreEqual(hex2.Id, army.CurrentHexId);
    }
    [TestMethod]
    public void MultipleArmiesMove()
    {
        // Arrange
        var world = new WorldState();

        var hex1 = new Hex(1, 0, 0);
        var hex2 = new Hex(2, 1, 0);
        var hex3 = new Hex(3, 2, 0);
        var hex4 = new Hex(4, 3, 0);

        world.Map.AddHex(hex1);
        world.Map.AddHex(hex2);
        world.Map.AddHex(hex3);
        world.Map.AddHex(hex4);

        var armyA = new Army(1, 1, 1, hex1.Id);
        var armyB = new Army(2, 2, 2, hex3.Id);

        world.Armies.Add(armyA);
        world.Armies.Add(armyB);

        armyA.OrderMove(hex2.Id);
        armyB.OrderMove(hex4.Id);

        var engine = new SimulationEngine(world);
        engine.RegisterSystem(new MovementSystem());


        // Act
        engine.Step();


        // Assert
        Assert.AreEqual(hex2.Id, armyA.CurrentHexId);
        Assert.AreEqual(hex4.Id, armyB.CurrentHexId);
    }
    [TestMethod]
    public void ArmyCannotMoveIntoMountain()
    {
        var world = new WorldState();

        var plain = new Hex(
            1,
            0,
            0,
            TerrainType.Plain);

        var mountain = new Hex(
            2,
            1,
            0,
            TerrainType.Mountain);

        world.Map.AddHex(plain);
        world.Map.AddHex(mountain);

        var army = new Army(
            1,
            1,
            1,
            plain.Id);

        world.Armies.Add(army);

        army.OrderMove(mountain.Id);

        var engine = new SimulationEngine(world);
        engine.RegisterSystem(new MovementSystem());

        engine.Step();

        Assert.AreEqual(
            plain.Id,
            army.CurrentHexId);
    }

    [TestMethod]
    public void ForestEntryCostsCooldown()
    {
        // Plain -> Forest -> Plain の3Hex
        var world = new WorldState();
        var plain1  = new Hex(1, 0, 0, TerrainType.Plain);
        var forest  = new Hex(2, 1, 0, TerrainType.Forest);
        var plain2  = new Hex(3, 2, 0, TerrainType.Plain);
        world.Map.AddHex(plain1);
        world.Map.AddHex(forest);
        world.Map.AddHex(plain2);

        var army = new Army(1, 1, 1, plain1.Id);
        world.Armies.Add(army);
        army.OrderMove(plain2.Id);

        var engine = new SimulationEngine(world);
        engine.RegisterSystem(new MovementSystem());

        // Tick1: plain1 -> forest（クールダウン1セット）
        engine.Step();
        Assert.AreEqual(forest.Id, army.CurrentHexId);
        Assert.AreEqual(1, army.MoveCooldown);

        // Tick2: クールダウン消費、forest に留まる
        engine.Step();
        Assert.AreEqual(forest.Id, army.CurrentHexId);
        Assert.AreEqual(0, army.MoveCooldown);

        // Tick3: forest -> plain2
        engine.Step();
        Assert.AreEqual(plain2.Id, army.CurrentHexId);
    }

    [TestMethod]
    public void PlainMovementNoCooldown()
    {
        var world = new WorldState();
        var hex1 = new Hex(1, 0, 0, TerrainType.Plain);
        var hex2 = new Hex(2, 1, 0, TerrainType.Plain);
        world.Map.AddHex(hex1);
        world.Map.AddHex(hex2);

        var army = new Army(1, 1, 1, hex1.Id);
        world.Armies.Add(army);
        army.OrderMove(hex2.Id);

        var engine = new SimulationEngine(world);
        engine.RegisterSystem(new MovementSystem());

        engine.Step();
        Assert.AreEqual(hex2.Id, army.CurrentHexId);
        Assert.AreEqual(0, army.MoveCooldown);
    }
}