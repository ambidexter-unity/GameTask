using System;
using System.Collections.Generic;
using UniRx;
using UnityEngine;
using UnityEngine.Assertions;

namespace Common.GameTask
{
	/// <inheritdoc cref="IGameTask" />
	/// <summary>
	/// Очередь задач.
	/// </summary>
	public class GameTaskQueue : IGameTask, IDisposable
	{
		private readonly Queue<IGameTask> _queue = new Queue<IGameTask>();
		private readonly BoolReactiveProperty _complete = new BoolReactiveProperty(false);

		private IGameTask _currentGameTask;
		private IDisposable _currentTaskCompleteHandler;

		private bool _isDisposed;

		// ITask

		public void Start()
		{
			if (_currentGameTask != null || _complete.Value || _isDisposed) return;
			StartNextTask();
		}

		public IReadOnlyReactiveProperty<bool> Complete => _complete;

		// \ITask

		// IDisposable

		public void Dispose()
		{
			if (_isDisposed) return;
			_isDisposed = true;

			foreach (var task in _queue)
			{
				(task as IDisposable)?.Dispose();
			}

			
			(_currentGameTask as IDisposable)?.Dispose();
			
			Clear();
			
			_complete.Dispose();
		}

		// \IDisposable

		/// <summary>
		/// Очистить очередь.
		/// </summary>
		public void Clear()
		{
			if (_isDisposed) return;

			_queue.Clear();
			_currentGameTask = null;
			
			_currentTaskCompleteHandler?.Dispose();
			_currentTaskCompleteHandler = null;
		}

		/// <summary>
		/// Добавить задачу в очередь.
		/// </summary>
		/// <param name="gameTask">Добавляемая задача.</param>
		public void Add(IGameTask gameTask)
		{
			if (_isDisposed) return;

			Assert.IsFalse(_complete.Value);
			_queue.Enqueue(gameTask);
		}

		private void StartNextTask()
		{
			Assert.IsNull(_currentTaskCompleteHandler);

			if (_queue.Count <= 0)
			{
				_currentGameTask = null;
				_complete.SetValueAndForceNotify(true);
				return;
			}

			_currentGameTask = _queue.Dequeue();
			if (_currentGameTask.Complete.Value)
			{
				Debug.LogWarning("Task in queue already completed.");
				// ReSharper disable once TailRecursiveCall
				StartNextTask();
				return;
			}

			_currentTaskCompleteHandler = _currentGameTask.Complete.Subscribe(value =>
			{
				if (!value) return;
				// ReSharper disable once AccessToModifiedClosure
				_currentTaskCompleteHandler?.Dispose();
				_currentTaskCompleteHandler = null;
				StartNextTask();
			});
			_currentGameTask.Start();
		}
	}
}