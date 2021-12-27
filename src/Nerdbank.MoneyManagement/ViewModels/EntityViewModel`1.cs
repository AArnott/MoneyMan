// Copyright (c) Andrew Arnott. All rights reserved.
// Licensed under the Ms-PL license. See LICENSE.txt file in the project root for full license information.

using Microsoft;

namespace Nerdbank.MoneyManagement.ViewModels;

public abstract class EntityViewModel<TEntity> : EntityViewModel
	where TEntity : ModelBase, new()
{
	protected EntityViewModel(MoneyFile moneyFile)
		: base(moneyFile)
	{
	}

	/// <summary>
	/// Gets the primary key for this entity.
	/// </summary>
	public int Id { get; private set; }

	public override bool IsPersisted => this.Id > 0;

	/// <summary>
	/// Gets or sets the model that underlies this view model.
	/// </summary>
	/// <value>May be <see langword="null"/> if this view model represents an entity that has not been created yet.</value>
	public TEntity? Model { get; set; }

	protected override bool IsModelSet => this.Model is object;

	protected override ModelBase? UndoTarget => this.Model;

	public override void ApplyToModel() => this.ApplyTo(this.Model ?? throw new InvalidOperationException("This view model has no model yet."));

	public void ApplyTo(TEntity model)
	{
		Requires.NotNull(model, nameof(model));
		Requires.Argument(this.Id == 0 || model.Id == this.Id, nameof(model), "The provided object is not the original template.");
		Verify.Operation(string.IsNullOrEmpty(this.Error), "View model is not in a valid state. Check the " + nameof(this.Error) + " property. " + this.Error);

		this.ApplyToCore(model);

		this.IsDirty = false;
		this.Model ??= model;
	}

	public void CopyFrom(TEntity model)
	{
		Requires.NotNull(model, nameof(model));

		this.Id = model.Id;

		using (this.SuspendAutoSave(saveOnDisposal: false))
		{
			this.CopyFromCore(model);
		}

		this.IsDirty = false;
		this.Model ??= model;
	}

	protected override void SaveCore()
	{
		this.Model ??= new();
		this.Model.Save(this.MoneyFile);

		// First insert of an entity assigns it an ID. Make sure the view model matches it.
		this.Id = this.Model.Id;
	}

	protected abstract void ApplyToCore(TEntity model);

	protected abstract void CopyFromCore(TEntity model);
}
