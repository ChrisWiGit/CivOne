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
using CivOne.Buildings;
using CivOne.Civilizations;
using CivOne.Graphics;
using CivOne.Tasks;
using CivOne.Units;
using CivOne.UserInterface;

namespace CivOne.Screens.Dialogs
{
	internal class DiplomatCity : BaseDialog
	{
		private const int FONT_ID = 0;

		private readonly IDiplomatCityService _service;
		private Menu _menu;

		private void EstablishEmbassy(object sender, EventArgs args)
		{
			_service.EstablishEmbassy();
			Cancel();
		}

		private void InvestigateCity(object sender, EventArgs args)
		{
			_service.InvestigateCity();
			Cancel();
		}

		private void InciteRevolt(object sender, EventArgs args)
		{
			_service.InciteRevolt();
			Cancel();
		}

		private void IndustrialSabotage(object sender, EventArgs args)
		{
			_service.IndustrialSabotage();
			Cancel();
		}

		private void MeetWithKing(object sender, EventArgs args)
		{
			_service.MeetWithKing();
			Cancel();
		}

		private void StealTechnology(object sender, EventArgs args)
		{
			_service.StealTechnology();
			Cancel();
		}

		protected override void FirstUpdate()
		{
			CreateMenu();
			base.FirstUpdate();
		}

		private void CreateMenu()
		{
			if (_menu is not null)
			{
				return;
			}

			int choices = 6;
			int high = (2 * Resources.GetFontHeight(FONT_ID)) + (choices * Resources.GetFontHeight(FONT_ID)) + 8;
			_menu = new Menu(Palette, Selection(3, 5 + (2 * Resources.GetFontHeight(FONT_ID)), 125, high))
			{
				X = 103,
				Y = 100,
				CenterTo320Coordinates = true,
				MenuWidth = 130,
				ActiveColour = 11,
				TextColour = 5,
				DisabledColour = 3,
				FontId = FONT_ID
			};

			_menu.Items.Add("Establish Embassy").OnSelect(EstablishEmbassy).SetEnabled(!_service.HasEmbassy);
			_menu.Items.Add("Investigate City").OnSelect(InvestigateCity);
			_menu.Items.Add("Steal Technology").OnSelect(StealTechnology).SetEnabled(!_service.TechStolen);
			_menu.Items.Add("Industrial Sabotage").OnSelect(IndustrialSabotage);
			_menu.Items.Add("Incite a Revolt").OnSelect(InciteRevolt).SetEnabled(!_service.HasPalace);
			_menu.Items.Add("Meet with King").OnSelect(MeetWithKing).SetEnabled(!_service.IsBarbarian);

			_menu.Cancel += Cancel;
			_menu.MissClick += Cancel;

			AddMenu(_menu);
		}

		private static int DialogHeight()
		{
			int choices = 6 + 3;
			return (choices * Resources.GetFontHeight(FONT_ID));
		}

		internal DiplomatCity(IDiplomatCityService service) : base(100, 80, 145, DialogHeight())
		{
			_service = service ?? throw new ArgumentNullException(nameof(service));

			DialogBox.DrawText($"{_service.TribeName} diplomat arrives", 0, 15, 5, 5);
			DialogBox.DrawText($"in {_service.CityName}", 0, 15, 5, 5 + Resources.GetFontHeight(FONT_ID));
		}
	}

	internal static class DiplomatCityDialogFactory
	{
		public static IDiplomatCityService CreateService(City enemyCity, Diplomat diplomat)
		{
			return new DiplomatCityService(enemyCity, diplomat);
		}

		public static IScreen CreateDialog(City enemyCity, Diplomat diplomat)
		{
			IDiplomatCityService service = CreateService(enemyCity, diplomat);
			return new DiplomatCity(service);
		}

		public static IScreen CreateDialog(IDiplomatCityService service)
		{
			return new DiplomatCity(service);
		}
	}

	internal interface IDiplomatCityService
	{
		void EstablishEmbassy();
		void InvestigateCity();
		void InciteRevolt();
		void IndustrialSabotage();
		void MeetWithKing();
		void StealTechnology();

		string TribeName { get; }
		string CityName { get; }
		bool HasEmbassy { get; }
		bool TechStolen { get; }
		bool HasPalace { get; }
		bool IsBarbarian { get; }
	}

	internal class DiplomatCityService(City enemyCity, Diplomat diplomat) : IDiplomatCityService
	{
		private static Player Human => Game.Instance.HumanPlayer;
		private readonly City _enemyCity = enemyCity ?? throw new ArgumentNullException(nameof(enemyCity));
		private readonly Diplomat _diplomat = diplomat ?? throw new ArgumentNullException(nameof(diplomat));

		public string TribeName => _enemyCity.Player.TribeName;

		public string CityName => _enemyCity.Name;

		public bool HasEmbassy => Human.HasEmbassy(_enemyCity.Player);

		public bool TechStolen => _enemyCity.TechStolen;

		public bool HasPalace => _enemyCity.HasBuilding<Palace>();

		public bool IsBarbarian => _enemyCity.Player.Civilization is Barbarian;

		public void EstablishEmbassy()
		{
			Human.EstablishEmbassy(_enemyCity.Player);
			Game.Instance.DisbandUnit(_diplomat);
		}

		public void InvestigateCity()
		{
			GameTask.Enqueue(Show.ViewCity(_enemyCity));
			Game.Instance.DisbandUnit(_diplomat);
		}

		public void InciteRevolt()
		{
			GameTask.Enqueue(Tasks.Show.DiplomatIncite(_enemyCity, _diplomat));
		}

		public void IndustrialSabotage()
		{
			GameTask.Enqueue(Message.Spy("Spies report:", $"{_diplomat.Sabotage(_enemyCity)}", $"in {_enemyCity.Name}"));
		}

		public void MeetWithKing()
		{
			GameTask.Enqueue(Tasks.Show.MeetKing(_enemyCity.Player));
		}

		public void StealTechnology()
		{
			if (_enemyCity.TechStolen)
			{
				GameTask.Insert(Message.General("No new technology found"));
				return;
			}

			IAdvance advance = _diplomat.GetAdvanceToSteal(_enemyCity.Player);
			if (advance == null)
			{
				GameTask.Insert(Message.General("No new technology found"));
				return;
			}

			GameTask task = new GetAdvance(_diplomat.Player, advance);
			task.Done += (s, a) =>
			{
				_enemyCity.TechStolen = true;
				Game.Instance.DisbandUnit(_diplomat);
				if (_diplomat.Player == Human || _enemyCity.Player == Human)
					GameTask.Insert(Message.Spy("Spies report:", $"{_diplomat.Player.TribeName} steal", $"{advance.Name}"));
			};
			GameTask.Enqueue(task);
		}
	}
}