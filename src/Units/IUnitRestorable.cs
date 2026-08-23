using System;

namespace CivOne.Units
{
    /// <summary>
    /// IUnit contains FortifyActive that has no setter.
    /// This value is restored from Status but
    /// cannot set otherwise.
    /// For UnitDtoMapper to restore FortifyActive, IUnitRestorable is introduced.
    /// </summary>
    public interface IUnitRestorable : IUnit
    {
        /// <summary>
		/// Indicates whether the unit is currently in the active fortify state, which is a specific status that can be true even if the unit is not fully fortified (i.e., Fortify property is false). This property has a setter in IUnitRestorable to allow it to be set directly during restoration from saved data, but it does not have a setter in IUnit to prevent it from being changed during normal gameplay outside of the restoration process.
        /// </summary>
        new bool FortifyActive { get; set; }

        /// <summary>
        /// Sets the unit's status flags based on the provided boolean values directly.
        /// This will force the exact status of the unit, including Sentry, FortifyActive, Fortify, and Veteran, without triggering any of the usual game logic or animations that would occur if you set the properties individually.
        /// </summary>
        /// <param name="sentry"></param>
        /// <param name="fortifyActive"></param>
        /// <param name="fortify"></param>
        /// <param name="veteran"></param>
        void ForceStatus(bool sentry, bool fortifyActive, bool fortify, bool veteran);

        /// <summary>
        /// Stores the home city GUID during YAML load until the city objects are fully
        /// hydrated and can be resolved by the <c>Game(GameState)</c> constructor.
        /// </summary>
        Guid? PendingHomeCityGuid { get; set; }
    }
}