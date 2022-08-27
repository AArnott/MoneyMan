CREATE TABLE "TaxLotAssignment" (
	"Id"                          INTEGER,
	"AcquiredTransactionEntryId"  INTEGER NOT NULL REFERENCES "TransactionEntry"("Id") ON DELETE CASCADE,
	"DispenseTransactionEntryId"  INTEGER NOT NULL REFERENCES "TransactionEntry"("Id") ON DELETE CASCADE,
	"Amount"                      REAL NOT NULL,
	PRIMARY KEY("Id")
);

CREATE INDEX "TaxLotAssignment_EntryIds" ON [TaxLotAssignment]("AcquiredTransactionEntryId", "DispenseTransactionEntryId");

CREATE VIEW ListUnsoldAssets AS
	SELECT t.[When] AS [AcquiredDate], t.[Id] AS [TransactionId], a.[Id] AS [AssetId], a.[Name] AS [AssetName], te.[Amount] AS [AcquiredAmount], (te.[Amount] - SUM(tl.[Amount])) AS [RemainingAmount]
	FROM TransactionEntry te
	JOIN Asset a ON a.Id = te.AssetId
	JOIN [Transaction] t ON t.Id = te.TransactionId
	LEFT OUTER JOIN TaxLotAssignment tl ON tl.AcquiredTransactionEntryId = te.Id
	WHERE a.Type = 1;
-- TODO: add a column with cost basis / price information for the purchase
-- TODO: for purposes of UI presentation, add a filter for transaction 
--       so that the RemainingAmount subtotal can exclude the transaction being shown,
--       since it will have a unique column dedicated to showing (and editing) that transaction.
-- TODO: How will tax lots work with transfers across accounts?
-- TODO: We may want to allow the user to Add shares with cost basis and purchase date information.
-- USE CASES:
--  * tax lots can be opened by purchase of shares
--  * tax lots can be closed by sale of shares
--  * tax lots can be opened by short sale
--  * tax lots can be closed by covering a short sale
--  * tax lots can be closed by removal of shares (without a sale)
--  * tax lots can be opened by adding of shares (without a purchase)
--  * tax lots must track a transfer of shares from one account to another.
