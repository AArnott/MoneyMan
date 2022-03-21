// Copyright (c) Andrew Arnott. All rights reserved.
// Licensed under the Ms-PL license. See LICENSE.txt file in the project root for full license information.

public class AccountFacts : EntityTestBase
{
	private Account account;

	public AccountFacts(ITestOutputHelper logger)
		: base(logger)
	{
		this.account = new Account
		{
			Name = "test",
			CurrencyAssetId = this.Money.PreferredAssetId,
		};
	}

	[Fact]
	public void BasicPropertiesSerialization()
	{
		string expected = "some name";
		this.account.Name = expected;
		Assert.Equal(expected, this.account.Name);

		this.account.Type = Account.AccountType.Investing;
		Assert.Equal(Account.AccountType.Investing, this.account.Type);

		this.account.CurrencyAssetId = 1; // assumed ID for the "USD" entry added by the sql scripts

		Account? account2 = this.SaveAndReload(this.account);

		Assert.NotEqual(0, this.account.Id);
		Assert.Equal(this.account.Id, account2.Id);
		Assert.Equal(expected, account2.Name);
		Assert.Equal(this.account.Type, account2.Type);
		Assert.Equal(this.account.CurrencyAssetId, account2.CurrencyAssetId);
	}

	[Fact]
	public void Name_CannotBeNull()
	{
		var acct = new Account { Name = null! };
		Assert.Throws<SQLite.NotNullConstraintViolationException>(() => this.Money.Insert(acct));
	}

	[Fact]
	public void GetBalances_AcrossTransactions()
	{
		this.Money.Insert(this.account);

		decimal expected = 0;
		Assert.Empty(this.Money.GetBalances(this.account));

		this.Money.Action.Deposit(this.account, new Amount(5, this.account.CurrencyAssetId!.Value));
		expected += 5;
		Assert.Equal(expected, this.Money.GetBalances(this.account)[this.account.CurrencyAssetId!.Value]);

		this.Money.Action.Withdraw(this.account, new Amount(2.5m, this.account.CurrencyAssetId!.Value));
		expected -= 2.5m;
		Assert.Equal(expected, this.Money.GetBalances(this.account)[this.account.CurrencyAssetId!.Value]);
	}

	[Fact]
	public void GetBalances_AcrossAccounts()
	{
		var account2 = new Account { Name = "test2", CurrencyAssetId = this.Money.PreferredAssetId };
		this.Money.Insert(this.account);
		this.Money.Insert(account2);

		this.Money.Action.Deposit(this.account, new Amount(5, this.account.CurrencyAssetId!.Value));
		this.Money.Action.Transfer(this.account, account2, new Amount(2.2m, this.account.CurrencyAssetId!.Value));

		Assert.Equal(2.8m, this.Money.GetBalances(this.account)[this.account.CurrencyAssetId!.Value]);
		Assert.Equal(2.2m, this.Money.GetBalances(account2)[this.account.CurrencyAssetId!.Value]);

		this.Money.Action.Transfer(account2, this.account, new Amount(0.4m, this.account.CurrencyAssetId!.Value));

		Assert.Equal(3.2m, this.Money.GetBalances(this.account)[this.account.CurrencyAssetId!.Value]);
		Assert.Equal(1.8m, this.Money.GetBalances(account2)[this.account.CurrencyAssetId!.Value]);
	}
}
