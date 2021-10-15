// Copyright (c) Andrew Arnott. All rights reserved.
// Licensed under the Ms-PL license. See LICENSE.txt file in the project root for full license information.

namespace MoneyMan.Avl.Views
{
	using Avalonia;
	using Avalonia.Controls;
	using Avalonia.Markup.Xaml;

	public partial class MainWindow : Window
	{
		public MainWindow()
		{
			this.InitializeComponent();
#if DEBUG
			this.AttachDevTools();
#endif
		}

		private void InitializeComponent()
		{
			AvaloniaXamlLoader.Load(this);
		}
	}
}
