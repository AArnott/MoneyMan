// Copyright (c) Andrew Arnott. All rights reserved.
// Licensed under the Ms-PL license. See LICENSE.txt file in the project root for full license information.

using System.ComponentModel.DataAnnotations;
using System.Diagnostics;
using Validation;

namespace Nerdbank.MoneyManagement.ViewModels;

[DebuggerDisplay("{" + nameof(DebuggerDisplay) + ",nq}")]
public class CategoryViewModel : EntityViewModel<Category>, ITransactionTarget
{
	private string name = string.Empty;

	public CategoryViewModel()
		: this(null, null)
	{
	}

	public CategoryViewModel(Category? model, MoneyFile? moneyFile)
		: base(moneyFile)
	{
		this.RegisterDependentProperty(nameof(this.Name), nameof(this.TransferTargetName));
		this.AutoSave = true;

		if (model is object)
		{
			this.CopyFrom(model);
		}
	}

	/// <inheritdoc cref="Category.Name"/>
	[Required]
	public string Name
	{
		get => this.name;
		set
		{
			Requires.NotNull(value, nameof(value));
			this.SetProperty(ref this.name, value);
		}
	}

	public string TransferTargetName => this.Name;

	private string DebuggerDisplay => this.Name;

	protected override void ApplyToCore(Category category)
	{
		category.Name = this.name;
	}

	protected override void CopyFromCore(Category category)
	{
		this.Name = category.Name;
	}

	protected override bool IsPersistedProperty(string propertyName) => propertyName is not nameof(this.TransferTargetName);
}
