// Copyright (c) Andrew Arnott. All rights reserved.
// Licensed under the Ms-PL license. See LICENSE.txt file in the project root for full license information.

public class MainPageViewModelBaseTests : MoneyTestBase
{
	public MainPageViewModelBaseTests(ITestOutputHelper logger)
		: base(logger)
	{
	}

	[Fact]
	public void InitialState()
	{
		MainPageViewModelBase viewModel = new();
		Assert.False(viewModel.IsFileOpen);
	}

	[Fact]
	public void FileCloseCommand()
	{
		MainPageViewModelBase viewModel = new();
		Assert.False(viewModel.FileCloseCommand.CanExecute(null));
		TestUtilities.AssertCommandCanExecuteChanged(viewModel.FileCloseCommand, () => viewModel.ReplaceViewModel(this.DocumentViewModel));
		Assert.True(viewModel.FileCloseCommand.CanExecute(null));
		Assert.True(viewModel.IsFileOpen);
		TestUtilities.AssertCommandCanExecuteChanged(viewModel.FileCloseCommand, () => viewModel.FileCloseCommand.Execute(null));
		Assert.False(viewModel.FileCloseCommand.CanExecute(null));
		Assert.False(viewModel.IsFileOpen);
	}
}
