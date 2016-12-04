
namespace Quala
{
	partial class LogManager
	{
		// ログタイプ
		public enum Level
		{
			INFO = 10,              // 一般情報
			WARNING = 20,           // 警告
			ERROR = 30,             // エラー
			CRITICAL_ERROR = 40,    // 重大なエラー
			TEST = 50,              // テスト
			DEBUG = 60,             // デバッグ
		}
	}
}
