using System;
using System.ComponentModel;
using System.Linq.Expressions;

namespace Quala
{
	partial class Extension
	{
		/// <summary>
		/// イベントを発行する
		/// </summary>
		/// <typeparam name="TResult">プロパティの型</typeparam>
		/// <param name="_this">イベントハンドラ</param>
		/// <param name="propertyName">プロパティ名を表すExpression。() => Nameのように指定する。</param>
		public static void Raise<TResult>(this PropertyChangedEventHandler _this,
			Expression<Func<TResult>> propertyName)
		{
			// ハンドラに何も登録されていない場合は何もしない
			if (_this == null) return;

			// ラムダ式のBodyを取得する。MemberExpressionじゃなかったら駄目
			var memberEx = propertyName.Body as MemberExpression;
			if (memberEx == null) throw new ArgumentException();

			// () => NameのNameの部分の左側に暗黙的に存在しているオブジェクトを取得する式をゲット
			var senderExpression = memberEx.Expression as ConstantExpression;
			// ConstraintExpressionじゃないと駄目
			if (senderExpression == null) throw new ArgumentException();

			// 式を評価してsender用のインスタンスを得る
			var sender = Expression.Lambda(senderExpression).Compile().DynamicInvoke();

			// 下準備が出来たので、イベント発行！！
			_this(sender, new PropertyChangedEventArgs(memberEx.Member.Name));
		}
	}
}
