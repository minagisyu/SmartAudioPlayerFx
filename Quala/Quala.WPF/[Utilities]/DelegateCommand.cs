using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Windows.Input;

namespace Quala.WPF
{
	public class DelegateCommand<T> : ICommand
	{
		// Fields
		private Func<T, bool> _canExecute;
		private Action<T> _execute;
		private static readonly bool IS_VALUE_TYPE;

		// Events
		public event EventHandler CanExecuteChanged
		{
			add
			{
				CommandManager.RequerySuggested += value;
			}
			remove
			{
				CommandManager.RequerySuggested -= value;
			}
		}

		// Methods
		static DelegateCommand()
		{
			DelegateCommand<T>.IS_VALUE_TYPE = typeof(T).IsValueType;
		}

		public DelegateCommand(Action<T> execute)
			: this(execute, o => true)
		{
		}

		public DelegateCommand(Action<T> execute, Func<T, bool> canExecute)
		{
			this._execute = execute;
			this._canExecute = canExecute;
		}

		public bool CanExecute(T parameter)
		{
			return this._canExecute(parameter);
		}

		private T Cast(object parameter)
		{
			if ((parameter == null) && DelegateCommand<T>.IS_VALUE_TYPE)
			{
				return default(T);
			}
			return (T)parameter;
		}

		public void Execute(T parameter)
		{
			this._execute(parameter);
		}

		bool ICommand.CanExecute(object parameter)
		{
			return this.CanExecute(this.Cast(parameter));
		}

		void ICommand.Execute(object parameter)
		{
			this.Execute(this.Cast(parameter));
		}
	}
}
