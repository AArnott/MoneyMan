-- This script represents the very first version of the MoneyMan schema.
-- It therefore only creates table and has no upgrade script.

PRAGMA foreign_keys = on;

CREATE TABLE "Account" (
	"Id" INTEGER PRIMARY KEY AUTOINCREMENT NOT NULL,
	"Name" TEXT NOT NULL,
	"IsClosed" INTEGER NOT NULL
);

CREATE TABLE "Category" (
	"Id" INTEGER PRIMARY KEY AUTOINCREMENT NOT NULL,
	"Name" TEXT NOT NULL,
	"ParentCategoryId" INTEGER
);

CREATE TABLE "SplitTransaction" (
	"Id" INTEGER PRIMARY KEY AUTOINCREMENT NOT NULL,
	"TransactionId" INTEGER NOT NULL,
	"CategoryId" INTEGER,
	"Memo" TEXT,
	"Amount" REAL NOT NULL
);
CREATE INDEX "SplitTransaction_TransactionId" on "SplitTransaction"("TransactionId");

CREATE TABLE "Transaction" (
	"Id" INTEGER PRIMARY KEY AUTOINCREMENT NOT NULL,
	"When" INTEGER NOT NULL,
	"CheckNumber" INTEGER,
	"Amount" REAL NOT NULL,
	"Memo" TEXT,
	"Payee" TEXT,
	"CategoryId" INTEGER,
	"CreditAccountId" INTEGER,
	"DebitAccountId" INTEGER,
	"Cleared" INTEGER NOT NULL
);
CREATE INDEX "Transaction_CategoryId" on "Transaction"("CategoryId");
CREATE INDEX "Transaction_CreditAccountId" on "Transaction"("CreditAccountId");
CREATE INDEX "Transaction_DebitAccountId" on "Transaction"("DebitAccountId");

CREATE TABLE "SchemaHistory" (
	"SchemaVersion" INTEGER PRIMARY KEY NOT NULL,
	"AppliedDateUtc" TEXT NOT NULL,
	"AppVersion" INTEGER NOT NULL
);
