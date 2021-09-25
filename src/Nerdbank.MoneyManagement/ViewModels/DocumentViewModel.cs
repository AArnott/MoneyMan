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
		private readonly MoneyFile? moneyFile;
		private decimal netWorth;
		private IList? selectedTransactions;
		private TransactionViewModel? selectedTransaction;

		public DocumentViewModel()
			: this(null)
		{
		}

		public DocumentViewModel(MoneyFile? moneyFile)
		{
			this.moneyFile = moneyFile;

			this.BankingPanel = new();
			this.CategoriesPanel = new(this);

			// Keep targets collection in sync with the two collections that make it up.
			this.CategoriesPanel.Categories.CollectionChanged += this.Categories_CollectionChanged;
			this.BankingPanel.Accounts.CollectionChanged += this.Accounts_CollectionChanged;

			if (moneyFile is object)
			{
				foreach (Account account in moneyFile.Accounts)
				{
					AccountViewModel viewModel = new(account, moneyFile, this);
					this.BankingPanel.Accounts.Add(viewModel);
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

		public bool IsFileOpen => this.moneyFile is object;

		public string Title => this.moneyFile is { Path: string path } ? $"Nerdbank Money Management - {Path.GetFileNameWithoutExtension(path)}" : "Nerdbank Money Management";

		public decimal NetWorth
		{
			get => this.netWorth;
			set => this.SetProperty(ref this.netWorth, value);
		}

		public BankingPanelViewModel BankingPanel { get; }

		public CategoriesPanelViewModel CategoriesPanel { get; }

		public ObservableCollection<ITransactionTarget> TransactionTargets { get; } = new();

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

		private string DebuggerDisplay => this.moneyFile?.Path ?? "(not backed by a file)";

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

		/// <summary>
		/// Creates a new <see cref="Account"/> and <see cref="AccountViewModel"/>.
		/// The <see cref="AccountViewModel"/> is added to the view model collection,
		/// but the <see cref="Account"/> will only be added to the database when a property on it has changed.
		/// </summary>
		/// <param name="name">The name for the new account.</param>
		/// <returns>The new <see cref="AccountViewModel"/>.</returns>
		public AccountViewModel NewAccount(string? name = null)
		{
			AccountViewModel viewModel = new(null, this.moneyFile, this)
			{
				Model = new Account(),
			};

			if (this.BankingPanel is object)
			{
				this.BankingPanel.Accounts.Add(viewModel);
			}

			if (name is object)
			{
				viewModel.Name = name;
			}

			return viewModel;
		}

		public void DeleteAccount(AccountViewModel account)
		{
			this.BankingPanel?.Accounts.Remove(account);
			if (this.BankingPanel?.SelectedAccount == account)
			{
				this.BankingPanel.SelectedAccount = null;
			}

			if (this.moneyFile is object && account.Model is object)
			{
				this.moneyFile.Delete(account.Model);
			}
		}

		public AccountViewModel GetAccount(int accountId) => this.BankingPanel?.Accounts.SingleOrDefault(acc => acc.Id == accountId) ?? throw new ArgumentException("No match found.");

		public CategoryViewModel GetCategory(int categoryId) => this.CategoriesPanel?.Categories.SingleOrDefault(cat => cat.Id == categoryId) ?? throw new ArgumentException("No match found.");

		/// <summary>
		/// Creates a new <see cref="TransactionViewModel"/> for this account,
		/// but does <em>not</em> add it to the collection.
		/// </summary>
		/// <returns>A new <see cref="TransactionViewModel"/> for an uninitialized transaction.</returns>
		public TransactionViewModel NewTransaction()
		{
			if (this.BankingPanel?.SelectedAccount is null)
			{
				throw new InvalidOperationException("No account is selected.");
			}

			TransactionViewModel viewModel = new(this.BankingPanel.SelectedAccount, null, this.moneyFile);
			viewModel.When = DateTime.Now;
			viewModel.Model = new();
			return viewModel;
		}

		public void DeleteTransaction(TransactionViewModel transaction)
		{
			transaction.ThisAccount.Transactions.Remove(transaction);
			if (this.moneyFile is object && transaction.Model is object)
			{
				this.moneyFile.Delete(transaction.Model);
			}
		}

		public CategoryViewModel NewCategory(string name = "")
		{
			CategoryViewModel newCategoryViewModel = new(null, this.moneyFile);
			newCategoryViewModel.Model = new Category();
			this.CategoriesPanel.Categories.Add(newCategoryViewModel);
			this.CategoriesPanel.SelectedCategory = newCategoryViewModel;
			this.CategoriesPanel.AddingCategory = newCategoryViewModel;
			newCategoryViewModel.Name = name;
			return newCategoryViewModel;
		}

		public void DeleteCategory(CategoryViewModel categoryViewModel)
		{
			ThrowUnopenedUnless(this.moneyFile is object);

			this.CategoriesPanel.Categories.Remove(categoryViewModel);
			if (categoryViewModel.Model is object)
			{
				this.moneyFile.Delete(categoryViewModel.Model);
			}

			if (this.CategoriesPanel.SelectedCategory == categoryViewModel)
			{
				this.CategoriesPanel.SelectedCategory = null;
			}
		}

		public void Dispose()
		{
			if (this.moneyFile is object)
			{
				this.moneyFile.EntitiesChanged -= this.Model_EntitiesChanged;
				this.moneyFile.Dispose();
			}
		}

		private static void ThrowUnopenedUnless([DoesNotReturnIf(false)] bool condition)
		{
			if (!condition)
			{
				throw new InvalidOperationException("A file must be open.");
			}
		}

		private void Model_EntitiesChanged(object? sender, MoneyFile.EntitiesChangedEventArgs e)
		{
			Assumes.NotNull(this.moneyFile);

			if (this.BankingPanel is object)
			{
				HashSet<int> impactedAccountIds = new();
				SearchForImpactedAccounts(e.Inserted);
				SearchForImpactedAccounts(e.Deleted);
				SearchForImpactedAccounts(e.Changed.Select(c => c.Before).Concat(e.Changed.Select(c => c.After)));
				foreach (AccountViewModel accountViewModel in this.BankingPanel.Accounts)
				{
					if (accountViewModel.Model is object && accountViewModel.Id.HasValue && impactedAccountIds.Contains(accountViewModel.Id.Value))
					{
						accountViewModel.Balance = this.moneyFile.GetBalance(accountViewModel.Model);

						foreach (ModelBase model in e.Inserted)
						{
							if (model is Transaction tx && IsRelated(tx, accountViewModel))
							{
								accountViewModel.NotifyTransactionChanged(tx);
							}
						}

						foreach ((ModelBase Before, ModelBase After) models in e.Changed)
						{
							if (models is { Before: Transaction beforeTx, After: Transaction afterTx } && (IsRelated(beforeTx, accountViewModel) || IsRelated(afterTx, accountViewModel)))
							{
								accountViewModel.NotifyTransactionChanged(afterTx);
							}
						}

						foreach (ModelBase model in e.Deleted)
						{
							if (model is Transaction tx && (tx.CreditAccountId == accountViewModel.Id || tx.DebitAccountId == accountViewModel.Id))
							{
								accountViewModel.NotifyTransactionDeleted(tx);
							}
						}
					}
				}

				static bool IsRelated(Transaction tx, AccountViewModel accountViewModel) => tx.CreditAccountId == accountViewModel.Id || tx.DebitAccountId == accountViewModel.Id;

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
					}
				}
			}

			this.NetWorth = this.moneyFile.GetNetWorth(new MoneyFile.NetWorthQueryOptions { AsOfDate = DateTime.Now });
		}

		private void Accounts_CollectionChanged(object? sender, System.Collections.Specialized.NotifyCollectionChangedEventArgs e)
		{
			switch (e.Action)
			{
				case System.Collections.Specialized.NotifyCollectionChangedAction.Add when e.NewItems is object:
					foreach (AccountViewModel account in e.NewItems)
					{
						this.TransactionTargets.Add(account);
					}

					break;
				case System.Collections.Specialized.NotifyCollectionChangedAction.Remove when e.OldItems is object:
					foreach (AccountViewModel account in e.OldItems)
					{
						this.TransactionTargets.Remove(account);
					}

					break;
			}
		}

		private void Categories_CollectionChanged(object? sender, System.Collections.Specialized.NotifyCollectionChangedEventArgs e)
		{
			switch (e.Action)
			{
				case System.Collections.Specialized.NotifyCollectionChangedAction.Add when e.NewItems is object:
					foreach (CategoryViewModel category in e.NewItems)
					{
						this.TransactionTargets.Add(category);
					}

					break;
				case System.Collections.Specialized.NotifyCollectionChangedAction.Remove when e.OldItems is object:
					foreach (CategoryViewModel category in e.OldItems)
					{
						this.TransactionTargets.Remove(category);
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
						this.viewModel.DeleteTransaction(transaction);
					}
				}
				else if (this.viewModel.SelectedTransaction is object)
				{
					this.viewModel.DeleteTransaction(this.viewModel.SelectedTransaction);
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
	}
}
