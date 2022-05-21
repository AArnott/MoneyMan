-- This schema update adds fields to help track and reconcile downloaded transactions.

ALTER TABLE "Account" ADD "OfxBankId" TEXT;
ALTER TABLE "Account" ADD "OfxAcctId" TEXT;
ALTER TABLE "TransactionEntry" ADD "OfxFitId" TEXT;

DROP VIEW "TransactionAndEntry";
CREATE VIEW "TransactionAndEntry" AS
	WITH AccountsAndTransactions AS (
		SELECT a.[Id] AS [AccountId], t.[Id] AS [TransactionId]
		FROM [Account] a
		JOIN [Transaction] t
		WHERE a.[Id] IN (SELECT [AccountId] FROM [TransactionEntry] WHERE [TransactionId] = t.[Id])
	)
	SELECT
		a.[Id] AS [ContextAccountId],
		t.[Id] AS [TransactionId],
		te.[Id] AS [TransactionEntryId],
		te.[AccountId] AS [AccountId],
		te.[OfxFitId] AS [OfxFitId],
		t.[When],
		t.[Action],
		t.[CheckNumber],
		t.[RelatedAssetId],
		t.[Payee],
		t.[Memo] AS [TransactionMemo],
		te.[Memo] AS [TransactionEntryMemo],
		te.[AccountId],
		te.[Amount],
		te.[AssetId],
		te.[Cleared]

	FROM [Account] a
	JOIN [Transaction] t
	INNER JOIN [TransactionEntry] te ON te.[TransactionId] = t.[Id]
	WHERE (a.[Id], t.[Id]) IN AccountsAndTransactions
	ORDER BY a.[Id], t.[Id], te.[Id];
