using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Text;
using System.Windows;
using System.Windows.Input;

namespace Quala.Windows.Mvvm
{
	// MVVM Toolkitベース + MVVMInfra(kazukiのブログ) + カスタマイズ
	// MVVMInfraのプロパティに知能を持たせるというアイディアは見事すぎる…。
	//
	public class ViewModel : IEditableObject, INotifyPropertyChanged
	{
		/// <summary>
		/// ViewModelPropertyを保持する。
		/// </summary>
		IList<IViewModelProperty> _properties = new List<IViewModelProperty>();

		/// <summary>
		/// 保持しているViewModelPropertyがすべて妥当か検証する。
		/// 妥当ならtrue。
		/// </summary>
		public virtual bool IsValid
		{
			get
			{
				foreach (var property in _properties)
				{
					if (property.IsValid == false)
						return false;
				}
				return true;
			}
		}

		#region IEditableObject

		bool _isEditing;

		/// <summary>
		/// 編集中ならtrue
		/// </summary>
		public bool IsEditing
		{
			get { return _isEditing; }
			private set
			{
				if (_isEditing == value) return;
				_isEditing = value;
				OnPropertyChanged("IsEditing");
				OnPropertyChanged("IsNotEditing");
			}
		}

		/// <summary>
		/// 編集中では無いならtrue</summary>
		/// <remarks>
		/// たとえばTextBox.IsReadOnly等とバインディングするとGood。</remarks>
		public bool IsNotEditing
		{
			get { return (!IsEditing); }
		}

		/// <summary>
		/// 編集を開始
		/// </summary>
		public void BeginEdit()
		{
			if (!IsEditing)
			{
				IsEditing = true;
				foreach (var property in _properties)
				{
					property.BeginEdit();
				}
			}
		}

		/// <summary>
		/// 編集をキャンセル
		/// </summary>
		public void CancelEdit()
		{
			if (IsEditing)
			{
				IsEditing = false;
				foreach (var property in _properties)
				{
					property.CancelEdit();
				}
			}
		}

		/// <summary>
		/// 編集を完了
		/// </summary>
		public void EndEdit()
		{
			if (IsEditing)
			{
				IsEditing = false;
				foreach (var property in _properties)
				{
					property.EndEdit();
				}
			}
		}
	
		#endregion
		#region INotifyPropertyChanged

		public event PropertyChangedEventHandler PropertyChanged;

		protected virtual void OnPropertyChanged(string name)
		{
			var h = PropertyChanged;
			if (h == null) return;

			h(this, new PropertyChangedEventArgs(name));
		}

		#endregion

		#region RegisterViewModelProperty & (ViewModelProperty実装)

		/// <summary>
		/// 値変換と値検証ロジックを指定して<br />
		/// ViewModelPropertyを作成しViewModelに登録する。</summary>
		/// <typeparam name="T">
		/// ViewModelProperty.Valueの型</typeparam>
		/// <param name="getViewModelValueAction">
		/// ModelからViewModel(T)用に値を変換するロジック</param>
		/// <param name="setModelValueAction">
		/// ViewModel(T)からModel用に値を変換しModelに格納するロジック</param>
		/// <param name="validateViewModelValueAction">
		/// ViewModelの値を検証するためのロジック<br />
		/// OKの場合null(or string.Empty)を返して、NGの場合にはエラーメッセージを返す</param>
		/// <returns></returns>
		protected ViewModelProperty<T> RegisterViewModelProperty<T>(
			Func<T> getViewModelValueAction,
			Action<T> setModelValueAction,
			Func<T, string> validateViewModelAction)
		{
			var property = new DefaultViewModelProperty<T>(
				getViewModelValueAction,
				setModelValueAction,
				validateViewModelAction);
			_properties.Add(property);
			return property;
		}

		/// <summary>
		/// 値変換ロジックを指定して<br />
		/// ViewModelPropertyを作成しViewModelに登録する。</summary>
		/// <typeparam name="T">
		/// ViewModelProperty.Valueの型</typeparam>
		/// <param name="getViewModelValueAction">
		/// ModelからViewModel(T)用に値を変換するロジック</param>
		/// <param name="setModelValueAction">
		/// ViewModel(T)からModel用に値を変換しModelに格納するロジック</param>
		/// <returns></returns>
		protected ViewModelProperty<T> RegisterViewModelProperty<T>(
			Func<T> getViewModelValueAction,
			Action<T> setModelValueAction)
		{
			return RegisterViewModelProperty(
				getViewModelValueAction,
				setModelValueAction,
				value => null);
		}

		/// <summary>
		/// 値検証ロジックを指定して<br />
		/// ViewModelPropertyを作成しViewModelに登録する。</summary>
		/// <typeparam name="T">
		/// ViewModelProperty.Valueの型</typeparam>
		/// <param name="validateViewModelValueAction">
		/// ViewModelの値を検証するためのロジック<br />
		/// OKの場合null(or string.Empty)を返して、NGの場合にはエラーメッセージを返す</param>
		/// <returns></returns>
		protected ViewModelProperty<T> RegisterViewModelProperty<T>(
			Func<T, string> validateViewModelAction)
		{
			var property = new SimpleViewModelProperty<T>(validateViewModelAction);
			_properties.Add(property);
			return property;
		}

		/// <summary>
		/// ViewModelPropertyを作成しViewModelに登録する。</summary>
		/// <typeparam name="T">
		/// ViewModelProperty.Valueの型</typeparam>
		/// <returns></returns>
		protected ViewModelProperty<T> RegisterViewModelProperty<T>()
		{
			return RegisterViewModelProperty<T>(_ => null);
		}

		/// <summary>
		/// ViewModelPropertyを作成しViewModelに登録する。</summary>
		/// <typeparam name="T">
		/// ViewModelProperty.Valueの型</typeparam>
		/// <returns></returns>
		protected ViewModelProperty<T> RegisterViewModelProperty<T>(T value)
		{
			var p = RegisterViewModelProperty<T>(_ => null);
			p.Value = value;
			return p;
		}

		#region ViewModelProperty Implements

		/// <summary>
		/// 検証ロジックのみ指定可能なViewModelProperty
		/// </summary>
		/// <typeparam name="T"></typeparam>
		sealed class SimpleViewModelProperty<T> : ViewModelProperty<T>
		{
			Func<T, string> _validateAction;

			public SimpleViewModelProperty(Func<T, string> validateAction)
			{
				_validateAction = validateAction;
				this.Error = _validateAction(this.Value);
			}

			private bool _isTx;
			private T _value;
			private T _backup;
			public override T Value
			{
				get
				{
					return _value;
				}
				set
				{
					if (Equals(_value, value)) return;

					_value = value;
					this.Error = _validateAction(_value);
					OnPropertyChanged("Value");
				}
			}

			public override void BeginEdit()
			{
				_isTx = true;
				_backup = _value;
			}

			public override void CancelEdit()
			{
				if (_isTx)
				{
					this.Value = _backup;
					_isTx = false;
				}
			}

			public override void EndEdit()
			{
				_isTx = false;
			}
		}

		/// <summary>
		/// Model←→ViewModel間のデータ変換と検証ロジックが指定可能なViewModelProperty
		/// </summary>
		/// <typeparam name="T"></typeparam>
		sealed class DefaultViewModelProperty<T> : ViewModelProperty<T>
		{
			#region 外部から指定するアクション

			/// <summary>
			/// Model -> ViewModelへの値の変換を行う
			/// </summary>
			private Func<T> _getViewModelValueAction;

			/// <summary>
			/// ViewModel -> Modelへの値の変換を行う
			/// </summary>
			private Action<T> _setModelValueAction;

			/// <summary>
			/// ViewModelの値の検証を行う。
			/// エラーがない場合はnullを返し、エラーがある場合は
			/// エラーメッセージを返す。
			/// </summary>
			private Func<T, string> _validateViewModelAction;
			#endregion

			public DefaultViewModelProperty(
				Func<T> getViewModelValueAction,
				Action<T> setModelValueAction,
				Func<T, string> validateViewModelAction)
			{
				_getViewModelValueAction = getViewModelValueAction;
				_setModelValueAction = setModelValueAction;
				_validateViewModelAction = validateViewModelAction;

				this.Value = _getViewModelValueAction();
			}

			bool _isTx;
			T _value;

			/// <summary>
			/// ViewModelで保持する値
			/// </summary>
			public override T Value
			{
				get
				{
					return _value;
				}
				set
				{
					_value = value;
					OnPropertyChanged("Value");
					this.Error = _validateViewModelAction(value);
					// 未編集中でデータの検証が済んでいればModelに書き戻す
					if (!_isTx && IsValid)
					{
						_setModelValueAction(this.Value);
					}
				}
			}

			public override void BeginEdit()
			{
				_isTx = true;
			}

			public override void CancelEdit()
			{
				if (_isTx)
				{
					this.Value = _getViewModelValueAction();
					_isTx = false;
				}
			}

			public override void EndEdit()
			{
				_isTx = false;
				if (IsValid)
				{
					_setModelValueAction(this.Value);
				}
			}
		}


		#endregion

		#endregion
	}
}
