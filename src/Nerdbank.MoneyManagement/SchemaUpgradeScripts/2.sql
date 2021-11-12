DROP TABLE "SplitTransaction";

ALTER TABLE "Transaction"
	ADD "ParentTransactionId" INTEGER REFERENCES "Transaction"("Id") ON DELETE CASCADE
