// Copyright (c) Andrew Arnott. All rights reserved.
// Licensed under the Ms-PL license. See LICENSE.txt file in the project root for full license information.

using System.Collections;
using System.Collections.Specialized;
using System.ComponentModel;
using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;
using Microsoft;
using PCLCommandBase;

namespace Nerdbank.MoneyManagement.ViewModels;

[DebuggerDisplay("{" + nameof(DebuggerDisplay) + ",nq}")]
public class DocumentViewModel : BindableBase, IDisposable
{
	private readonly bool ownsMoneyFile;
	private readonly SortedObservableCollection<ITransactionTarget> transactionTargets = new(TransactionTargetSort.Instance);
	private decimal netWorth;
	private IList? selectedTransactions;
	private TransactionViewModel? selectedTransaction;
	private SelectableViews selectedViewIndex;

	public DocumentViewModel()
		: this(null)
	{
	}

	public DocumentViewModel(MoneyFile? moneyFile, bool ownsMoneyFile = true)
	{
		this.MoneyFile = moneyFile;
		this.ownsMoneyFile = ownsMoneyFile;

		this.BankingPanel = new();
		this.AssetsPanel = new(this);
		this.CategoriesPanel = new(this);
		this.AccountsPanel = new(this);

		// Keep targets collection in sync with the two collections that make it up.
		this.CategoriesPanel.Categories.CollectionChanged += this.Categories_CollectionChanged;
		this.Reset();

		if (moneyFile is object)
		{
			moneyFile.EntitiesChanged += this.Model_EntitiesChanged;
			moneyFile.PropertyChanged += this.MoneyFile_PropertyChanged;
		}

		this.DeleteTransactionsCommand = new DeleteTransactionCommandImpl(this);
		this.JumpToSplitTransactionParentCommand = new JumpToSplitTransactionParentCommandImpl(this);
		this.UndoCommand = new UndoCommandImpl(this);
		this.SaveCommand = new SaveCommandImpl(this);
	}

	public enum SelectableViews
	{
		/// <summary>
		/// The banking tab.
		/// </summary>
		Banking = 0,

		/// <summary>
		/// The assets tax.
		/// </summary>
		Assets,

		/// <summary>
		/// The accounts tab.
		/// </summary>
		Accounts,

		/// <summary>
		/// The categories tab.
		/// </summary>
		Categories,
	}

	public bool IsFileOpen => this.MoneyFile is object;

	public string Title => this.MoneyFile is { Path: string path } ? $"Nerdbank Money Management - {Path.GetFileNameWithoutExtension(path)}" : "Nerdbank Money Management";

	public decimal NetWorth
	{
		get => this.netWorth;
		set => this.SetProperty(ref this.netWorth, value);
	}

	public BankingPanelViewModel BankingPanel { get; }

	public AssetsPanelViewModel AssetsPanel { get; }

	public AccountsPanelViewModel AccountsPanel { get; }

	public CategoriesPanelViewModel CategoriesPanel { get; }

	public SelectableViews SelectedViewIndex
	{
		get => this.selectedViewIndex;
		set => this.SetProperty(ref this.selectedViewIndex, value);
	}

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

	/// <summary>
	/// Gets a command that jumps from a split member to its parent transaction in another account.
	/// </summary>
	public CommandBase JumpToSplitTransactionParentCommand { get; }

	public CommandBase UndoCommand { get; }

	public CommandBase SaveCommand { get; }

	public string DeleteCommandCaption => "_Delete";

	public string JumpToSplitTransactionParentCommandCaption => "_Jump to split parent";

	public string UndoCommandCaption => this.MoneyFile?.UndoStack.FirstOrDefault().Activity is string top ? $"Undo {top}" : "Undo";

	/// <summary>
	/// Gets or sets a view-supplied mechanism to prompt or notify the user.
	/// </summary>
	public IUserNotification? UserNotification { get; set; }

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

	public void Save() => this.MoneyFile?.Save();

	/// <summary>
	/// Reconstructs the entire view model graph given arbitrary changes that may have been made to the database.
	/// </summary>
	public void Reset()
	{
		this.transactionTargets.Clear();
		this.transactionTargets.Add(SplitCategoryPlaceholder.Singleton);

		if (this.MoneyFile is object)
		{
			this.AssetsPanel.ClearViewModel();
			this.AccountsPanel.ClearViewModel();
			this.BankingPanel.ClearViewModel();
			foreach (Account account in this.MoneyFile.Accounts)
			{
				AccountViewModel viewModel = new(account, this);
				this.AccountsPanel.Add(viewModel);
				this.BankingPanel.Add(viewModel);
			}

			this.CategoriesPanel.ClearViewModel();
			foreach (Category category in this.MoneyFile.Categories)
			{
				CategoryViewModel viewModel = new(category, this.MoneyFile);
				this.CategoriesPanel.Categories.Add(viewModel);
			}

			foreach (Asset asset in this.MoneyFile.Assets)
			{
				AssetViewModel viewModel = new(asset, this.MoneyFile);
				this.AssetsPanel.Add(viewModel);
			}

			this.netWorth = this.MoneyFile.GetNetWorth(new MoneyFile.NetWorthQueryOptions { AsOfDate = DateTime.Now });
		}
	}

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

	private void MoneyFile_PropertyChanged(object? sender, PropertyChangedEventArgs e)
	{
		if (e.PropertyName == nameof(this.MoneyFile.UndoStack))
		{
			this.OnPropertyChanged(nameof(this.UndoCommandCaption));
		}
	}

	/// <summary>
	/// Makes a best effort to select a given entity in the app.
	/// </summary>
	/// <param name="model">The model to select.</param>
	private void Select(ModelBase model)
	{
		switch (model)
		{
			case Transaction transaction:
				this.SelectedViewIndex = SelectableViews.Banking;
				int? accountid = transaction.CreditAccountId ?? transaction.DebitAccountId;
				if (accountid.HasValue)
				{
					AccountViewModel? account = this.BankingPanel.FindAccount(accountid.Value);
					if (account is object)
					{
						this.BankingPanel.SelectedAccount = account;
						TransactionViewModel? transactionViewModel = account.FindTransaction(transaction.Id);
						if (transactionViewModel is object)
						{
							this.SelectedTransaction = transactionViewModel;
						}
					}
				}

				break;
			case Category category:
				this.SelectedViewIndex = SelectableViews.Categories;
				this.CategoriesPanel.SelectedCategory = this.CategoriesPanel.FindCategory(category.Id);
				break;
			case Account account:
				this.SelectedViewIndex = SelectableViews.Accounts;
				this.AccountsPanel.SelectedAccount = this.AccountsPanel.FindAccount(account.Id);
				break;
		}
	}

	private abstract class SelectedTransactionCommandBase : CommandBase
	{
		private INotifyCollectionChanged? subscribedSelectedTransactions;

		internal SelectedTransactionCommandBase(DocumentViewModel viewModel)
		{
			this.ViewModel = viewModel;
			this.ViewModel.PropertyChanged += this.ViewModel_PropertyChanged;
			this.SubscribeToSelectionChanged();
			this.CanExecuteChanged += (s, e) => this.OnPropertyChanged(nameof(this.IsEnabled));
		}

		public bool IsEnabled => this.CanExecute();

		protected DocumentViewModel ViewModel { get; }

		public sealed override bool CanExecute(object? parameter = null)
		{
			if (!base.CanExecute(parameter))
			{
				return false;
			}

			if (this.ViewModel.SelectedTransactions is object)
			{
				// When multiple transaction selection is supported, enable the command if *any* of the selected commands are not splits in foreign accounts.
				return this.CanExecute(this.ViewModel.SelectedTransactions);
			}

			if (this.ViewModel.SelectedTransaction is object)
			{
				return this.CanExecute(new TransactionViewModel[] { this.ViewModel.SelectedTransaction });
			}

			return false;
		}

		protected abstract bool CanExecute(IList transactionViewModels);

		protected sealed override Task ExecuteCoreAsync(object? parameter = null, CancellationToken cancellationToken = default)
		{
			if (this.ViewModel.SelectedTransactions is object)
			{
				return this.ExecuteCoreAsync(this.ViewModel.SelectedTransactions, cancellationToken);
			}

			if (this.ViewModel.SelectedTransaction is object)
			{
				return this.ExecuteCoreAsync(new TransactionViewModel[] { this.ViewModel.SelectedTransaction }, cancellationToken);
			}

			return Task.CompletedTask;
		}

		protected abstract Task ExecuteCoreAsync(IList transactionViewModels, CancellationToken cancellationToken);

		private void ViewModel_PropertyChanged(object? sender, PropertyChangedEventArgs e)
		{
			if (e.PropertyName is nameof(this.ViewModel.SelectedTransactions))
			{
				this.SubscribeToSelectionChanged();
			}
			else if (e.PropertyName is nameof(this.ViewModel.SelectedTransaction))
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

			this.subscribedSelectedTransactions = this.ViewModel.SelectedTransactions as INotifyCollectionChanged;

			if (this.subscribedSelectedTransactions is object)
			{
				this.subscribedSelectedTransactions.CollectionChanged += this.SelectedTransactions_CollectionChanged;
			}
		}
	}

	private class DeleteTransactionCommandImpl : SelectedTransactionCommandBase
	{
		internal DeleteTransactionCommandImpl(DocumentViewModel viewModel)
			: base(viewModel)
		{
		}

		protected override bool CanExecute(IList transactionViewModels)
		{
			foreach (object item in transactionViewModels)
			{
				if (item is TransactionViewModel { IsSplitInForeignAccount: false })
				{
					return true;
				}
			}

			return false;
		}

		protected override Task ExecuteCoreAsync(IList transactionViewModels, CancellationToken cancellationToken)
		{
			using IDisposable? undo = this.ViewModel.MoneyFile?.UndoableTransaction($"Delete {transactionViewModels.Count} transactions", transactionViewModels.OfType<TransactionViewModel>().FirstOrDefault()?.Model);
			foreach (TransactionViewModel transaction in transactionViewModels.OfType<TransactionViewModel>().ToList())
			{
				transaction.ThisAccount.DeleteTransaction(transaction);
			}

			return Task.CompletedTask;
		}
	}

	private class JumpToSplitTransactionParentCommandImpl : SelectedTransactionCommandBase
	{
		internal JumpToSplitTransactionParentCommandImpl(DocumentViewModel viewModel)
			: base(viewModel)
		{
		}

		protected override bool CanExecute(IList transactionViewModels)
		{
			return transactionViewModels is { Count: 1 } && transactionViewModels[0] is TransactionViewModel { IsSplitInForeignAccount: true };
		}

		protected override Task ExecuteCoreAsync(IList transactionViewModels, CancellationToken cancellationToken)
		{
			if (transactionViewModels is { Count: 1 } && transactionViewModels[0] is TransactionViewModel { IsSplitInForeignAccount: true } tx)
			{
				tx.JumpToSplitParent();
			}

			return Task.CompletedTask;
		}
	}

	private class UndoCommandImpl : CommandBase
	{
		private readonly DocumentViewModel documentViewModel;

		public UndoCommandImpl(DocumentViewModel documentViewModel)
		{
			this.documentViewModel = documentViewModel;
			if (documentViewModel.MoneyFile is object)
			{
				documentViewModel.MoneyFile.PropertyChanged += this.MoneyFile_PropertyChanged;
			}
		}

		public override bool CanExecute(object? parameter = null) => base.CanExecute(parameter) && this.documentViewModel.MoneyFile?.UndoStack.Any() is true;

		protected override Task ExecuteCoreAsync(object? parameter = null, CancellationToken cancellationToken = default)
		{
			ModelBase? model = this.documentViewModel.MoneyFile?.Undo();
			this.documentViewModel.Reset();
			if (model is object)
			{
				this.documentViewModel.Select(model);
			}

			return Task.CompletedTask;
		}

		private void MoneyFile_PropertyChanged(object? sender, PropertyChangedEventArgs e)
		{
			this.OnCanExecuteChanged();
		}
	}

	private class SaveCommandImpl : CommandBase
	{
		private readonly DocumentViewModel documentViewModel;

		public SaveCommandImpl(DocumentViewModel documentViewModel)
		{
			this.documentViewModel = documentViewModel;
			if (documentViewModel.MoneyFile is object)
			{
				documentViewModel.MoneyFile.PropertyChanged += this.MoneyFile_PropertyChanged;
			}
		}

		public override bool CanExecute(object? parameter = null) => base.CanExecute(parameter) && this.documentViewModel.MoneyFile?.UndoStack.Any() is true;

		protected override Task ExecuteCoreAsync(object? parameter = null, CancellationToken cancellationToken = default)
		{
			this.documentViewModel.MoneyFile?.Save();
			return Task.CompletedTask;
		}

		private void MoneyFile_PropertyChanged(object? sender, PropertyChangedEventArgs e)
		{
			this.OnCanExecuteChanged();
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
