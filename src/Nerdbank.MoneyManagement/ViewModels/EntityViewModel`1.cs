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
				if (e.PropertyName is object && this.IsPersistedProperty(e.PropertyName))
				{
					this.IsDirty = true;
					if (this.AutoSave && this.Model is object)
					{
						this.ApplyToModel();
						if (this.MoneyFile is { IsDisposed: false })
						{
							this.Model.Save(this.MoneyFile);

							// First insert of an entity assigns it an ID. Make sure the view model matches it.
							this.Id = this.Model.Id;
						}
					}
				}
			};
		}

		protected EntityViewModel(MoneyFile? moneyFile)
			: this()
		{
			this.MoneyFile = moneyFile;
		}

		/// <summary>
		/// Gets the primary key for this entity.
		/// </summary>
		public int? Id { get; private set; }

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
			Requires.NotNull(model, nameof(model));
			Requires.Argument(this.Id is null || model.Id == this.Id, nameof(model), "The provided object is not the original template.");

			this.ApplyToCore(model);

			this.IsDirty = false;
			this.Model ??= model;
		}

		public void CopyFrom(TEntity model)
		{
			Requires.NotNull(model, nameof(model));

			this.Id = model.Id;

			bool autoSave = this.AutoSave;
			this.AutoSave = false;
			try
			{
				this.CopyFromCore(model);
			}
			finally
			{
				this.AutoSave = autoSave;
			}

			this.IsDirty = false;
			this.Model ??= model;
		}

		protected abstract void ApplyToCore(TEntity model);

		protected abstract void CopyFromCore(TEntity model);

		protected virtual bool IsPersistedProperty(string propertyName) => true;
	}
}
