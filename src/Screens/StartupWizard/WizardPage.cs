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

namespace CivOne.Screens.StartupWizard
{
	internal sealed class WizardPage
	{
		public string Title { get; init; }
		public string[] Lines { get; init; } = [];
		public IReadOnlyList<WizardEntry> Entries { get; init; } = [];
		public int EntriesYOffset { get; init; }
		public IReadOnlyList<(string Label, string Url)> Links { get; init; } = [];

		/// <summary>
		/// Optional callback invoked while this page is active to detect external context changes.
		/// Return <see langword="true"/> to request a page rebuild/refresh.
		/// </summary>
		public Func<bool> HasContextChanged { get; init; }
	}
}
