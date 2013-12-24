namespace __Primitives__
{
	using System;
	using System.Collections.Generic;
	using System.ComponentModel;
	using System.Linq;
	using System.Text;

	class NotificationObject : INotifyPropertyChanged
	{
		public event PropertyChangedEventHandler PropertyChanged;

		protected void RaisePropertyChanged(string propertyName)
		{
			var h = PropertyChanged;
			if (h != null)
			{
				h(this, new PropertyChangedEventArgs(propertyName));
			}
		}
	}
}
