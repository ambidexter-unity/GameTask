using System;
using System.Collections.Generic;
using System.Linq;
using UniRx;
using UnityEngine;
using UnityEngine.Assertions;

namespace Common.GameTask
{
	/// <inheritdoc cref="IGameTask" />
	/// <summary>
	/// Параллельное выполнение задач.
	/// </summary>
	public class GameTaskConcurent : IGameTask, IDisposable
	{
		private readonly List<IGameTask> _tasks = new List<IGameTask>();
		private readonly BoolReactiveProperty _complete = new BoolReactiveProperty(false);

		private readonly Dictionary<IDisposable, IGameTask>
			_completeHandlers = new Dictionary<IDisposable, IGameTask>();

		private bool _isDisposed;

		// ITask

		public void Start()
		{
			if (_completeHandlers.Any() || Complete.Value || _isDisposed) return;

			if (_tasks.Count > 0)
			{
				_tasks.ForEach(SubscribeComplete);
				_tasks.ForEach(task => task.Start());
				_tasks.Clear();
			}
			else
			{
				_complete.SetValueAndForceNotify(true);
			}
		}

		public IReadOnlyReactiveProperty<bool> Complete => _complete;

		// \ITask

		// IDisposable

		public void Dispose()
		{
			if (_isDisposed) return;
			_isDisposed = true;

			_tasks.ForEach(task => (task as IDisposable)?.Dispose());
			foreach (var pair in _completeHandlers)
			{
				(pair.Value as IDisposable)?.Dispose();
			}

			Clear();

			_complete.Dispose();
		}

		// \IDisposable

		/// <summary>
		/// Очистить параллельное выполнение.
		/// </summary>
		public void Clear()
		{
			if (_isDisposed) return;

			foreach (var pair in _completeHandlers)
			{
				pair.Key.Dispose();
			}

			_tasks.Clear();
			_completeHandlers.Clear();
		}

		/// <summary>
		/// Добавить задачу в параллельное выполнение.
		/// </summary>
		/// <param name="gameTask">Добавляемая задача.</param>
		/// <exception cref="Exception">Параллельное выполнение уже запущено.</exception>
		public void Add(IGameTask gameTask)
		{
			if (_isDisposed) return;

			Assert.IsFalse(Complete.Value);
			if (_completeHandlers.Any()) throw new Exception("Concurent already executed.");
			_tasks.Add(gameTask);
		}

		private void SubscribeComplete(IGameTask gameTask)
		{
			if (gameTask.Complete.Value)
			{
				Debug.LogWarning("Task in concurent already completed.");
				return;
			}

			IDisposable d = null;
			d = gameTask.Complete.Subscribe(value =>
			{
				if (!value) return;

				// ReSharper disable AccessToModifiedClosure, AssignNullToNotNullAttribute
				_completeHandlers.Remove(d);
				d.Dispose();
				// ReSharper enable AccessToModifiedClosure, AssignNullToNotNullAttribute

				if (!_completeHandlers.Any())
				{
					_complete.SetValueAndForceNotify(true);
				}
			});

			_completeHandlers.Add(d, gameTask);
		}
	}
}