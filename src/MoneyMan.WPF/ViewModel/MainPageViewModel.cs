// Copyright (c) Andrew Arnott. All rights reserved.
// Licensed under the Ms-PL license. See LICENSE.txt file in the project root for full license information.

namespace MoneyMan.ViewModel
{
	using Nerdbank.MoneyManagement.ViewModels;
	using PCLCommandBase;

	public class MainPageViewModel : BindableBase
	{
		private string statusMessage = "Ready";
		private bool updateAvailable;
		private string version = ThisAssembly.AssemblyInformationalVersion;
		private DocumentViewModel document = new DocumentViewModel();
		private int? downloadingUpdatePercentage;

		public DocumentViewModel Document
		{
			get => this.document;
			set => this.SetProperty(ref this.document, value);
		}

		public string StatusMessage
		{
			get => this.statusMessage;
			set => this.SetProperty(ref this.statusMessage, value);
		}

		public bool UpdateAvailable
		{
			get => this.updateAvailable;
			set => this.SetProperty(ref this.updateAvailable, value);
		}

		public int? DownloadingUpdatePercentage
		{
			get => this.downloadingUpdatePercentage;
			set => this.SetProperty(ref this.downloadingUpdatePercentage, value);
		}

		public string Version
		{
			get => this.version;
			set => this.SetProperty(ref this.version, value);
		}
	}
}
