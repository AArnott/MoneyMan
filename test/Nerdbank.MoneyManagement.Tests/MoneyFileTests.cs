// Copyright (c) Andrew Arnott. All rights reserved.
// Licensed under the Ms-PL license. See LICENSE.txt file in the project root for full license information.

using System.Diagnostics;
using SQLite;

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
		using (MoneyFile? money = this.Load(alwaysOnDisk: true))
		{
			Assert.NotNull(money);
			Assert.True(File.Exists(this.dbPath));
		}
	}

	[Fact]
	public void Account_StoreReloadAndChange()
	{
		int accountKey;
		using (MoneyFile money = this.Load(alwaysOnDisk: true))
		{
			var account = new Account { Name = "foo" };
			money.Insert(account);
			Assert.NotEqual(0, account.Id);
			accountKey = account.Id;
		}

		using (MoneyFile money = this.Load(alwaysOnDisk: true))
		{
			Account? account = money.Accounts.First();
			Assert.Equal(accountKey, account.Id);
			Assert.Equal("foo", account.Name);
			account.Name = "bar";
			money.Update(account);
		}

		using (MoneyFile money = this.Load(alwaysOnDisk: true))
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
		money.Action.Deposit(account, new Amount(3, account.CurrencyAssetId.Value));
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
			money.Action.Deposit(acct1, 7, DateTime.Parse("1/1/2015"));
			money.Action.Deposit(acct2, 3, DateTime.Parse("1/1/2016"));
			money.Action.Withdraw(acct1, 2.5m, DateTime.Parse("2/1/2016"));
			money.Action.Transfer(acct1, acct2, 1, DateTime.Parse("2/1/2016"));
			money.Action.Withdraw(acct1, 1.3m, DateTime.Parse("2/1/2016 11:59 PM"));
			money.Action.Withdraw(acct1, 4m, DateTime.Parse("2/2/2016"));
			money.Action.Withdraw(acct1, 0.3m, DateTime.Parse("2/2/2222"));

			Assert.Equal(6.2m, money.GetNetWorth(new MoneyFile.NetWorthQueryOptions { AsOfDate = DateTime.Parse("2/1/2016") }));
			Assert.Equal(2.2m, money.GetNetWorth());
			Assert.Equal(1.9m, money.GetNetWorth(new MoneyFile.NetWorthQueryOptions { AsOfDate = DateTime.Parse("2/2/2222") }));
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
			money.Action.Deposit(openAccount, 7);
			money.Action.Deposit(closedAccount, 3);

			Assert.Equal(7, money.GetNetWorth());
			Assert.Equal(10, money.GetNetWorth(new MoneyFile.NetWorthQueryOptions { IncludeClosedAccounts = true }));
		}
	}

	[Fact]
	public void GetNetWorth_IncludingSecurities()
	{
		using MoneyFile money = this.Load();

		Account brokerage = new Account { Name = "brokerage", CurrencyAssetId = money.PreferredAssetId, Type = Account.AccountType.Investing };
		Asset msft = new Asset { Name = "Microsoft" };
		Asset aapl = new Asset { Name = "Aaple" };
		money.InsertAll(brokerage, msft, aapl);

		decimal expectedWorth = 0;
		DateTime when = DateTime.Today.AddDays(-2);

		decimal amount = 7;
		money.Action.Deposit(brokerage, amount, when);
		expectedWorth += amount;

		amount = 2;
		money.Action.Add(brokerage, new Amount(amount, msft.Id), when);
		AssetPrice msftPriceBefore = new AssetPrice { When = when.AddDays(-1), PriceInReferenceAsset = 13, ReferenceAssetId = money.PreferredAssetId, AssetId = msft.Id };
		AssetPrice msftPriceAfter = new AssetPrice { When = when.AddDays(1), PriceInReferenceAsset = 11, ReferenceAssetId = money.PreferredAssetId, AssetId = msft.Id };
		expectedWorth += amount * msftPriceBefore.PriceInReferenceAsset;

		amount = 2;
		money.Action.Add(brokerage, new Amount(amount, aapl.Id), when);
		AssetPrice aaplPriceBefore = new AssetPrice { When = when.AddDays(-1), PriceInReferenceAsset = 13, ReferenceAssetId = money.PreferredAssetId, AssetId = aapl.Id };
		AssetPrice aaplPriceAfter = new AssetPrice { When = when.AddDays(1), PriceInReferenceAsset = 11, ReferenceAssetId = money.PreferredAssetId, AssetId = aapl.Id };
		expectedWorth += amount * aaplPriceBefore.PriceInReferenceAsset;

		money.InsertAll(msftPriceBefore, msftPriceAfter, aaplPriceBefore, aaplPriceAfter);

		decimal netWorth = money.GetNetWorth(new MoneyFile.NetWorthQueryOptions { AsOfDate = when });
		Assert.Equal(expectedWorth, netWorth);
	}

	[Fact]
	public void GetValue_IncludingSecurities()
	{
		using MoneyFile money = this.Load();

		Account brokerage = new Account { Name = "brokerage", CurrencyAssetId = money.PreferredAssetId, Type = Account.AccountType.Investing };
		Asset msft = new Asset { Name = "Microsoft" };
		Asset aapl = new Asset { Name = "Aaple" };
		money.InsertAll(brokerage, msft, aapl);

		decimal expectedWorth = 0;
		DateTime when = DateTime.Today.AddDays(-2);
		decimal amount = 7;
		money.Action.Deposit(brokerage, amount, when);
		expectedWorth += amount;

		amount = 2;
		money.Action.Add(brokerage, new Amount(amount, msft.Id), when);
		AssetPrice msftPriceBefore = new AssetPrice { When = when.AddDays(-1), PriceInReferenceAsset = 13, ReferenceAssetId = money.PreferredAssetId, AssetId = msft.Id };
		AssetPrice msftPriceAfter = new AssetPrice { When = when.AddDays(1), PriceInReferenceAsset = 11, ReferenceAssetId = money.PreferredAssetId, AssetId = msft.Id };
		expectedWorth += amount * msftPriceBefore.PriceInReferenceAsset;

		money.Action.Add(brokerage, new Amount(amount, aapl.Id), when);
		AssetPrice aaplPriceBefore = new AssetPrice { When = when.AddDays(-1), PriceInReferenceAsset = 13, ReferenceAssetId = money.PreferredAssetId, AssetId = aapl.Id };
		AssetPrice aaplPriceAfter = new AssetPrice { When = when.AddDays(1), PriceInReferenceAsset = 11, ReferenceAssetId = money.PreferredAssetId, AssetId = aapl.Id };
		expectedWorth += amount * aaplPriceBefore.PriceInReferenceAsset;

		money.InsertAll(msftPriceBefore, msftPriceAfter, aaplPriceBefore, aaplPriceAfter);

		decimal value = money.GetValue(brokerage, when);
		Assert.Equal(expectedWorth, value);
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
		this.Load(alwaysOnDisk: true).Dispose();
		using (SQLiteConnection db = new(this.dbPath))
		{
			int currentVersion = db.ExecuteScalar<int>("SELECT MAX(SchemaVersion) FROM SchemaHistory");
			db.Execute("INSERT INTO SchemaHistory VALUES (?, ?, ?)", currentVersion + 1, DateTime.Now, "test");
		}

		MoneyFile? money = null;
		try
		{
			Assert.Throws<InvalidOperationException>(() => money = this.Load(alwaysOnDisk: true));
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

	private MoneyFile Load(bool alwaysOnDisk = false)
	{
		var file = MoneyFile.Load(alwaysOnDisk || Debugger.IsAttached ? this.dbPath : ":memory:");
		file.Logger = new TestLoggerAdapter(this.logger);
		return file;
	}
}
