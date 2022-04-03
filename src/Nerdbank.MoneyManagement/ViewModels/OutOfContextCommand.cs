// Copyright (c) Andrew Arnott. All rights reserved.
// Licensed under the Ms-PL license. See LICENSE.txt file in the project root for full license information.

namespace Nerdbank.MoneyManagement.ViewModels;

/// <summary>
/// A command that is always disabled and hidden.
/// </summary>
public class OutOfContextCommand : UICommandBase
{
	private OutOfContextCommand()
	{
	}

	public static UICommandBase Instance { get; } = new OutOfContextCommand();

	public override string Caption => string.Empty;

	public override bool Visible => false;

	public override bool CanExecute(object? parameter = null) => false;

	protected override Task ExecuteCoreAsync(object? parameter = null, CancellationToken cancellationToken = default) => Task.FromException(new InvalidOperationException());
}
