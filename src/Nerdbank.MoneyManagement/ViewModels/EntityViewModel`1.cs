// Copyright (c) Andrew Arnott. All rights reserved.
// Licensed under the Ms-PL license. See LICENSE.txt file in the project root for full license information.

namespace Nerdbank.MoneyManagement.ViewModels
{
	using PCLCommandBase;

	public abstract class EntityViewModel<TEntity> : BindableBase
		where TEntity : class
	{
		public abstract void ApplyTo(TEntity category);

		public abstract void CopyFrom(TEntity category);
	}
}
