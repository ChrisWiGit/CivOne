using System.Text;

namespace CivTranslateInteractive;

public interface IValuesFileRepository
{
	void Write(string filePath, IReadOnlyList<string> values);
	IReadOnlyList<string> Read(string filePath);
}

public sealed class ValuesFileRepository : IValuesFileRepository
{
	public void Write(string filePath, IReadOnlyList<string> values)
	{
		string? directoryPath = Path.GetDirectoryName(filePath);
		if (!string.IsNullOrWhiteSpace(directoryPath))
		{
			Directory.CreateDirectory(directoryPath);
		}

		File.WriteAllLines(filePath, values, new UTF8Encoding(encoderShouldEmitUTF8Identifier: false));
	}

	public IReadOnlyList<string> Read(string filePath)
	{
		if (!File.Exists(filePath))
		{
			throw new FileNotFoundException("Values file not found.", filePath);
		}

		return File.ReadAllLines(filePath, Encoding.UTF8);
	}
}
