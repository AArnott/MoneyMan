// Copyright (c) Andrew Arnott. All rights reserved.
// Licensed under the Ms-PL license. See LICENSE.txt file in the project root for full license information.

using System.Threading;
using System.Threading.Tasks;
using Nerdbank.MoneyManagement.ViewModels;

internal class UserNotificationMock : IUserNotification
{
	internal int AskOrCancelCounter { get; set; }

	internal int ConfirmCounter { get; set; }

	internal int NotifyCounter { get; set; }

	internal IUserNotification.UserAction ChosenAction { get; set; } = IUserNotification.UserAction.Yes;

	public Task<IUserNotification.UserAction> AskOrCancelAsync(string text, IUserNotification.UserAction defaultButton, CancellationToken cancellationToken = default)
	{
		this.AskOrCancelCounter++;
		return Task.FromResult(this.ChosenAction);
	}

	public Task<bool> ConfirmAsync(string text, bool defaultConfirm, CancellationToken cancellationToken = default)
	{
		this.ConfirmCounter++;
		return Task.FromResult(this.ChosenAction == IUserNotification.UserAction.Yes);
	}

	public Task NotifyAsync(string text, CancellationToken cancellationToken = default)
	{
		this.NotifyCounter++;
		return Task.CompletedTask;
	}
}
