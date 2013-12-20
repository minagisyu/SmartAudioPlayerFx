using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.ComponentModel;

namespace Quala.Windows.Mvvm
{
	// MVVMInfraより。
	/// <summary>
	/// ViewModelのプロパティの持つべきインターフェースを定義する
	/// </summary>
	public interface IViewModelProperty : IEditableObject, INotifyPropertyChanged, IDataErrorInfo
	{
		/// <summary>
		/// プロパティの値が妥当かどうかを取得する。<br/>
		/// 妥当な場合trueが返る。
		/// </summary>
		bool IsValid { get; }
	}

	public abstract class ViewModelProperty<T> : IViewModelProperty
	{
		/// <summary>
		/// プロパティの値を取得または設定する
		/// </summary>
		public abstract T Value { get; set; }

		public override string ToString()
		{
			return Value == null ? "" : Value.ToString();
		}

		#region IViewModelProperty

		public bool IsValid
		{
			get { return string.IsNullOrEmpty(this.Error); }
		}

		#endregion
		#region INotifyPropertyChanged

		public event PropertyChangedEventHandler PropertyChanged;

		/// <summary>
		/// 引数で指定したプロパティ名の変更イベントを発行する。
		/// </summary>
		/// <param name="propertyNames">変更イベントを発行するプロパティ名</param>
		protected virtual void OnPropertyChanged(params string[] propertyNames)
		{
			var h = PropertyChanged;
			if (h == null) return;
			foreach (var propertyName in propertyNames)
			{
				h(this, new PropertyChangedEventArgs(propertyName));
			}
		}

		#endregion
		#region IDataErrorInfo

		string _error;

		public string Error
		{
			get { return _error; }
			set
			{
				if (Equals(_error, value)) return;

				_error = value;
				OnPropertyChanged("Error");
			}
		}

		public string this[string columnName]
		{
			get
			{
				return Equals("Value", columnName) ?
					this.Error : null;
			}
		}

		#endregion
		#region IEditableObject

		public abstract void BeginEdit();

		public abstract void CancelEdit();

		public abstract void EndEdit();

		#endregion

	}
}
