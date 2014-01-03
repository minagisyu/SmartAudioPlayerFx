using System;
using System.Linq;
using System.Reactive.Linq;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Controls.Primitives;
using System.Windows.Input;
using System.Windows.Interop;
using System.Windows.Media;
using Codeplex.Reactive.Extensions;
using SmartAudioPlayer;
using SmartAudioPlayerFx.Data;
using SmartAudioPlayerFx.Managers;
using Drawing = System.Drawing;
using WinForms = System.Windows.Forms;

namespace SmartAudioPlayerFx.Views
{
	sealed partial class MediaListWindow : Window
	{
		public MediaListWindowViewModel ViewModel { get; private set; }
		
		public MediaListWindow()
		{
			InitializeComponent();
			InitializeViewModel();
		}
		void InitializeViewModel()
		{
			DataContext = ViewModel = new MediaListWindowViewModel();
			Task.Run(async () =>
			{
				await ViewModel.Initialized;

				//=[ Window ]
				App.Current.Deactivated += delegate
				{
					// 非アクティブになった時、許可されていればウィンドウを閉じる
					if (ViewModel.IsAutoCloseWhenInactive.Value)
						Hide();
				};

				//=[ Toolbar ]
				currentMediaTitle.MouseLeftButtonUp += delegate
				{
					var item = ViewModel.CurrentMedia.Value;
					if (item == null) return;

					// itemに対応するTreeItem郡を取得
					var dir = item.GetFilePathDir();
					var treeitems = ViewModel.GetTreeItems(dir);
					if (treeitems != null)
					{
						// ツリーを順に開いていく
						// 同時にItemContainerGeneratorでコンテナを取得(BringIntoViewするため)
						ItemsControl container = treeView;
						treeitems.ForEach(i =>
						{
							i.IsExpanded = true;

							// MEMO:
							// WPF内部でまだコンテナが生成されていない場合、強制的に作成
							if (container.ItemContainerGenerator.Status == GeneratorStatus.NotStarted)
							{
								ItemsContainerForceGenerate(container.ItemContainerGenerator);
							}

							var tmp = container
								.ItemContainerGenerator
								.ContainerFromItem(i) as ItemsControl;
							if (tmp != null)
								container = tmp;
						});
						var treeLastItem = treeitems.Last();
						treeLastItem.IsSelected = true;
						if (container != null)
							container.BringIntoView();
					}

					// itemに対応するListItemを取得
					var path = item.FilePath;
					var listItem = ViewModel.GetListItem(path);
					if (listItem != null)
					{
						r_listbox.ScrollIntoView(listItem);
					}
				};

				//=[ SearchText ]
				MediaListItemsSource _prevSearchListFocus = null;
				Observable.FromEventPattern<RoutedEventHandler, RoutedEventArgs>(v => searchTextBox.GotFocus += v, v => searchTextBox.GotFocus -= v)
					.Zip(Observable.FromEventPattern<MouseButtonEventHandler, MouseButtonEventArgs>(v => searchTextBox.PreviewMouseUp += v, v => searchTextBox.PreviewMouseUp -= v),
						(_, __) => RoutedEventArgs.Empty)
					.Subscribe(_ =>
					{
						// 検索テキストボックスのテキスト全選択処理
						// GotFocus後、PreviewMouseUpが飛んできてからSelectAll()を実行する
						searchTextBox.SelectAll();
					});
				searchTextBox.TextChanged += delegate
				{
					// フォーカス オン・オフで枠の色を変える, プレースホルダの表示を切り替える
					var isEmpty = string.IsNullOrEmpty(searchTextBox.Text);
					searchTextDelete.Visibility = isEmpty ? Visibility.Collapsed : Visibility.Visible;
					searchTextBox.Background = isEmpty ? (Brush)searchTextBorder.Resources["PlaceHolderStringBrush"] : Brushes.Transparent;
				};
				searchTextBox.GotFocus += delegate
				{
					// フォーカスもらったら、検索用リストに切り替える為にTextChangedを呼ぶ
					// ListFocusが設定されてるならバックアップ
					if (ViewModel.ListFocus.Value != null)
					{
						_prevSearchListFocus = ViewModel.ListFocus.Value;
					}
					RaiseTextChanged(_prevSearchListFocus);
				};
				searchTextBox.TextChanged += delegate
				{
					RaiseTextChanged(_prevSearchListFocus);
				};

				//=[ TreeView ]
				MediaListItemsSource _selectedTreeListFocus = null;
				treeView.GotFocus += delegate
				{
					ViewModel.ListFocus.Value = _selectedTreeListFocus;
				};
				treeView.SelectedItemChanged += delegate
				{
					// 選択項目が変化したら専用のListFocusConditionを生成させて設定する (prevListFocusも上書き)
					var selected_item = treeView.SelectedItem as MediaTreeItemViewModel;
					if (selected_item == null) return;
					Task.Run(() =>
					{
						var old = ViewModel.ListFocus.Value;
						if (old != null)
							old.Dispose();	// ちょっと重いので非同期で？
					});
					_selectedTreeListFocus = selected_item.CreateListItemsSource();
					ViewModel.ListFocus.Value = _selectedTreeListFocus;
				};

				//=[ ListBox ]
				r_listbox.MouseDoubleClick += (_, ev) =>
				{
					if (ev.ChangedButton != MouseButton.Left) return;
					var selected_item = r_listbox.SelectedItem as IListEntry;
					if (selected_item == null) return;

					// ダブルクリックされた位置が本当に自分か調べる(イベントの透過を考慮する処理)
					var element = r_listbox.ItemContainerGenerator.ContainerFromItem(selected_item) as UIElement;
					if (element.InputHitTest(ev.GetPosition(element)) == null)
						return; // ダブルクリックされた IInputElement がない

					// コマンド実行
					ViewModel.ListSelectedCommand.Execute(selected_item);
				};
				ViewModel.ListFocus
					.ObserveOnUIDispatcher()
					.Subscribe(_ =>
					{
						// ListFocusを変更したらリストを上へ変更
						var generator = r_listbox.ItemContainerGenerator;
						var panel = generator.FindItemsHostPanel(r_listbox) as VirtualizingStackPanel;
						if (panel != null)
						{
							// 1回だと失敗するときがあるので2回やってみる
							panel.SetVerticalOffset(0);
							panel.SetVerticalOffset(0);
						}
					});

			});
		}
		void RaiseTextChanged(MediaListItemsSource prevSearchListFocus)
		{
			// テキストが変更されたのでListConditionを変更してリストを更新
			var word = searchTextBox.Text;
			if (string.IsNullOrEmpty(word))
			{
				// テキストクリアされたのでリストを元の内容に戻す
				ViewModel.ListFocus.Value = prevSearchListFocus;
				return;
			}
			// 検索するまでウェイトをかけてみる
			// todo: 繰り返し処理されちゃう？
			Observable.Timer(TimeSpan.FromMilliseconds(100))
				.ObserveOnUIDispatcher()
				.Subscribe(delegate
				{
					// 検索ワードが変更されたらここでの処理を破棄する
					if (!word.Equals(searchTextBox.Text))
					{
						return;
					}
					var targetVF = new MediaDBViewFocus_SearchWord(ManagerServices.MediaDBViewManager.FocusPath.Value, word);
					ViewModel.ListFocus.Value = new MediaListItemsSource(targetVF);
				});
		}

		// Window.Ownerを基準に丁度いい位置へ移動
		public void WindowRelayout()
		{
			var owner = this.Owner;
			if (owner == null) return;
			var handle = new WindowInteropHelper(this).Handle;
			if (handle == IntPtr.Zero) return;

			// リ・レイアウト
			var rc = WinForms.Screen.FromHandle(handle).WorkingArea;
			var pt = new Drawing.Point((int)owner.Left, (int)owner.Top);
			pt.X += (int)owner.RenderSize.Width;
			pt -= new Drawing.Size((int)this.RenderSize.Width, (int)this.RenderSize.Height);

			// サイズ変更グリップの有効・無効, 新しいウィンドウの位置設定
			resizeBorder.Left = resizeBorder.Top = resizeBorder.Right = resizeBorder.Bottom = null;
			if (rc.Left > pt.X)
			{
				pt.X = (int)owner.Left;
				resizeBorder.Right = 5;
			}
			else
			{
				resizeBorder.Left = 5;
			}
			if (rc.Top > pt.Y)
			{
				pt.Y = (int)(owner.Top + owner.RenderSize.Height);
				resizeBorder.Bottom = 5;
			}
			else
			{
				resizeBorder.Top = 5;
			}

			this.Left = pt.X;
			this.Top = pt.Y;
		}

		// Itemコンテナを強制的に作成
		// http://www.stack-fr.com/stackoverflow/fr/forcing-wpf-to-create-the-items-in-an-itemscontrol-630124.html
		void ItemsContainerForceGenerate(IItemContainerGenerator generator)
		{
			GeneratorPosition pos = generator.GeneratorPositionFromIndex(-1);
			using (generator.StartAt(pos, GeneratorDirection.Forward))
			{
				bool isNewlyRealized;
				while(true)
				{
					isNewlyRealized = false;
					DependencyObject cntr = generator.GenerateNext(out isNewlyRealized);
					if (isNewlyRealized)
					{
						generator.PrepareItemContainer(cntr);
					}
					else
					{
						break;
					}
				}
			}
		}

	}
}
