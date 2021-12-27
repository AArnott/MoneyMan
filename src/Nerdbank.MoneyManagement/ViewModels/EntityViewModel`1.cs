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
	public int Id { get; private set; }

	public override bool IsPersisted => this.Id > 0;

	/// <summary>
	/// Gets the model that underlies this view model.
	/// </summary>
	public TEntity Model { get; private set; }

	protected override ModelBase? UndoTarget => this.Model;

	public void CopyFrom(TEntity model)
	{
		Requires.NotNull(model, nameof(model));

		this.Model = model;
		using (this.SuspendAutoSave(saveOnDisposal: false))
		{
			this.Id = this.Model.Id;
			this.CopyFromCore();
		}

		this.IsDirty = false;
	}

	protected override void SaveCore()
	{
		this.MoneyFile.InsertOrReplace(this.Model);

		// First insert of an entity assigns it an ID. Make sure the view model matches it.
		this.Id = this.Model.Id;
	}

	protected abstract void CopyFromCore();
}
