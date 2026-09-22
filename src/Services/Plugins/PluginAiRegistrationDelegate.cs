using System;
using System.Collections.Generic;
using CivOne.Agents;

namespace CivOne.Services.Plugins
{
	/// <summary>
	/// Keeps the AI variants offered by plugins in sync with the agent registry.
	/// Registration is lazy: the descriptors are enough to build the selection menu, and an AI is
	/// only created once a player is actually resolved to it.
	/// </summary>
	internal sealed class PluginAiRegistrationDelegate
	{
		private readonly List<Guid> _registeredIds = [];

		private static void Log(string text, params object[] parameters) => RuntimeHandler.Runtime.Log(text, parameters);

		/// <summary>
		/// Replaces all previously registered plugin AIs with the ones the given providers offer.
		/// </summary>
		/// <param name="providers">
		/// The AI providers of the currently enabled plugins.
		/// </param>
		/// <param name="translationService">
		/// The translation service passed to the providers so they can localize their text.
		/// </param>
		public void Refresh(IReadOnlyList<IPluginAiProvider> providers, ITranslationService translationService)
		{
			ArgumentNullException.ThrowIfNull(providers);
			ArgumentNullException.ThrowIfNull(translationService);

			UnregisterAll();

			foreach (IPluginAiProvider provider in providers)
			{
				RegisterProvider(provider, translationService);
			}
		}

		private void UnregisterAll()
		{
			foreach (Guid id in _registeredIds)
			{
				AgentLoaderEntry.Unregister(id);
			}
			_registeredIds.Clear();
		}

		private void RegisterProvider(IPluginAiProvider provider, ITranslationService translationService)
		{
			IReadOnlyList<AiDescriptor> descriptors;
			try
			{
				descriptors = provider.GetAiDescriptors(translationService);
			}
			catch (Exception exception) when (PluginFailure.IsLoadFailure(exception))
			{
				Log($"Plugins: {provider.GetType().FullName} failed to list its AI variants: {exception.Message}");
				return;
			}

			if (descriptors == null) return;

			foreach (AiDescriptor descriptor in descriptors)
			{
				RegisterDescriptor(provider, descriptor, translationService);
			}
		}

		private void RegisterDescriptor(IPluginAiProvider provider, AiDescriptor descriptor, ITranslationService translationService)
		{
			if (descriptor == null) return;
			if (descriptor.Id == Guid.Empty)
			{
				Log($"Plugins: {provider.GetType().FullName} offers an AI variant without an identifier, skipping.");
				return;
			}

			// Plugin input: an enum field can still hold an undefined cast value.
			AiDifficulty difficulty = Enum.IsDefined(descriptor.DefaultDifficulty)
				? descriptor.DefaultDifficulty
				: AiDifficulty.Unspecified;
			AiDefinition definition = new(
				descriptor.Id,
				descriptor.Name,
				descriptor.Description,
				descriptor.Author,
				difficulty);

			AgentLoaderEntry.RegisterLazy(
				descriptor.Id,
				definition,
				() => CreateAi(provider, descriptor, translationService));

			_registeredIds.Add(descriptor.Id);
		}

		private static IAgentRegistration CreateAi(
			IPluginAiProvider provider,
			AiDescriptor descriptor,
			ITranslationService translationService)
		{
			IAgentRegistration registration = provider.CreateAi(
				descriptor.Id,
				new AiCreationContext(translationService));

			Guid actualId = registration.GetInformation().GetUuid();
			if (actualId != descriptor.Id)
			{
				// The descriptor identifier is the one persisted in the save game, so it wins. A
				// mismatch means the plugin would resolve differently after a reload, which is worth
				// reporting even though the game continues.
				Log($"Plugins: AI '{descriptor.Name}' reports UUID {actualId} but was registered as {descriptor.Id}.");
			}

			return registration;
		}
	}
}
