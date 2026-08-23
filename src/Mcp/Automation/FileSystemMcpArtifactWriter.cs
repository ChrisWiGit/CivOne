using System;
using System.IO;

namespace CivOne.Mcp.Automation
{
	public sealed class FileSystemMcpArtifactWriter : IMcpArtifactWriter
	{
		private readonly string _rootFolder;

		public string WriteArtifact(string sessionId, string extension, byte[] bytes)
		{
			if (bytes == null || bytes.Length == 0)
				throw new ArgumentException("Artifact bytes cannot be empty.", nameof(bytes));

			sessionId = string.IsNullOrWhiteSpace(sessionId) ? "default" : sessionId;
			extension = string.IsNullOrWhiteSpace(extension) ? "bin" : extension.TrimStart('.');

			string folder = Path.Combine(_rootFolder, sessionId);
			Directory.CreateDirectory(folder);

			string fileName = $"capture-{DateTime.UtcNow:yyyyMMdd-HHmmss-fff}.{extension}";
			string outputPath = Path.Combine(folder, fileName);
			File.WriteAllBytes(outputPath, bytes);
			return outputPath;
		}

		public FileSystemMcpArtifactWriter(string rootFolder)
		{
			if (string.IsNullOrWhiteSpace(rootFolder))
				throw new ArgumentException("Value cannot be null or whitespace.", nameof(rootFolder));
			_rootFolder = rootFolder;
		}
	}
}
