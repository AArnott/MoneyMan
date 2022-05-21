// Copyright (c) Andrew Arnott. All rights reserved.
// Licensed under the Ms-PL license. See LICENSE.txt file in the project root for full license information.

using System.Collections;
using System.Windows.Input;
using PCLCommandBase;

namespace Nerdbank.MoneyManagement.ViewModels;

public class PickerWindowViewModel : BindableBase, IPresentedWindowViewModel
{
	private readonly CommandImpl proceedCommand;
	private string proceedCommandTitle = "Proceed";
	private string cancelCommandTitle = "Cancel";
	private string message;
	private string? title;
	private object? selectedOption;
	private IList options;

	public PickerWindowViewModel(string message, IList options)
	{
		this.proceedCommand = new CommandImpl(delegate
		{
			this.ShouldProceed = true;
			this.Closing?.Invoke(this, EventArgs.Empty);
		});
		this.CancelCommand = new CommandImpl(delegate
		{
			this.Closing?.Invoke(this, EventArgs.Empty);
		});
		this.message = message;
		this.options = options;
	}

	public event EventHandler? Closing;

	public ICommand ProceedCommand => this.proceedCommand;

	public string ProceedCommandTitle
	{
		get => this.proceedCommandTitle;
		set => this.SetProperty(ref this.proceedCommandTitle, value);
	}

	public ICommand CancelCommand { get; }

	public string CancelCommandTitle
	{
		get => this.cancelCommandTitle;
		set => this.SetProperty(ref this.cancelCommandTitle, value);
	}

	public string? Title
	{
		get => this.title;
		set => this.SetProperty(ref this.title, value);
	}

	public string Message
	{
		get => this.message;
		set => this.SetProperty(ref this.message, value);
	}

	public IList Options
	{
		get => this.options;
		set => this.SetProperty(ref this.options, value);
	}

	public object? SelectedOption
	{
		get => this.selectedOption;
		set
		{
			this.SetProperty(ref this.selectedOption, value);
			this.proceedCommand.IsEnabled = value is object;
		}
	}

	public bool ShouldProceed { get; set; }

	internal object GetSelectedOptionOrThrowCancelled()
	{
		if (!this.ShouldProceed || this.SelectedOption is null)
		{
			throw new OperationCanceledException();
		}

		return this.SelectedOption;
	}

	private class CommandImpl : CommandBase
	{
		private readonly Action action;
		private bool isEnabled = true;

		internal CommandImpl(Action action)
		{
			this.action = action;
		}

		internal bool IsEnabled
		{
			get => this.isEnabled;
			set
			{
				this.isEnabled = value;
				this.OnCanExecuteChanged();
			}
		}

		public override bool CanExecute(object? parameter = null) => base.CanExecute(parameter) && this.IsEnabled;

		protected override Task ExecuteCoreAsync(object? parameter = null, CancellationToken cancellationToken = default)
		{
			this.action();
			return Task.CompletedTask;
		}
	}
}
