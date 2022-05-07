// Copyright (c) Andrew Arnott. All rights reserved.
// Licensed under the Ms-PL license. See LICENSE.txt file in the project root for full license information.

namespace Nerdbank.MoneyManagement.ViewModels;

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

	/// <summary>
	/// Invites the user to select an account to apply an operation to.
	/// </summary>
	/// <param name="prompt">The message to present to the user that explains what the user is picking for.</param>
	/// <param name="defaultAccount">The account that should be selected as the default.</param>
	/// <param name="cancellationToken">A cancellation token.</param>
	/// <returns>The account the user selected, or <see langword="null" /> if the user canceled.</returns>
	Task<AccountViewModel?> ChooseAccountAsync(string prompt, AccountViewModel? defaultAccount, CancellationToken cancellationToken = default);

	/// <summary>
	/// Asks the user to select a file to import.
	/// </summary>
	/// <param name="title">The title for the open file dialog.</param>
	/// <param name="filter">The filter of file types to display.</param>
	/// <param name="filterIndex">The default filter to select. 1-based index.</param>
	/// <param name="cancellationToken">A cancellation token.</param>
	/// <returns>The selected file name, or <see langword="null" /> if the user cancelled the operation.</returns>
	Task<string?> PickFileAsync(string title, string filter, int filterIndex, CancellationToken cancellationToken = default);

	/// <summary>
	/// Presents a modal view for a given view model.
	/// </summary>
	/// <param name="viewModel">The view model to bind to the view. The runtime type will determine which view is selected to present to the user.</param>
	/// <param name="cancellationToken">A cancellation token which may dismiss the UI automatically.</param>
	/// <returns>A task that completes when the view is dismissed.</returns>
	/// <exception cref="NotSupportedException">Thrown if the <paramref name="viewModel"/> is not of a recognized type.</exception>
	Task PresentAsync(IPresentedWindowViewModel viewModel, CancellationToken cancellationToken = default);
}
