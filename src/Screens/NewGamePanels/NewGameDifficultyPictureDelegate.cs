using System.Drawing;

namespace CivOne.Screens.NewGamePanels
{
	/// <summary>
	/// Knows where the difficulty portraits sit in the difficulty background picture.
	/// The screen uses it to cut a single portrait out of the background and to tell which portrait a
	/// mouse click hit.
	/// The portraits are staggered: they alternate between a left and a right column and step down by a
	/// fixed amount, which is how the original background arranges them.
	/// </summary>
	internal class NewGameDifficultyPictureDelegate
	{
		/// <summary>
		/// Width of one portrait.
		/// </summary>
		public const int PictureWidth = 53;

		/// <summary>
		/// Height of one portrait.
		/// </summary>
		public const int PictureHeight = 47;

		/// <summary>
		/// Number of portraits the background picture holds.
		/// </summary>
		public const int PictureCount = 5;

		/// <summary>
		/// Result of <see cref="GetDifficultyAt"/> when no portrait was hit.
		/// </summary>
		public const int NoDifficulty = -1;

		private const int LeftColumnX = 21;
		private const int RightColumnX = 80;
		private const int FirstPictureY = 6;
		private const int PictureOffsetY = 35;

		/// <summary>
		/// Returns the area of the portrait that belongs to a difficulty, relative to the background
		/// picture.
		/// Difficulties without an own portrait reuse the last one.
		/// </summary>
		/// <param name="difficulty">Index of the difficulty.</param>
		/// <returns>The area of the portrait inside the background picture.</returns>
		public virtual Rectangle GetPictureBounds(int difficulty)
		{
			int pictureIndex = difficulty;
			if (pictureIndex < 0)
			{
				pictureIndex = 0;
			}
			if (pictureIndex > PictureCount - 1)
			{
				pictureIndex = PictureCount - 1;
			}

			int x = (pictureIndex % 2) == 0 ? LeftColumnX : RightColumnX;
			int y = FirstPictureY + (PictureOffsetY * pictureIndex);
			return new Rectangle(x, y, PictureWidth, PictureHeight);
		}

		/// <summary>
		/// Returns the difficulty whose portrait covers a point on the screen.
		/// </summary>
		/// <param name="location">The clicked point, in screen coordinates.</param>
		/// <param name="offsetX">Horizontal position of the background picture on the screen.</param>
		/// <param name="offsetY">Vertical position of the background picture on the screen.</param>
		/// <param name="difficultyCount">Number of difficulties the game offers.</param>
		/// <returns>The index of the difficulty, or <see cref="NoDifficulty"/> when no portrait was hit.</returns>
		/// <example>
		/// <code>
		/// int difficulty = pictures.GetDifficultyAt(args.Location, OffsetX, OffsetY, difficulties.Length);
		/// </code>
		/// </example>
		public virtual int GetDifficultyAt(Point location, int offsetX, int offsetY, int difficultyCount)
		{
			int count = difficultyCount < PictureCount ? difficultyCount : PictureCount;
			for (int difficulty = 0; difficulty < count; difficulty++)
			{
				Rectangle bounds = GetPictureBounds(difficulty);
				bounds.Offset(offsetX, offsetY);
				if (bounds.Contains(location))
				{
					return difficulty;
				}
			}

			return NoDifficulty;
		}
	}
}
