using System.Collections;
using System.Collections.Generic;
using CivOne.Enums;

namespace CivOne.Screens
{
	public record CitizenDrawInfo(int CitizenIndex, int X, int SpecialistIndex, Citizen Citizen);
	public interface ICityCitizenLayoutService
	{
		IEnumerable<CitizenDrawInfo> EnumerateCitizens();

		int Width();

		bool IsBigCity { get; }
		int CitizenOffset { get; }

		/// <summary>
		/// Factory method to create an instance of ICityCitizenLayoutService for the specified city.
		/// CW: Not sure if this is a good idea for dependency injection with a factory method in an interface.
		/// However, it centralizes the creation logic and makes it easier to manage dependencies and change implementations for 
		/// all consumers of this interface.
		/// However #2, it is not good for unit testing/mocking.
		/// I guess we do not change it often, so it is acceptable.
		/// </summary>
		/// <param name="city">The city for which to create the layout service.</param>
		/// <returns>An instance of ICityCitizenLayoutService for the specified city.</returns>
		static ICityCitizenLayoutService Create(City city) => new CityCitizenLayoutServiceImpl(city);
	}

}