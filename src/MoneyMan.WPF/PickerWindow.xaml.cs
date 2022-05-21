// Copyright (c) Andrew Arnott. All rights reserved.
// Licensed under the Ms-PL license. See LICENSE.txt file in the project root for full license information.

using System.Windows;
using Nerdbank.MoneyManagement.ViewModels;

namespace MoneyMan;

/// <summary>
/// Interaction logic for PickerWindow.xaml.
/// </summary>
public partial class PickerWindow : Window
{
	public PickerWindow(PickerWindowViewModel viewModel)
	{
		this.InitializeComponent();
		this.DataContext = viewModel;
	}
}
