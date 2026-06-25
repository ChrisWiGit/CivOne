using System.Collections;
using System.Collections.Generic;

namespace CivOne.Services.Random
{
	public interface IRandomService
	{
		int NextInt(int maxExclusive);
		int NextInt(int min, int maxExclusive);
		byte NextByte(byte min, byte maxExclusive) => (byte)NextInt(min, maxExclusive);
		byte NextByte(byte maxExclusive) => (byte)NextInt(maxExclusive);

		ushort NextUShort(ushort maxExclusive) => (ushort)NextInt(maxExclusive);
		ushort NextUShort(ushort min, ushort maxExclusive) => (ushort)NextInt(min, maxExclusive);
		
		/// <summary>
		/// Determines if a random event occurs based on a percentage chance.
		/// </summary>
		/// <param name="percent">The percentage chance (0-100) for the event to occur.</param>
		/// <returns>True if the random event occurs, false otherwise.</returns>
		/// <remarks>
		/// This method generates a random number between 0 and 99 (inclusive) and compares it
		/// against the provided percentage. For example, a 30% chance means the method returns
		/// <c>true</c> approximately 30% of the time when called repeatedly.
		/// </remarks>
		bool Hit(int percent);


		/// <summary>
		/// Determines if a random event occurs based on a fractional chance defined by a numerator and denominator.
		/// For example, a 1 in 4 chance would be represented as numerator=1 and denominator=4. The method returns true if the random event occurs, and false otherwise.
		/// The method generates a random number between 0 (inclusive) and the denominator (exclusive) and checks if it is less than the numerator. 
		/// If the numerator is greater than or equal to the denominator, the method will always return true. 
		/// If the numerator is less than or equal to zero, the method will always return false.
		/// Use this method when you want to represent probabilities that are not easily expressed as a percentage, such as a 1 in 3 chance (numerator=1, denominator=3) or a 2 in 5 chance (numerator=2, denominator=5).
		/// </summary>
		/// <param name="numerator">The numerator representing the number of successful outcomes. Must be non-negative.</param>
		/// <param name="denominator">The denominator representing the total number of possible outcomes. Must be positive.</param>
		/// <returns>True if the random event occurs, false otherwise.</returns>
		bool Hit(int numerator, int denominator);


		/// <summary>
		/// Returns a random index for the given list, which can be used to select a random element from the list. 
		/// The method generates a random integer between 0 (inclusive) and the count of the list (exclusive).
		/// For example, if the list has 5 elements, the method will return a random index between 0 and 4, which can be used to access a random element from the list. 
		/// If the list is empty, the method will throw an exception due to the invalid range for random number generation. 
		/// Use this method in conjunction with <see cref="NextElement{T}(IList{T})"/> to easily select a random element from a list.
		/// </summary>
		/// <typeparam name="T">The type of elements in the list.</typeparam>
		/// <param name="list">The list from which to select a random index. Must not be null.</param>
		/// <returns>A random index between 0 (inclusive) and the count of the list (exclusive).</returns>
		/// <exception cref="ArgumentNullException">Thrown if the provided list is null.</exception>
		/// <exception cref="ArgumentOutOfRangeException">Thrown if the provided list is empty.</exception>
		/// <returns></returns>
		int NextIndex<T>(IList<T> list) => NextInt(list.Count);

		/// <summary>
		/// Returns a random index for the given array, which can be used to select a random element from the array. 
		/// The method generates a random integer between 0 (inclusive) and the length of the array (exclusive).
		/// For example, if the array has 5 elements, the method will return a random index between 0 and 4, which can be used to access a random element from the array. 
		/// If the array is empty, the method will throw an exception due to the invalid range for random number generation. 
		/// Use this method in conjunction with <see cref="NextElement{T}(T[])"/> to easily select a random element from an array.
		/// </summary>
		/// <typeparam name="T">The type of elements in the array.</typeparam>
		/// <param name="array">The array from which to select a random index. Must not be null.</param>
		/// <returns>A random index between 0 (inclusive) and the length of the array (exclusive).</returns>
		/// <exception cref="ArgumentNullException">Thrown if the provided array is null.</exception>
		/// <exception cref="ArgumentOutOfRangeException">Thrown if the provided array is empty.</exception>
		int NextIndex<T>(T[] array) => NextInt(array.Length);

		/// <summary>
		/// Returns a random element from the given list. 
		/// The method uses <see cref="NextIndex{T}(IList{T})"/> to generate a random index and returns the element at that index from the list. 
		/// For example, if the list contains the elements ["apple", "banana", "cherry"], the method will return one of these elements at random. 
		/// If the list is empty, the method will throw an exception due to the invalid range for random number generation. 
		/// Use this method to easily select a random element from a list without having to manually generate a random index.
		/// </summary>
		/// <example>
		/// <code>
		/// IList<string> fruits = new List<string> { "apple", "banana", "cherry" };
		/// string randomFruit = randomService.NextElement(fruits);
		/// Console.WriteLine(randomFruit); // Outputs a random fruit from the list
		/// </code>
		/// </example>
		/// <typeparam name="T">The type of elements in the list.</typeparam>
		/// <param name="list">The list from which to select a random element. Must not be null.</param>
		/// <returns>A random element from the list.</returns>
		/// <exception cref="ArgumentNullException">Thrown if the provided list is null.</exception>
		/// <exception cref="ArgumentOutOfRangeException">Thrown if the provided list is empty.</exception>
		T NextElement<T>(IList<T> list) => list[NextIndex(list)];

		/// <summary>
		/// Returns a random element from the given array. 
		/// The method uses <see cref="NextIndex{T}(T[])"/> to generate a random index and returns the element at that index from the array. 
		/// For example, if the array contains the elements ["red", "green", "blue"], the method will return one of these elements at random. 
		/// If the array is empty, the method will throw an exception due to the invalid range for random number generation. 
		/// Use this method to easily select a random element from an array without having to manually generate a random index.
		/// </summary>
		/// <example>
		/// <code>
		/// string[] colors = new string[] { "red", "green", "blue" };
		/// string randomColor = randomService.NextElement(colors);
		/// Console.WriteLine(randomColor); // Outputs a random color from the array
		/// </code>
		/// </example>
		/// <typeparam name="T">The type of elements in the array.</typeparam>
		/// <param name="array">The array from which to select a random element. Must not be null.</param>
		/// <returns>A random element from the array.</returns>
		/// <exception cref="ArgumentNullException">Thrown if the provided array is null.</exception>
		/// <exception cref="ArgumentOutOfRangeException">Thrown if the provided array is empty.</exception>
		T NextElement<T>(T[] array) => array[NextIndex(array)];
	}
}
