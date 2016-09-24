using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Quala
{
	/// <summary>
	/// モデル向け多重参照されるオブジェクト
	/// ReferenceObject.Managerクラスでインスタンスの生成、破棄を管理する
	/// 内部に参照カウントを持ち、Dispose()呼び出し時に0で破棄処理が走る
	/// </summary>
	public partial class ReferenceObject
	{
		private ReferenceObject()
		{

		}

	}
}
