using System;
using System.Linq;
using System.Runtime.CompilerServices;
using Xunit;

namespace CivOne.UnitTests.Plugins
{
	/// <summary>
	/// Verifies that disabling a plugin really releases its assembly.
	/// This is the only test that proves the collectible load context and the cache teardown work
	/// together: either one alone would leave the assembly in the process forever.
	/// </summary>
	public class PluginUnloadTests
	{
		/// <summary>
		/// After disabling, the plugin drops its assembly reference.
		/// </summary>
		[Fact]
		public void DisablingPlugin_ReleasesTheAssembly()
		{
			using PluginTestFixture fixture = new();
			fixture.InstallTestPlugin();

			Plugin plugin = fixture.Service.Plugins.Single();
			Assert.False(plugin.IsUnloaded);
			Assert.NotNull(plugin.Assembly);

			plugin.Enabled = false;

			Assert.True(plugin.IsUnloaded);
			Assert.Null(plugin.Assembly);
		}

		/// <summary>
		/// Re-enabling a plugin loads its assembly again.
		/// </summary>
		[Fact]
		public void ReEnablingPlugin_LoadsTheAssemblyAgain()
		{
			using PluginTestFixture fixture = new();
			fixture.InstallTestPlugin();

			Plugin plugin = fixture.Service.Plugins.Single();
			plugin.Enabled = false;
			Assert.True(plugin.IsUnloaded);

			plugin.Enabled = true;

			Assert.False(plugin.IsUnloaded);
			Assert.NotNull(plugin.Assembly);
			Assert.Contains(
				fixture.Service.EnabledAssemblies,
				x => x.GetName().Name == "CivOne.TestPlugin");
		}

		/// <summary>
		/// The load context is actually collected once the plugin is disabled.
		/// Collection is best effort by nature - a running game still holding plugin instances would
		/// keep the context alive - but with no game running nothing must reference it any more.
		/// </summary>
		[Fact]
		public void DisablingPlugin_CollectsTheLoadContext()
		{
			using PluginTestFixture fixture = new();
			fixture.InstallTestPlugin();

			WeakReference contextReference = DisableAndTrackContext(fixture);

			for (int attempt = 0; attempt < 10 && contextReference.IsAlive; attempt++)
			{
				GC.Collect();
				GC.WaitForPendingFinalizers();
			}

			Assert.False(
				contextReference.IsAlive,
				"The plugin load context was not collected, so the plugin assembly stays in the " +
				"process. Something still references a type created from it.");
		}

		/// <summary>
		/// Disables the plugin inside its own stack frame so no local keeps the assembly alive.
		/// </summary>
		/// <param name="fixture">
		/// The fixture holding the plugin service.
		/// </param>
		/// <returns>
		/// A weak reference to the plugin's load context.
		/// </returns>
		[MethodImpl(MethodImplOptions.NoInlining)]
		private static WeakReference DisableAndTrackContext(PluginTestFixture fixture)
		{
			Plugin plugin = fixture.Service.Plugins.Single();
			WeakReference reference = new(plugin.LoadContextForTests);

			plugin.Enabled = false;
			return reference;
		}
	}
}
