using System.Windows.Controls;
using System.Windows.Controls.Primitives;
using System.Windows;
using System.Windows.Media;

namespace Quala.WPF.Extensions
{
	// ItemsControlUtil http://pro.art55.jp/?eid=1161413
	public static class IItemsContainerGeneratorExtension
	{
		public static Panel FindItemsHostPanel(this IItemContainerGenerator generator, DependencyObject control)
		{
			int count = VisualTreeHelper.GetChildrenCount(control);
			for (int i = 0; i < count; i++)
			{
				DependencyObject child = VisualTreeHelper.GetChild(control, i);
				if (IsItemsHostPanel(generator, child))
					return (Panel)child;

				Panel panel = FindItemsHostPanel(generator, child);
				if (panel != null)
					return panel;
			}
			return null;
		}

		public static bool IsItemsHostPanel(IItemContainerGenerator generator, DependencyObject target)
		{
			var panel = target as Panel;
			return panel != null && panel.IsItemsHost && generator == generator.GetItemContainerGeneratorForPanel(panel);
		}
	}
}
