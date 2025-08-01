// CivOne
//
// To the extent possible under law, the person who associated CC0 with
// CivOne has waived all copyright and related or neighboring rights
// to CivOne.
//
// You should have received a copy of the CC0 legalcode along with this
// work. If not, see <http://creativecommons.org/publicdomain/zero/1.0/>.

using System;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using CivOne.Advances;
using CivOne.Buildings;
using CivOne.Civilizations;
using CivOne.Enums;
using CivOne.Graphics;
using CivOne.IO;
using CivOne.Screens;
using CivOne.Screens.Dialogs;
using CivOne.Tasks;
using CivOne.Tiles;
using CivOne.UserInterface;
using CivOne.Wonders;

namespace CivOne.Units
{
	internal abstract class BaseUnit : BaseInstance, IUnit
	{
		protected int _x, _y;

		protected Order _order;
		public Order order
		{
			get
			{
				return _order;
			}
			set
			{
				_order = value;
				MovesSkip = 0;
				MovesLeft = Move;
				PartMoves = 0;
			}
		}

		/// <summary>
		/// A unit is busy if it has no moves left OR is sentry/fortify/has-orders.
		/// </summary>
		public virtual bool Busy
		{
			get
			{
				return (MovesLeft <= 0 && PartMoves <= 0) || Sentry || Fortify || _order != Order.None;
			}
			set
			{
				Sentry = false;
				Fortify = false;
				FortifyActive = false;
				MovesSkip = 0;
			}
		}

		public virtual bool HasAction
		{
			get
			{
				return order != Order.None || Sentry || Fortify || FortifyActive || !Goto.IsEmpty;
			}
		}
		public virtual bool HasMovesLeft
		{
			get
			{
				return MovesLeft > 0 || PartMoves > 0;
			}
		}

		public bool Veteran { get; set; }
		public bool FortifyActive { get; private set; }
		private bool _fortify = false;
		public bool Fortify
		{
			get
			{
				return (_fortify || FortifyActive);
			}
			set
			{
				if (Class != UnitClass.Land) return;
				if (this is Settlers) return;
				if (!value)
					_fortify = false;
				else if (Fortify)
					return;
				else
					FortifyActive = true;
			}
		}

		private bool _sentry;
		public bool Sentry
		{
			get
			{
				return _sentry;
			}
			set
			{
				if (_sentry == value) return;
				_sentry = value;
				if (!_sentry || !Game.Started) return;
				SkipTurn();
			}
		}
		protected void SentryWithoutSkipTurn()
		{
			if (Sentry) return;
			_sentry = true;
		}

		public virtual void SentryOnShip()
		{
			SentryWithoutSkipTurn();
		}

		public bool Moving => Movement != null;
		public MoveUnit Movement { get; protected set; }

		private int AttackStrength(IUnit defendUnit)
		{
			// Step 1: Determine the nominal attack value of the attacking unit and multiply it by 8.
			int attackStrength = ((int)Attack * 8);

			if (Owner == 0)
			{
				// Step 2: If the attacking unit is a Barbarian unit and the defending unit is player-controlled, multiply the attack strength by the Difficulty Modifier, then divide it by 4.
				if (Human == defendUnit.Owner)
				{
					attackStrength *= (Game.Difficulty + 1);
					attackStrength /= 4;
				}

				// Step 3: If the attacking unit is a Barbarian unit and the defensing unit is AI-controlled, divide the attack strength by 2.
				if (Human != defendUnit.Owner)
				{
					attackStrength /= 2;
				}

				// Step 4: If the attacking unit is a Barbarian unit and the defending unit is inside a city and the defending civilization does not control any other cities, set the attack strength to zero.
				// This actually makes the defending unit invincible in this special case. Might well save you from being obliterated by that unlucky hut at 3600BC.
				if (defendUnit.Tile.City != null && Game.GetPlayer(defendUnit.Owner).Cities.Length == 1)
				{
					attackStrength = 0;
				}

				// Step 5: If the attacking unit is a Barbarian unit and the defending unit is inside a city with a Palace, divide the attack strength by 2.
				if (defendUnit.Tile.City != null && defendUnit.Tile.City.HasBuilding<Palace>())
				{
					attackStrength /= 2;
				}
			}

			// Step 6: If the attacking unit is a veteran unit, increase the attack strength by 50%.
			if (Veteran)
			{
				attackStrength += (attackStrength / 2);
			}

			// Step 7: If the attacking unit has only 0.2 movement points left, multiply the attack strength by 2, then divide it by 3. If the attacking unit has only 0.1 movement points left, then just divide by 3 instead.
			if (MovesLeft == 0)
			{
				attackStrength *= PartMoves;
				attackStrength /= 3;
			}

			// Step 8: If the attacking unit is a Barbarian unit and the defending unit is player-controlled, check the difficulty level. On Chieftain and Warlord levels, divide the attack strength by 2.
			if (Owner == 0 && Human == defendUnit.Owner)
			{
				if (Game.Difficulty < 2)
				{
					attackStrength /= 2;
				}
			}

			// Step 9: If the attacking unit is player-controlled, check the difficulty level. On Chieftain level, multiply the attack strength by 2.
			// So on Chieftain difficulty, it is often better to attack than be attacked, even with a defensive unit.
			if (Human == Owner && Game.Difficulty == 0)
			{
				attackStrength *= 2;
			}

			return attackStrength;
		}

		private int DefendStrength(IUnit defendUnit, IUnit attackUnit)
		{
			// Check City Walls for step 5
			bool cityWalls = (defendUnit.Tile.City != null && defendUnit.Tile.City.HasBuilding<CityWalls>());

			// Step 1: Determine the nominal defense value of defending unit.
			int defendStrength = (int)defendUnit.Defense;

			if (defendUnit.Class == UnitClass.Land || (defendUnit.Class == UnitClass.Water && cityWalls && attackUnit.Attack != 12))
			{
				int fortificationModifier = 4;
				if (defendUnit.Tile.Fortress)
					fortificationModifier = 8;
				else if (defendUnit.Fortify || defendUnit.FortifyActive)
					fortificationModifier = 6;

				// Step 2: If the defending unit is a ground unit, multiply the defense strength by the Terrain Modifier.
				// This modifier effectively includes a factor of 2.
				defendStrength *= defendUnit.Tile.Defense;

				if (!cityWalls || attackUnit.Attack == 12)
				{
					// Step 3: If the defending unit is a ground unit, multiply the defense strength by the Fortification Modifier.
					// This modifier effectively includes a factor of 4, resulting in a combined factor of 8.
					defendStrength *= fortificationModifier;
				}
			}

			// Step 4: If the defending unit is a sea or air unit, multiply the defense strength by 8.
			// This effectively treats the Terrain Modifier as 2, regardless of the actual terrain type. It also means that these units will never benefit from the Fortification Modifier.
			if (defendUnit.Class != UnitClass.Land && (!cityWalls || attackUnit.Attack == 12))
			{
				defendStrength *= 8;
			}

			// Step 5: If the defending unit is inside a city with City Walls and the nominal attack value of the attacking unit is NOT equal to 12, check the domain of the defending unit. If the domain is NOT air, re-calculate steps 1 and 2 (ignore steps 3 and 4) and multiply the result by 12.
			// When determining if the attacking unit ignores City Walls, the game just checks for attack value, not unit type. So if you change any unit's attack rating to 12, the game will have it ignore City Walls as well.
			if (cityWalls && attackUnit.Attack != 12)
			{
				defendStrength *= 12;
			}

			// Step 6: If the defending unit is a veteran unit, increase the defense strength by 50%.
			if (defendUnit.Veteran)
			{
				defendStrength += (defendStrength / 2);
			}

			return defendStrength;
		}

		private bool AttackOutcome(IUnit attackUnit, ITile defendTile)
		{
			IUnit defendUnit = defendTile.Units.OrderByDescending(x => x.Attack * (x.Veteran ? 1.5 : 1)).ThenBy(x => (int)x.Type).First();

			int attackStrength = AttackStrength(defendUnit);
			int defenseStrength = DefendStrength(defendUnit, attackUnit);
			int randomAttack = Common.Random.Next(attackStrength);
			int randomDefense = Common.Random.Next(defenseStrength);
			bool win = (randomAttack > randomDefense);
			if (win && attackUnit.Owner == 0 && defendUnit.Tile.City != null)
			{
				 // If the attacking unit is a Barbarian unit and the defending unit is inside a city, then, if the attacking unit won, the procedure will be repeated once
				 // This time, the attacking unit wins on a tie.
				randomAttack = Common.Random.Next(attackStrength);
				randomDefense = Common.Random.Next(defenseStrength);
				win = (randomAttack >= randomDefense);
			}

			// 50% chance to award veteran status to the winner
			if (Common.Random.Next(100) < 50)
			{
				if (win && !attackUnit.Veteran) attackUnit.Veteran = true;
				if (!win && !defendUnit.Veteran) defendUnit.Veteran = true;
			}

			return win;
		}

		internal virtual bool Confront(int relX, int relY)
		{
            Goto = Point.Empty;             // Cancel any goto mode when Confronting

			if (Class == UnitClass.Land && (this is Diplomat || this is Caravan))
			{
				// TODO fire-eggs this should never happen? Diplomat/Caravan have overloads
				return false;
			}

			Movement = new MoveUnit(relX, relY);

			ITile moveTarget = Map[X, Y][relX, relY];
			if (moveTarget == null) return false;
			if (moveTarget.Units.Length == 0 && moveTarget.City != null && moveTarget.City.Owner != Owner)
			{
				if (Class != UnitClass.Land) // can't occupy city with sea/air unit
				{
					GameTask.Enqueue(Message.Error("-- Civilization Note --", TextFile.Instance.GetGameText($"ERROR/OCCUPY")));
					Movement = null;
					return false;
				}

				City capturedCity = moveTarget.City;
				Movement.Done += (s, a) =>
				{
					Action changeOwner = delegate()
					{
						Player previousOwner = Game.GetPlayer(capturedCity.Owner);

						if (capturedCity.HasBuilding<Palace>())
							capturedCity.RemoveBuilding<Palace>();
						capturedCity.Food = 0;
						capturedCity.Shields = 0;
						while (capturedCity.Units.Length > 0)
							Game.DisbandUnit(capturedCity.Units[0]);
						capturedCity.Owner = Owner;

						if (!capturedCity.HasBuilding<CityWalls>())
						{
							capturedCity.Size--;
						}

						previousOwner.IsDestroyed();
					};

					IList<IAdvance> advancesToSteal = GetAdvancesToSteal(capturedCity.Player);

                    if (Human == capturedCity.Owner || Human == Owner)
					{
						Player cityOwner = Game.GetPlayer(capturedCity.Owner);
						float totalSize = cityOwner.Cities.Sum(x => x.Size);
						int totalGold = cityOwner.Gold;

						// Issue #56 : Gold formula from CivFanatics
						int captureGold = (int)(totalGold * ((float)capturedCity.Size / totalSize));

                        Game.GetPlayer(capturedCity.Owner).Gold -= (short)captureGold;
                        Game.CurrentPlayer.Gold += (short)captureGold;

                        string[] lines = { $"{Game.CurrentPlayer.TribeNamePlural} capture",
                                           $"{capturedCity.Name}. {captureGold} gold",
                                           "pieces plundered." };

                        EventHandler doneCapture = (s1,a1) =>
                        {
                            changeOwner();

                            if (capturedCity.Size == 0 || Human != Owner) return;
                            GameTask.Insert(Show.CityManager(capturedCity));

							// fire-eggs 20190628 no 'select advance' dialog when non-human is doing the capturing!
							if (advancesToSteal.Any() && Human == Owner)
								GameTask.Enqueue(Show.SelectAdvanceAfterCityCapture(Player, advancesToSteal));
						};

                        if (Game.Animations)
                        {
                            Show captureCity = Show.CaptureCity(capturedCity, lines);
                            captureCity.Done += doneCapture;
                            GameTask.Insert(captureCity);
                        }
                        else
                        {
                            IScreen captureNews = new Newspaper(capturedCity, lines, false);
                            captureNews.Closed += doneCapture;
                            Common.AddScreen(captureNews);
                        }
					}
					else
					{
						changeOwner();
						if (advancesToSteal.Any())
							Player.AddAdvance(advancesToSteal.First());
					}
					MoveEnd(s, a);
				};
			}
			else if (this is Nuclear)
			{
				int xx = (X - Common.GamePlay.X + relX) * 16;
				int yy = (Y - Common.GamePlay.Y + relY) * 16;
				Show nuke = Show.Nuke(xx, yy);

				if (Map[X, Y][relX, relY].City != null)
					PlaySound("airnuke");
				else
					PlaySound("s_nuke");

				nuke.Done += (s, a) =>
				{
					foreach (ITile tile in Map.QueryMapPart(X + relX - 1, Y + relY - 1, 3, 3))
					{
						while (tile.Units.Length > 0)
						{
							Game.DisbandUnit(tile.Units[0]);
						}
					}
				};
				GameTask.Enqueue(nuke);
			}
			else if (AttackOutcome(this, Map[X, Y][relX, relY]))
			{
				Movement.Done += (s, a) =>
				{
					if (this is Cannon)
					{
						PlaySound("cannon");
					}
					else if (this is Musketeers || this is Riflemen || this is Armor || this is Artillery || this is MechInf)
					{
						PlaySound("s_land");
					}
					else
					{
						PlaySound("they_die");
					}

					// fire-eggs 20190702 capture barbarian leader, get ransom
					IUnit[] attackedUnits = moveTarget.Units;

					CaptureBarbarianLeader(attackedUnits);

					IUnit unit = attackedUnits.FirstOrDefault();

					DestroyAttackedUnit(unit);

					if (MovesLeft == 0)
					{
						PartMoves = 0;
					}
					else if (MovesLeft > 0)
					{
						if (this is Bomber)
						{
							SkipTurn();
						}
						else
						{
							MovesLeft--;
						}
					}
					Movement = null;
					if (Map[X, Y][relX, relY].City != null)
					{
						if (!Map[X, Y][relX, relY].City.HasBuilding<CityWalls>())
						{
							Map[X, Y][relX, relY].City.Size--;
						}
					}
				};
			}
			else
			{
				Movement.Done += (s, a) =>
				{
					if (this is Cannon)
					{
						PlaySound("cannon");
					}
					else if (this is Musketeers || this is Riflemen || this is Armor || this is Artillery || this is MechInf)
					{
						PlaySound("s_land");
					}
					else
					{
						PlaySound("we_die");
					}
					GameTask.Insert(Show.DestroyUnit(this, false));
					Movement = null;
				};
			}
			GameTask.Insert(Movement);
			return false;
		}

		private static void DestroyAttackedUnit(IUnit unit)
		{
			if (unit != null)
			{
				var task = Show.DestroyUnit(unit, true);

				// fire-eggs 20190729 when destroying last city, check for civ destruction ASAP
				if (unit.Owner != 0)
					task.Done += (s1, a1) => { Game.GetPlayer(unit.Owner).IsDestroyed(); };

				GameTask.Insert(task);
			}
		}

		private void CaptureBarbarianLeader(IUnit[] attackedUnits)
		{
			bool isAirUnit = this.Class == UnitClass.Air;
			if (isAirUnit)
			{
				// CW: air units cannot capture barbarian leader
				return;
			}

			// CW: only a single barbarian diplomat can be captured and receive ransom
				bool isBarbarianLeader = attackedUnits.Length == 1 && attackedUnits.First().Owner == Barbarian.Owner && attackedUnits.First() is Diplomat;

			if (!isBarbarianLeader)
			{
				return;
			}

			Player.Gold += 100;

			if (Human == Player)
			{
				Common.AddScreen(new MessageBox("Barbarian leader captured!", "100$ ransom paid."));
			}
		}

		private IList<IAdvance> GetAdvancesToSteal(Player victim)
		{
			return victim.Advances
			.Where(p => Player.Advances.All(p2 => p2.Id != p.Id))
			.OrderBy(a => Common.Random.Next(0, 1000))
			.Take(3)
			.ToList();
		}

        public bool CanMoveTo(int relX, int relY)
        {
			// TODO only referenced by land units, move to BaseUnitLand?

            // Issue #93: fix problems with zone-of-control.

            // refactored out for unit testability
            ITile moveTarget = Map[X, Y][relX, relY];
            if (moveTarget == null) return false;

			// Issue #116: allow ship-borne units to move to any unoccupied tile
			if (Map[X,Y].IsOcean)
            {
				return !moveTarget.Units.Any(u => u.Owner != Owner);
            }

            var thisUnits = Map[X, Y].GetBorderTiles().SelectMany(t => t.Units);
            var destUnits = moveTarget.GetBorderTiles().SelectMany(t => t.Units);

            // Any enemy units around my position OR the target position?
            bool thisBlocked = thisUnits.Any(u => u.Owner != Owner);
            bool destBlocked = destUnits.Any(u => u.Owner != Owner);
            bool destOK = moveTarget.Units.Any(u => u.Owner == Owner) || moveTarget.HasCity;

            // Cannot move from a square adjacent to enemy unit to a square adjacent to enemy unit
            // but _can_ move to square occupied by own units or to any undefended city square
            return destOK || !thisBlocked || !destBlocked;
        }

		public virtual bool MoveTo(int relX, int relY)
		{
			if (Movement != null) return false;

			ITile moveTarget = Map[X, Y][relX, relY];
			if (moveTarget == null) return false;

			// if (!MoveTargets.Any(t => t.X == moveTarget.X && t.Y == moveTarget.Y))
			if (!MoveTargets.Any(t => t.SameLocationAs(moveTarget)))
			{
				// Target tile is invalid
				// TODO: For some tiles, display a message detailing why the move is illegal
				return false;
			}

			bool? attacked = ConfrontCity(moveTarget, relX, relY);
			if (attacked.HasValue)
			{
				return attacked.Value;
			}

			if (HasEnemyOnTarget(moveTarget))
			{
				return ConfrontEnemy(moveTarget, relX, relY);
			}

			// if (!MoveTargets.Any(t => t.X == moveTarget.X && t.Y == moveTarget.Y))
			if (!MoveTargets.Any(t => t.SameLocationAs(moveTarget)))
			{
				// Target tile is invalid
				// TODO: For some tiles, display a message detailing why the move is illegal
				return false;
			}

			MovementTo(relX, relY);
			return true;
		}

		protected virtual bool? ConfrontCity(ITile moveTarget, int relX, int relY)
		{
			// fire-eggs Caravan needs to be able to move into owner city
			bool hasTargetCity = moveTarget.City != null;
			bool belongsTargetCityOwner = moveTarget.City?.Owner == Owner;

			// if (moveTarget.City != null && (moveTarget.City.Owner != Owner || this is Caravan))
			if (hasTargetCity && !belongsTargetCityOwner)
			{
				return Confront(relX, relY);
			}

			return null;
		}

		protected bool HasEnemyOnTarget(ITile moveTarget)
		{
			return moveTarget.Units.Any(u => u.Owner != Owner);
		}


		protected virtual bool ConfrontEnemy(ITile moveTarget, int relX, int relY)
		{
			Goto = Point.Empty;             // Cancel any goto mode

			if (!CanAttackEnemy(moveTarget))
			{
				// Units cannot attack air units.
				// Exception in Fighter.CanAttackEnemy().
				return false;
			}

			// Issue #84 : failure to prompt on low-strength attack
			if (Human == Owner && MovesLeft == 0 && PartMoves > 0)
			{
				GameTask.Enqueue(Show.WeakAttack(this, relX, relY));
				return true;
			}

			return Confront(relX, relY);
		}

		protected virtual bool CanAttackEnemy(ITile moveTarget)
		{
			// Cannot attack not air units. Only if on a Carrier.
			return moveTarget.Units.Any(u => u.Class != UnitClass.Air) ||
				moveTarget.Units.Any(u => u is not BaseUnitAir);
		}

		private void MoveEnd(object sender, EventArgs args)
		{
			ITile previousTile = Map[_x, _y];
			X += Movement.RelX;
			Y += Movement.RelY;
			if (X == Goto.X && Y == Goto.Y)
			{
				Goto = Point.Empty;
			}
			Movement = null;

			Explore();
			MovementDone(previousTile);
		}

		protected void MovementTo(int relX, int relY)
		{
			MovementStart(Tile);
			Movement = new MoveUnit(relX, relY);
			Movement.Done += MoveEnd;
			GameTask.Insert(Movement);
		}

		protected virtual void MovementStart(ITile previousTile)
		{
		}

		protected virtual void MovementDone(ITile previousTile)
		{
			if (MovesLeft > 0) MovesLeft--;

			Tile.Visit(Owner);

			if (Tile.Hut)
			{
				Tile.Hut = false;
			}
		}

		private static readonly IBitmap[] _iconCache = new IBitmap[28];
		public virtual IBitmap Icon { get; private set; }
		private string _name;
		public string Name
		{
			get => Modifications.LastOrDefault(x => x.Name.HasValue)?.Name.Value ?? _name;
			protected set => _name = value;
		}
		public byte PageCount => 2;
		public Picture DrawPage(byte pageNumber)
		{
			string[] text = new string[0];
			switch (pageNumber)
			{
				case 1:
					text = Resources.GetCivilopediaText("BLURB2/" + _name.ToUpper());
					break;
				case 2:
					text = Resources.GetCivilopediaText("BLURB2/" + _name.ToUpper() + "2");
					break;
				default:
					Log("Invalid page number: {0}", pageNumber);
					break;
			}

			Picture output = new Picture(320, 200);

			output.AddLayer(this.ToBitmap(1), 215, 47);

			int yy = 76;
			foreach (string line in text)
			{
				Log(line);
				output.DrawText(line, 6, 1, 12, yy);
				yy += 9;
			}

			if (pageNumber == 2)
			{
				yy += 8;
				string requiredTech = "";
				if (RequiredTech != null) requiredTech = RequiredTech.Name;
				output.DrawText(string.Format("Requires {0}", requiredTech), 6, 9, 100, yy); yy += 8;
				output.DrawText(string.Format("Cost: {0}0 resources.", Price), 6, 9, 100, yy); yy += 8;
				output.DrawText(string.Format("Attack Strength: {0}", Attack), 6, 12, 100, yy); yy += 8;
				output.DrawText(string.Format("Defense Strength: {0}", Defense), 6, 12, 100, yy); yy += 8;
				output.DrawText(string.Format("Movement Rate: {0}", Move), 6, 5, 100, yy);
			}

			return output;
		}

		private IAdvance _requiredTech;
		public IAdvance RequiredTech
		{
			get => Modifications.LastOrDefault(x => x.Requires.HasValue)?.Requires.Value.ToInstance() ?? _requiredTech;
			protected set => _requiredTech = value;
		}

		public IWonder RequiredWonder { get; protected set; }

		private IAdvance _obsoleteTech;
		public IAdvance ObsoleteTech
		{
			get => Modifications.LastOrDefault(x => x.Obsolete.HasValue)?.Obsolete.Value.ToInstance() ?? _obsoleteTech;
			protected set => _obsoleteTech = value;
		}

		public UnitClass Class { get; protected set; }
		public UnitType Type { get; protected set; }
		public City Home { get; private set; }

        private short _buyPrice;
		public short BuyPrice
		{
			get => Modifications.LastOrDefault(x => x.BuyPrice.HasValue)?.BuyPrice.Value ?? _buyPrice;
			private set => _buyPrice = value;
		}

		public byte ProductionId => (byte)Type;

		private byte _price;
		public byte Price
		{
			get => Modifications.LastOrDefault(x => x.Price.HasValue)?.Price.Value ?? _price;
			protected set => _price = value;
		}

		public UnitRole Role { get; internal set; }

		private byte _attack;
		public byte Attack
		{
			get => Modifications.LastOrDefault(x => x.Attack.HasValue)?.Attack.Value ?? _attack;
			protected set => _attack = value;
		}

		private byte _defense;
		public byte Defense
		{
			get => Modifications.LastOrDefault(x => x.Defense.HasValue)?.Defense.Value ?? _defense;
			protected set => _defense = value;
		}

		private byte _move;
		public byte Move
		{
			get => Modifications.LastOrDefault(x => x.Moves.HasValue)?.Moves.Value ?? _move;
			protected set => _move = value;
		}

		public int X
		{
			get
			{
				return _x;
			}
			set
			{
				int val = value;
				while (val < 0) val += Map.WIDTH;
				while (val >= Map.WIDTH) val -= Map.WIDTH;
				if (_x == -1 && _y != -1) Explore();
				_x = val;
			}
		}
		public int Y
		{
			get
			{
				return _y;
			}
			set
			{
				if (value < 0 || value >= Map.HEIGHT) return;
				if (_y == -1 && _x != -1 && value != -1) Explore();
				_y = value;
			}
		}

		public Point Goto { get; set; }

		public ITile Tile => Map[_x, _y];

		/// <summary>
		/// Identical to Tile, but used to start with better naming.
		/// </summary>
		public ITile Location => Map[_x, _y];

		private byte _owner;
		public byte Owner
		{
			get => _owner;
			set
			{
				_owner = value;
				if (Game.Started) Tile.Visit(_owner);
			}
		}

		public Player Player => Game.GetPlayer(Owner);

		public byte Status
		{
			set
			{
				bool[] bits = new bool[8];
				for (int i = 0; i < 8; i++)
					bits[i] = (((value >> i) & 1) > 0);
				if (bits[0]) Sentry = true;
				else if (bits[2]) FortifyActive = true;
				else if (bits[3]) _fortify = true;
				
				if (this is Settlers)
				{
					(this as Settlers).SetStatus(bits);
				}

				Veteran = bits[5];
			}
		}
		public byte MovesLeft { get; set; }
		public int MovesSkip { get; set; }
		public byte PartMoves { get; set; }
		
		public virtual void NewTurn()
		{
			if (FortifyActive)
			{
				FortifyActive = false;
				_fortify = true;
			}
			if (MovesSkip > 0)
			{
				--MovesSkip;
				SkipTurn();
			}

			MovesLeft = Move;
            PartMoves = 0;
			Explore();
		}

		public void SetHome()
		{
			if (Map[X, Y].City == null) return;
			Home = Map[X, Y].City;
		}

		public void SetHome(City city) => Home = city;

		public void Pillage()
		{
			if (!(Tile.Irrigation || Tile.Mine || Tile.Road || Tile.RailRoad))
				return;
			
			if (Tile.Irrigation)
				Tile.Irrigation = false;
			else if (Tile.Mine)
				Tile.Mine = false;
			else if (Tile.Road)
				Tile.Road = false;
			else if (Tile.RailRoad)
			{
				Tile.RailRoad = false;
				Tile.Road = true;
			}
			SkipTurn();			
		}

		public virtual void SkipTurn()
		{
			MovesLeft = 0;
			PartMoves = 0;
		}
		
		protected void SetIcon(char page, int col, int row)
		{
			if (_iconCache[(int)Type] == null)
			{
				_iconCache[(int)Type] = Resources[$"ICONPG{page}"][col * 160, row * 62, 160, 60]
					.ColourReplace((byte)(GFX256 ? 253 : 15), 0);
			}
			Icon = _iconCache[(int)Type];
		}

		protected MenuItem<int> MenuNoOrders() => MenuItem<int>.Create("No Orders").SetShortcut("space").OnSelect((s, a) => SkipTurn());

		protected MenuItem<int> MenuFortify() => MenuItem<int>.Create("Fortify").SetShortcut("f").OnSelect((s, a) => Fortify = true);

		protected MenuItem<int> MenuWait() => MenuItem<int>.Create("Wait").SetShortcut("w").OnSelect((s, a) => Game.UnitWait());

		protected MenuItem<int> MenuSentry() => MenuItem<int>.Create("Sentry").SetShortcut("s").OnSelect((s, a) => Sentry = true);

		protected MenuItem<int> MenuGoTo() => MenuItem<int>.Create("GoTo").OnSelect((s, a) => GameTask.Enqueue(Show.Goto));

		protected MenuItem<int> MenuPillage() => MenuItem<int>.Create("Pillage").SetShortcut("P").OnSelect((s, a) => Pillage());

		protected MenuItem<int> MenuHomeCity() => MenuItem<int>.Create("Home City").SetShortcut("h").OnSelect((s, a) => SetHome());

		protected MenuItem<int> MenuDisbandUnit() => MenuItem<int>.Create("Disband Unit").SetShortcut("D").OnSelect((s, a) => Game.DisbandUnit(this));

		public abstract IEnumerable<MenuItem<int>> MenuItems { get; }

		protected abstract bool ValidMoveTarget(ITile tile);

		public IEnumerable<ITile> MoveTargets => Map[X, Y].GetBorderTiles().Where(t => ValidMoveTarget(t));

		protected void Explore(int range, bool sea = false)
		{
			if (Game == null) return;
			Player player = Game.GetPlayer(Owner);
			if (player == null) return;
			player.Explore(X, Y, range, sea);
			if (player.IsHuman) Common.GamePlay?.RefreshMap();
		}

		public virtual void Explore() => Explore(1);

		internal static IBitmap GetBaseSprite(UnitType type)
		{
			if (!_modifications.ContainsKey(type)) return null;
			return _modifications[type].LastOrDefault(x => x.Sprite != null && x.Sprite.GifToBitmap() != null)?.Sprite.GifToBitmap();
		}

		private static Dictionary<UnitType, List<UnitModification>> _modifications = new Dictionary<UnitType, List<UnitModification>>();
		internal static void LoadModifications()
		{
			_modifications.Clear();

			UnitModification[] unitModifications = Reflect.GetModifications<UnitModification>().ToArray();
			if (unitModifications.Length == 0) return;

			Log("Applying unit modifications");

			foreach (UnitModification modification in Reflect.GetModifications<UnitModification>())
			{
				if (!_modifications.ContainsKey(modification.UnitType))
					_modifications.Add(modification.UnitType, new List<UnitModification>());
				_modifications[modification.UnitType].Add(modification);
			}

			Log("Finished applying unit modifications");
		}
		public IEnumerable<UnitModification> Modifications => _modifications.ContainsKey(Type) ? _modifications[Type].ToArray() : new UnitModification[0];

		public byte FuelOrProgress { get; set; }
		public byte Fuel { get => FuelOrProgress; set => FuelOrProgress = value; }
		public byte WorkProgress { get => FuelOrProgress; set => FuelOrProgress = value; }

		public int NearestCity
		{
			get
			{
				if (Game.Instance.GetCities().Length == 0)
				{
					return 0;
				}
				return Game.Instance.GetCities().Select(c => Common.DistanceToTile(_x, _y, c.X, c.Y)).Min();
			}
		}

		protected BaseUnit(byte price = 1, byte attack = 1, byte defense = 1, byte move = 1)
		{
			Price = price;
			BuyPrice = (short)((Price + 4) * 10 * Price);
			Attack = attack;
			Defense = defense;
			Move = move;
			X = -1;
			Y = -1;
			Goto = Point.Empty;
			Owner = 0;
			Status = 0;
			MovesSkip = 0;
			RequiredWonder = null;
			FuelOrProgress = 0;
		}
	}
}