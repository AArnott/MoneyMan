// Copyright (c) Andrew Arnott. All rights reserved.
// Licensed under the Ms-PL license. See LICENSE.txt file in the project root for full license information.

using System.Diagnostics;

namespace Nerdbank.MoneyManagement.ViewModels;

[DebuggerDisplay("{" + nameof(DebuggerDisplay) + ",nq}")]
public class EnumValueViewModel<T> : IEquatable<EnumValueViewModel<T>>
	where T : Enum
{
	public EnumValueViewModel(T value, string caption)
	{
		this.Value = value;
		this.Caption = caption;
	}

	public T Value { get; }

	public string Caption { get; }

	private string DebuggerDisplay => this.Caption;

	public bool Equals(EnumValueViewModel<T>? other) => other is not null && this.Value.Equals(other.Value);

	public override bool Equals(object? obj) => obj is EnumValueViewModel<T> other && this.Equals(other);

	public override int GetHashCode() => this.Value.GetHashCode();
}
