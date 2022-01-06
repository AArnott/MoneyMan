-- Migrate categories to accounts (without ledgers)

INSERT INTO "Account" ([Name], [Type], [IsClosed])
SELECT [Name], 2 AS [Type], 0 AS [IsClosed] FROM [Category];

CREATE TABLE "TransactionEntry" (
	"Id"            INTEGER PRIMARY KEY AUTOINCREMENT NOT NULL,
	"TransactionId" INTEGER NOT NULL REFERENCES "Transaction"("Id") ON DELETE CASCADE,
	"Memo"          TEXT,
	"AccountId"     INTEGER NOT NULL REFERENCES "Account"("Id") ON DELETE CASCADE,
	"Amount"        REAL NOT NULL,
	"AssetId"       INTEGER NOT NULL REFERENCES "Asset"("Id") ON DELETE RESTRICT,
	"Cleared"       INTEGER NOT NULL DEFAULT(0)
);

-- Migrate top-level transactions that did not include splits

INSERT INTO "TransactionEntry"
([TransactionId], [AccountId], [Amount], [AssetId], [Cleared])
SELECT [Id], [CreditAccountId], [CreditAmount], [CreditAssetId], [CreditCleared]
FROM [Transaction]
WHERE [CreditAssetId] IS NOT NULL AND [CreditAccountId] IS NOT NULL AND [ParentTransactionId] IS NULL AND [CategoryId] != -1;

INSERT INTO "TransactionEntry"
([TransactionId], [AccountId], [Amount], [AssetId], [Cleared])
SELECT [Id], [DebitAccountId], -[DebitAmount], [DebitAssetId], [DebitCleared]
FROM [Transaction]
WHERE [DebitAssetId] IS NOT NULL AND [DebitAccountId] IS NOT NULL AND [ParentTransactionId] IS NULL AND [CategoryId] != -1;

INSERT INTO "TransactionEntry"
([TransactionId], [AccountId], [Amount], [AssetId])
SELECT [Id], (SELECT [Id] FROM [Account] a WHERE a.[Type] = 2 AND a.[Name] = (SELECT [Name] FROM [Category] c WHERE c.[Id] = t.[CategoryId])), [DebitAmount], [DebitAssetId]
FROM [Transaction] t
WHERE [DebitAssetId] IS NOT NULL AND [DebitAccountId] IS NOT NULL AND [ParentTransactionId] IS NULL AND [CategoryId] != -1 AND [CategoryId] IS NOT NULL;

INSERT INTO "TransactionEntry"
([TransactionId], [AccountId], [Amount], [AssetId])
SELECT [Id], (SELECT [Id] FROM [Account] a WHERE a.[Type] = 2 AND a.[Name] = (SELECT [Name] FROM [Category] c WHERE c.[Id] = t.[CategoryId])), -[CreditAmount], [CreditAssetId]
FROM [Transaction] t
WHERE [CreditAssetId] IS NOT NULL AND [CreditAccountId] IS NOT NULL AND [ParentTransactionId] IS NULL AND [CategoryId] != -1 AND [CategoryId] IS NOT NULL;

-- Migrate the splits

INSERT INTO "TransactionEntry"
([TransactionId], [AccountId], [Amount], [AssetId], [Cleared])
SELECT [ParentTransactionId], [CreditAccountId], [CreditAmount], [CreditAssetId], [CreditCleared]
FROM [Transaction]
WHERE [CreditAssetId] IS NOT NULL AND [CreditAccountId] IS NOT NULL AND [ParentTransactionId] IS NOT NULL;

INSERT INTO "TransactionEntry"
([TransactionId], [AccountId], [Amount], [AssetId], [Cleared])
SELECT [ParentTransactionId], [DebitAccountId], -[DebitAmount], [DebitAssetId], [DebitCleared]
FROM [Transaction]
WHERE [DebitAssetId] IS NOT NULL AND [DebitAccountId] IS NOT NULL AND [ParentTransactionId] IS NOT NULL;

INSERT INTO "TransactionEntry"
([TransactionId], [AccountId], [Amount], [AssetId])
SELECT [ParentTransactionId], (SELECT [Id] FROM [Account] a WHERE a.[Type] = 2 AND a.[Name] = (SELECT [Name] FROM [Category] c WHERE c.[Id] = t.[CategoryId])), [DebitAmount], [DebitAssetId]
FROM [Transaction] t
WHERE [DebitAssetId] IS NOT NULL AND [DebitAccountId] IS NOT NULL AND [ParentTransactionId] IS NOT NULL AND [CategoryId] != -1 AND [CategoryId] IS NOT NULL;

INSERT INTO "TransactionEntry"
([TransactionId], [AccountId], [Amount], [AssetId])
SELECT [ParentTransactionId], (SELECT [Id] FROM [Account] a WHERE a.[Type] = 2 AND a.[Name] = (SELECT [Name] FROM [Category] c WHERE c.[Id] = t.[CategoryId])), -[CreditAmount], [CreditAssetId]
FROM [Transaction] t
WHERE [CreditAssetId] IS NOT NULL AND [CreditAccountId] IS NOT NULL AND [ParentTransactionId] IS NOT NULL AND [CategoryId] != -1 AND [CategoryId] IS NOT NULL;

-- Update indexes

CREATE INDEX "TransactionEntry_TransactionId" ON [TransactionEntry]("TransactionId");
CREATE INDEX "TransactionEntry_AccountId" ON [TransactionEntry]("AccountId");

DROP INDEX "Transaction_CreditAccountId";
DROP INDEX "Transaction_DebitAccountId";
DROP INDEX "Transaction_CreditAssetId";
DROP INDEX "Transaction_DebitAssetId";
DROP INDEX "Transaction_CategoryId";
DROP INDEX "Transaction_ParentTransactionId";

-- Drop the columns that are now represented in the TransactionEntry table.

ALTER TABLE "Transaction" DROP "CreditAccountId";
ALTER TABLE "Transaction" DROP "CreditAmount";
ALTER TABLE "Transaction" DROP "CreditAssetId";
ALTER TABLE "Transaction" DROP "CreditCleared";
ALTER TABLE "Transaction" DROP "DebitAccountId";
ALTER TABLE "Transaction" DROP "DebitAmount";
ALTER TABLE "Transaction" DROP "DebitAssetId";
ALTER TABLE "Transaction" DROP "DebitCleared";
ALTER TABLE "Transaction" DROP "CategoryId";
ALTER TABLE "Transaction" DROP "ParentTransactionId";

-- Categories are now represented as accounts.
DROP TABLE "Category";

-- This view helps us get all the transactions and entries with a single query.
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
