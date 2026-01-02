using System.Linq;
using CivOne.Enums;
using CivOne.Tiles;
using CivOne.Units;
using Xunit;

namespace CivOne.UnitTests
{
    public class ZOCTests : TestsBase2
    {
        // TODO game created in base is NOT always consistent in players!  WHY???

        private bool TestZoc(bool defendedCity)
        {
            /* Establish the scenario described in Issue #93:
              | enemy |      |
              | city  |      |
              --------+------+-------
              |       |      | enemy
              |       |      |  unit
              +-------+------+-------
              |       |player|  
              |       | unit |
              The player unit wants to try to move up & left.

              This is OK if the enemy city is undefended.
              This is not OK if the enemy city is defended.
            */
            // find another player
            var otherP = Game.Instance.Players.First(p => p.Civilization.Name != "Chinese");

            // give other player a city
            Game.Instance.AddCity(otherP, 3, 52, 14);
            Assert.Equal(true, Map.Instance[52,14].HasCity);

            // set up other player units
            if (defendedCity)
                Game.Instance.CreateUnit(UnitType.Militia, 52, 14, Game.Instance.PlayerNumber(otherP));
            Game.Instance.CreateUnit(UnitType.Militia,54,15, Game.Instance.PlayerNumber(otherP));

            // give human a unit
            var chariot = Game.Instance.CreateUnit(UnitType.Chariot, 53, 16, Game.Instance.PlayerNumber(playa));

            // try to move the human unit up and left
            var gm = new Screens.GamePlayPanels.GameMap();
            Game.Instance._currentPlayer = Game.Instance.PlayerNumber(playa);
            Game.Instance.ActiveUnit = chariot;
            return gm.MoveTo(-1, -1);
        }

        [Fact]
        public void ZOCFixTest1()
        {
            // Test bugfix for issue #93. SWY's code failed this.

            Assert.False(TestZoc(true));
        }

        [Fact]
        public void ZOCFixTest2()
        {
            // Test bugfix for issue #93: insure the opposite case isn't broken

            Assert.True(TestZoc(false));
        }

        [Fact]
        public void ZOKRevertTest1()
        {
            // Fixing Issue #93, I reverted the situation where the player's unit should be
            // able to move to a space with it's own unit

            // find another player
            var otherP = Game.Instance.Players.First(p => p.Civilization.Name != "Chinese");

            // give other player a city
            Game.Instance.AddCity(otherP, 3, 52, 14);
            Assert.Equal(true, Map.Instance[52,14].HasCity);

            // set up other player unit in city
            Game.Instance.CreateUnit(UnitType.Militia, 52, 14, Game.Instance.PlayerNumber(otherP));

            // give human two units
            var chariot1 = Game.Instance.CreateUnit(UnitType.Chariot, 53, 14, Game.Instance.PlayerNumber(playa));
            var chariot2 = Game.Instance.CreateUnit(UnitType.Chariot, 52, 15, Game.Instance.PlayerNumber(playa));

            // try to move the human unit down and left to own unit
            var gm = new Screens.GamePlayPanels.GameMap();
            Game.Instance._currentPlayer = Game.Instance.PlayerNumber(playa);
            Game.Instance.ActiveUnit = chariot1;
            Assert.True(gm.MoveTo(-1, +1));
        }
        [Fact]
        public void ZOKRevertTest2()
        {
            // Confirm that the player's unit should be able to move to a space with it's own city,
            // even when "blocked" by enemy unit: player unit should be able to move down one space below:
            /*
              | enemy |player|
              | city  | unit |
              --------+------+-------
              |       |player| 
              |       | city | 
              +-------+------+-------
              |       |      |  
              |       |      |
             */

            // find another player
            var otherP = Game.Instance.Players.First(p => p.Civilization.Name != "Chinese");

            // give other player a unit
            Game.Instance.CreateUnit(UnitType.Militia, 51, 13, Game.Instance.PlayerNumber(otherP));

            // give human a city
            Game.Instance.AddCity(playa, 3, 52, 14);
            Assert.Equal(true, Map.Instance[52,14].HasCity);

            // give human a unit
            var chariot1 = Game.Instance.CreateUnit(UnitType.Chariot, 52, 13, Game.Instance.PlayerNumber(playa));

            // try to move the human unit down to own city
            var gm = new Screens.GamePlayPanels.GameMap();
            Game.Instance._currentPlayer = Game.Instance.PlayerNumber(playa);
            Game.Instance.ActiveUnit = chariot1;
            Assert.True(gm.MoveTo(0, +1));
        }
        [Fact]
        public void ZOKRevertTest3()
        {
            // find another player
            var otherP = Game.Instance.Players.First(p => p.Civilization.Name != "Chinese");

            // give other player a city [undefended]
            Game.Instance.AddCity(otherP, 3, 52, 14);
            Assert.Equal(true, Map.Instance[52,14].HasCity);

            // give other player a unit
            Game.Instance.CreateUnit(UnitType.Militia, 51, 13, Game.Instance.PlayerNumber(otherP));

            // give human a unit
            var chariot1 = Game.Instance.CreateUnit(UnitType.Chariot, 52, 13, Game.Instance.PlayerNumber(playa));

            // try to move the human unit down to enemy city
            Game.Instance._currentPlayer = Game.Instance.PlayerNumber(playa);
            Game.Instance.ActiveUnit = chariot1;
            Assert.True(((BaseUnit)chariot1).CanMoveTo(0, +1));
        }

        /// <summary>
        /// Common setup for MarineAttackTests.
        /// </summary>
        /// <returns>the chariot onboard a ship</returns>
        private IUnit SetupMarineAttackTest()
        {
            // Issue #116: in MicroproseCiv, can move from ship to any unoccupied land space. Broken after 
            // changes for issue #93 [see above]

            // Using seed of 7595, Earth, Chinese, the initial city is
            // like so:
            //  P P O  - P: plains         P 1 O  - ship at location 2
            //  P C O  - O: ocean          P C 2  - any unit on ship should be able
            //  M P P  - M: Mtns, C: city  M 3 4  - to move to locations 1, 3, 4

            // Establish initial city
            var unit = Game.Instance.GetUnits().First(x => playa == x.Owner);
            City acity = Game.Instance.AddCity(playa, 1, unit.X, unit.Y);


            // Confirm it was set up properly
            ITile tile = Map.Instance[unit.X, unit.Y];
            Assert.Equal(true, tile.HasCity);
            Assert.True(tile is Grassland); // NOTE: if the tile is Ocean, likely failed to load MAP.PIC

            // find another player
            var otherP = Game.Instance.Players.First(p => p.Civilization.Name != "Chinese");
            var enemyShip = Game.Instance.CreateUnit(UnitType.Trireme, unit.X + 1, unit.Y, Game.Instance.PlayerNumber(otherP));
            var enemyChariot = Game.Instance.CreateUnit(UnitType.Chariot, unit.X + 1, unit.Y, Game.Instance.PlayerNumber(otherP));

            Game.Instance._currentPlayer = Game.Instance.PlayerNumber(otherP);
            Game.Instance.ActiveUnit = enemyChariot;

            return enemyChariot;
        }

        [Fact]
        public void MarineAttackTest1()
        {
            // Test a shipborne unit can land on an location next to an enemy city
            // Location '1', see setup routine above
            BaseUnit enemyChariot = (BaseUnit)SetupMarineAttackTest();
            Assert.True(enemyChariot.CanMoveTo(-1, -1));
        }

        [Fact]
        public void MarineAttackTest3()
        {
            // Test a shipborne unit can land on an location next to an enemy city
            // Location '3', see setup routine above
            BaseUnit enemyChariot = (BaseUnit)SetupMarineAttackTest();
            Assert.True(enemyChariot.CanMoveTo(-1, +1));
        }
        [Fact]
        public void MarineAttackTest4()
        {
            // Test a shipborne unit can land on an location next to an enemy city
            // Location '4', see setup routine above
            BaseUnit enemyChariot = (BaseUnit)SetupMarineAttackTest();
            Assert.True(enemyChariot.CanMoveTo(0, +1));
        }
        [Fact]
        public void MarineAttackTestF1()
        {
            // Exercise the opposite case: shipboard unit cannot goto city because it is defended
            BaseUnit enemyChariot = (BaseUnit)SetupMarineAttackTest();
            Assert.False(enemyChariot.CanMoveTo(-1, 0));
        }
    }
}