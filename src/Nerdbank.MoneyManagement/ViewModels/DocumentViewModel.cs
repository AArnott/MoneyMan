// Copyright (c) Andrew Arnott. All rights reserved.
// Licensed under the Ms-PL license. See LICENSE.txt file in the project root for full license information.

namespace Nerdbank.MoneyManagement.ViewModels
{
	using System;
	using System.Collections.Generic;
	using System.Collections.ObjectModel;
	using System.Diagnostics;
	using System.IO;
	using System.Linq;
	using Microsoft;
	using PCLCommandBase;

	[DebuggerDisplay("{" + nameof(DebuggerDisplay) + ",nq}")]
	public class DocumentViewModel : BindableBase, IDisposable
	{
		private readonly MoneyFile? moneyFile;
		private decimal netWorth;

		public DocumentViewModel()
			: this(null)
		{
		}

		public DocumentViewModel(MoneyFile? moneyFile)
		{
			this.moneyFile = moneyFile;

			if (moneyFile is object)
			{
				this.AccountsPanel = new AccountsPanelViewModel();
				foreach (Account account in moneyFile.Accounts)
				{
					AccountViewModel viewModel = new(account, moneyFile, this);
					this.AccountsPanel.Accounts.Add(viewModel);
					this.TransactionTargets.Add(viewModel);
				}

				this.CategoriesPanel = new CategoriesPanelViewModel(moneyFile);
				foreach (Category category in moneyFile.Categories)
				{
					CategoryViewModel viewModel = new(category, moneyFile);
					this.CategoriesPanel.Categories.Add(viewModel);
					this.TransactionTargets.Add(viewModel);
				}

				// Keep targets collection in sync with the two collections that make it up.
				this.CategoriesPanel.Categories.CollectionChanged += this.Categories_CollectionChanged;
				this.AccountsPanel.Accounts.CollectionChanged += this.Accounts_CollectionChanged;

				this.netWorth = moneyFile.GetNetWorth(new MoneyFile.NetWorthQueryOptions { AsOfDate = DateTime.Now });
				moneyFile.EntitiesChanged += this.Model_EntitiesChanged;
			}
		}

		public bool IsFileOpen => this.moneyFile is object;

		public string Title => this.moneyFile is { Path: string path } ? $"Nerdbank Money Management - {Path.GetFileNameWithoutExtension(path)}" : "Nerdbank Money Management";

		public decimal NetWorth
		{
			get => this.netWorth;
			set => this.SetProperty(ref this.netWorth, value);
		}

		public AccountsPanelViewModel? AccountsPanel { get; }

		public CategoriesPanelViewModel? CategoriesPanel { get; }

		public ObservableCollection<ITransactionTarget> TransactionTargets { get; } = new();

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
		/// <returns>The new <see cref="AccountViewModel"/>.</returns>
		public AccountViewModel NewAccount()
		{
			AccountViewModel viewModel = new(null, this.moneyFile, this)
			{
				Model = new Account(),
			};

			if (this.AccountsPanel is object)
			{
				this.AccountsPanel.Accounts.Add(viewModel);
			}

			return viewModel;
		}

		public void DeleteAccount(AccountViewModel account)
		{
			this.AccountsPanel?.Accounts.Remove(account);
			if (this.AccountsPanel?.SelectedAccount == account)
			{
				this.AccountsPanel.SelectedAccount = null;
			}

			if (this.moneyFile is object && account.Model is object)
			{
				this.moneyFile.Delete(account.Model);
			}
		}

		public AccountViewModel GetAccount(int accountId) => this.AccountsPanel?.Accounts.SingleOrDefault(acc => acc.Id == accountId) ?? throw new ArgumentException("No match found.");

		public CategoryViewModel GetCategory(int categoryId) => this.CategoriesPanel?.Categories.SingleOrDefault(cat => cat.Id == categoryId) ?? throw new ArgumentException("No match found.");

		public void Dispose()
		{
			if (this.moneyFile is object)
			{
				this.moneyFile.EntitiesChanged -= this.Model_EntitiesChanged;
				this.moneyFile.Dispose();
			}
		}

		private void Model_EntitiesChanged(object? sender, MoneyFile.EntitiesChangedEventArgs e)
		{
			Assumes.NotNull(this.moneyFile);

			if (this.AccountsPanel is object)
			{
				HashSet<int> impactedAccountIds = new();
				SearchForImpactedAccounts(e.Inserted);
				SearchForImpactedAccounts(e.Deleted);
				SearchForImpactedAccounts(e.Changed.Select(c => c.Before).Concat(e.Changed.Select(c => c.After)));
				foreach (AccountViewModel accountViewModel in this.AccountsPanel.Accounts)
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
	}
}
