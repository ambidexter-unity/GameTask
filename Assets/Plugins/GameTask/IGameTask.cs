using UniRx;

namespace Common.GameTask
{
	/// <summary>
	/// Задача.
	/// </summary>
	public interface IGameTask
	{
		/// <summary>
		/// Запуск задачи на исполнение.
		/// </summary>
		void Start();

		/// <summary>
		/// Сигнал о завершении задачи.
		/// </summary>
		IReadOnlyReactiveProperty<bool> Complete { get; }
	}
}