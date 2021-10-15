// Copyright (c) Andrew Arnott. All rights reserved.
// Licensed under the Ms-PL license. See LICENSE.txt file in the project root for full license information.

namespace MoneyMan.Avl
{
	using System;
	using Avalonia.Controls;
	using Avalonia.Controls.Templates;
	using MoneyMan.Avl.ViewModels;

	public class ViewLocator : IDataTemplate
	{
		public bool SupportsRecycling => false;

		public IControl Build(object data)
		{
			var name = data.GetType().FullName!.Replace("ViewModel", "View");
			var type = Type.GetType(name);

			if (type is not null)
			{
				return (Control)Activator.CreateInstance(type)!;
			}
			else
			{
				return new TextBlock { Text = "Not Found: " + name };
			}
		}

		public bool Match(object data)
		{
			return data is ViewModelBase;
		}
	}
}
