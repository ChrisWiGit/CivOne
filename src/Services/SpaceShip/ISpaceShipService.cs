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
using CivOne.Enums;

namespace CivOne.Services.SpaceShip
{
	public interface ISpaceShipService
	{
		bool CanAddAnyPart();
		bool CanAddPart(SpaceShipComponentType partType);
		bool TryAddPart(SpaceShipComponentType partType);
		bool CanLaunch();
		SpaceShipScreenData GetScreenData();
	}

	public interface IPlayerSpaceRace
	{
		SpaceShipComponentType[,] SpaceShipGrid { get; set; }
		ushort SpaceShipPopulation { get; set; }
		short SpaceShipLaunchYear { get; set; }
		bool HasSpaceFlightAdvance();
		bool HasPlasticsAdvance();
		bool HasRoboticsAdvance();
		bool HasApolloProgram();
	}

	public interface ISpaceShipServiceFactory
	{
		ISpaceShipService Create(Player player);
	}

	public interface ISpaceShipPlacementRules
	{
		bool CanAddPart(IPlayerSpaceRace player, SpaceShipComponentType partType);
		bool TryAddPart(IPlayerSpaceRace player, SpaceShipComponentType partType);
	}

	public interface ISpaceShipLaunchRules
	{
		bool CanLaunch(IPlayerSpaceRace player);
	}

	public interface ISpaceShipScreenDataFactory
	{
		SpaceShipScreenData Create(IPlayerSpaceRace player, bool canLaunch);
	}

	public interface ISpaceShipSlotBlueprint
	{
		string[] SlotMap { get; }
		string[] StructuralOrderMap { get; }
		string[] PropulsionOrderMap { get; }
		string[] FuelOrderMap { get; }
		string[] SolarPanelOrderMap { get; }
		string[] HabitationOrderMap { get; }
		string[] LifeSupportOrderMap { get; }
		IReadOnlyDictionary<char, (int w, int h)> Footprint { get; }
		(int x, int y)[] StructuralOrder { get; }
		int MaxStructuralSlots { get; }
		(int x, int y)[] PropulsionOrder { get; }
		(int x, int y)[] FuelOrder { get; }
		(int x, int y)[] SolarPanelOrder { get; }
		(int x, int y)[] LifeSupportOrder { get; }
		(int x, int y)[] HabitationOrder { get; }
		SpaceShipOverlaySprite[] OverlaySprites { get; }
		(int x, int y)[] ComponentOrder { get; }
		(int x, int y)[] ModuleOrder { get; }
	}

	/// <summary>
	/// Well-known ids for spaceship overlay sprite groups.
	/// Multiple overlay entries can share the same id and will be rendered together.
	/// </summary>
	public static class SpaceShipOverlaySpriteIds
	{
		/// <summary>
		/// Overlay group id used for command module overlays.
		/// </summary>
		public const uint CommandModule = 1;
	}

	/// <summary>
	/// Overlay sprite entry bound to grid coordinates with optional pixel offsets.
	/// <para>
	/// SpriteId is used to select a logical overlay group, SpriteType defines the concrete sprite to draw.
	/// </para>
	/// </summary>
	/// <param name="FieldX">Grid x coordinate (12x12 field space).</param>
	/// <param name="FieldY">Grid y coordinate (12x12 field space).</param>
	/// <param name="PixelOffsetX">Additional x offset in pixels.</param>
	/// <param name="PixelOffsetY">Additional y offset in pixels.</param>
	/// <param name="SpriteId">Logical overlay id used for grouped rendering selection.</param>
	/// <param name="SpriteType">Concrete spaceship sprite type to render.</param>
	/// <param name="ZIndex">Render order where lower values are drawn first.</param>
	/// <param name="visible">Visibility flag for conditional drawing.</param>
	public readonly record struct SpaceShipOverlaySprite(
		byte FieldX, byte FieldY, byte PixelOffsetX, byte PixelOffsetY, uint SpriteId, SpaceShipComponentType SpriteType, int ZIndex, bool visible)
	{
		/// <summary>
		/// Creates an overlay sprite entry without pixel offset and hidden by default.
		/// </summary>
		public SpaceShipOverlaySprite(byte FieldX, byte FieldY, uint spriteId, SpaceShipComponentType spriteType, int zIndex) : this(FieldX, FieldY, 0, 0, spriteId, spriteType, zIndex, false)
		{
		}

		/// <summary>
		/// Converts field x coordinate + offset to absolute pixel x.
		/// </summary>
		public int PixelX(short GridCellSize) => FieldX * GridCellSize + PixelOffsetX;

		/// <summary>
		/// Converts field y coordinate + offset to absolute pixel y.
		/// </summary>
		public int PixelY(short GridCellSize) => FieldY * GridCellSize + PixelOffsetY;

		/// <summary>
		/// Gets whether this overlay entry should currently be drawn.
		/// </summary>
		public bool IsVisible() => visible;

		/// <summary>
		/// Returns a copy with updated visibility.
		/// </summary>
		public SpaceShipOverlaySprite WithVisibility(bool visible) => new(FieldX, FieldY, PixelOffsetX, PixelOffsetY, SpriteId, SpriteType, ZIndex, visible);

		/// <summary>
		/// Returns a copy with additional pixel offsets.
		/// </summary>
		public SpaceShipOverlaySprite WithPixelOffset(byte offsetX, byte offsetY) =>
			new(FieldX, FieldY, (byte)(PixelOffsetX + offsetX), (byte)(PixelOffsetY + offsetY), SpriteId, SpriteType, ZIndex, visible);
	}


	public interface ISpaceShipSlotBlueprintFactory
	{
		ISpaceShipSlotBlueprint Create();
	}
}
