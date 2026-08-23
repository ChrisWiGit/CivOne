// CivOne
//
// To the extent possible under law, the person who associated CC0 with
// CivOne has waived all copyright and related or neighboring rights
// to CivOne.
//
// You should have received a copy of the CC0 legalcode along with this
// work. If not, see <http://creativecommons.org/publicdomain/zero/1.0/>.

using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Linq;

namespace CivOne.Screens
{
	/// <summary>
	/// Provides managed input lifecycle support for <see cref="BaseScreen"/>.
	/// This partial owns creation, registration, closing, and disposal of input overlay screens.
	/// </summary>
	/// <remarks>
	/// Use this partial when input overlays should be owned by <see cref="BaseScreen"/> instead of local fields.
	/// Derived screens usually override <see cref="CreateManagedInput"/> and call <see cref="EnsureManagedInput"/> when input mode starts.
	/// For detailed behavior notes and examples, see docs/BaseScreen.ManagedInputs.md.
	/// </remarks>
	public abstract partial class BaseScreen
	{
		private readonly List<IScreen> _inputs = [];
		private bool _managedInputInitialized;

		[SuppressMessage("Design", "CA1002:Do not expose generic lists", Justification = "The Inputs property is protected and only used internally by BaseScreen-derived classes to manage owned input overlays.")]
		protected List<IScreen> Inputs => _inputs;

		protected bool HasInput => Inputs.Count != 0;

		/// <summary>
		/// Override to provide a lazily created, single managed input for this screen.
		/// Return null when no managed input is needed.
		/// </summary>
		protected virtual IScreen? CreateManagedInput()
		{
			return null;
		}

		/// <summary>
		/// Creates and adds the managed input once.
		/// Returns the existing or created input instance, or null when no input is provided.
		/// </summary>
		protected IScreen? EnsureManagedInput()
		{
			if (_managedInputInitialized)
			{
				return Inputs.FirstOrDefault();
			}

			_managedInputInitialized = true;
			IScreen? input = CreateManagedInput();
			if (input == null)
			{
				return null;
			}

			AddInput(input);
			return input;
		}

		protected void AddInput(IScreen input)
		{
			Inputs.Add(input);
			Common.AddScreen(input);
		}

		protected void CloseInputs()
		{
			foreach (IScreen input in Inputs.ToArray())
			{
				Common.DestroyScreen(input);
			}

			Inputs.Clear();
		}

		private void DisposeInputs()
		{
			foreach (IScreen input in Inputs.Distinct().ToArray())
			{
				input.Dispose();
			}

			Inputs.Clear();
		}
	}
}