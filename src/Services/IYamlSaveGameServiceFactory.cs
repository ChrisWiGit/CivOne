using System;

namespace CivOne.Services
{
	public interface IYamlSaveGameServiceFactory
	{
		IYamlSaveGameService Create(Game game);
	}
}