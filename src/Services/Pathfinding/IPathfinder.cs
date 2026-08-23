using System.Drawing;
using CivOne.Units;

namespace CivOne.Services.Pathfinding
{
	internal interface IPathfinder
	{
		PathStepResult GetNextStep(IUnit unit, Point destination);
	}
}