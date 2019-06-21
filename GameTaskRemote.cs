using System;
using UniRx;

namespace Common.GameTask
{
	/// <inheritdoc />
	/// <summary>
	/// Отложенная задача. Принимает замыкание, которое будет вызвано в момент старта задачи.
	/// </summary>
	public class GameTaskRemote : IGameTask
	{
		private readonly Func<IGameTask> _closure;
		private readonly BoolReactiveProperty _complete = new BoolReactiveProperty(false);

		private IDisposable _completeHandler;
		private IGameTask _gameTask;

		public GameTaskRemote(Func<IGameTask> closure)
		{
			_closure = closure;
		}

		// ITask

		public void Start()
		{
			if (_gameTask != null) return;
			_gameTask = _closure.Invoke();
			_completeHandler = _gameTask.Complete.Subscribe(value =>
			{
				_complete.SetValueAndForceNotify(value);
				if (!value) return;
				_completeHandler.Dispose();
				_completeHandler = null;
			});
			_gameTask.Start();
		}

		IReadOnlyReactiveProperty<bool> IGameTask.Complete => _complete;

		// \ITask
	}
}