// CivOne
//
// To the extent possible under law, the person who associated CC0 with
// CivOne has waived all copyright and related or neighboring rights
// to CivOne.
//
// You should have received a copy of the CC0 legalcode along with this
// work. If not, see <http://creativecommons.org/publicdomain/zero/1.0/>.

using System;
using CivOne.Advances;
using CivOne.IO.Text;
using CivOne.Screens;
using CivOne.Services;
using CivOne.Units;
using CivOne.Enums;
using System.Diagnostics;

namespace CivOne.Tasks
{
	internal class Orders : GameTask
	{
		private City? _city;
		private Player? _player;
		private IUnit? _unit;
		private int _x, _y;
		private Order _order;

		private static void Error(string error)
		{
			GameTask.Enqueue(Message.Error(TranslationServiceFactory.GetCurrent().Translate("-- Civilization Note --"), TextFileFactory.Get().GetGameText($"ERROR/{error}")));
		}

		private void CityManagerClosed(object? _, EventArgs args)
		{
			EndTask();
		}

		private void CityViewed(object? _, EventArgs args)
		{
			if (Common.HasScreenType<CityManager>()) return;

			CityManager cityManager = new(_city!);
			cityManager.Closed += CityManagerClosed;
			Common.AddScreen(cityManager);
		}

		private void CityFounded(object? _, EventArgs args)
		{
			CityView cityView = new(_city!, firstView: true);
			cityView.Closed += CityViewed;
			Common.AddScreen(cityView);
		}

		private void CityNameAccept(object? sender, EventArgs args)
		{
			if (sender == null) 
			{
				Debug.Assert(false, "Error: sender is null in CityNameAccept");
				EndTask(); //not sure, but we may want to end the task here to avoid to get stuck anyhow.
				return;
			}
			
			if (sender is CityName cityName)
			{
				int nameId = cityName.NameId;
				Game.CityNames[nameId] = cityName.Value ?? Game.CityNames[nameId]; // If the value is null, we keep the existing name.
				FoundCity(_player, nameId);
			} 
			else
			{
				Debug.Assert(false, $"Expected sender to be of type CityName, but got {sender.GetType().FullName ?? "unknown"}");
				Runtime.Log("Error: Expected sender to be of type CityName, but got {0}", sender.GetType().FullName ?? "unknown");
			}
			EndTask();
		}

		private void CityNameCancel(object? _, EventArgs args)
		{
			Human.CityNamesSkipped++;
			DecreaseUnitsMovesLeft();
			EndTask();
		}

		private void DecreaseUnitsMovesLeft()
		{
			if (_unit == null) 
			{
				// This happens if a unit is moving into a hut that founds a city, but the unit is not a settler.
				return;
			}
			_unit.MovesLeft--;
		}

		private void FoundCity(Player? player, int nameId)
		{
			if (player == null)
			{
				Debug.Assert(false, "Error: player is null in FoundCity");
				EndTask(); //not sure, but we may want to end the task here to avoid to get stuck anyhow.
				return;
			}
			_city = Game.AddCity(player, nameId, _x, _y);

			if (_city == null)
			{
				// should not happen because an existing city will just be joined and
				// this code will not be reached
				Game.UpdateResources(Map[_x, _y]);
				EndTask();

				Runtime.Log($"Error: Code should not be reached when founding a city at ({_x}, {_y}) for player {player.Civilization.Name} with nameId {nameId}");
				return;
			}

			// Settlers are consumed when a city is founded.
			if (_unit is Settlers)
			{
				Game.DisbandUnit(_unit);
			}

			if (!player.IsHuman)
			{
				Game.UpdateResources(_city.Tile);
				EndTask();
				
				return;
			}

			if (!Game.Animations)
			{
				CityViewed(this, EventArgs.Empty);
				return;
			}

			CityView cityScreen = CityView.FoundCityWithAnimation(_city);
			cityScreen.Closed += CityFounded;
			cityScreen.Skipped += CityViewed;
			Common.AddScreen(cityScreen);
		}

		private void OrderNewCity()
		{
			if (_unit != null && _unit is not Settlers)
			{
				Error("SETTLERS");
				EndTask();
				return;
			}

			if (_unit is Settlers settlers)
			{
				_player = settlers.Player;
				_x = settlers.X;
				_y = settlers.Y;
			}

			if (Map[_x, _y].IsOcean)
			{
				EndTask();
				return;
			}

			if (Map[_x, _y].City != null)
			{
				// There is already a city here
				if (_unit is Settlers)
				{
					if (Map[_x, _y].City.Size >= 10)
					{
						// City is 10 or larger, can not join city
						Error("ADDCITY");
						EndTask();
						return;
					}
					Map[_x, _y].City.Size++;
					Game.DisbandUnit(_unit);
				}
				EndTask();
				return;
			}

			if (_player == null)
			{
				EndTask();
				return;
			}

			int nameId = Game.CityNameId(_player);
			if (!ShowCityNamePromptScreenForHuman(_player, nameId))
			{
				FoundCity(_player, nameId);
			}
		}

		private bool ShowCityNamePromptScreenForHuman(Player player, int nameId)
		{
			if (!player.IsHuman) return false;

			CityName cityName = new(nameId, Game.CityNames[nameId]);
			cityName.Accept += CityNameAccept;
			cityName.Cancel += CityNameCancel;
			Common.AddScreen(cityName);

			return true;
		}

		private void Irrigate()
		{
			if (_unit is Settlers settlers)
			{
				settlers.BuildIrrigation();
				EndTask();

				return;
			}

			Error("SETTLERS");
			EndTask();
		}

		private void Mines()
		{
			if (_unit is Settlers settlers)
			{
				settlers.BuildMines();
				EndTask();

				return;
			}

			Error("SETTLERS");
			EndTask();
		}

		private void Fortress()
		{
			if (_unit is Settlers settlers)
			{
				Player? player = Game.GetPlayer(settlers.Owner);
				if (player?.HasAdvance<Construction>() == true)
				{
					settlers.BuildFortress();
				}
				EndTask();

				return;
			}

			Error("SETTLERS");
			EndTask();
		}

		private void Road()
		{
			if (_unit is Settlers settlers)
			{
				settlers.BuildRoad();
				EndTask();

				return;
			}

			Error("SETTLERS");
			EndTask();
		}
		private void ClearPollution()
		{
			if (_unit is Settlers settlers)
			{
				settlers.ClearPollution();
				EndTask();

				return;
			}

			Error("SETTLERS");
			EndTask();
		}

		private void UnitWait()
		{
			Game.UnitWait();
			EndTask();
		}

		public override void Run()
		{
			switch (_order)
			{
				case Order.NewCity:
					OrderNewCity();
					break;
				case Order.Irrigate:
					Irrigate();
					break;
				case Order.Mines:
					Mines();
					break;
				case Order.Fortress:
					Fortress();
					break;
				case Order.Road:
					Road();
					break;
				case Order.Wait:
					UnitWait();
					break;
				case Order.ClearPollution:
					ClearPollution();
					break;
				default:
					EndTask();
					break;
			}
		}

		public static Orders FoundCity(IUnit? unit = null)
		{
			return new Orders()
			{
				_unit = unit,
				_order = Order.NewCity
			};
		}

		public static Orders NewCity(Player player, int x, int y)
		{
			return new Orders()
			{
				_player = player,
				_order = Order.NewCity,
				_x = x,
				_y = y
			};
		}

		public static Orders BuildIrrigation(IUnit unit)
		{
			return new Orders()
			{
				_unit = unit,
				_order = Order.Irrigate
			};
		}

		public static Orders BuildMines(IUnit unit)
		{
			return new Orders()
			{
				_unit = unit,
				_order = Order.Mines
			};
		}

		public static Orders BuildFortress(IUnit unit)
		{
			return new Orders()
			{
				_unit = unit,
				_order = Order.Fortress
			};
		}

		public static Orders BuildRoad(IUnit unit)
		{
			return new Orders()
			{
				_unit = unit,
				_order = Order.Road
			};
		}

		public static Orders ClearPollution(IUnit unit)
		{
			return new Orders()
			{
				_unit = unit,
				_order = Order.ClearPollution
			};
		}

		public static Orders Wait(IUnit unit)
		{
			return new Orders()
			{
				_unit = unit,
				_order = Order.Wait
			};
		}

		private Orders()
		{
		}
	}
}