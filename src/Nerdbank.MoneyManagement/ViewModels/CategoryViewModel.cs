// Copyright (c) Andrew Arnott. All rights reserved.
// Licensed under the Ms-PL license. See LICENSE.txt file in the project root for full license information.

namespace Nerdbank.MoneyManagement.ViewModels
{
	using PCLCommandBase;
	using Validation;

	public class CategoryViewModel : EntityViewModel<Category>
	{
		private string name = string.Empty;

		/// <summary>
		/// Gets the primary key for this entity.
		/// </summary>
		public int? Id { get; private set; }

		/// <inheritdoc cref="Category.Name"/>
		public string Name
		{
			get => this.name;
			set
			{
				Requires.NotNullOrEmpty(value, nameof(value));
				this.SetProperty(ref this.name, value);
			}
		}

		public override void ApplyTo(Category category)
		{
			Requires.NotNull(category, nameof(category));
			Requires.Argument(this.Id is null || category.Id == this.Id, nameof(category), "The provided object is not the original template.");

			category.Name = this.name;
		}

		public override void CopyFrom(Category category)
		{
			Requires.NotNull(category, nameof(category));

			this.Name = category.Name;
			this.Id = category.Id;
		}
	}
}
