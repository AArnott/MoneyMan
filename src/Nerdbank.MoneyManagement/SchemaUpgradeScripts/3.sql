PRAGMA foreign_keys = off;

-- Add foreign keys to the existing columns of the Transaction table
CREATE TABLE "Transaction_new" (
	"Id"                  INTEGER PRIMARY KEY AUTOINCREMENT NOT NULL,
	"When"                INTEGER NOT NULL,
	"CheckNumber"         INTEGER,
	"Amount"              REAL NOT NULL,
	"Memo"                TEXT,
	"Payee"               TEXT,
	"CategoryId"          INTEGER REFERENCES "Category"("Id")    ON DELETE SET NULL,
	"CreditAccountId"     INTEGER REFERENCES "Account"("Id")     ON DELETE SET NULL,
	"DebitAccountId"      INTEGER REFERENCES "Account"("Id")     ON DELETE SET NULL,
	"ParentTransactionId" INTEGER REFERENCES "Transaction_new"("Id") ON DELETE CASCADE,
	"Cleared"             INTEGER NOT NULL
);

INSERT INTO "Transaction_new"
SELECT "Id", "When", "CheckNumber", "Amount", "Memo", "Payee", "CategoryId", "CreditAccountId", "DebitAccountId", "ParentTransactionId", "Cleared" 
FROM "Transaction";

DROP TABLE "Transaction";

ALTER TABLE "Transaction_new" RENAME TO "Transaction";

-- Add foreign key constraint to the Category table
CREATE TABLE "Category_new" (
	"Id"               INTEGER PRIMARY KEY AUTOINCREMENT NOT NULL,
	"Name"             TEXT NOT NULL,
	"ParentCategoryId" INTEGER REFERENCES "Category_new"("Id") ON DELETE CASCADE
);

INSERT INTO "Category_new"
SELECT "Id", "Name", "ParentCategoryId"
FROM "Category";

DROP TABLE "Category";

ALTER TABLE "Category_new" RENAME TO "Category";

-- Ensure our new foreign constraints will be satisfied by...
-- Step 1: Inserting rows for what used to be magic IDs
INSERT INTO "Category" ("Id", "Name") VALUES (-1, "--split--");

-- Step 2: Removing references to rows that do not exist.
UPDATE "Transaction"
   SET "CategoryId" = NULL
 WHERE "CategoryId" NOT IN (SELECT "Id" FROM "Category");

UPDATE "Transaction"
   SET "CreditAccountId" = NULL
 WHERE "CreditAccountId" NOT IN (SELECT "Id" FROM "Account");

UPDATE "Transaction"
   SET "DebitAccountId" = NULL
 WHERE "DebitAccountId" NOT IN (SELECT "Id" FROM "Account");

DELETE FROM "Category" WHERE "ParentCategoryId" NOT IN (SELECT "Id" FROM "Category");

PRAGMA foreign_keys = on;

-- Recreate indexes that were deleted with dropped tables
CREATE INDEX "Transaction_CategoryId" on "Transaction"("CategoryId");
CREATE INDEX "Transaction_CreditAccountId" on "Transaction"("CreditAccountId");
CREATE INDEX "Transaction_DebitAccountId" on "Transaction"("DebitAccountId");

-- And add a new useful index
CREATE INDEX "Transaction_ParentTransactionId" on "Transaction"("ParentTransactionId");

-- Delete any orphaned transactions
DELETE FROM "Transaction" WHERE "CreditAccountId" IS NULL AND "DebitAccountId" IS NULL;
