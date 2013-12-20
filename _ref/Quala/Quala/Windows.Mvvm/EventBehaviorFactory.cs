using System;
using System.Windows;
using System.Windows.Input;

namespace Quala.Windows.Mvvm
{
	/// <summary>
	/// EventBehavior作成補助クラス。
	/// RoutedEventをコマンドとして使うためのラッパー。
	/// based on http://blog.functionalfun.net/2008/09/hooking-up-commands-to-events-in-wpf.html
	/// </summary>
	/// <example>
	/// [EXAMPLE A] // MouseDoubleClickEventに反応するDoubleClickCommandを作成
	/// public static class ControlBehaviours
	/// {
	///		public static readonly DependencyProperty DoubleClickCommand =
	///			EventBehaviourFactory.CreateCommandExecutionEventBehaviour(
	///				Control.MouseDoubleClickEvent, "DoubleClickCommand", typeof (ControlBehaviours));
	///		public static void SetDoubleClickCommand(Control o, ICommand command) { o.SetValue(DoubleClickCommand, command); }
	///		public static void GetDoubleClickCommand(Control o) { o.GetValue(DoubleClickCommand); }
	///	}
	///	
	/// [EXAMPLE B] // TextChangedEventに反応するTextChangedCommandを作成
	/// public static class TextBoxBehaviour
	/// {
	///		public static readonly DependencyProperty TextChangedCommand =
	///			EventBehaviourFactory.CreateCommandExecutionEventBehaviour(
	///				TextBox.TextChangedEvent, "TextChangedCommand", typeof (TextBoxBehaviour));
	///		public static void SetTextChangedCommand(DependencyObject o, ICommand value) { o.SetValue(TextChangedCommand, value); }
	///		public static ICommand GetTextChangedCommand(DependencyObject o) { return o.GetValue(TextChangedCommand) as ICommand; }
	///	}
	/// </example>
	public static class EventBehaviorFactory
	{
		public static DependencyProperty CreateCommandExecutionEventBehavior(
			RoutedEvent routedEvent, string propertyName, Type ownerType)
		{
			return DependencyProperty.RegisterAttached(
				propertyName, typeof(ICommand), ownerType,
				new PropertyMetadata(null, new ExecuteCommandOnRoutedEventBehavior(routedEvent).PropertyChangedHandler));
		}

		/// <summary>
		/// An internal class to handle listening for an event and executing a command,
		/// when a Command is assigned to a particular DependencyProperty
		/// </summary>
		class ExecuteCommandOnRoutedEventBehavior : ExecuteCommandBehavior
		{
			readonly RoutedEvent routedEvent;
			
			public ExecuteCommandOnRoutedEventBehavior(RoutedEvent routedEvent)
			{
				this.routedEvent = routedEvent;
			}

			/// <summary>
			/// Handles attaching or Detaching Event handlers when a Command is assigned or unassigned
			/// </summary>
			/// <param name="sender"></param>
			/// <param name="oldValue"></param>
			/// <param name="newValue"></param>
			protected override void AdjustEventHandlers(DependencyObject sender, object oldValue, object newValue)
			{
				var element = sender as UIElement;
				if (element == null) return;

				if (oldValue != null)
					element.RemoveHandler(routedEvent, new RoutedEventHandler(EventHandler));

				if (newValue != null)
					element.AddHandler(routedEvent, new RoutedEventHandler(EventHandler));
			}

			protected void EventHandler(object sender, RoutedEventArgs e)
			{
				HandleEvent(sender, e);
			}

		}

		internal abstract class ExecuteCommandBehavior
		{
			protected DependencyProperty property;

			protected abstract void AdjustEventHandlers(DependencyObject sender, object oldValue, object newValue);

			protected void HandleEvent(object sender, EventArgs e)
			{
				var dp = sender as DependencyObject;
				if (dp == null) return;

				var command = dp.GetValue(property) as ICommand;
				if (command == null) return;

				if (command.CanExecute(e))
					command.Execute(e);
			}

			public void PropertyChangedHandler(DependencyObject sender, DependencyPropertyChangedEventArgs e)
			{
				// the first time the property changes,
				// make a note of which property we are supposed to be watching
				if (property == null)
					property = e.Property;

				AdjustEventHandlers(sender, e.OldValue, e.NewValue);
			}
		}
	}
}
