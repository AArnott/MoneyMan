// Copyright (c) Andrew Arnott. All rights reserved.
// Licensed under the Ms-PL license. See LICENSE.txt file in the project root for full license information.

using Microsoft;

namespace Nerdbank.MoneyManagement.ViewModels;

public abstract class EntityViewModel<TEntity> : EntityViewModel
	where TEntity : ModelBase, new()
{
	protected EntityViewModel(MoneyFile moneyFile, TEntity? model = null)
		: base(moneyFile)
	{
		this.Model = model ?? new TEntity();
	}

	/// <summary>
	/// Gets the primary key for this entity.
	/// </summary>
	public int Id => this.Model.Id;

	public override bool IsPersisted => this.Id > 0;

	/// <summary>
	/// Gets the model that underlies this view model.
	/// </summary>
	public TEntity Model { get; private set; }

	protected override ModelBase? UndoTarget => this.Model;

	public static implicit operator TEntity(EntityViewModel<TEntity> viewModel)
	{
		Verify.Operation(!viewModel.IsDirty, "Implicit conversion from view model to model is not allowed when view model is dirty.");
		return viewModel.Model;
	}

	public void CopyFrom(TEntity model)
	{
		Requires.NotNull(model, nameof(model));

		this.Model = model;
		using (this.SuspendAutoSave(saveOnDisposal: false))
		{
			this.CopyFromCore();
		}

		this.IsDirty = false;
	}

	protected override bool IsPersistedProperty(string propertyName) => base.IsPersistedProperty(propertyName) && propertyName is not (nameof(this.Id) or nameof(this.IsDirty));

	protected override void SaveCore()
	{
		this.ApplyToModel();
		this.MoneyFile.InsertOrReplace(this.Model);
	}

	protected abstract void CopyFromCore();
}
