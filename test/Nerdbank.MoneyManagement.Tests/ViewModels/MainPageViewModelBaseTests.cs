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
		Assert.False(this.MainPageViewModel.FileCloseCommand.CanExecute(null));
		TestUtilities.AssertCommandCanExecuteChanged(this.MainPageViewModel.FileCloseCommand, () => this.MainPageViewModel.ReplaceViewModel(this.DocumentViewModel));
		Assert.True(this.MainPageViewModel.FileCloseCommand.CanExecute(null));
		Assert.True(this.MainPageViewModel.IsFileOpen);
		TestUtilities.AssertCommandCanExecuteChanged(this.MainPageViewModel.FileCloseCommand, () => this.MainPageViewModel.FileCloseCommand.Execute(null));
		Assert.False(this.MainPageViewModel.FileCloseCommand.CanExecute(null));
		Assert.False(this.MainPageViewModel.IsFileOpen);
	}

	[Fact]
	public void ImportCommand_HiddenWithoutAnOpenDocument()
	{
		Assert.False(this.MainPageViewModel.ImportFileCommand.CanExecute());
		Assert.False(this.MainPageViewModel.ImportFileCommand.Visible);
	}

	[Fact]
	public void ImportCommand_EnabledWithOpenDocumentWithOrWithoutAccounts()
	{
		this.LoadDocument();

		Assert.Empty(this.DocumentViewModel.AccountsPanel.Accounts);
		Assert.True(this.MainPageViewModel.ImportFileCommand.CanExecute());
		Assert.True(this.MainPageViewModel.ImportFileCommand.Visible);

		var account = this.DocumentViewModel.AccountsPanel.NewAccount(Account.AccountType.Banking, "Checking");
		Assert.True(this.MainPageViewModel.ImportFileCommand.CanExecute());
		Assert.True(this.MainPageViewModel.ImportFileCommand.Visible);

		this.DocumentViewModel.BankingPanel.SelectedAccount = account;
		Assert.True(this.MainPageViewModel.ImportFileCommand.CanExecute());
		Assert.True(this.MainPageViewModel.ImportFileCommand.Visible);
	}
}
