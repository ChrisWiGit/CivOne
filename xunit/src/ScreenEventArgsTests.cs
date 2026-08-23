using CivOne.Enums;
using CivOne.Events;
using Xunit;

namespace CivOne.UnitTests
{
	/// <summary>
	/// Verifies screen mouse event payload construction.
	/// </summary>
	public class ScreenEventArgsTests
	{
		/// <summary>
		/// Ensures constructor values for modifier and wheel delta are preserved.
		/// </summary>
		[Fact]
		public void ConstructorWithModifierAndWheelDeltaPreservesValues()
		{
			var actual = new ScreenEventArgs(12, 34, MouseButton.Left, KeyModifier.Control, -1);

			Assert.Equal(12, actual.X);
			Assert.Equal(34, actual.Y);
			Assert.Equal(MouseButton.Left, actual.Buttons);
			Assert.Equal(KeyModifier.Control, actual.Modifier);
			Assert.Equal(-1, actual.WheelDelta);
		}
	}
}
