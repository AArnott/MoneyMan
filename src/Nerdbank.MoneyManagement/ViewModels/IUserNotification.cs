// Copyright (c) Andrew Arnott. All rights reserved.
// Licensed under the Ms-PL license. See LICENSE.txt file in the project root for full license information.

namespace Nerdbank.MoneyManagement.ViewModels
{
	using System;
	using System.Threading;
	using System.Threading.Tasks;

	/// <summary>
	/// A view that displays notifications and prompts to the user.
	/// </summary>
	public interface IUserNotification
	{
		/// <summary>
		/// User actions that may be taken.
		/// </summary>
		public enum UserAction
		{
			/// <summary>
			/// The user accepts the choice.
			/// </summary>
			Yes,

			/// <summary>
			/// The user declines the choice.
			/// </summary>
			No,

			/// <summary>
			/// The user cancels the action.
			/// </summary>
			Cancel,
		}

		/// <summary>
		/// Displays a message to the user.
		/// </summary>
		/// <param name="text">The message to display to the user.</param>
		/// <param name="cancellationToken">A token to dismiss the UI immediately.</param>
		/// <returns>A task that cancels when the message has been dismissed.</returns>
		Task NotifyAsync(string text, CancellationToken cancellationToken = default);

		/// <summary>
		/// Displays a choice to the user, offering yes or no actions.
		/// </summary>
		/// <param name="text">The message to prompt the user with.</param>
		/// <param name="defaultConfirm"><see langword="true" /> to make the default action be to confirm the operation.</param>
		/// <param name="cancellationToken">A cancellation token that may dismiss the UI before the user takes any action.</param>
		/// <returns>The user's selected action.</returns>
		/// <exception cref="OperationCanceledException">Thrown if <paramref name="cancellationToken"/> is canceled before the user chooses an action.</exception>
		Task<bool> ConfirmAsync(string text, bool defaultConfirm, CancellationToken cancellationToken = default);

		/// <summary>
		/// Displays a choice to the user, offering yes, no, and cancel actions.
		/// </summary>
		/// <param name="text">The message to prompt the user with.</param>
		/// <param name="defaultButton">The default action to suggest to the user.</param>
		/// <param name="cancellationToken">A cancellation token that may dismiss the UI before the user takes any action.</param>
		/// <returns>The user's selected action.</returns>
		/// <exception cref="OperationCanceledException">Thrown if <paramref name="cancellationToken"/> is canceled before the user chooses an action.</exception>
		Task<UserAction> AskOrCancelAsync(string text, UserAction defaultButton, CancellationToken cancellationToken = default);
	}
}
