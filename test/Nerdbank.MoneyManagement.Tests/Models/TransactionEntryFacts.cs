// Copyright (c) Andrew Arnott. All rights reserved.
// Licensed under the Ms-PL license. See LICENSE.txt file in the project root for full license information.

public class TransactionEntryFacts : EntityTestBase
{
	public TransactionEntryFacts(ITestOutputHelper logger)
		: base(logger)
	{
		this.EnableSqlLogging();
	}

	[Fact]
	public void BasicPropertiesSerialization()
	{
		const decimal amount = 123.456m;
		const string memo = "Some memo";
		const string ofxFitId = "bankTranId";
		const ClearedState cleared = ClearedState.Cleared;

		var transaction = new Transaction
		{
			Id = 1,
		};
		var asset = new Asset
		{
			Id = 2,
		};
		var account = new Account
		{
			Id = 3,
		};
		this.Money.InsertAll(transaction, asset, account);

		var te = new TransactionEntry
		{
			TransactionId = transaction.Id,
			AssetId = asset.Id,
			AccountId = account.Id,
			Amount = amount,
			Cleared = cleared,
			Memo = memo,
			OfxFitId = ofxFitId,
		};

		Assert.Equal(account.Id, te.AccountId);
		Assert.Equal(asset.Id, te.AssetId);
		Assert.Equal(transaction.Id, te.TransactionId);
		Assert.Equal(amount, te.Amount);
		Assert.Equal(cleared, te.Cleared);
		Assert.Equal(memo, te.Memo);
		Assert.Equal(ofxFitId, te.OfxFitId);

		TransactionEntry? te2 = this.SaveAndReload(te);

		Assert.NotEqual(0, te.Id);
		Assert.Equal(te.Id, te2.Id);

		Assert.Equal(account.Id, te2.AccountId);
		Assert.Equal(asset.Id, te2.AssetId);
		Assert.Equal(transaction.Id, te2.TransactionId);
		Assert.Equal(amount, te2.Amount);
		Assert.Equal(cleared, te2.Cleared);
		Assert.Equal(memo, te2.Memo);
		Assert.Equal(ofxFitId, te2.OfxFitId);
	}

	[Fact]
	public void CopyFromTransactionAndEntryConstructor()
	{
		TransactionAndEntry transactionAndEntry = new()
		{
			AccountId = 2,
			Amount = 3,
			AssetId = 4,
			Cleared = ClearedState.Cleared,
			TransactionEntryId = 5,
			TransactionEntryMemo = "memo",
			OfxFitId = "abc",
			TransactionId = 6,
		};

		TransactionEntry te = new(transactionAndEntry);

		Assert.Equal(transactionAndEntry.AccountId, te.AccountId);
		Assert.Equal(transactionAndEntry.Amount, te.Amount);
		Assert.Equal(transactionAndEntry.AssetId, te.AssetId);
		Assert.Equal(transactionAndEntry.Cleared, te.Cleared);
		Assert.Equal(transactionAndEntry.TransactionEntryId, te.Id);
		Assert.Equal(transactionAndEntry.TransactionEntryMemo, te.Memo);
		Assert.Equal(transactionAndEntry.OfxFitId, te.OfxFitId);
		Assert.Equal(transactionAndEntry.TransactionId, te.TransactionId);
	}
}
