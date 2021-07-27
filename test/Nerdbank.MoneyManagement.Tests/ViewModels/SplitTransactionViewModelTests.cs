// Copyright (c) Andrew Arnott. All rights reserved.
// Licensed under the Ms-PL license. See LICENSE.txt file in the project root for full license information.

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Nerdbank.MoneyManagement;
using Nerdbank.MoneyManagement.Tests;
using Nerdbank.MoneyManagement.ViewModels;
using Xunit;
using Xunit.Abstractions;

public class SplitTransactionViewModelTests : TestBase
{
	private SplitTransactionViewModel viewModel = new();

	private CategoryViewModel category = new();

	private decimal amount = 5.5m;

	private string memo = "Some memo";

	public SplitTransactionViewModelTests(ITestOutputHelper logger)
		: base(logger)
	{
	}

	[Fact]
	public void Amount()
	{
		TestUtilities.AssertPropertyChangedEvent(
			this.viewModel,
			() => this.viewModel.Amount = this.amount,
			nameof(this.viewModel.Amount));
		Assert.Equal(this.amount, this.viewModel.Amount);
	}

	[Fact]
	public void Category()
	{
		TestUtilities.AssertPropertyChangedEvent(
			this.viewModel,
			() => this.viewModel.Category = this.category,
			nameof(this.viewModel.Category));
		Assert.Equal(this.category, this.viewModel.Category);
	}

	[Fact]
	public void Memo()
	{
		TestUtilities.AssertPropertyChangedEvent(
			this.viewModel,
			() => this.viewModel.Memo = this.memo,
			nameof(this.viewModel.Memo));
		Assert.Equal(this.memo, this.viewModel.Memo);
	}

	[Fact]
	public void ApplyTo()
	{
		Assert.Throws<ArgumentNullException>(() => this.viewModel.ApplyTo(null!));

		SplitTransaction splitTransaction = new();

		this.viewModel.Amount = this.amount;
		this.viewModel.Memo = this.memo;
		this.viewModel.ApplyTo(splitTransaction);

		Assert.Equal(this.amount, splitTransaction.Amount);
		Assert.Equal(this.memo, splitTransaction.Memo);
	}

	[Fact]
	public void CopyFrom()
	{
		Assert.Throws<ArgumentNullException>("transaction", () => this.viewModel.CopyFrom(null!, new Dictionary<int, CategoryViewModel>()));
		Assert.Throws<ArgumentNullException>("categories", () => this.viewModel.CopyFrom(new(), null!));

		SplitTransaction splitTransaction = new SplitTransaction
		{
			Amount = this.amount,
			Memo = this.memo,
			CategoryId = 3,
		};
		var categories = new Dictionary<int, CategoryViewModel>
		{
			{ 3, new CategoryViewModel() },
		};

		this.viewModel.CopyFrom(splitTransaction, categories);

		Assert.Equal(splitTransaction.Amount, this.viewModel.Amount);
		Assert.Equal(splitTransaction.Memo, this.viewModel.Memo);
		Assert.Same(categories[3], this.viewModel.Category);

		splitTransaction.CategoryId = null;
		this.viewModel.CopyFrom(splitTransaction, categories);
		Assert.Null(this.viewModel.Category);
	}
}
