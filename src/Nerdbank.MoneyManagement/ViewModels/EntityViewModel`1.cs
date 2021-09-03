// Copyright (c) Andrew Arnott. All rights reserved.
// Licensed under the Ms-PL license. See LICENSE.txt file in the project root for full license information.

namespace Nerdbank.MoneyManagement.ViewModels
{
	using System;
	using Microsoft;
	using PCLCommandBase;

	public abstract class EntityViewModel<TEntity> : BindableBase
		where TEntity : ModelBase
	{
		protected EntityViewModel()
		{
			this.PropertyChanged += (s, e) =>
			{
				this.IsDirty = true;
				if (this.AutoSave && this.Model is object)
				{
					this.ApplyToModel();
					if (this.MoneyFile is object)
					{
						this.Model.Save(this.MoneyFile);
					}
				}
			};
		}

		protected EntityViewModel(TEntity? model, MoneyFile? moneyFile)
			: this()
		{
			this.Model = model;
			this.MoneyFile = moneyFile;
			if (model is object)
			{
				this.CopyFrom(model);
			}
		}

		/// <summary>
		/// Gets or sets a value indicating whether this entity has been changed since <see cref="ApplyTo(TEntity)"/> was last called.
		/// </summary>
		public bool IsDirty { get; set; }

		/// <summary>
		/// Gets or sets the model that underlies this view model.
		/// </summary>
		/// <value>May be <see langword="null"/> if this view model represents an entity that has not been created yet.</value>
		public TEntity? Model { get; set; }

		/// <summary>
		/// Gets or sets the file to which this view model should be saved or from which it should be refreshed.
		/// </summary>
		public MoneyFile? MoneyFile { get; set; }

		/// <summary>
		/// Gets or sets a value indicating whether changes to this view model are automatically persisted to the model.
		/// </summary>
		protected bool AutoSave { get; set; }

		/// <summary>
		/// Writes this view model to the underlying model.
		/// </summary>
		/// <exception cref="InvalidOperationException">Thrown if <see cref="Model"/> is <see langword="null"/>.</exception>
		public void ApplyToModel() => this.ApplyTo(this.Model ?? throw new InvalidOperationException("This view model has no model yet."));

		public void ApplyTo(TEntity model)
		{
			this.ApplyToCore(model);
			this.IsDirty = false;
			this.Model ??= model;
		}

		public void CopyFrom(TEntity model)
		{
			this.CopyFromCore(model);
			this.IsDirty = false;
			this.Model ??= model;
		}

		protected abstract void ApplyToCore(TEntity model);

		protected abstract void CopyFromCore(TEntity model);
	}
}
