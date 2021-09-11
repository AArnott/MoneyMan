﻿// Copyright (c) Andrew Arnott. All rights reserved.
// Licensed under the Ms-PL license. See LICENSE.txt file in the project root for full license information.

namespace Nerdbank.MoneyManagement.ViewModels
{
	using System;
	using System.Collections.Generic;
	using System.Diagnostics;
	using System.IO;
	using Microsoft;
	using PCLCommandBase;

	[DebuggerDisplay("{" + nameof(DebuggerDisplay) + ",nq}")]
	public class DocumentViewModel : BindableBase, IDisposable
	{
		private readonly MoneyFile? model;
		private decimal netWorth;

		public DocumentViewModel()
			: this(null)
		{
		}

		public DocumentViewModel(MoneyFile? model)
		{
			this.model = model;

			if (model is object)
			{
				this.AccountsPanel = new AccountsPanelViewModel();
				foreach (Account account in model.Accounts)
				{
					this.AccountsPanel.Accounts.Add(new AccountViewModel(account, model));
				}

				this.CategoriesPanel = new CategoriesPanelViewModel(model);
				foreach (Category category in model.Categories)
				{
					this.CategoriesPanel.Categories.Add(new CategoryViewModel(category, model));
				}

				this.netWorth = model.GetNetWorth(new MoneyFile.NetWorthQueryOptions { AsOfDate = DateTime.Now });
				model.EntitiesChanged += this.Model_EntitiesChanged;
			}
		}

		public bool IsFileOpen => this.model is object;

		public string Title => this.model is { Path: string path } ? $"Nerdbank Money Management - {Path.GetFileNameWithoutExtension(path)}" : "Nerdbank Money Management";

		public decimal NetWorth
		{
			get => this.netWorth;
			set => this.SetProperty(ref this.netWorth, value);
		}

		public AccountsPanelViewModel? AccountsPanel { get; }

		public CategoriesPanelViewModel? CategoriesPanel { get; }

		private string DebuggerDisplay => this.model?.Path ?? "(not backed by a file)";

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

		public void Dispose()
		{
			if (this.model is object)
			{
				this.model.EntitiesChanged -= this.Model_EntitiesChanged;
				this.model.Dispose();
			}
		}

		private void Model_EntitiesChanged(object? sender, MoneyFile.EntitiesChangedEventArgs e)
		{
			Assumes.NotNull(this.model);

			if (this.AccountsPanel is object)
			{
				HashSet<int> impactedAccountIds = new();
				SearchForImpactedAccounts(e.InsertedOrChanged);
				SearchForImpactedAccounts(e.Deleted);
				foreach (AccountViewModel accountViewModel in this.AccountsPanel.Accounts)
				{
					if (accountViewModel.Model is object && accountViewModel.Id.HasValue && impactedAccountIds.Contains(accountViewModel.Id.Value))
					{
						accountViewModel.Balance = this.model.GetBalance(accountViewModel.Model);
					}
				}

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

			this.NetWorth = this.model.GetNetWorth(new MoneyFile.NetWorthQueryOptions { AsOfDate = DateTime.Now });
		}
	}
}
