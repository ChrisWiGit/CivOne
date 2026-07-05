// CivOne
//
// To the extent possible under law, the person who associated CC0 with
// CivOne has waived all copyright and related or neighboring rights
// to CivOne.
//
// You should have received a copy of the CC0 legalcode along with this
// work. If not, see <http://creativecommons.org/publicdomain/zero/1.0/>.

using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using CivOne.Events;
using CivOne.Tasks;

namespace CivOne
{
	public abstract class GameTask : BaseInstance
	{
		private static GameTask? _currentTask;
		private static readonly List<GameTask> _tasks = [];

		public static bool Any() => _tasks.Count > 0;
		public static bool Is<T>() where T : GameTask => _currentTask != null && _currentTask is T;
		public static bool Fast => Common.HasAttribute<FastAttribute>(_currentTask);
		public static int Count<T>() where T : GameTask => _tasks.Count(t => t is T);
		
		internal static void ClearAll()
		{
			_currentTask = null;
			_tasks.Clear();
		}

        public static int HowMany() => _tasks.Count;
		public static GameTask? Current() => _currentTask;

		private static void NextTask()
		{
			_currentTask = _tasks[0];
			TaskEventArgs eventArgs = new();
			Started?.Invoke(_currentTask, eventArgs);
			if (eventArgs.Aborted)
				_currentTask.EndTask();
			else
				_currentTask.Run();
		}
		
		public static bool Update()
		{
			if (_currentTask != null)
				return _currentTask.NextStep();
			else if (_tasks.Count == 0)
				return false;
			
			NextTask();
			return true;
		}

		public static void Enqueue(GameTask? task)
		{
			if (task == null) return;
			task.Done += Finish;
			_tasks.Add(task);
		}

		public static void Insert(GameTask? task)
		{
			if (task == null) return;
			task.Done += Finish;
			_tasks.Insert(0, task);
		}

		private static void Finish(object? sender, EventArgs args)
		{
			ArgumentNullException.ThrowIfNull(sender);
			Debug.Assert(sender is GameTask, "Sender of GameTask.Done event is not a GameTask");

			if (sender is not GameTask)
			{
				return;
			}

			GameTask task = (sender as GameTask)!; // only to silence the compiler warning: possible null reference.

			_ = _tasks.Remove(task);
			if (_tasks.Count == 0)
			{
				_currentTask = null;
				return;
			}

			NextTask();
		}

		public static event EventHandler<TaskEventArgs>? Started;
		public event EventHandler? Done;

		protected virtual bool NextStep() => false;

		public abstract void Run();

		protected void EndTask()
		{
			if (Done == null) return;
			Done(this, EventArgs.Empty);
			Done = null;
		}
	}
}