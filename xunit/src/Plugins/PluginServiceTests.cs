using System;
using System.Linq;
using System.Reflection;
using CivOne.Agents;
using CivOne.Units;
using Xunit;

namespace CivOne.UnitTests.Plugins
{
	/// <summary>
	/// Verifies plugin discovery, capability wiring and error handling against a real plugin
	/// assembly loaded from disk.
	/// </summary>
	public class PluginServiceTests
	{
		private const string TestPluginFile = "CivOne.TestPlugin.dll";
		private const string TestAiProviderType = "CivOne.TestPlugin.TestAiProvider";

		/// <summary>
		/// The plugin assembly in the plugins directory is found and its metadata is read.
		/// </summary>
		[Fact]
		public void Plugins_FindsTheInstalledPlugin()
		{
			using PluginTestFixture fixture = new();
			fixture.InstallTestPlugin();

			Plugin plugin = Assert.Single(fixture.Service.Plugins);

			Assert.Equal(TestPluginFile, plugin.Filename);
			Assert.Equal("CivOne Test Plugin", plugin.Name);
			Assert.Equal("CivOne Tests", plugin.Author);
			Assert.Equal("1.0.0", plugin.Version);
			Assert.True(plugin.Enabled);
		}

		/// <summary>
		/// The plugin assembly is offered for game content discovery while the plugin is enabled.
		/// This is the mechanism that lets a plugin contribute content types at all.
		/// </summary>
		[Fact]
		public void EnabledAssemblies_ContainsThePluginAssembly()
		{
			using PluginTestFixture fixture = new();
			fixture.InstallTestPlugin();

			_ = fixture.Service.Plugins;

			Assert.Contains(
				fixture.Service.EnabledAssemblies,
				x => x.GetName().Name == "CivOne.TestPlugin");
		}

		/// <summary>
		/// The plugin assembly loads into its own context and does not bring its own copy of the
		/// shared contracts. Without this, no plugin type would ever be assignable to a host type.
		/// </summary>
		[Fact]
		public void PluginAssembly_UsesTheHostContracts()
		{
			using PluginTestFixture fixture = new();
			fixture.InstallTestPlugin();

			Plugin plugin = fixture.Service.Plugins.Single();
			Assembly assembly = Assert.IsAssignableFrom<Assembly>(plugin.Assembly);

			Type modification = assembly.GetType("CivOne.TestPlugin.TestWarriorModification")!;
			Assert.True(
				typeof(UnitModification).IsAssignableFrom(modification),
				"The plugin's modification type is not assignable to the host's UnitModification, " +
				"which means the plugin load context loaded its own copy of CivOne.API.");
		}

		/// <summary>
		/// Modification subclasses inside a plugin are discovered.
		/// Modification is an abstract class, so it can only be matched by assignability.
		/// </summary>
		[Fact]
		public void GetModifications_FindsThePluginModification()
		{
			using PluginTestFixture fixture = new();
			fixture.InstallTestPlugin();

			_ = fixture.Service.Plugins;

			UnitModification[] modifications = [.. Reflect.GetModifications<UnitModification>()];

			UnitModification modification = Assert.Single(modifications);
			Assert.Equal(Enums.UnitType.Militia, modification.UnitType);
		}

		/// <summary>
		/// A disabled plugin contributes nothing.
		/// </summary>
		[Fact]
		public void DisabledPlugin_ContributesNoModifications()
		{
			using PluginTestFixture fixture = new();
			fixture.InstallTestPlugin();

			Plugin plugin = fixture.Service.Plugins.Single();
			plugin.Enabled = false;

			Assert.Empty(Reflect.GetModifications<UnitModification>());
			Assert.Empty(fixture.Service.EnabledAssemblies);
		}

		/// <summary>
		/// The AI capability provider is discovered and instantiated.
		/// </summary>
		[Fact]
		public void AiProviders_DiscoversThePluginProvider()
		{
			using PluginTestFixture fixture = new();
			fixture.InstallTestPlugin();

			_ = fixture.Service.Plugins;

			Assert.Single(fixture.Service.AiProviders);
		}

		/// <summary>
		/// The plugin AI appears in the selection menu, and building the menu does not create it.
		/// This is what makes GetAiDescriptors the lightweight call it claims to be.
		/// </summary>
		[Fact]
		public void PluginAi_IsListedWithoutBeingCreated()
		{
			using PluginTestFixture fixture = new();
			fixture.InstallTestPlugin();

			_ = fixture.Service.Plugins;

			Assert.Contains(
				AgentLoaderEntry.GetAvailableDefinitions(),
				x => x.DisplayName == "Test AI" && x.Difficulty == AiDifficulty.Prince);

			Assert.Equal(0, fixture.ReadPluginStatic(TestAiProviderType, "CreateAiCallCount"));
			Assert.NotEqual(0, fixture.ReadPluginStatic(TestAiProviderType, "DescriptorCallCount"));
		}

		/// <summary>
		/// The AI is created only when a player is actually resolved to it.
		/// </summary>
		[Fact]
		public void PluginAi_IsCreatedOnFirstResolve()
		{
			using PluginTestFixture fixture = new();
			fixture.InstallTestPlugin();

			_ = fixture.Service.Plugins;

			Guid aiId = (Guid)fixture.ReadPluginStatic(TestAiProviderType, "TestAiId")!;

			Assert.True(AgentRegistry.Instance.TryResolveAi(aiId, out IAgentRegistration? registration));
			Assert.NotNull(registration);
			Assert.Equal(aiId, registration.GetInformation().GetUuid());
			Assert.Equal(1, fixture.ReadPluginStatic(TestAiProviderType, "CreateAiCallCount"));

			// A second resolve reuses the created instance.
			Assert.True(AgentRegistry.Instance.TryResolveAi(aiId, out _));
			Assert.Equal(1, fixture.ReadPluginStatic(TestAiProviderType, "CreateAiCallCount"));
		}

		/// <summary>
		/// Disabling a plugin removes its AI from the selection menu again.
		/// </summary>
		[Fact]
		public void DisablingPlugin_RemovesItsAi()
		{
			using PluginTestFixture fixture = new();
			fixture.InstallTestPlugin();

			Plugin plugin = fixture.Service.Plugins.Single();
			Guid aiId = (Guid)fixture.ReadPluginStatic(TestAiProviderType, "TestAiId")!;
			Assert.Contains(AgentLoaderEntry.GetAvailableDefinitions(), x => x.Id == aiId);

			plugin.Enabled = false;

			Assert.DoesNotContain(AgentLoaderEntry.GetAvailableDefinitions(), x => x.Id == aiId);
			Assert.False(AgentRegistry.Instance.TryResolveAi(aiId, out _));
		}

		/// <summary>
		/// A file with a .dll extension that is not a managed assembly is skipped, and the valid
		/// plugins next to it still load.
		/// </summary>
		[Fact]
		public void BrokenPluginFile_IsSkipped()
		{
			using PluginTestFixture fixture = new();
			fixture.InstallBrokenPlugin("not-an-assembly.dll");
			fixture.InstallTestPlugin();

			Plugin plugin = Assert.Single(fixture.Service.Plugins);
			Assert.Equal(TestPluginFile, plugin.Filename);
		}

		/// <summary>
		/// A missing plugins directory is not an error.
		/// </summary>
		[Fact]
		public void MissingPluginsDirectory_LoadsNothing()
		{
			using PluginTestFixture fixture = new();
			fixture.RemoveDirectory();

			Assert.Empty(fixture.Service.Plugins);
		}

		/// <summary>
		/// The map generator and image capabilities are discovered through the same mechanism, but
		/// the test plugin offers none, so the lists stay empty.
		/// </summary>
		[Fact]
		public void UnconsumedCapabilities_AreEmptyForAPluginThatOffersNone()
		{
			using PluginTestFixture fixture = new();
			fixture.InstallTestPlugin();

			_ = fixture.Service.Plugins;

			Assert.Empty(fixture.Service.MapGeneratorProviders);
			Assert.Empty(fixture.Service.ImageProviders);
		}
	}
}
