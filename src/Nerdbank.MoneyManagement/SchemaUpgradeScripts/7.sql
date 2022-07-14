-- Create a commission category 
ALTER TABLE "Configuration" ADD "CommissionAccountId" INTEGER REFERENCES "Account"("Id") ON DELETE RESTRICT;
INSERT INTO Account (Name, Type, IsClosed) VALUES ("Commission", 2, 0);
UPDATE "Configuration" SET "CommissionAccountId" = last_insert_rowid();
