// Copyright (c) Andrew Arnott. All rights reserved.
// Licensed under the Ms-PL license. See LICENSE.txt file in the project root for full license information.

using System.ComponentModel;
using System.ComponentModel.DataAnnotations;
using System.Reflection;
using Microsoft;
using PCLCommandBase;

namespace Nerdbank.MoneyManagement.ViewModels;

public abstract class EntityViewModel<TEntity> : BindableBase, IDataErrorInfo
	where TEntity : ModelBase, new()
{
	protected EntityViewModel()
	{
		this.PropertyChanged += (s, e) =>
		{
			if (e.PropertyName is object && this.IsPersistedProperty(e.PropertyName))
			{
				this.IsDirty = true;
				if (this.MoneyFile?.IsDisposed is not true && this.AutoSave && this.Model is object && string.IsNullOrEmpty(this.Error))
				{
					this.Save();
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
	/// Occurs when the entity is saved to the database.
	/// </summary>
	public event EventHandler? Saved;

	/// <summary>
	/// Gets the primary key for this entity.
	/// </summary>
	public int? Id { get; private set; }

	/// <inheritdoc cref="ModelBase.IsPersisted"/>
	public bool IsPersisted => this.Model?.IsPersisted is true;

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
			PropertyInfo[] propertyInfos = this.GetType().GetProperties(BindingFlags.Public | BindingFlags.Instance);

			foreach (PropertyInfo propertyInfo in propertyInfos)
			{
				if (propertyInfo.DeclaringType?.IsAssignableFrom(typeof(EntityViewModel<TEntity>)) is false)
				{
					var errorMsg = this[propertyInfo.Name];
					if (errorMsg?.Length > 0)
					{
						return errorMsg;
					}
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

	public void Save()
	{
		this.ApplyToModel();
		if (this.MoneyFile is { IsDisposed: false })
		{
			bool wasPersisted = this.IsPersisted;
			using IDisposable? transaction = this.MoneyFile.UndoableTransaction((this.IsPersisted ? "Update" : "Add") + $" {this.GetType().Name}", this.Model);

			this.Model ??= new();
			this.Model.Save(this.MoneyFile);

			// First insert of an entity assigns it an ID. Make sure the view model matches it.
			this.Id = this.Model.Id;

			this.Saved?.Invoke(this, EventArgs.Empty);

			if (!wasPersisted)
			{
				this.OnPropertyChanged(nameof(this.IsPersisted));
			}
		}
	}

	protected abstract void ApplyToCore(TEntity model);

	protected abstract void CopyFromCore(TEntity model);

	protected virtual bool IsPersistedProperty(string propertyName) => true;

	protected AutoSaveSuspension SuspendAutoSave(bool saveOnDisposal = true) => new(this, saveOnDisposal);

	protected struct AutoSaveSuspension : IDisposable
	{
		private readonly EntityViewModel<TEntity> entity;
		private readonly bool saveOnDisposal;
		private readonly bool oldAutoSave;

		internal AutoSaveSuspension(EntityViewModel<TEntity> entity, bool saveOnDisposal)
		{
			this.entity = entity;
			this.saveOnDisposal = saveOnDisposal;
			this.oldAutoSave = entity.AutoSave;
			entity.AutoSave = false;
		}

		public void Dispose()
		{
			this.entity.AutoSave = this.oldAutoSave;
			if (this.saveOnDisposal && this.entity.IsDirty)
			{
				this.entity.Save();
			}
		}
	}
}
