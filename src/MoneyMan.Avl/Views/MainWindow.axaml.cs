// Copyright (c) Andrew Arnott. All rights reserved.
// Licensed under the Ms-PL license. See LICENSE.txt file in the project root for full license information.

using Avalonia;
using Avalonia.Controls;
using Avalonia.Markup.Xaml;
using MoneyMan.Avl.ViewModels;

namespace MoneyMan.Avl.Views;

public partial class MainWindow : Window
{
	public MainWindow()
	{
		this.InitializeComponent();
		this.DataContext = this.ViewModel;
		this.ViewModel.MainWindow = this;

#if DEBUG
		this.AttachDevTools();
#endif
	}

	public MainPageViewModel ViewModel
	{
		get => (MainPageViewModel)this.Resources["viewModel"]!;
		set => this.Resources["viewModel"] = value;
	}

	private void InitializeComponent()
	{
		AvaloniaXamlLoader.Load(this);
	}
}
