// Copyright (c) Andrew Arnott. All rights reserved.
// Licensed under the Ms-PL license. See LICENSE.txt file in the project root for full license information.

using Nerdbank.MoneyManagement;
using SQLite;
using Xunit;
using Xunit.Abstractions;

public class MoneyFileTests : IDisposable
{
	private readonly ITestOutputHelper logger;
	private string dbPath;

	public MoneyFileTests(ITestOutputHelper logger)
	{
		this.logger = logger;
		this.dbPath = Path.Combine(Path.GetTempPath(), Path.GetRandomFileName());
	}

	public void Dispose()
	{
		File.Delete(this.dbPath);
	}

	[Fact]
	public void Load_ThrowsOnNullOrEmpty()
	{
		Assert.Throws<ArgumentNullException>(() => MoneyFile.Load(null!));
		Assert.Throws<ArgumentException>(() => MoneyFile.Load(string.Empty));
	}

	[Fact]
	public void Load_NonExistentFile()
	{
		using (MoneyFile? money = this.Load())
		{
			Assert.NotNull(money);
			Assert.True(File.Exists(this.dbPath));
		}
	}

	[Fact]
	public void Account_StoreReloadAndChange()
	{
		int accountKey;
		using (MoneyFile money = this.Load())
		{
			var account = new Account { Name = "foo" };
			money.Insert(account);
			Assert.NotEqual(0, account.Id);
			accountKey = account.Id;
		}

		using (MoneyFile money = this.Load())
		{
			Account? account = money.Accounts.First();
			Assert.Equal(accountKey, account.Id);
			Assert.Equal("foo", account.Name);
			account.Name = "bar";
			money.Update(account);
		}

		using (MoneyFile money = this.Load())
		{
			Assert.Equal(1, money.Accounts.Count());
			Account? account = money.Get<Account>(accountKey);
			Assert.Equal(accountKey, account.Id);
			Assert.Equal("bar", account.Name);
		}
	}

	[Fact]
	public void GetBalances()
	{
		using MoneyFile money = this.Load();
		Assert.Throws<ArgumentNullException>(() => money.GetBalances(null!));
		Assert.Throws<ArgumentException>(() => money.GetBalances(new Account()));

		Account account = new() { Name = "Checking", CurrencyAssetId = money.PreferredAssetId };
		money.Insert(account);
		Assert.Empty(money.GetBalances(account));
		Transaction tx = new() { CreditAccountId = account.Id, CreditAmount = 3, CreditAssetId = account.CurrencyAssetId };
		money.Insert(tx);
		Assert.Equal(3, money.GetBalances(account)[account.CurrencyAssetId!.Value]);
	}

	[Fact]
	public void GetNetWorth_WithAndWithoutAsOfDate()
	{
		using (MoneyFile? money = this.Load())
		{
			var acct1 = new Account { Name = "first", CurrencyAssetId = money.PreferredAssetId };
			var acct2 = new Account { Name = "second", CurrencyAssetId = money.PreferredAssetId };

			money.InsertAll(acct1, acct2);
			money.Insert(new Transaction { CreditAccountId = acct1.Id, When = DateTime.Parse("1/1/2015"), CreditAmount = 7, CreditAssetId = acct1.CurrencyAssetId });
			money.Insert(new Transaction { CreditAccountId = acct2.Id, When = DateTime.Parse("1/1/2016"), CreditAmount = 3, CreditAssetId = acct2.CurrencyAssetId });
			money.Insert(new Transaction { DebitAccountId = acct1.Id, When = DateTime.Parse("2/1/2016"), DebitAmount = 2.5m, DebitAssetId = acct1.CurrencyAssetId });
			money.Insert(new Transaction { DebitAccountId = acct1.Id, CreditAccountId = acct2.Id, When = DateTime.Parse("2/1/2016"), CreditAmount = 1, CreditAssetId = acct2.CurrencyAssetId, DebitAmount = 1, DebitAssetId = acct1.CurrencyAssetId });
			money.Insert(new Transaction { DebitAccountId = acct1.Id, When = DateTime.Parse("2/1/2016 11:59 PM"), DebitAmount = 1.3m, DebitAssetId = acct1.CurrencyAssetId });
			money.Insert(new Transaction { DebitAccountId = acct1.Id, When = DateTime.Parse("2/2/2016"), DebitAmount = 4m, DebitAssetId = acct1.CurrencyAssetId });
			money.Insert(new Transaction { DebitAccountId = acct1.Id, When = DateTime.Parse("2/2/2222"), DebitAmount = 0.3m, DebitAssetId = acct1.CurrencyAssetId });

			Assert.Equal(6.2m, money.GetNetWorth(new MoneyFile.NetWorthQueryOptions { AsOfDate = DateTime.Parse("2/1/2016") }));
			Assert.Equal(1.9m, money.GetNetWorth());
		}
	}

	[Fact]
	public void GetNetWorth_WithClosedAccounts()
	{
		using (MoneyFile? money = this.Load())
		{
			var openAccount = new Account { Name = "first", Type = Account.AccountType.Banking, CurrencyAssetId = money.PreferredAssetId };
			var closedAccount = new Account { Name = "second", Type = Account.AccountType.Banking, CurrencyAssetId = money.PreferredAssetId, IsClosed = true };

			money.InsertAll(openAccount, closedAccount);
			money.Insert(openAccount.Deposit(new Amount(7, openAccount.CurrencyAssetId.Value)));
			money.Insert(closedAccount.Deposit(new Amount(3, closedAccount.CurrencyAssetId.Value)));

			Assert.Equal(7, money.GetNetWorth());
			Assert.Equal(10, money.GetNetWorth(new MoneyFile.NetWorthQueryOptions { IncludeClosedAccounts = true }));
		}
	}

	[Fact]
	public void Insert_RaisesEvent()
	{
		using MoneyFile money = this.Load();
		Account account = new() { Name = "Checking" };
		Assert.RaisedEvent<MoneyFile.EntitiesChangedEventArgs> evt = Assert.Raises<MoneyFile.EntitiesChangedEventArgs>(h => money.EntitiesChanged += h, h => money.EntitiesChanged -= h, () => money.Insert(account));
		Assert.Same(money, evt.Sender);
		Assert.Same(account, Assert.Single(evt.Arguments.Inserted));
		Assert.Empty(evt.Arguments.Deleted);
	}

	[Fact]
	public void InsertAll_RaisesEvent()
	{
		using MoneyFile money = this.Load();
		Account account = new() { Name = "Checking" };
		Assert.RaisedEvent<MoneyFile.EntitiesChangedEventArgs> evt = Assert.Raises<MoneyFile.EntitiesChangedEventArgs>(h => money.EntitiesChanged += h, h => money.EntitiesChanged -= h, () => money.InsertAll(account));
		Assert.Same(money, evt.Sender);
		Assert.Same(account, Assert.Single(evt.Arguments.Inserted));
		Assert.Empty(evt.Arguments.Deleted);
	}

	[Fact]
	public void Update_RaisesEvent()
	{
		using MoneyFile money = this.Load();
		Account account = new() { Name = "Checking" };
		money.Insert(account);
		account.Name = "Checking 2";

		Assert.RaisedEvent<MoneyFile.EntitiesChangedEventArgs> evt = Assert.Raises<MoneyFile.EntitiesChangedEventArgs>(h => money.EntitiesChanged += h, h => money.EntitiesChanged -= h, () => money.Update(account));
		Assert.Same(money, evt.Sender);
		var changeRecord = Assert.Single(evt.Arguments.Changed);
		Assert.Same(account, changeRecord.After);
		Assert.Equal("Checking 2", ((Account)changeRecord.After).Name);
		Assert.Equal("Checking", ((Account)changeRecord.Before).Name);
		Assert.Empty(evt.Arguments.Deleted);
	}

	[Fact]
	public void InsertOrReplace_RaisesEvent()
	{
		using MoneyFile money = this.Load();
		Account account = new() { Name = "Checking" };

		Assert.RaisedEvent<MoneyFile.EntitiesChangedEventArgs> evt = Assert.Raises<MoneyFile.EntitiesChangedEventArgs>(h => money.EntitiesChanged += h, h => money.EntitiesChanged -= h, () => money.InsertOrReplace(account));
		Assert.Same(money, evt.Sender);
		Assert.Same(account, Assert.Single(evt.Arguments.Inserted));
		Assert.Empty(evt.Arguments.Deleted);
	}

	[Fact]
	public void Delete_RaisesEvent()
	{
		using MoneyFile money = this.Load();
		Account account = new() { Name = "Checking" };

		Assert.RaisedEvent<MoneyFile.EntitiesChangedEventArgs> evt = Assert.Raises<MoneyFile.EntitiesChangedEventArgs>(h => money.EntitiesChanged += h, h => money.EntitiesChanged -= h, () => money.Delete(account));
		Assert.Same(money, evt.Sender);
		Assert.Empty(evt.Arguments.Inserted);
		Assert.Empty(evt.Arguments.Changed);
		Assert.Same(account, Assert.Single(evt.Arguments.Deleted));
	}

	[Fact]
	public void Disposal_LeadsOtherMethodsToThrow()
	{
		MoneyFile money = this.Load();
		money.Dispose();
		Assert.Throws<ObjectDisposedException>(() => money.Insert(new Account { Name = "Checking" }));
		Assert.Throws<ObjectDisposedException>(() => money.InsertAll(new Account { Name = "Checking" }));
		Assert.Throws<ObjectDisposedException>(() => money.Update(new Account { Name = "Checking" }));
		Assert.Throws<ObjectDisposedException>(() => money.InsertOrReplace(new Account { Name = "Checking" }));
		Assert.Throws<ObjectDisposedException>(() => money.Delete(new Account { Name = "Checking" }));
		Assert.Throws<ObjectDisposedException>(() => money.GetNetWorth());
		Assert.Throws<ObjectDisposedException>(() => money.GetBalances(new Account { Id = 3, Name = "Checking" }));
		Assert.Throws<ObjectDisposedException>(() => money.Categories);
		Assert.Throws<ObjectDisposedException>(() => money.Accounts);
		Assert.Throws<ObjectDisposedException>(() => money.Transactions);
		Assert.Throws<ObjectDisposedException>(() => money.CheckIntegrity());
	}

	[Fact]
	public void Dispose_Twice()
	{
		MoneyFile money = this.Load();
		money.Dispose();
		money.Dispose();
	}

	[Fact]
	public void LoadNewerThanCurrentFileFormatThrows()
	{
		this.Load().Dispose();
		using (SQLiteConnection db = new(this.dbPath))
		{
			int currentVersion = db.ExecuteScalar<int>("SELECT MAX(SchemaVersion) FROM SchemaHistory");
			db.Execute("INSERT INTO SchemaHistory VALUES (?, ?, ?)", currentVersion + 1, DateTime.Now, "test");
		}

		MoneyFile? money = null;
		try
		{
			Assert.Throws<InvalidOperationException>(() => money = this.Load());
		}
		catch (InvalidOperationException)
		{
			// Expected exception thrown.
		}
		finally
		{
#pragma warning disable CA1508 // Avoid dead conditional code
			money?.Dispose();
#pragma warning restore CA1508 // Avoid dead conditional code
		}
	}

	private MoneyFile Load()
	{
		var file = MoneyFile.Load(this.dbPath);
		file.Logger = new TestLoggerAdapter(this.logger);
		return file;
	}
}
