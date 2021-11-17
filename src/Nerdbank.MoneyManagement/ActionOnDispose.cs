// Copyright (c) Andrew Arnott. All rights reserved.
// Licensed under the Ms-PL license. See LICENSE.txt file in the project root for full license information.

namespace Nerdbank.MoneyManagement;

internal class ActionOnDispose : IDisposable
{
	private readonly Action disposeAction;

	internal ActionOnDispose(Action disposeAction)
	{
		this.disposeAction = disposeAction;
	}

	public void Dispose() => this.disposeAction();
}
