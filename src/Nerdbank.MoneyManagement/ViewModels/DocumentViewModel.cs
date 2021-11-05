// Copyright (c) Andrew Arnott. All rights reserved.
// Licensed under the Ms-PL license. See LICENSE.txt file in the project root for full license information.

namespace Nerdbank.MoneyManagement.ViewModels
{
	using System;
	using System.Collections;
	using System.Collections.Generic;
	using System.Collections.ObjectModel;
	using System.Collections.Specialized;
	using System.ComponentModel;
	using System.Diagnostics;
	using System.Diagnostics.CodeAnalysis;
	using System.IO;
	using System.Linq;
	using System.Threading;
	using System.Threading.Tasks;
	using Microsoft;
	using PCLCommandBase;

	[DebuggerDisplay("{" + nameof(DebuggerDisplay) + ",nq}")]
	public class DocumentViewModel : BindableBase, IDisposable
	{
		private readonly bool ownsMoneyFile;
		private readonly SortedObservableCollection<ITransactionTarget> transactionTargets = new(TransactionTargetSort.Instance);
		private decimal netWorth;
		private IList? selectedTransactions;
		private TransactionViewModel? selectedTransaction;

		public DocumentViewModel()
			: this(null)
		{
		}

		public DocumentViewModel(MoneyFile? moneyFile, bool ownsMoneyFile = true)
		{
			this.MoneyFile = moneyFile;
			this.ownsMoneyFile = ownsMoneyFile;

			this.BankingPanel = new();
			this.CategoriesPanel = new(this);
			this.AccountsPanel = new(this);

			this.transactionTargets.Add(SplitCategoryPlaceholder.Singleton);

			// Keep targets collection in sync with the two collections that make it up.
			this.CategoriesPanel.Categories.CollectionChanged += this.Categories_CollectionChanged;

			if (moneyFile is object)
			{
				foreach (Account account in moneyFile.Accounts)
				{
					AccountViewModel viewModel = new(account, this);
					this.AccountsPanel.Add(viewModel);
					this.BankingPanel.Add(viewModel);
				}

				foreach (Category category in moneyFile.Categories)
				{
					CategoryViewModel viewModel = new(category, moneyFile);
					this.CategoriesPanel.Categories.Add(viewModel);
				}

				this.netWorth = moneyFile.GetNetWorth(new MoneyFile.NetWorthQueryOptions { AsOfDate = DateTime.Now });
				moneyFile.EntitiesChanged += this.Model_EntitiesChanged;
			}

			this.DeleteTransactionsCommand = new DeleteTransactionCommandImpl(this);
		}

		public bool IsFileOpen => this.MoneyFile is object;

		public string Title => this.MoneyFile is { Path: string path } ? $"Nerdbank Money Management - {Path.GetFileNameWithoutExtension(path)}" : "Nerdbank Money Management";

		public decimal NetWorth
		{
			get => this.netWorth;
			set => this.SetProperty(ref this.netWorth, value);
		}

		public BankingPanelViewModel BankingPanel { get; }

		public AccountsPanelViewModel AccountsPanel { get; }

		public CategoriesPanelViewModel CategoriesPanel { get; }

		public IReadOnlyCollection<ITransactionTarget> TransactionTargets => this.transactionTargets;

		public TransactionViewModel? SelectedTransaction
		{
			get => this.selectedTransaction;
			set => this.SetProperty(ref this.selectedTransaction, value);
		}

		/// <summary>
		/// Gets or sets a collection of selected transactions.
		/// </summary>
		/// <remarks>
		/// This is optional. When set, the <see cref="DeleteTransactionsCommand"/> will use this collection as the set of transactions to delete.
		/// When not set, the <see cref="SelectedTransaction"/> will be used by the <see cref="DeleteTransactionsCommand"/>.
		/// </remarks>
		public IList? SelectedTransactions
		{
			get => this.selectedTransactions;
			set => this.SetProperty(ref this.selectedTransactions, value);
		}

		/// <summary>
		/// Gets a command that deletes all transactions in the <see cref="SelectedTransactions"/> collection, if that property is set;
		/// otherwise the <see cref="SelectedTransaction"/> is deleted.
		/// </summary>
		public CommandBase DeleteTransactionsCommand { get; }

		public string DeleteCommandCaption => "_Delete";

		internal MoneyFile? MoneyFile { get; }

		private string DebuggerDisplay => this.MoneyFile?.Path ?? "(not backed by a file)";

		public static DocumentViewModel CreateNew(string moneyFilePath)
		{
			if (File.Exists(moneyFilePath))
			{
				File.Delete(moneyFilePath);
			}

			return CreateNew(MoneyFile.Load(moneyFilePath));
		}

		public static DocumentViewModel CreateNew(MoneyFile model)
		{
			try
			{
				TemplateData.InjectTemplateData(model);
				return new DocumentViewModel(model);
			}
			catch
			{
				model.Dispose();
				throw;
			}
		}

		public static DocumentViewModel Open(string moneyFilePath)
		{
			if (!File.Exists(moneyFilePath))
			{
				throw new FileNotFoundException("Unable to find MoneyMan file.", moneyFilePath);
			}

			MoneyFile model = MoneyFile.Load(moneyFilePath);
			try
			{
				return new DocumentViewModel(model);
			}
			catch
			{
				model.Dispose();
				throw;
			}
		}

		public AccountViewModel GetAccount(int accountId) => this.BankingPanel?.Accounts.SingleOrDefault(acc => acc.Id == accountId) ?? throw new ArgumentException("No match found.");

		public CategoryViewModel GetCategory(int categoryId) => this.CategoriesPanel?.Categories.SingleOrDefault(cat => cat.Id == categoryId) ?? throw new ArgumentException("No match found.");

		public void Dispose()
		{
			if (this.MoneyFile is object)
			{
				this.MoneyFile.EntitiesChanged -= this.Model_EntitiesChanged;
				if (this.ownsMoneyFile)
				{
					this.MoneyFile.Dispose();
				}
			}
		}

		internal void AddTransactionTarget(ITransactionTarget target) => this.transactionTargets.Add(target);

		internal void RemoveTransactionTarget(ITransactionTarget target) => this.transactionTargets.Remove(target);

		private static void ThrowUnopenedUnless([DoesNotReturnIf(false)] bool condition)
		{
			if (!condition)
			{
				throw new InvalidOperationException("A file must be open.");
			}
		}

		private void Model_EntitiesChanged(object? sender, MoneyFile.EntitiesChangedEventArgs e)
		{
			Assumes.NotNull(this.MoneyFile);

			HashSet<int> impactedAccountIds = new();
			SearchForImpactedAccounts(e.Inserted);
			SearchForImpactedAccounts(e.Deleted);
			SearchForImpactedAccounts(e.Changed.Select(c => c.Before).Concat(e.Changed.Select(c => c.After)));
			foreach (AccountViewModel accountViewModel in this.BankingPanel.Accounts)
			{
				if (accountViewModel.Model is object && accountViewModel.Id.HasValue && impactedAccountIds.Contains(accountViewModel.Id.Value))
				{
					accountViewModel.Balance = this.MoneyFile.GetBalance(accountViewModel.Model);

					foreach (ModelBase model in e.Inserted)
					{
						if (model is Transaction tx && IsRelated(tx, accountViewModel))
						{
							accountViewModel.NotifyTransactionChanged(tx);
						}

						if (model is SplitTransaction split && IsRelatedSplit(split, accountViewModel))
						{
							accountViewModel.NotifyTransactionChanged(split);
						}
					}

					foreach ((ModelBase Before, ModelBase After) models in e.Changed)
					{
						if (models is { Before: Transaction beforeTx, After: Transaction afterTx } && (IsRelated(beforeTx, accountViewModel) || IsRelated(afterTx, accountViewModel)))
						{
							accountViewModel.NotifyTransactionChanged(afterTx);
						}

						if (models is { Before: SplitTransaction beforeSplitTx, After: SplitTransaction afterSplitTx } && (IsRelatedSplit(beforeSplitTx, accountViewModel) || IsRelatedSplit(afterSplitTx, accountViewModel)))
						{
							accountViewModel.NotifyTransactionChanged(afterSplitTx);
						}
					}

					foreach (ModelBase model in e.Deleted)
					{
						if (model is Transaction tx && (tx.CreditAccountId == accountViewModel.Id || tx.DebitAccountId == accountViewModel.Id))
						{
							accountViewModel.NotifyTransactionDeleted(tx);
						}

						if (model is SplitTransaction split && (split.CreditAccountId == accountViewModel.Id || split.DebitAccountId == accountViewModel.Id))
						{
							accountViewModel.NotifyTransactionDeleted(split);
						}
					}
				}
			}

			static bool IsRelated(Transaction tx, AccountViewModel accountViewModel) => tx.CreditAccountId == accountViewModel.Id || tx.DebitAccountId == accountViewModel.Id;
			static bool IsRelatedSplit(SplitTransaction tx, AccountViewModel accountViewModel) => tx.CreditAccountId == accountViewModel.Id || tx.DebitAccountId == accountViewModel.Id;

			void SearchForImpactedAccounts(IEnumerable<ModelBase> models)
			{
				foreach (ModelBase model in models)
				{
					if (model is Transaction tx)
					{
						if (tx.CreditAccountId is int creditId)
						{
							impactedAccountIds.Add(creditId);
						}

						if (tx.DebitAccountId is int debitId)
						{
							impactedAccountIds.Add(debitId);
						}
					}
					else if (model is SplitTransaction splitTransaction)
					{
						if (splitTransaction.CreditAccountId is int creditId)
						{
							impactedAccountIds.Add(creditId);
						}

						if (splitTransaction.DebitAccountId is int debitId)
						{
							impactedAccountIds.Add(debitId);
						}
					}
				}
			}

			this.NetWorth = this.MoneyFile.GetNetWorth(new MoneyFile.NetWorthQueryOptions { AsOfDate = DateTime.Now });
		}

		private void Categories_CollectionChanged(object? sender, NotifyCollectionChangedEventArgs e)
		{
			switch (e.Action)
			{
				case NotifyCollectionChangedAction.Add when e.NewItems is object:
					foreach (CategoryViewModel category in e.NewItems)
					{
						this.AddTransactionTarget(category);
					}

					break;
				case NotifyCollectionChangedAction.Remove when e.OldItems is object:
					foreach (CategoryViewModel category in e.OldItems)
					{
						this.RemoveTransactionTarget(category);
					}

					break;
			}
		}

		private class DeleteTransactionCommandImpl : CommandBase
		{
			private readonly DocumentViewModel viewModel;
			private INotifyCollectionChanged? subscribedSelectedTransactions;

			internal DeleteTransactionCommandImpl(DocumentViewModel viewModel)
			{
				this.viewModel = viewModel;
				this.viewModel.PropertyChanged += this.ViewModel_PropertyChanged;
				this.SubscribeToSelectionChanged();
			}

			public override bool CanExecute(object? parameter = null) => base.CanExecute(parameter) && (this.viewModel.SelectedTransactions?.Count > 0 || this.viewModel.SelectedTransaction is object);

			protected override Task ExecuteCoreAsync(object? parameter, CancellationToken cancellationToken)
			{
				if (this.viewModel.SelectedTransactions is object)
				{
					foreach (TransactionViewModel transaction in this.viewModel.SelectedTransactions.OfType<TransactionViewModel>().ToList())
					{
						transaction.ThisAccount.DeleteTransaction(transaction);
					}
				}
				else if (this.viewModel.SelectedTransaction is { } transaction)
				{
					transaction.ThisAccount.DeleteTransaction(this.viewModel.SelectedTransaction);
				}

				return Task.CompletedTask;
			}

			private void ViewModel_PropertyChanged(object? sender, PropertyChangedEventArgs e)
			{
				if (e.PropertyName is nameof(this.viewModel.SelectedTransactions))
				{
					this.SubscribeToSelectionChanged();
				}
				else if (e.PropertyName is nameof(this.viewModel.SelectedTransaction))
				{
					this.OnCanExecuteChanged();
				}
			}

			private void SelectedTransactions_CollectionChanged(object? sender, NotifyCollectionChangedEventArgs e) => this.OnCanExecuteChanged();

			private void SubscribeToSelectionChanged()
			{
				if (this.subscribedSelectedTransactions is object)
				{
					this.subscribedSelectedTransactions.CollectionChanged -= this.SelectedTransactions_CollectionChanged;
				}

				this.subscribedSelectedTransactions = this.viewModel.SelectedTransactions as INotifyCollectionChanged;

				if (this.subscribedSelectedTransactions is object)
				{
					this.subscribedSelectedTransactions.CollectionChanged += this.SelectedTransactions_CollectionChanged;
				}
			}
		}

		private class TransactionTargetSort : IComparer<ITransactionTarget>
		{
			internal static readonly TransactionTargetSort Instance = new();

			private TransactionTargetSort()
			{
			}

			public int Compare(ITransactionTarget? x, ITransactionTarget? y)
			{
				if (x is null)
				{
					return y is null ? 0 : -1;
				}
				else if (y is null)
				{
					return 1;
				}

				if (x.GetType() != y.GetType())
				{
					// First list categories.
					int index =
						x is CategoryViewModel && y is not CategoryViewModel ? -1 :
						y is CategoryViewModel && x is not CategoryViewModel ? 1 :
						0;
					if (index != 0)
					{
						return index;
					}

					// Then list the special split target.
					index =
						x is SplitCategoryPlaceholder ? -1 :
						y is SplitCategoryPlaceholder ? 1 :
						0;
					if (index != 0)
					{
						return index;
					}
				}

				return StringComparer.CurrentCultureIgnoreCase.Compare(x.Name, y.Name);
			}
		}
	}
}
