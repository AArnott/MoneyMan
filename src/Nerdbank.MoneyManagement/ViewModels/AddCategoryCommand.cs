// Copyright (c) Andrew Arnott. All rights reserved.
// Licensed under the Ms-PL license. See LICENSE.txt file in the project root for full license information.

namespace Nerdbank.MoneyManagement.ViewModels
{
	using System.Threading;
	using System.Threading.Tasks;
	using PCLCommandBase;

	public class AddCategoryCommand : CommandBase
	{
		private readonly CategoriesPanelViewModel viewModel;

		public AddCategoryCommand(CategoriesPanelViewModel viewModel)
		{
			this.viewModel = viewModel;
		}

		protected override Task ExecuteCoreAsync(object parameter, CancellationToken cancellationToken)
		{
			CategoryViewModel newCategoryViewModel = new();
			this.viewModel.Categories.Add(newCategoryViewModel);
			this.viewModel.SelectedCategory = newCategoryViewModel;
			return Task.CompletedTask;
		}
	}
}
