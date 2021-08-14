// Copyright (c) Andrew Arnott. All rights reserved.
// Licensed under the Ms-PL license. See LICENSE.txt file in the project root for full license information.

namespace MoneyMan.WPF.ViewModel
{
	using Nerdbank.MoneyManagement.ViewModels;
	using PCLCommandBase;

	public class MainPageViewModel : BindableBase
	{
		private DocumentViewModel document = new DocumentViewModel();

		public DocumentViewModel Document
		{
			get => this.document;
			set => this.SetProperty(ref this.document, value);
		}
	}
}
