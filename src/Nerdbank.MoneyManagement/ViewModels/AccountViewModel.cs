// Copyright (c) Andrew Arnott. All rights reserved.
// Licensed under the Ms-PL license. See LICENSE.txt file in the project root for full license information.

namespace Nerdbank.MoneyManagement.ViewModels
{
	using System.Collections.ObjectModel;
	using System.Diagnostics;
	using System.Linq;
	using System.Threading;
	using System.Threading.Tasks;
	using System.Windows.Input;
	using PCLCommandBase;
	using Validation;

	[DebuggerDisplay("{" + nameof(DebuggerDisplay) + ",nq}")]
	public class AccountViewModel : EntityViewModel<Account>
	{
		private TransactionViewModel? selectedTransaction;
		private string? name;
		private bool isClosed;

		public AccountViewModel()
		{
			this.DeleteTransactionCommand = new DeleteTransactionCommandImpl(this);
		}

		public AccountViewModel(Account model)
			: this()
		{
			this.CopyFrom(model);
		}

		public string? Name
		{
			get => this.name;
			set => this.SetProperty(ref this.name, value);
		}

		public bool IsClosed
		{
			get => this.isClosed;
			set => this.SetProperty(ref this.isClosed, value);
		}

		public ObservableCollection<TransactionViewModel> Transactions { get; } = new();

		public TransactionViewModel? SelectedTransaction
		{
			get => this.selectedTransaction;
			set => this.SetProperty(ref this.selectedTransaction, value);
		}

		/// <summary>
		/// Gets a command that deletes the transactions where <see cref="TransactionViewModel.IsSelected"/> is <see langword="true"/>.
		/// </summary>
		public ICommand DeleteTransactionCommand { get; }

		private string? DebuggerDisplay => this.Name;

		public override void ApplyTo(Account account)
		{
			Requires.NotNull(account, nameof(account));

			account.Name = this.name;
			account.IsClosed = this.IsClosed;
		}

		public override void CopyFrom(Account account)
		{
			Requires.NotNull(account, nameof(account));

			this.Name = account.Name;
			this.IsClosed = account.IsClosed;
		}

		private class DeleteTransactionCommandImpl : CommandBase
		{
			private readonly AccountViewModel viewModel;

			internal DeleteTransactionCommandImpl(AccountViewModel viewModel)
			{
				this.viewModel = viewModel;
				this.viewModel.PropertyChanged += (s, e) =>
				{
					if (e.PropertyName == nameof(AccountViewModel.SelectedTransaction))
					{
						this.OnCanExecuteChanged();
					}
				};
			}

			public override bool CanExecute(object? parameter = null) => base.CanExecute(parameter) && this.viewModel.SelectedTransaction is object;

			protected override Task ExecuteCoreAsync(object? parameter, CancellationToken cancellationToken)
			{
				for (int i = this.viewModel.Transactions.Count - 1; i >= 0; i--)
				{
					if (this.viewModel.Transactions[i].IsSelected)
					{
						this.viewModel.Transactions.RemoveAt(i);
					}
				}

				return Task.CompletedTask;
			}
		}
	}
}
