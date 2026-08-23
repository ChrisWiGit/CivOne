using System;

namespace CivOne.Services.Pathfinding
{
	internal enum PathStepStatus
	{
		Success,
		NoPath,
		Disabled,
		InvalidRequest
	}

	internal readonly record struct PathStepResult(PathStepStatus Status, int NextX, int NextY)
	{
		public bool HasStep => Status == PathStepStatus.Success;

		public static PathStepResult Success(int nextX, int nextY) =>
			new(PathStepStatus.Success, nextX, nextY);

		public static PathStepResult NoPath() =>
			new(PathStepStatus.NoPath, -1, -1);

		public static PathStepResult Disabled() =>
			new(PathStepStatus.Disabled, -1, -1);

		public static PathStepResult InvalidRequest() =>
			new(PathStepStatus.InvalidRequest, -1, -1);
	}
}