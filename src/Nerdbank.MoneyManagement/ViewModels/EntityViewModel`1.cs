// Copyright (c) Andrew Arnott. All rights reserved.
// Licensed under the Ms-PL license. See LICENSE.txt file in the project root for full license information.

namespace Nerdbank.MoneyManagement.ViewModels
{
	using System;
	using System.Collections.Generic;
	using System.ComponentModel;
	using System.ComponentModel.DataAnnotations;
	using System.Linq;
	using System.Reflection;
	using Microsoft;
	using PCLCommandBase;

	public abstract class EntityViewModel<TEntity> : BindableBase, IDataErrorInfo
		where TEntity : ModelBase
	{
		protected EntityViewModel()
		{
			this.PropertyChanged += (s, e) =>
			{
				if (e.PropertyName is object && this.IsPersistedProperty(e.PropertyName))
				{
					this.IsDirty = true;
					if (this.AutoSave && this.Model is object && string.IsNullOrEmpty(this.Error))
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

		public virtual string Error
		{
			get
			{
				PropertyInfo[] propertyInfos = this.GetType().GetProperties(BindingFlags.DeclaredOnly | BindingFlags.Public | BindingFlags.Instance);

				foreach (PropertyInfo propertyInfo in propertyInfos)
				{
					var errorMsg = this[propertyInfo.Name];
					if (errorMsg is not null)
					{
						return errorMsg;
					}
				}

				return string.Empty;
			}
		}

		/// <summary>
		/// Gets or sets a value indicating whether changes to this view model are automatically persisted to the model.
		/// </summary>
		protected bool AutoSave { get; set; }

		public virtual string this[string columnName]
		{
			get
			{
				var validationResults = new List<ValidationResult>();

				PropertyInfo? property = this.GetType().GetProperty(columnName);
				Requires.Argument(property is not null, nameof(columnName), "No property by that name.");

				ValidationContext validationContext = new(this)
				{
					MemberName = columnName,
				};

				var isValid = Validator.TryValidateProperty(property.GetValue(this), validationContext, validationResults);
				if (isValid)
				{
					return string.Empty;
				}

				return validationResults.First().ErrorMessage ?? string.Empty;
			}
		}

		/// <summary>
		/// Writes this view model to the underlying model.
		/// </summary>
		/// <exception cref="InvalidOperationException">Thrown if <see cref="Model"/> is <see langword="null"/>.</exception>
		public void ApplyToModel() => this.ApplyTo(this.Model ?? throw new InvalidOperationException("This view model has no model yet."));

		public void ApplyTo(TEntity model)
		{
			Requires.NotNull(model, nameof(model));
			Requires.Argument(this.Id is null || model.Id == this.Id, nameof(model), "The provided object is not the original template.");
			Verify.Operation(string.IsNullOrEmpty(this.Error), "View model is not in a valid state. Check the " + nameof(this.Error) + " property.");

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
