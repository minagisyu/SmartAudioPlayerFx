using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Dynamic;
using System.Linq;
using System.Reflection;

namespace Quala.ComponentModel
{
	/// <summary>
	/// INotifyPropertyChangedラッパー+プロパティ単体の変更通知取得ラッパー。<br />
	/// Tのパブリックプロパティと「プロパティ名Changed」という名前のイベントもどきを動的に作り出す。<br />
	/// _Valueでインスタンスに直接アクセスできます。(readonly)
	/// </summary>
	/// <remarks>
	/// class Foo { public string Hoge { get; set; } }
	///          ↓
	/// NotifyWrapper&lt;Foo&gt;.Name property (get/set)
	/// NotifyWrapper&lt;Foo&gt;.NameChanged delegate (+=/-=)
	/// ((INotifyPropertyChanged)NotifyWrapper&lt;Foo&gt;).PropertyChanged event (+=/-=)
	/// </remarks>
	/// <typeparam name="T"></typeparam>
	public class NotifyWrapper<T> : DynamicObject, INotifyPropertyChanged
	{
		public readonly T _Value;
		Dictionary<string, Tuple<PropertyInfo, bool>> properties;	// <name, <info, has_setter>>
		Dictionary<string, EventHandler> events;					// <name, delegate>
		public event PropertyChangedEventHandler PropertyChanged;

		public NotifyWrapper() : this(Activator.CreateInstance<T>()) { }
		public NotifyWrapper(T instance)
		{
			_Value = instance;
			properties = typeof(T).GetProperties()
				.ToDictionary(i => i.Name, i => new Tuple<PropertyInfo, bool>(i, i.GetSetMethod() != null));
			// プロパティ名Changed という名前のイベントを自動生成
			events = properties
				.Where(i => i.Value.Item2)
				.ToDictionary(i => i.Key + "Changed", i => new EventHandler(delegate { })); // チェック面倒なので空のデリゲートを...
		}

		public override bool TryGetMember(GetMemberBinder binder, out object result)
		{
			Tuple<PropertyInfo, bool> pi;
			EventHandler ev;
			result =
				properties.TryGetValue(binder.Name, out pi) ? pi.Item1.GetValue(_Value, null) :
				events.TryGetValue(binder.Name, out ev) ? ev :
				null;
			return (result != null);
		}

		public override bool TrySetMember(SetMemberBinder binder, object value)
		{
			Tuple<PropertyInfo, bool> pi;
			if (properties.TryGetValue(binder.Name, out pi))
			{
				if (pi.Item2 == false)
					return false;
				pi.Item1.SetValue(_Value, value, null);
				events[binder.Name + "Changed"](_Value, EventArgs.Empty);
				if (PropertyChanged != null)
					PropertyChanged(_Value, new PropertyChangedEventArgs(binder.Name));
				return true;
			}
			var ev = value as EventHandler;
			if (ev != null && events.ContainsKey(binder.Name))
			{
				events[binder.Name] = ev;
				return true;
			}
			return false;
		}

		// 必須じゃないけどとりあえず
		public override IEnumerable<string> GetDynamicMemberNames()
		{
			return properties.Select(i => i.Key)
				.Concat(events.Select(i => i.Key))
				.OrderBy(i => i);
		}

	}
}
