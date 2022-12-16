-- Drop the redundant te.AccountId column from the view.
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
		te.[Amount],
		te.[AssetId],
		te.[Cleared]

	FROM [Account] a
	JOIN [Transaction] t
	INNER JOIN [TransactionEntry] te ON te.[TransactionId] = t.[Id]
	WHERE (a.[Id], t.[Id]) IN AccountsAndTransactions
	ORDER BY a.[Id], t.[Id], te.[Id];

-- Tax lot support
CREATE TABLE "TaxLot" (
	"Id"                         INTEGER PRIMARY KEY AUTOINCREMENT NOT NULL,
	"CreatingTransactionEntryId" INTEGER NOT NULL REFERENCES "TransactionEntry"("Id") ON DELETE CASCADE,
	"AcquiredDate"               INTEGER, -- when null, use the TransactionEntry's date
	"Amount"                     REAL, -- when null, use the TransactionEntry's amount
	"CostBasisAmount"            REAL,
	"CostBasisAssetId"           REAL
);

CREATE TABLE "TaxLotAssignment" (
	"Id"                          INTEGER PRIMARY KEY AUTOINCREMENT NOT NULL,
	"TaxLotId"                    INTEGER NOT NULL REFERENCES "TaxLot"("Id") ON DELETE CASCADE,
	"ConsumingTransactionEntryId" INTEGER NOT NULL REFERENCES "TransactionEntry"("Id") ON DELETE CASCADE,
	"Amount"                      REAL NOT NULL,
	"Pinned"                      INTEGER NOT NULL DEFAULT(0)
);

CREATE INDEX "TaxLotAssignment_ConsumingTransactionEntryId" ON [TaxLotAssignment]("ConsumingTransactionEntryId");
CREATE INDEX "TaxLotAssignment_TaxLotId" ON [TaxLotAssignment]("TaxLotId");
CREATE UNIQUE INDEX "TaxLotAssignment_TaxLotId_ConsumingTransactionEntryId" ON [TaxLotAssignment]("TaxLotId", "ConsumingTransactionEntryId");

CREATE VIEW UnsoldAsset AS
	SELECT
		COALESCE(tl.[AcquiredDate], t.[When]) AS [AcquiredDate],
		t.[Id] AS [TransactionId],
		a.[Id] AS [AssetId],
		tl.[Id] AS [TaxLotId],
		a.[Name] AS [AssetName],
		COALESCE(tl.[Amount], te.[Amount]) AS [AcquiredAmount],
		(COALESCE(tl.[Amount], te.[Amount]) - SUM(COALESCE(tla.[Amount],0))) AS [RemainingAmount]
	FROM TransactionEntry te
	JOIN Asset a ON a.Id = te.AssetId
	JOIN [Transaction] t ON t.Id = te.TransactionId
	JOIN TaxLot tl ON tl.CreatingTransactionEntryId = te.Id
	LEFT OUTER JOIN TaxLotAssignment tla ON tla.TaxLotId = tl.Id
	WHERE a.Type = 1
	GROUP BY tl.Id
	HAVING RemainingAmount > 0;

CREATE VIEW ConsumedTaxLot AS
	SELECT
		tl.[Id] AS [TaxLotId],
		tla.ConsumingTransactionEntryId AS ConsumingTransactionEntryId,
		COALESCE(tl.[AcquiredDate], t.[When]) AS [AcquiredDate],
		tla.[Amount] AS [Amount],
		(tla.[Amount] / COALESCE(tl.[Amount], te.[Amount]) * tl.CostBasisAmount) AS [CostBasisAmount],
		tl.[CostBasisAssetId] AS [CostBasisAssetId]
	FROM TaxLotAssignment tla 
	JOIN TaxLot tl ON tl.Id = tla.TaxLotId
	JOIN TransactionEntry te ON te.Id = tl.CreatingTransactionEntryId
	JOIN [Transaction] t ON t.Id = te.TransactionId;
