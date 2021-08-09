// Copyright (c) Andrew Arnott. All rights reserved.
// Licensed under the Ms-PL license. See LICENSE.txt file in the project root for full license information.

namespace Nerdbank.MoneyManagement.ViewModels
{
	using System;
	using System.ComponentModel;
	using System.Threading;
	using System.Threading.Tasks;
	using PCLCommandBase;
	using Validation;

	public class DeleteCategoryCommand : CommandBase
	{
		private readonly CategoriesPanelViewModel viewModel;

		public DeleteCategoryCommand(CategoriesPanelViewModel viewModel)
		{
			this.viewModel = viewModel;
			viewModel.PropertyChanged += this.ViewModel_PropertyChanged;
		}

		public override bool CanExecute(object parameter) => base.CanExecute(parameter) && this.viewModel.SelectedCategory is object;

		protected override Task ExecuteCoreAsync(object parameter, CancellationToken cancellationToken)
		{
			this.viewModel.Categories.Remove(this.viewModel.SelectedCategory ?? throw new InvalidOperationException("No category is selected."));
			return Task.CompletedTask;
		}

		private void ViewModel_PropertyChanged(object? sender, PropertyChangedEventArgs e)
		{
			if (e.PropertyName == nameof(CategoriesPanelViewModel.SelectedCategory))
			{
				this.OnCanExecuteChanged();
			}
		}
	}
}
