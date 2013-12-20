using System;
using System.ComponentModel;
using System.Linq.Expressions;

namespace Quala
{
	partial class Extension
	{
		/// <summary>
		/// handlerがnull以外ならhandlerを実行します。
		/// Invoke()された場合はtrueが返ります。
		/// </summary>
		/// <param name="handler"></param>
		/// <param name="sender"></param>
		/// <param name="e"></param>
		/// <returns>Invoke()された場合はtrueが返ります。</returns>
		public static bool InvokeOrIgnore(this PropertyChangedEventHandler handler, object sender, PropertyChangedEventArgs e)
		{
			if(handler != null)
			{
				handler(sender, e);
				return true;
			}
			return false;
		}

		/// <summary>
		/// valueとnewValueが異なる場合にvalueにnewValueを代入し、handlerを呼び出します。
		/// 値はobject.Equals(value, newValue)で比較されます。
		/// handlerがnullの場合は呼び出されません。
		/// 値に変化があった場合はtrueが返ります。
		/// </summary>
		/// <typeparam name="T"></typeparam>
		/// <param name="handler"></param>
		/// <param name="value"></param>
		/// <param name="newValue"></param>
		/// <param name="sender"></param>
		/// <param name="e"></param>
		/// <returns>値に変化があった場合はtrueが返ります。</returns>
		public static bool CheckAndInvokeOrIgnore<T>(this PropertyChangedEventHandler handler, object sender, PropertyChangedEventArgs e, ref T value, T newValue)
		{
			if(object.Equals(value, newValue) == false)
			{
				value = newValue;
				if(handler != null)
				{
					handler(sender, e);
				}
				return true;
			}
			return false;
		}

		/// <summary>
		/// 複数のPropertyChangedEventArgsを一括で。
		/// </summary>
		/// <param name="handler"></param>
		/// <param name="sender"></param>
		/// <param name="e"></param>
		public static void MultiInvoke(this PropertyChangedEventHandler handler, object sender, params PropertyChangedEventArgs[] e)
		{
			if(handler == null) return;

			foreach(var ev in e)
			{
				handler(sender, ev);
			}
		}

		/// <summary>
		/// p.AddPropertyChanged(o => o.Name, NameChanged) な感じで使う
		/// </summary>
		/// <typeparam name="TObj"></typeparam>
		/// <typeparam name="TProp"></typeparam>
		/// <param name="_this"></param>
		/// <param name="propertyName"></param>
		/// <param name="handler"></param>
 
		public static void AddPropertyChanged<TObj, TProp>(this TObj _this,
			Expression<Func<TObj, TProp>> propertyName, Action<TObj> handler)
			where TObj : INotifyPropertyChanged
		{
			// プロパティ名を取得して
			var name = ((MemberExpression)propertyName.Body).Member.Name;
			// 引数で指定されたプロパティ名と同じだったら、handlerを実行するように
			// PropertyChangedイベントに登録する
			_this.PropertyChanged += (_, e) =>
			{
				if (e.PropertyName == name)
				{
					handler(_this);
				}
			};
		}

	}
}
