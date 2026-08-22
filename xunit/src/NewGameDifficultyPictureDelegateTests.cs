using System.Drawing;
using CivOne.Screens.NewGamePanels;
using Xunit;

namespace CivOne.UnitTests
{
	/// <summary>
	/// Covers the layout of the difficulty portraits: where a portrait is cut out of the background
	/// picture, and which portrait a click on the screen hits.
	/// </summary>
	public sealed class NewGameDifficultyPictureDelegateTests
	{
		private const int OffsetX = 40;
		private const int OffsetY = 20;

		private readonly NewGameDifficultyPictureDelegate _delegateUnderTest = new();

		/// <summary>
		/// Returns the middle of the portrait of a difficulty, in screen coordinates.
		/// </summary>
		/// <param name="difficulty">Index of the difficulty.</param>
		/// <returns>The centre of the portrait.</returns>
		private Point PictureCenter(int difficulty)
		{
			Rectangle bounds = _delegateUnderTest.GetPictureBounds(difficulty);
			return new Point(
				OffsetX + bounds.X + (bounds.Width / 2),
				OffsetY + bounds.Y + (bounds.Height / 2));
		}

		/// <summary>
		/// A click in the middle of a portrait picks the difficulty of that portrait.
		/// </summary>
		/// <param name="difficulty">Index of the difficulty.</param>
		[Theory]
		[InlineData(0)]
		[InlineData(1)]
		[InlineData(2)]
		[InlineData(3)]
		[InlineData(4)]
		public void ClickOnPicturePicksItsDifficulty(int difficulty)
		{
			int result = _delegateUnderTest.GetDifficultyAt(
				PictureCenter(difficulty),
				OffsetX,
				OffsetY,
				NewGameDifficultyPictureDelegate.PictureCount);

			Assert.Equal(difficulty, result);
		}

		/// <summary>
		/// A click next to every portrait picks nothing.
		/// </summary>
		[Fact]
		public void ClickBesidePicturesPicksNothing()
		{
			Point besidePictures = new(OffsetX + 200, OffsetY + 190);

			int result = _delegateUnderTest.GetDifficultyAt(
				besidePictures,
				OffsetX,
				OffsetY,
				NewGameDifficultyPictureDelegate.PictureCount);

			Assert.Equal(NewGameDifficultyPictureDelegate.NoDifficulty, result);
		}

		/// <summary>
		/// Only the portraits of the offered difficulties can be clicked.
		/// </summary>
		[Fact]
		public void PicturesOfMissingDifficultiesAreNotPicked()
		{
			int result = _delegateUnderTest.GetDifficultyAt(PictureCenter(4), OffsetX, OffsetY, 3);

			Assert.Equal(NewGameDifficultyPictureDelegate.NoDifficulty, result);
		}

		/// <summary>
		/// The portraits do not overlap, so no click can hit two of them.
		/// </summary>
		[Fact]
		public void PicturesDoNotOverlap()
		{
			for (int difficulty = 0; difficulty < NewGameDifficultyPictureDelegate.PictureCount; difficulty++)
			{
				for (int other = difficulty + 1; other < NewGameDifficultyPictureDelegate.PictureCount; other++)
				{
					Rectangle bounds = _delegateUnderTest.GetPictureBounds(difficulty);
					Rectangle otherBounds = _delegateUnderTest.GetPictureBounds(other);

					Assert.False(bounds.IntersectsWith(otherBounds));
				}
			}
		}

		/// <summary>
		/// A difficulty without an own portrait reuses the last one, and negative values the first.
		/// </summary>
		[Fact]
		public void DifficultiesOutsideThePictureRangeReuseTheOuterPictures()
		{
			Rectangle lastPicture = _delegateUnderTest.GetPictureBounds(NewGameDifficultyPictureDelegate.PictureCount - 1);
			Rectangle firstPicture = _delegateUnderTest.GetPictureBounds(0);

			Assert.Equal(lastPicture, _delegateUnderTest.GetPictureBounds(NewGameDifficultyPictureDelegate.PictureCount + 3));
			Assert.Equal(firstPicture, _delegateUnderTest.GetPictureBounds(-1));
		}
	}
}
