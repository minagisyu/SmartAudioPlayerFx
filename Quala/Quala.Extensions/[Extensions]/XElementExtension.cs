using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Linq.Expressions;
using System.Reflection;
using System.Xml.Linq;

namespace Quala.Extensions
{
	public static class XElementExtension
	{
		/// <summary>
		/// 子要素を取得or作成(追加)して返します
		/// </summary>
		/// <param name="element"></param>
		/// <param name="name"></param>
		/// <param name="predicate">
		/// 複数要素がある場合に要素を選択する条件、合致する最後の要素を採用します。
		/// (nullなら無条件)
		/// </param>
		/// <returns></returns>
		public static XElement GetOrCreateElement(this XElement element, XName name, Predicate<XElement> predicate = null)
		{
			var elements = element.Elements(name);
			if (predicate != null)
				elements = elements.Where(i => predicate(i));

			var elm = elements.LastOrDefault();
			if (elm == null)
			{
				elm = new XElement(name);
				element.Add(elm);
			}
			return elm;
		}

		/// <summary>
		/// 属性値を取得し返します。
		/// 属性値が取得出来ない場合はdefaultValueが返ります。
		/// </summary>
		/// <param name="expression">「() => Property」という感じのラムダ式でプロパティやフィールドを指定</param>
		/// <returns></returns>
		public static T GetAttributeValueEx<T>(this XElement element, Expression<Func<T>> expression, T defaultValue = default(T))
		{
			var exp = expression.Body as MemberExpression;
			if (exp == null && expression.Body is UnaryExpression)
				exp = ((UnaryExpression)expression.Body).Operand as MemberExpression;
			if (exp == null)
				throw new NotSupportedException();

			return GetAttributeValueEx<T>(element, exp.Member.Name, defaultValue);
		}
		/// <summary>
		/// 属性値を取得し返します。
		/// 属性値が取得出来ない場合はdefaultValueが返ります。
		/// </summary>
		/// <param name="name">属性名</param>
		/// <returns></returns>
		public static T GetAttributeValueEx<T>(this XElement element, string name, T defaultValue = default(T))
		{
			// 属性＆変換チェック
			var attr = (element != null) ? element.Attribute(name) : null;
			var conv = TypeDescriptor.GetConverter(typeof(T));
			if (attr != null &&
				string.IsNullOrEmpty(attr.Value) == false &&
				conv.CanConvertFrom(typeof(string)))
			{
				// 変換
				var value = (T)conv.ConvertFromString(attr.Value);
				return value;
			}

			return defaultValue;
		}

		public static IEnumerable<T> GetArrayValues<T>(this XElement element, string name, Func<XElement, T> func)
		{
			// チェック
			if (func == null || element == null) return new T[0];

			// 要素チェック
			var elm = element.Element(name);
			if (elm == null) return new T[0];

			return elm.Elements("Item").Select(el => func(el));
		}

		/// <summary>
		/// 属性値を取得し、selecterによる検証を経てプロパティやフィールドに書き込みます。
		/// 属性値が取得できない場合は書き込まれません。
		/// </summary>
		/// <typeparam name="TInstance"></typeparam>
		/// <typeparam name="TValue"></typeparam>
		/// <param name="element"></param>
		/// <param name="instance">expression式で指定するプロパティ(フィールド)をもつオブジェクトのインスタンス、staticな場合は(object)null</param>
		/// <param name="expression">「_ => _.Property」という感じのラムダ式でプロパティやフィールドを指定、staticメンバの場合は「_ => Property」。</param>
		/// <param name="selecter">取得した属性値を検証・変換して返す、無効な値の排除などに利用</param>
		/// <returns></returns>
		public static XElement GetAttributeValueEx<TInstance, TValue>(this XElement element, TInstance instance, Expression<Func<TInstance, TValue>> expression, Func<TValue, TValue> selecter = null)
		{
			return GetAttributeValueEx<TInstance, TValue>(element, instance, expression, null, selecter);
		}
		/// <summary>
		/// 属性値を取得し、selecterによる検証を経てプロパティやフィールドに書き込みます。
		/// 属性値が取得できない場合は書き込まれません。
		/// </summary>
		/// <typeparam name="TInstance"></typeparam>
		/// <typeparam name="TValue"></typeparam>
		/// <param name="element"></param>
		/// <param name="instance">expression式で指定するプロパティ(フィールド)をもつオブジェクトのインスタンス、staticな場合は(object)null</param>
		/// <param name="expression">「_ => _.Property」という感じのラムダ式でプロパティやフィールドを指定、staticメンバの場合は「_ => Property」。</param>
		/// <param name="name">属性名を指定、nullの場合はexpression.Member.Nameから取得</param>
		/// <param name="selecter">取得した属性値を検証・変換して返す、無効な値の排除などに利用</param>
		/// <returns></returns>
		public static XElement GetAttributeValueEx<TInstance, TValue>(this XElement element, TInstance instance, Expression<Func<TInstance, TValue>> expression, string name, Func<TValue, TValue> selecter = null)
		{
			var exp = expression.Body as MemberExpression;
			if (exp == null && expression.Body is UnaryExpression)
				exp = ((UnaryExpression)expression.Body).Operand as MemberExpression;
			if (exp == null)
				throw new NotSupportedException();

			// 属性＆変換チェック
			var attr = (element != null) ? element.Attribute(name ?? exp.Member.Name) : null;
			var conv = TypeDescriptor.GetConverter(typeof(TValue));
			if (attr != null &&
				string.IsNullOrEmpty(attr.Value) == false &&
				conv.CanConvertFrom(typeof(string)))
			{
				// 変換
				var value = (TValue)conv.ConvertFromString(attr.Value);
				if (selecter != null)
					value = selecter(value);

				// フィールドorプロパティへ書き込み
				if (exp.Member is FieldInfo)
				{
					((FieldInfo)exp.Member).SetValue(instance, value);
				}
				else if (exp.Member is PropertyInfo)
				{
					((PropertyInfo)exp.Member).SetValue(instance, value, null);
				}
			}

			return element;
		}

		/// <summary>
		/// 名前や値をラムダ式で渡せるSetAttributeValue()
		/// </summary>
		/// <param name="element"></param>
		/// <param name="expression">
		/// 「() => this.Property」という感じのラムダ式でプロパティやフィールドを指定。
		/// メンバー名が属性名に、値が属性値になります。
		/// </param>
		/// <returns></returns>
		public static XElement SetAttributeValueEx(this XElement element, Expression<Func<object>> expression)
		{
			var exp = expression.Body as MemberExpression;
			if (exp == null && expression.Body is UnaryExpression)
				exp = ((UnaryExpression)expression.Body).Operand as MemberExpression;
			if (exp == null)
				throw new NotSupportedException();

			var value = expression.Compile()();
			SetAttributeValueEx(element, exp.Member.Name, value);
			return element;
		}
		/// <summary>
		/// メソッドチェインで気持ちよくなるためのSetAttributeValue()
		/// </summary>
		/// <param name="element"></param>
		/// <param name="name">属性名</param>
		/// <param name="value">属性値</param>
		/// <returns></returns>
		public static XElement SetAttributeValueEx(this XElement element, XName name, object value)
		{
			element.SetAttributeValue(name, value);
			return element;
		}

		/// <summary>
		/// 子要素に対して処理を行ないます。
		/// nameに該当する子要素がない場合はsubProcessは呼ばれず、複数ある場合は複数回呼ばれます。
		/// </summary>
		/// <param name="element"></param>
		/// <param name="name"></param>
		/// <param name="create">要素が存在しない場合作成する？</param>
		/// <param name="subProcess"></param>
		/// <returns></returns>
		public static XElement SubElement(this XElement element, XName name, bool create, Action<XElement> subProcess)
		{
			if (element != null)
			{
				var elements = element.Elements(name).ToArray();
				if (elements.Any())
				{
					if(subProcess != null)
						elements.ForEach(subProcess);
				}
				else if (create)
				{
					var elm = new XElement(name);
					element.Add(elm);
					subProcess?.Invoke(elm);
				}
			}
			return element;
		}

	}
}
