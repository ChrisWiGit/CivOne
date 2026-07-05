using System;
using System.Collections.Generic;
using System.Text;
using System.Text.Json;

namespace CivOne.Mcp.Tools
{
	public sealed class McpGameStatePathResolver
	{
		private enum SegmentKind
		{
			Property,
			Index
		}

		private readonly struct PathSegment(SegmentKind kind, string? propertyName, int index)
		{
			public SegmentKind Kind { get; } = kind;
			public string? PropertyName { get; } = propertyName;
			public int Index { get; } = index;
			public string DisplayText => Kind == SegmentKind.Property ? PropertyName ?? string.Empty : $"[{Index}]";
		}

		public static bool TryResolve(JsonElement root, string path, out JsonElement resolved, out string? failedSegment, out string? errorMessage)
		{
			resolved = default;
			failedSegment = null;

			if (string.IsNullOrWhiteSpace(path))
			{
				errorMessage = "Path is required.";
				return false;
			}

			if (!TryParsePath(path, out IReadOnlyList<PathSegment> segments, out failedSegment, out errorMessage))
				return false;

			JsonElement current = root;
			foreach (PathSegment segment in segments)
			{
				if (segment.Kind == SegmentKind.Property)
				{
					if (current.ValueKind != JsonValueKind.Object)
					{
						failedSegment = segment.DisplayText;
						errorMessage = "Current node is not an object.";
						return false;
					}

					if (!TryGetPropertyIgnoreCase(current, segment.PropertyName, out JsonElement propertyValue))
					{
						failedSegment = segment.DisplayText;
						errorMessage = "Property does not exist.";
						return false;
					}

					current = propertyValue;
					continue;
				}

				if (current.ValueKind != JsonValueKind.Array)
				{
					failedSegment = segment.DisplayText;
					errorMessage = "Current node is not an array.";
					return false;
				}

				if (segment.Index < 0 || segment.Index >= current.GetArrayLength())
				{
					failedSegment = segment.DisplayText;
					errorMessage = "Array index out of range.";
					return false;
				}

				current = current[segment.Index];
			}

			resolved = current;
			return true;
		}

		private static bool TryParsePath(string path, out IReadOnlyList<PathSegment> segments, out string? failedSegment, out string? errorMessage)
		{
			segments = [];
			failedSegment = null;
			errorMessage = null;

			List<PathSegment> parsedSegments = [];

			int i = 0;
			while (i < path.Length)
			{
				char current = path[i];

				if (current == '.')
				{
					i++;
					continue;
				}

				if (current == '[')
				{
					int endBracket = path.IndexOf(']', i + 1);
					if (endBracket < 0)
					{
						failedSegment = path[i..];
						errorMessage = "Missing closing bracket.";
						return false;
					}

					string indexText = path[(i + 1)..endBracket];
					if (!int.TryParse(indexText, out int index))
					{
						failedSegment = $"[{indexText}]";
						errorMessage = "Array index must be an integer.";
						return false;
					}

					parsedSegments.Add(new PathSegment(SegmentKind.Index, null, index));
					i = endBracket + 1;
					continue;
				}

				int start = i;
				while (i < path.Length && path[i] != '.' && path[i] != '[')
					i++;

				string property = path[start..i];
				if (string.IsNullOrWhiteSpace(property))
				{
					failedSegment = ".";
					errorMessage = "Invalid empty path segment.";
					return false;
				}

				parsedSegments.Add(new PathSegment(SegmentKind.Property, property, -1));
			}

			if (parsedSegments.Count == 0)
			{
				failedSegment = path;
				errorMessage = "Path is required.";
				return false;
			}

			segments = parsedSegments;
			return true;
		}

		private static bool TryGetPropertyIgnoreCase(JsonElement element, string? propertyName, out JsonElement propertyValue)
		{
			if (propertyName == null)
			{
				propertyValue = default;
				return false;
			}
			foreach (JsonProperty property in element.EnumerateObject())
			{
				if (string.Equals(property.Name, propertyName, StringComparison.OrdinalIgnoreCase))
				{
					propertyValue = property.Value;
					return true;
				}
			}

			propertyValue = default;
			return false;
		}
	}
}
