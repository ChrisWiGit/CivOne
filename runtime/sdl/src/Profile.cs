// CivOne
//
// To the extent possible under law, the person who associated CC0 with
// CivOne has waived all copyright and related or neighboring rights
// to CivOne.
//
// You should have received a copy of the CC0 legalcode along with this
// work. If not, see <http://creativecommons.org/publicdomain/zero/1.0/>.

using System;
using System.Collections.Concurrent;
using System.IO;
using System.Text;
using System.Xml;
using System.Xml.Linq;
using CivOne.Services;

namespace CivOne
{
	internal sealed class Profile
	{
		private const string ROOT_ELEMENT = "CivOneProfile";

		private readonly IRuntime _runtime;
		private readonly string _name;
		private readonly string _filename;

		private void Log(string text, params object[] parameters) => _runtime.Log(text, parameters);

		private static XmlWriter CreateXmlWriter(Stream stream)
		{
			XmlWriter xw = XmlWriter.Create(stream, new XmlWriterSettings()
			{
				Indent = true,
				IndentChars = "\t",
				NewLineChars = "\n"
			});
			return xw;
		}

		private void CreateProfile()
		{
			if (File.Exists(_filename))
			{
				Log($"Recreating profile {_name}");
				File.Delete(_filename);
			}

			string? profileDirectory = Path.GetDirectoryName(_filename);
			if (!string.IsNullOrEmpty(profileDirectory))
			{
				Directory.CreateDirectory(profileDirectory);
			}

			using FileStream fs = new(_filename, FileMode.Create, FileAccess.Write);
			using XmlWriter xw = CreateXmlWriter(fs);

			XDocument xDoc = new();
			xDoc.Add(new XElement(ROOT_ELEMENT));
			xDoc.Save(xw);
		}

		public string? GetSetting(string key)
		{
			if (!File.Exists(_filename)) CreateProfile();

			using FileStream fs = new(_filename, FileMode.Open, FileAccess.Read, FileShare.Read);
			XDocument xDoc = XDocument.Load(fs);
			XElement? xRoot;
			if ((xRoot = xDoc.Element(ROOT_ELEMENT)) == null)
			{
				Log($"Profile {_name} error: Root element missing");
				CreateProfile();
				// Re-read after recreation to return the value instead of null
				using FileStream fs2 = new(_filename, FileMode.Open, FileAccess.Read, FileShare.Read);
				XDocument xDoc2 = XDocument.Load(fs2);
				xRoot = xDoc2.Element(ROOT_ELEMENT);
			}

			return xRoot?.Element(key)?.Value;
		}

		private readonly AtomicFileReplacementService _atomicFileReplacementService = new ();

		public void SetSetting(string key, string value)
		{
			if (!File.Exists(_filename)) CreateProfile();

			XDocument xDoc;
			XElement? xRoot, xElement;
			using (FileStream fs = new(_filename, FileMode.Open, FileAccess.Read, FileShare.Read))
			{
				xDoc = XDocument.Load(fs);
				if ((xRoot = xDoc.Element(ROOT_ELEMENT)) == null)
				{
					Log($"Profile {_name} error: Root element missing");
					fs.Close();
					CreateProfile();
					SetSetting(key, value);
					return;
				}
			}

			xElement = xRoot.Element(key);
			if (xElement == null)
			{
				xElement = new XElement(key);
				xRoot.Add(xElement);
			}
			xElement.Value = value;

			string? profileDirectory = Path.GetDirectoryName(_filename);
			if (!string.IsNullOrEmpty(profileDirectory))
			{
				Directory.CreateDirectory(profileDirectory);
			}

			// Atomic write: prevents profile corruption on crash/power-loss mid-write.
			_atomicFileReplacementService.ReplaceFile(_filename, stream =>
			{
				using XmlWriter xw = CreateXmlWriter(stream);
				xDoc.Save(xw);
			});
		}

		// Thread-safe profile cache: prevents double-add ArgumentException under concurrent Get()
		// from MCP/render threads, and uses OrdinalIgnoreCase to avoid culture-dependent ToLower (ü, türk. I, ...).
		private static readonly ConcurrentDictionary<string, Profile> _profiles = new(StringComparer.OrdinalIgnoreCase);
		
		public static Profile Get(Runtime runtime, string name = "default") =>
			_profiles.GetOrAdd(name, n => new Profile(runtime, n));

		private Profile(IRuntime runtime, string name)
		{
			_runtime = runtime;
			_name = name;
			_filename = Path.Combine(runtime.StorageDirectory, $"{name}.profile");
		}
	}
}