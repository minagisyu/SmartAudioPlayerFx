using System;
using System.IO;
using System.Runtime.InteropServices;
using System.Security;

namespace SmartAudioPlayer
{
	/// <summary>
	/// mp3infp.dllを使用してタグ情報を取得します
	/// </summary>
	public static class MediaTagUtil
	{
		static readonly object getlock = new object();

		/// <summary>
		/// 指定パスのファイルからタグ情報の取得を試みます
		/// </summary>
		/// <param name="filePath"></param>
		/// <returns></returns>
		public static IMediaTagInfo Get(string filePath)
		{
			lock (getlock)
			{
				return new MediaTagInfo_Mp3infp(filePath);
			}
		}

		public static void TagEditGUI(string filepath)
		{
			var ret = MediaTagInfo_Mp3infp.Mp3infp.ViewPropEx(
				IntPtr.Zero,
				filepath,
				MediaTagInfo_Mp3infp.Mp3infp.ActivePage.ID3v2,
				false, 0, 0);
		}

		public interface IMediaTagInfo
		{
			/// <summary>
			/// タグ情報が読み込まれたときはtrue
			/// </summary>
			bool IsTagLoaded { get; }

			/// <summary>
			/// ファイルパス
			/// </summary>
			string FilePath { get; }

			/// <summary>
			/// タイトル、読み込み失敗orタグ存在しない時はファイル名
			/// </summary>
			string Title { get; }

			/// <summary>
			/// アーティスト、読み込み失敗orタグ存在しない時は空白
			/// </summary>
			string Artist { get; }

			/// <summary>
			/// アルバム、読み込み失敗orタグ存在しない時は空白
			/// </summary>
			string Album { get; }

			/// <summary>
			/// コメント、読み込み失敗orタグ存在しない時は空白
			/// </summary>
			string Comment { get; }
		}

		/// <summary>
		/// ファイルからタグ情報を取得します。
		/// (要mp3infp.dll)
		/// </summary>
		sealed class MediaTagInfo_Mp3infp : IMediaTagInfo
		{
			static bool disabled = false;
			static readonly object sync = new object();

			static MediaTagInfo_Mp3infp()
			{
				lock (sync)
				{
					try
					{
						var version = Mp3infp.GetVer();
						disabled = (version < 0x253);
					}
					catch
					{
						disabled = true;
					}
				}
			}

			/// <summary>
			/// ファイルからタグ情報を収集。
			/// 最低限、FilePathとTitleは埋まります。
			/// (情報がない場合、それ以外の項目はString.Emptyになります)
			/// </summary>
			/// <param name="filepath">ファイルパス</param>
			public MediaTagInfo_Mp3infp(string filepath)
			{
				FilePath = filepath;
				Title = Path.GetFileName(FilePath);

				if (disabled)
				{
					IsTagLoaded = false;
					Title = Path.GetFileName(FilePath);
					Artist = string.Empty;
					Album = string.Empty;
					Comment = string.Empty;
					return;
				}

				try
				{
					lock (sync)
					{
						Mp3infp.SetConf("wave_CodecFind", "0");
						Mp3infp.SetConf("avi_CodecFind", "0");
						if (Mp3infp.Load(IntPtr.Zero, FilePath) == 0)
						{
							switch (Mp3infp.GetFileType())
							{
								case Mp3infp.FileType.MP3: CollectByMP3(); break;
								case Mp3infp.FileType.MP4: CollectByMpeg4(); break;
								case Mp3infp.FileType.AVI: CollectByAvi(); break;
								case Mp3infp.FileType.WAV: CollectByWav(); break;
								case Mp3infp.FileType.WMA: CollectByWMA(); break;
								case Mp3infp.FileType.OGG: CollectByOGG(); break;
								case Mp3infp.FileType.VQF: CollectByVQF(); break;
								case Mp3infp.FileType.APE: CollectByAPE(); break;
								default:
									IsTagLoaded = false;
									break;
							}

							IsTagLoaded = true;
							// タイトル補正。空白ならファイル名を。
							if (string.IsNullOrWhiteSpace(Title))
								Title = Path.GetFileName(FilePath);
							// nullなら空白に
							Artist = Artist ?? string.Empty;
							Album = Album ?? string.Empty;
							Comment = Comment ?? string.Empty;
							// Comment補正 (最後の空行を削除)
							while (Comment.Length > 1)
							{
								// 最後の文字は\nか？
								if (Comment[Comment.Length - 1] == '\n')
								{
									// その前の文字は\rならそっちも削除
									Comment = (Comment.Length > 2 && Comment[Comment.Length - 2] == '\r') ?
										Comment.Substring(0, Comment.Length - 2) : Comment.Substring(Comment.Length - 1);
								}
								else
								{
									break;
								}
							}
						}
						else
						{
							IsTagLoaded = false;
						}
					}
				}
				catch
				{
					IsTagLoaded = false;
				}
			}

			#region IMediaTagInfo

			public bool IsTagLoaded { get; private set; }
			public string FilePath { get; private set; }
			public string Title { get; private set; }
			public string Artist { get; private set; }
			public string Album { get; private set; }
			public string Comment { get; private set; }

			#endregion
			#region CollectByXXX

			string Mp3infp_GetValue(string key)
			{
				string value;
				Mp3infp.GetValue(key, out value);
				return value;
			}

			void CollectByMP3()
			{
				var tagtype = Mp3infp.Mp3_GetTagType();

				// MP3v2 -> APE -> RIFF -> MP3v1の順で探す。
				if (tagtype.HasFlag(Mp3infp.HasMp3.ID3V2))
				{
					Title = Mp3infp_GetValue("INAM_v2");
					Artist = Mp3infp_GetValue("IART_v2");
					Album = Mp3infp_GetValue("IPRD_v2");
					Comment = Mp3infp_GetValue("ICMT_v2");
				}
				else if (tagtype.HasFlag(Mp3infp.HasMp3.APEV1) || tagtype.HasFlag(Mp3infp.HasMp3.APEV2))
				{
					Title = Mp3infp_GetValue("INAM_APE");
					Artist = Mp3infp_GetValue("IART_APE");
					Album = Mp3infp_GetValue("IPRD_APE");
					Comment = Mp3infp_GetValue("ICMT_APE");
				}
				else if (tagtype.HasFlag(Mp3infp.HasMp3.RIFFSIF))
				{
					Title = Mp3infp_GetValue("INAM_rmp");
					Artist = Mp3infp_GetValue("IART_rmp");
					Album = Mp3infp_GetValue("IPRD_rmp");
					Comment = Mp3infp_GetValue("ICMT_rmp");
				}
				else if (tagtype.HasFlag(Mp3infp.HasMp3.ID3V1))
				{
					Title = Mp3infp_GetValue("INAM_v1");
					Artist = Mp3infp_GetValue("IART_v1");
					Album = Mp3infp_GetValue("IPRD_v1");
					Comment = Mp3infp_GetValue("ICMT_v1");
				}
			}

			void CollectByWav()
			{
				Title = Mp3infp_GetValue("INAM");
				Artist = Mp3infp_GetValue("IART");
				Album = Mp3infp_GetValue("IPRD");
				Comment = Mp3infp_GetValue("ICMT");
			}

			void CollectByAvi()
			{
				Title = Mp3infp_GetValue("INAM");
				Artist = Mp3infp_GetValue("IART");
				Comment = Mp3infp_GetValue("ICMT");
			}

			void CollectByVQF()
			{
				Title = Mp3infp_GetValue("INAM");
				Artist = Mp3infp_GetValue("IART");
				Comment = Mp3infp_GetValue("ICMT");
			}

			void CollectByWMA()
			{
				Title = Mp3infp_GetValue("INAM");
				Artist = Mp3infp_GetValue("IART");
				Album = Mp3infp_GetValue("IPRD");
				Comment = Mp3infp_GetValue("ICMT");
			}

			void CollectByOGG()
			{
				Title = Mp3infp_GetValue("INAM");
				Artist = Mp3infp_GetValue("IART");
				Album = Mp3infp_GetValue("IPRD");
				Comment = Mp3infp_GetValue("ICMT");
			}

			void CollectByAPE()
			{
				Title = Mp3infp_GetValue("INAM");
				Artist = Mp3infp_GetValue("IART");
				Album = Mp3infp_GetValue("IPRD");
				Comment = Mp3infp_GetValue("ICMT");
			}

			void CollectByMpeg4()
			{
				Title = Mp3infp_GetValue("INAM");
				Artist = Mp3infp_GetValue("IART");
				Album = Mp3infp_GetValue("IPRD");
				Comment = Mp3infp_GetValue("ICMT");
			}

			#endregion
			#region Mp3infp API Definition
			/// <summary>
			/// mp3infp.dll interop
			/// (for 2.54a)
			/// </summary>
			/// <remarks>
			/// ////////////////////////////////////////////////////////////////////
			/// 表1　mp3infp_GetValue()/mp3infp_SetValue()でszValueNameに指定する名前一覧
			/// ////////////////////////////////////////////////////////////////////
			/// 
			/// 	[共通](※1)
			/// 	ファイル名				"FILE"	(v2.41～)
			/// 	拡張子					"FEXT"	(v2.41～)
			/// 	パス					"PATH"	(v2.41～)
			/// 	サイズ(byte単位)		"SIZ1"	(v2.41～)
			/// 	サイズ(Kbyte単位)		"SIZK"	(v2.41～)
			/// 	サイズ(Mbyte単位)		"SIZM"	(v2.41～)
			/// 
			/// 	[MP3]					ID3v1		ID3v2		RiffSIF		APE
			/// 	フォーマット(※1)		"AFMT"		"AFMT"		"AFMT"		"AFMT"
			/// 	演奏時間(※1)			"TIME"		"TIME"		"TIME"		"TIME"
			/// 	タイトル				"INAM_v1"	"INAM_v2"	"INAM_rmp"	"INAM_APE"
			/// 	アーティスト			"IART_v1"	"IART_v2"	"IART_rmp"	"IART_APE"
			/// 	アルバム				"IPRD_v1"	"IPRD_v2"	"IPRD_rmp"	"IPRD_APE"
			/// 	コメント				"ICMT_v1"	"ICMT_v2"	"ICMT_rmp"	"ICMT_APE"
			/// 	作成日					"ICRD_v1"	"ICRD_v2"	"ICRD_rmp"	"ICRD_APE"
			/// 	ジャンル				"IGNR_v1"	"IGNR_v2"	"IGNR_rmp"	"IGNR_APE"
			/// 	(ID3v2/RiffSIF)
			/// 	著作権								"ICOP_v2"	"ICOP_rmp"
			/// 	ソフトウェア/エンコーダ				"ISFT_v2"	"ISFT_rmp"
			/// 	(ID3v2)
			/// 	作曲								"COMP_v2"
			/// 	Orig.アーティスト					"OART_v2"
			/// 	URL									"URL_v2"
			/// 	エンコードした人					"ENC2_v2"
			/// 	(RiffSIF)
			/// 	ソース											"ISRC_rmp"
			/// 	エンジニア										"IENG_rmp"
			/// 	(ID3v1/2)
			/// 	トラック番号			"TRACK_v1"	"TRACK_v2"				"TRACK_APE"
			/// 
			/// 	[WAV]
			/// 	フォーマット(※1)		"AFMT"
			/// 	演奏時間(※1)			"TIME"
			/// 	タイトル(※2)			"INAM"
			/// 	タイトル(※2)			"ISBJ"
			/// 	アーティスト			"IART"
			/// 	アルバム				"IPRD"
			/// 	コメント				"ICMT"
			/// 	作成日					"ICRD"
			/// 	ジャンル				"IGNR"
			/// 	著作権					"ICOP"
			/// 	ソフトウェア			"ISFT"
			/// 	ソース					"ISRC"
			/// 	エンジニア				"IENG"
			/// 
			/// 	[AVI]
			/// 	音声フォーマット(※1)	"AFMT"	
			/// 	映像フォーマット(※1)	"VFMT"
			/// 	時間(※1)				"TIME"
			/// 	タイトル(※2)			"INAM"
			/// 	タイトル(※2)			"ISBJ"
			/// 	アーティスト			"IART"
			/// 	コメント				"ICMT"
			/// 	作成日					"ICRD"
			/// 	ジャンル				"IGNR"
			/// 	著作権					"ICOP"
			/// 	ソフトウェア			"ISFT"
			/// 	ソース					"ISRC"
			/// 	エンジニア				"IENG"
			/// 	AVIバージョン			"AVIV"	(v2.37～)
			/// 
			/// 	[VQF]
			/// 	フォーマット(※1)		"AFMT"
			/// 	演奏時間(※1)			"TIME"
			/// 	タイトル				"INAM"
			/// 	アーティスト			"IART"
			/// 	コメント				"ICMT"
			/// 	著作権					"ICOP"
			/// 	保存名					"FILE"
			/// 
			/// 	[WMA]
			/// 	音声フォーマット(※1)	"AFMT"
			/// 	映像フォーマット(※1)	"VFMT"
			/// 	時間(※1)				"TIME"
			/// 	タイトル				"INAM"
			/// 	トラック				"TRACK"
			/// 	アーティスト			"IART"
			/// 	アルバム				"IPRD"
			/// 	コメント				"ICMT"
			/// 	作成日					"ICRD"
			/// 	ジャンル				"IGNR"
			/// 	著作権					"ICOP"
			/// 	URL(Album)				"URL1"
			/// 	URL(関連)				"URL2"
			/// 
			/// 	[OGG]
			/// 	フォーマット(※1)		"AFMT"
			/// 	演奏時間(※1)			"TIME"
			/// 	タイトル				"INAM"
			/// 	アーティスト			"IART"
			/// 	アルバム				"IPRD"
			/// 	コメント				"ICMT"
			/// 	作成日					"ICRD"
			/// 	ジャンル				"IGNR"
			/// 	トラック番号			"TRACK"
			/// 
			/// 	[APE]
			/// 	フォーマット(※1)		"AFMT"
			/// 	演奏時間(※1)			"TIME"
			/// 	タイトル				"INAM"
			/// 	アーティスト			"IART"
			/// 	アルバム				"IPRD"
			/// 	コメント				"ICMT"
			/// 	作成日					"ICRD"
			/// 	ジャンル				"IGNR"
			/// 	トラック番号			"TRACK"
			/// 
			/// 	[MP4]	(v2.53～)
			/// 	音声フォーマット(※1)	"AFMT"
			/// 	映像フォーマット(※1)	"VFMT"
			/// 	タイトル				"INAM"
			/// 	アーティスト			"IART"
			/// 	アルバム				"IPRD"
			/// 	グループ				"IGRP"
			/// 	作曲					"COMPOSER"
			/// 	ジャンル				"IGNR"
			/// 	トラック番号1			"TRACK1"		(1以上の数値)
			/// 	トラック番号2			"TRACK2"		(1以上の数値)
			/// 	ディスク番号1			"DISC1"			(1以上の数値)
			/// 	ディスク番号2			"DISC2"			(1以上の数値)
			/// 	テンポ					"BPM"			(数値)
			/// 	作成日					"ICRD"			(4桁の数値 例："2004")
			/// 	コンピレーション		"COMPILATION"	("1" or "0")
			/// 	コメント				"ICMT"
			/// 	ツール					"TOOL"
			/// 
			/// 
			/// (※1)mp3infp_SetValue()では利用できません。
			/// (※2)mp3infpではロード時にINAMを優先、無ければISBJを表示。セーブ時にはISBJを削除、INAMを保存します。
			/// 
			/// 
			/// ////////////////////////////////////////////////////////////////////
			/// mp3infp_SetConf()指定する設定項目・値一覧
			/// ////////////////////////////////////////////////////////////////////
			/// 
			/// 		[Waveファイルのコーデック名称の取得方法](Ver2.42～)
			/// 		(項目名)
			/// 		"wave_CodecFind"
			/// 		(値)
			/// 		"0"(default)	mp3infp内蔵辞書 → Windows APIを利用の順で検索(高速)
			/// 		"1"				Windows APIを利用 → 自力解析の順で検索(低速)
			/// 		"2"				mp3infp内蔵辞書(高速)
			/// 		"3"				Windows APIを利用(低速)
			/// 	
			/// 		[Aviファイルのコーデック名称の取得方法](Ver2.42～)
			/// 		(項目名)
			/// 		"avi_CodecFind"
			/// 		(値)
			/// 		"0"(default)	mp3infp内蔵辞書 → Windows APIを利用の順で検索(高速)
			/// 		"1"				Windows APIを利用 → 自力解析の順で検索(低速)
			/// 		"2"				mp3infp内蔵辞書(高速)
			/// 		"3"				Windows APIを利用(低速)
			/// 
			/// 		[ID3v1で拡張ジャンルを使用する](Ver2.43～)
			/// 		(項目名)
			/// 		"mp3_UseExtGenre"
			/// 		(値)
			/// 		"0"(default)	無効
			/// 		"1"				有効
			/// 
			/// 		[ID3v2で文字列をUnicodeで書き込む](Ver2.43～)
			/// 		(項目名)
			/// 		"mp3_ID3v2Unicode"
			/// 		(値)
			/// 		"0"(default)	無効
			/// 		"1"				有効
			/// 
			/// 		[ID3v2を非同期化する](Ver2.43～)
			/// 		(項目名)
			/// 		"mp3_ID3v2Unsync"
			/// 		(値)
			/// 		"0"				無効
			/// 		"1"(default)	有効
			/// 
			/// 		[保存時のID3v2バージョン](Ver2.43～)
			/// 		※この設定値はmp3infp_Load()の実行によって上書きされます。
			/// 		(項目名)
			/// 		"mp3_SaveID3v2Version"
			/// 		(値)
			/// 		"2.2"			ID3v2.2
			/// 		"2.3"(default)	ID3v2.3
			/// 		"2.4"			ID3v2.4
			/// 		</remarks>
			[SuppressUnmanagedCodeSecurity]
			internal static class Mp3infp
			{
				// unicode阪を使う
				const string mp3infp = "mp3infp.dll";
				static readonly object sync = new object();
				static IntPtr _dllModule = IntPtr.Zero;

				static Mp3infp()
				{
					lock (sync)
					{
						if (_dllModule != IntPtr.Zero)
							return;

						_dllModule = PreLoadMp3infpDll();
					}
				}

				#region DllLoader

				// LoadLibrary APIで予めDLLを読み込むことでx86/x64両対応のP/Invokeを実現する

				[DllImport("kernel32")]
				static extern IntPtr LoadLibrary(string fileName);

				static IntPtr PreLoadMp3infpDll()
				{
					var directory = AppDomain.CurrentDomain.BaseDirectory;
					if (directory == null)
						return IntPtr.Zero;

					var fileName = Path.Combine(directory, mp3infp);
					if (File.Exists(fileName))
						return IntPtr.Zero;

					var processorArchitecture = Environment.GetEnvironmentVariable("PROCESSOR_ARCHITECTURE");
					if (processorArchitecture == null)
						return IntPtr.Zero;

					fileName = Path.Combine(directory, processorArchitecture, mp3infp);
					if (!File.Exists(fileName))
					{
						string platformName = GetPlatformName(processorArchitecture);

						if (platformName == null)
							return IntPtr.Zero;

						fileName = Path.Combine(directory, platformName, mp3infp);

						if (!File.Exists(fileName))
							return IntPtr.Zero;
					}

					try
					{
						return LoadLibrary(fileName);
					}
					catch (Exception)
					{
					}

					return IntPtr.Zero;
				}

				static string GetPlatformName(string processorArchitecture)
				{
					if (String.IsNullOrEmpty(processorArchitecture))
						return null;

					switch (processorArchitecture)
					{
						case "x86":
							return "Win32";
						case "AMD64":
							return "x64";
						case "IA64":
							return "Itanium";
						case "ARM":
							return "WinCE";
						default:
							return null;
					}
				}

				#endregion

				/// <summary>
				/// mp3infpのバージョンを取得する (Ver2.11～)
				/// </summary>
				/// <returns>
				/// バージョン情報
				/// Ver.2.11	= 0x0211
				/// </returns>
				[DllImport(mp3infp, EntryPoint = "mp3infp_GetVer", CharSet = CharSet.Ansi)]
				public static extern uint GetVer();

				/// <summary>
				/// mp3infpに対応したファイル形式のプロパティを開く(モーダルダイアログ版)
				/// </summary>
				/// <remarks>
				/// 指定ファイルのプロパティをmp3infpのタブをアクティブにして開きます
				/// ※シェルエクステンションを使用せずにmp3infp.dll単独の動作となります
				/// ※シェルエクステンション標準のプロパティページは表示されません
				/// </remarks>
				/// <param name="hWnd">
				/// 呼び出し元ウインドウハンドル
				/// 呼び出し元ウインドウ上にダイアログを表示します
				/// NULLならデスクトップを指定したとみなします
				/// </param>
				/// <param name="szFileName">対象ファイル名をフルパスで指定</param>
				/// <param name="dwPage">
				/// ・mp3infpの何ページ目をアクティブにするか指定する(0=ID3v1 / 1=ID3v2 / 2=RiffSIF / 3=APE(Ver2.47))
				/// ・タグを含まないmp3の場合のみ有効
				/// ・タグを含む場合はID3v2/APE/RiffSIF/ID3v1の順で検索して、最初に見つかったタグをアクティブにします
				/// </param>
				/// <param name="modeless">
				/// TRUEならプロパティを表示したまま制御を返します。
				/// 戻り値にはプロパティのウインドウハンドルが入ります。
				/// FALSEならプロパティを閉じるまで制御を返しません。
				/// </param>
				/// <param name="param1">未使用(0を指定してください)</param>
				/// <param name="param2">未使用(0を指定してください)</param>
				/// <returns>成功=0以上/失敗=-1</returns>
				[DllImport(mp3infp, EntryPoint = "mp3infp_ViewPropExW", CharSet = CharSet.Unicode)]
				public static extern int ViewPropEx(IntPtr hWnd, string szFileName,
					ActivePage dwPage, bool modeless, uint param1, uint param2);

				/// <summary>
				/// mp3infpに対応したファイル形式のプロパティを開く
				/// </summary>
				/// <param name="hWnd">呼び出し元ウインドウハンドル</param>
				/// <param name="szFileName">象ファイル名をフルパスで指定。</param>
				/// <param name="dwPage">
				/// ・mp3infpの何ページ目をアクティブにするか指定する(0=ID3v1 / 1=ID3v2 / 2=RiffSIF / 3=APE(Ver2.47))
				/// ・タグを含まないmp3の場合のみ有効
				/// ・タグを含む場合はID3v2/APE/RiffSIF/ID3v1の順で検索して、最初に見つかったタグをアクティブにします
				/// </param>
				/// <returns>成功=TRUE/失敗=FALSE</returns>
				[DllImport(mp3infp, EntryPoint = "mp3infp_ViewPropW", CharSet = CharSet.Unicode)]
				public static extern bool ViewProp(IntPtr hWnd, string szFileName, ActivePage dwPage);

				/// <summary>
				/// タグ情報をロードする (Ver2.26～)
				/// </summary>
				/// <param name="hWnd">呼び出し元ウインドウを指定します。無い場合はNULL。</param>
				/// <param name="szFileName">対象ファイル名をフルパスで指定。</param>
				/// <returns>
				/// -1=ロード失敗
				/// ERROR_SUCCESS(0)=成功
				/// (その他)=Win32エラーコード (FormatMessageで文字列を取得できる)
				/// </returns>
				[DllImport(mp3infp, EntryPoint = "mp3infp_LoadW", CharSet = CharSet.Unicode)]
				public static extern uint Load(IntPtr hWnd, string szFileName);

				/// <summary>
				/// mp3infpの動作設定 (Ver2.42～)
				/// </summary>
				/// <param name="tag">設定項目(表2参照)</param>
				/// <param name="value">設定値(表2参照)</param>
				/// <returns>成功=TRUE/失敗=FALSE</returns>
				/// <remarks>
				/// ・他のプロセスのmp3infp.dll/シェル拡張のmp3infpには影響しない
				/// ・設定内容は保存されない
				/// </remarks>
				[DllImport(mp3infp, EntryPoint = "mp3infp_SetConfW", CharSet = CharSet.Unicode)]
				public static extern bool SetConf(string tag, string value);

				/// <summary>
				/// ファイルの種類を取得する (Ver2.26～)
				/// </summary>
				/// <remarks>
				/// mp3infp_Load()の後に呼び出してください
				/// </remarks>
				/// <returns></returns>
				[DllImport(mp3infp, EntryPoint = "mp3infp_GetType", CharSet = CharSet.Ansi)]
				public static extern FileType GetFileType();

				/// <summary>
				/// mp3が持っているタグの種類を取得する (Ver2.26～)
				/// </summary>
				/// <remarks>
				/// mp3infp_Load()の後に呼び出してください
				/// </remarks>
				/// <returns></returns>
				[DllImport(mp3infp, EntryPoint = "mp3infp_mp3_GetTagType", CharSet = CharSet.Ansi)]
				public static extern HasMp3 Mp3_GetTagType();

				/// <summary>
				/// タグ情報を取得する (Ver2.26～)
				/// </summary>
				/// <remarks>
				/// mp3infp_Load()の後に呼び出してください
				/// </remarks>
				/// <param name="szValueName">タグの種類を示す名前(表1を参照)</param>
				/// <param name="buf">タグ情報を示すバッファのポインタを受け取るポインタ</param>
				/// <returns>成功=TRUE/失敗=FALSE</returns>
				[DllImport(mp3infp, EntryPoint = "mp3infp_GetValueW", CharSet = CharSet.Unicode)]
				public static extern bool GetValue(string szValueName, out IntPtr buf);

				public static bool GetValue(string szValueName, out string buf)
				{
					IntPtr tmp;
					var result = GetValue(szValueName, out tmp);
					buf = (tmp != IntPtr.Zero)
						? Marshal.PtrToStringAuto(tmp)
						: string.Empty;
					return result;
				}

				/// <summary>
				/// タグ情報を設定する (Ver.2.43～)
				/// </summary>
				/// <remarks>
				/// mp3infp_Load()の後に呼び出してください
				/// </remarks>
				/// <param name="szValueName">タグの種類を示す名前(表1を参照)</param>
				/// <param name="buf">タグ情報を示す文字列</param>
				/// <returns>
				/// -1=失敗
				/// ERROR_SUCCESS=成功
				/// </returns>
				[DllImport(mp3infp, EntryPoint = "mp3infp_SetValueW", CharSet = CharSet.Unicode)]
				public static extern uint SetValue(string szValueName, string buf);

				/// <summary>
				/// タグ情報を保存する (Ver.2.43～)
				/// </summary>
				/// <remarks>
				/// mp3infp_Load()の後に呼び出してください
				/// </remarks>
				/// <param name="szFileName">対象ファイル名をフルパスで指定。</param>
				/// <returns>
				/// -1=失敗
				/// -2=2Gバイトを超えるファイルを扱うことはできません。(WAVファイルのみ)
				/// ERROR_SUCCESS=成功
				/// (その他)=Win32エラーコード (FormatMessageで文字列を取得できる)
				/// </returns>
				[DllImport(mp3infp, EntryPoint = "mp3infp_SaveW", CharSet = CharSet.Unicode)]
				public static extern uint Save(string szFileName);

				/// <summary>
				/// ID3TAG V1を作成する (Ver.2.43～)
				/// </summary>
				/// <remarks>
				/// mp3infp_Load()の後に呼び出してください
				/// 変更は直ちに反映されます
				/// mp3ファイルにのみ利用してください(wavファイルは対象外)
				/// </remarks>
				/// <param name="szFileName">対象ファイル名をフルパスで指定。</param>
				/// <returns>
				/// -1=失敗
				/// ERROR_SUCCESS=成功
				/// (その他)=Win32エラーコード (FormatMessageで文字列を取得できる)
				/// </returns>
				[DllImport(mp3infp, EntryPoint = "mp3infp_mp3_MakeId3v1W", CharSet = CharSet.Unicode)]
				public static extern uint Mp3_MakeId3v1(string szFileName);

				/// <summary>
				/// ID3TAG V1を削除する (Ver.2.43～)
				/// </summary>
				/// <remarks>
				/// mp3infp_Load()の後に呼び出してください
				/// 変更は直ちに反映されます
				/// mp3ファイルにのみ利用してください(wavファイルは対象外)
				/// </remarks>
				/// <param name="szFileName">対象ファイル名をフルパスで指定。</param>
				/// <returns>
				/// -1=失敗
				/// ERROR_SUCCESS=成功
				/// (その他)=Win32エラーコード (FormatMessageで文字列を取得できる)
				/// </returns>
				[DllImport(mp3infp, EntryPoint = "mp3infp_mp3_DelId3v1W", CharSet = CharSet.Unicode)]
				public static extern uint Mp3_DelId3v1(string szFileName);

				/// <summary>
				/// ID3TAG V2を作成する (Ver.2.43～)
				/// </summary>
				/// <remarks>
				/// mp3infp_Load()の後に呼び出してください
				/// 変更は直ちに反映されます
				/// mp3ファイルにのみ利用してください(wavファイルは対象外)
				/// </remarks>
				/// <param name="szFileName">対象ファイル名をフルパスで指定。</param>
				/// <returns>
				/// -1=失敗
				/// ERROR_SUCCESS=成功
				/// (その他)=Win32エラーコード (FormatMessageで文字列を取得できる)
				/// </returns>
				[DllImport(mp3infp, EntryPoint = "mp3infp_mp3_MakeId3v2W", CharSet = CharSet.Unicode)]
				public static extern uint Mp3_MakeId3v2(string szFileName);

				/// <summary>
				/// ID3TAG V2を削除する (Ver.2.43～)
				/// </summary>
				/// <remarks>
				/// mp3infp_Load()の後に呼び出してください
				/// 変更は直ちに反映されます
				/// mp3ファイルにのみ利用してください(wavファイルは対象外)
				/// </remarks>
				/// <param name="szFileName">対象ファイル名をフルパスで指定。</param>
				/// <returns>
				/// -1=失敗
				/// ERROR_SUCCESS=成功
				/// (その他)=Win32エラーコード (FormatMessageで文字列を取得できる)
				/// </returns>
				[DllImport(mp3infp, EntryPoint = "mp3infp_mp3_DelId3v2W", CharSet = CharSet.Unicode)]
				public static extern uint Mp3_DelId3v2(string szFileName);

				/// <summary>
				/// mp3形式からRMP形式に変換する (Ver.2.43～)
				/// </summary>
				/// <remarks>
				/// mp3infp_Load()の後に呼び出してください
				/// 変更は直ちに反映されます
				/// mp3ファイルにのみ利用してください(wavファイルは対象外)
				/// </remarks>
				/// <param name="szFileName">対象ファイル名をフルパスで指定。</param>
				/// <returns>
				/// -1=失敗
				/// ERROR_SUCCESS=成功
				/// (その他)=Win32エラーコード (FormatMessageで文字列を取得できる)
				/// </returns>
				[DllImport(mp3infp, EntryPoint = "mp3infp_mp3_MakeRMPW", CharSet = CharSet.Unicode)]
				public static extern uint Mp3_MakeRMP(string szFileName);

				/// <summary>
				/// RMP形式からmp3形式に変換する (Ver.2.43～)
				/// </summary>
				/// <remarks>
				/// mp3infp_Load()の後に呼び出してください
				/// 変更は直ちに反映されます
				/// mp3ファイルにのみ利用してください(wavファイルは対象外)
				/// </remarks>
				/// <param name="szFileName">対象ファイル名をフルパスで指定。</param>
				/// <returns>
				/// -1=失敗
				/// ERROR_SUCCESS=成功
				/// (その他)=Win32エラーコード (FormatMessageで文字列を取得できる)
				/// </returns>
				[DllImport(mp3infp, EntryPoint = "mp3infp_mp3_DelRMPW", CharSet = CharSet.Unicode)]
				public static extern uint Mp3_DelRMP(string szFileName);

				/// <summary>
				/// APE Tagを作成する (Ver.2.47～)
				/// </summary>
				/// <remarks>
				/// mp3infp_Load()の後に呼び出してください
				/// 変更は直ちに反映されます
				/// mp3ファイルにのみ利用してください(wavファイルは対象外)
				/// </remarks>
				/// <param name="szFileName">対象ファイル名をフルパスで指定。</param>
				/// <returns>
				/// -1=失敗
				/// ERROR_SUCCESS=成功
				/// (その他)=Win32エラーコード (FormatMessageで文字列を取得できる)
				/// </returns>
				[DllImport(mp3infp, EntryPoint = "mp3infp_mp3_MakeApeTagW", CharSet = CharSet.Unicode)]
				public static extern uint Mp3_MakeApeTag(string szFileName);

				/// <summary>
				/// APE Tagを削除する (Ver.2.47～)
				/// </summary>
				/// <remarks>
				/// mp3infp_Load()の後に呼び出してください
				/// 変更は直ちに反映されます
				/// mp3ファイルにのみ利用してください(wavファイルは対象外)
				/// </remarks>
				/// <param name="szFileName">対象ファイル名をフルパスで指定。</param>
				/// <returns>
				/// -1=失敗
				/// ERROR_SUCCESS=成功
				/// (その他)=Win32エラーコード (FormatMessageで文字列を取得できる)
				/// </returns>
				[DllImport(mp3infp, EntryPoint = "mp3infp_mp3_DelApeTagW", CharSet = CharSet.Unicode)]
				public static extern uint Mp3_DelApeTag(string szFileName);

				public enum ActivePage : uint
				{
					ID3v1 = 0,
					ID3v2 = 1,
					RiffSif = 2,
					// Ver2.47
					APE = 3,
				}

				// MP3INFP_FILE_xxxx
				public enum FileType : uint
				{
					// Ver2.26～
					UNKNOWN = 0x00,
					MP3 = 0x01,
					WAV = 0x02,
					AVI = 0x03,
					VQF = 0x04,
					WMA = 0x05,
					OGG = 0x07,
					APE = 0x08,
					// Ver2.53～
					MP4 = 0x09,
				}

				// MP3INFP_HAS_MP3_xxxx
				[Flags]
				public enum HasMp3 : uint
				{
					// Ver2.27～
					ID3V1 = 0x00000001,
					ID3V2 = 0x00000002,
					RIFFSIF = 0x00000004,
					// v2.43～
					ID3V1_0 = 0x00000008,
					ID3V1_1 = 0x00000010,
					ID3V2_2 = 0x00000020,
					ID3V2_3 = 0x00000040,
					ID3V2_4 = 0x00000080,
					// v2.47～
					APEV1 = 0x00000100,
					APEV2 = 0x00000200,
				}

			}
			#endregion

		}
	}
}
