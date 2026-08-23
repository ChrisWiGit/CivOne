using System;
using CivOne.Mcp.Contracts;
using CivOne.Persistence;

namespace CivOne.Mcp.Tools
{
	internal static class McpJsonToolResponse
	{
		public static McpResponse JsonResponse(object? id, object payload, JsonSaveGameStateWriter jsonWriter, int maxJsonChars)
		{
			string json = jsonWriter.AsString(payload);
			if (json.Length > maxJsonChars)
				json = BuildTruncatedPayload(json, jsonWriter, maxJsonChars);

			return McpResponse.Success(id, McpToolCallResult.Text(json));
		}

		private static string BuildTruncatedPayload(string sourceJson, JsonSaveGameStateWriter jsonWriter, int maxJsonChars)
		{
			const int reserveChars = 512;
			int previewChars = Math.Max(0, Math.Min(sourceJson.Length, maxJsonChars - reserveChars));
			string preview = sourceJson[..previewChars];

			while (preview.Length > 0)
			{
				string candidate = jsonWriter.AsString(new
				{
					ok = false,
					truncated = true,
					maxChars = maxJsonChars,
					returnedChars = preview.Length,
					totalChars = sourceJson.Length,
					strategy = "head-preview",
					dataPreview = preview,
					error = new { code = "PAYLOAD_TRUNCATED", message = "The payload exceeded the configured size limit and was truncated." }
				});

				if (candidate.Length <= maxJsonChars)
					return candidate;

				preview = preview[..Math.Max(0, preview.Length - Math.Min(256, preview.Length))];
			}

			return jsonWriter.AsString(new
			{
				ok = false,
				truncated = true,
				maxChars = maxJsonChars,
				returnedChars = 0,
				totalChars = sourceJson.Length,
				error = new { code = "PAYLOAD_TRUNCATED", message = "The payload exceeded the configured size limit and was truncated." }
			});
		}
	}
}