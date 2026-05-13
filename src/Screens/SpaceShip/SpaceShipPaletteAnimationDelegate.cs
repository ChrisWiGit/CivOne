// CivOne
//
// To the extent possible under law, the person who associated CC0 with
// CivOne has waived all copyright and related or neighboring rights
// to CivOne.
//
// You should have received a copy of the CC0 legalcode along with this
// work. If not, see <http://creativecommons.org/publicdomain/zero/1.0/>.

using CivOne.Graphics;

namespace CivOne.Screens.SpaceShipAssets
{
	internal sealed class SpaceShipPaletteAnimationDelegate(Palette palette)
	{
		private const int LightsAnimationSpeed = 45;
		private const int ModulesAnimationSpeed = 5;

		private const byte LightOnColorIndex = 128;
		private const byte LightOffColorIndex = 129;

		private const byte HabitationModuleLightStartCycleColorIndex = 192;
		private const byte HabitationModuleLightEndCycleColorIndex = 207;

		private const byte LifeModuleLightStartCycleColorIndex = 208;
		private const byte LifeModuleLightEndCycleColorIndex = 223;

		private readonly Palette _palette = palette;
		private readonly Colour _originalLightOnColor = palette[LightOnColorIndex];
		private readonly Colour _originalLightOffColor = palette[LightOffColorIndex];

		private int _lastLightState = -1;
		private int _lastModuleCycleTick = -1;

		internal bool Update(uint gameTick)
		{
			bool lightsChanged = UpdateLightsAnimation(gameTick);
			bool modulesChanged = UpdateModuleLightsAnimation(gameTick);
			return lightsChanged || modulesChanged;
		}

		private static void CyclePaletteRange(Palette palette, int start, int end)
		{
			Colour reserve = palette[end];
			for (int i = end; i > start; i--)
			{
				palette[i] = palette[i - 1];
			}
			palette[start] = reserve;
		}

		private bool UpdateLightsAnimation(uint gameTick)
		{
			int state = (int)(gameTick / LightsAnimationSpeed) % 2;
			if (state == _lastLightState)
			{
				return false;
			}

			_lastLightState = state;
			bool lightsOn = state == 0;
			_palette[LightOnColorIndex] = lightsOn ? _originalLightOnColor : _originalLightOffColor;
			_palette[LightOffColorIndex] = lightsOn ? _originalLightOffColor : _originalLightOnColor;
			return true;
		}

		private bool UpdateModuleLightsAnimation(uint gameTick)
		{
			int secondTick = (int)(gameTick / ModulesAnimationSpeed);
			if (secondTick == _lastModuleCycleTick)
			{
				return false;
			}

			_lastModuleCycleTick = secondTick;
			CyclePaletteRange(_palette, HabitationModuleLightStartCycleColorIndex, HabitationModuleLightEndCycleColorIndex);
			CyclePaletteRange(_palette, LifeModuleLightStartCycleColorIndex, LifeModuleLightEndCycleColorIndex);
			return true;
		}
	}
}
