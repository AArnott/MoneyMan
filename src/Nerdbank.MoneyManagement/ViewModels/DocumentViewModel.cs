// Copyright (c) Andrew Arnott. All rights reserved.
// Licensed under the Ms-PL license. See LICENSE.txt file in the project root for full license information.

namespace Nerdbank.MoneyManagement.ViewModels
{
	using System;
	using System.Collections.ObjectModel;
	using System.ComponentModel;
	using System.Diagnostics;
	using System.IO;
	using System.Threading;
	using System.Threading.Tasks;
	using System.Windows.Input;
	using Microsoft;
	using Microsoft.Win32;
	using PCLCommandBase;

	[DebuggerDisplay("{" + nameof(DebuggerDisplay) + ",nq}")]
	public class DocumentViewModel : BindableBase, IDisposable
	{
		private MoneyFile? model;

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

				this.CategoriesPanel = new CategoriesPanelViewModel();
				foreach (Category category in model.Categories)
				{
					this.CategoriesPanel.Categories.Add(new CategoryViewModel(category, model));
				}
			}
		}

		public bool IsFileOpen => this.model is object;

		public string Title => this.model is { Path: string path } ? $"Nerdbank Money Management - {Path.GetFileNameWithoutExtension(path)}" : "Nerdbank Money Management";

		public AccountsPanelViewModel? AccountsPanel { get; }

		public CategoriesPanelViewModel? CategoriesPanel { get; }

		private string DebuggerDisplay => this.model?.Path ?? "(not backed by a file)";

		public static DocumentViewModel CreateNew(string moneyFilePath)
		{
			if (File.Exists(moneyFilePath))
			{
				File.Delete(moneyFilePath);
			}

			MoneyFile model = MoneyFile.Load(moneyFilePath);
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
			this.model?.Dispose();
		}
	}
}
