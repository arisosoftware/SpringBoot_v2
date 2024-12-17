Thanks for the clarification! I now understand that you'd like to avoid using a **cursor** entirely and focus on using a **`WHILE` loop** to process the records. Below is the updated solution where we use a **`WHILE` loop** to process the records and ensure the related records are properly checked and excluded.

The process is:

1. **Start with the oldest 50 cancelled `CreditHistory` records** and add them to the `#finalist` table.
2. **Check if all related records for each `CreditID` are included in the `#finalist`**.
3. If not all related records are in `#finalist`, exclude the current `CreditID` from the `#finalist` and pick the next valid record to add.
4. Repeat the process until all records in the `#finalist` are valid (i.e., all related `CreditHistory` records for the same `CreditID` are also in the `#finalist`).

### **Updated Approach (No Cursors, Using `WHILE` Loop)**

#### T-SQL Solution:

```sql
-- Step 1: Declare necessary variables
DECLARE @CreditHistoryID INT, @CreditID INT, @IsOut BIT, @RelatedRecords INT;

-- Step 2: Create the #finalist table to store selected CreditHistory records
CREATE TABLE #finalist (
    CreditHistoryID INT PRIMARY KEY,
    CreditID INT,
    WorkflowDate DATETIME,
    IsOut BIT DEFAULT 0  -- Default value is 0 (valid); 1 means excluded (out)
);

-- Step 3: Insert the oldest 50 cancelled CreditHistory records into #finalist
INSERT INTO #finalist (CreditHistoryID, CreditID, WorkflowDate)
SELECT TOP 50
    ch.CreditHistoryID,
    ch.CreditID,
    ch.WorkflowDate
FROM CreditHistory ch
WHERE ch.WorkflowStatus = 'cancelled'
ORDER BY ch.WorkflowDate ASC;

-- Step 4: Initialize a loop to process the finalists and check the related records
WHILE EXISTS (SELECT 1 FROM #finalist WHERE IsOut = 0)  -- Loop until no more records to check
BEGIN
    -- Step 4a: Check if there are any CreditHistory records marked "out"
    -- Check if all related CreditHistory records for the same CreditID are present in the finalist
    UPDATE #finalist
    SET IsOut = 1
    WHERE CreditID IN (
        SELECT DISTINCT CreditID
        FROM #finalist f
        WHERE f.IsOut = 0
        AND EXISTS (
            SELECT 1
            FROM CreditHistory ch2
            WHERE ch2.CreditID = f.CreditID
            AND ch2.CreditHistoryID NOT IN (SELECT CreditHistoryID FROM #finalist WHERE IsOut = 0)
        )
    );

    -- Step 4b: Insert the next oldest record from CreditHistory (not yet in finalist)
    -- Find the next valid record for CreditID not yet in finalist and insert it
    INSERT INTO #finalist (CreditHistoryID, CreditID, WorkflowDate)
    SELECT TOP 1 
        ch.CreditHistoryID, 
        ch.CreditID, 
        ch.WorkflowDate
    FROM CreditHistory ch
    WHERE ch.CreditHistoryID NOT IN (SELECT CreditHistoryID FROM #finalist WHERE IsOut = 0)
      AND ch.WorkflowStatus = 'cancelled'
    ORDER BY ch.WorkflowDate ASC;

    -- Step 4c: Exit the loop if all CreditHistory records in #finalist are valid
    -- If no more invalid records exist, the loop will stop
    IF NOT EXISTS (
        SELECT 1 FROM #finalist WHERE IsOut = 0 
        AND EXISTS (
            SELECT 1
            FROM CreditHistory ch2
            WHERE ch2.CreditID = #finalist.CreditID
            AND ch2.CreditHistoryID NOT IN (SELECT CreditHistoryID FROM #finalist WHERE IsOut = 0)
        )
    )
    BEGIN
        BREAK;  -- Exit the loop if no more invalid records remain
    END
END;

-- Step 5: Proceed with deletion of related records for the finalized CreditIDs
-- 5a: Delete from Credit_Counterparty (links CreditHistory to CounterpartyHistory)
DELETE FROM Credit_Counterparty
WHERE CreditHistoryID IN (SELECT CreditHistoryID FROM #finalist WHERE IsOut = 0);

-- 5b: Delete related CounterpartyHistory records for each CreditHistory in the finalist
DELETE FROM CounterpartyHistory
WHERE CounterpartyHistoryID IN (
    SELECT CounterpartyHistoryID
    FROM Credit_Counterparty cc
    JOIN #finalist f ON cc.CreditHistoryID = f.CreditHistoryID
    WHERE f.IsOut = 0
);

-- 5c: Delete from CreditHistory for the CreditIDs in finalist
DELETE FROM CreditHistory
WHERE CreditHistoryID IN (SELECT CreditHistoryID FROM #finalist WHERE IsOut = 0);

-- Step 6: Final Cleanup
DROP TABLE #finalist;  -- Remove the temporary table
```

### **Explanation of Key Parts:**

1. **Initial `#finalist` population**: We first insert the **oldest 50 cancelled `CreditHistory` records** into the `#finalist` table, ordered by `WorkflowDate`.

2. **`WHILE` Loop**: 
   - The loop runs as long as there are records in the `#finalist` table where `IsOut = 0` (valid records).
   - Inside the loop, we check if there are any **related `CreditHistory` records for the same `CreditID`** that are not included in `#finalist`.
   - If any related records are missing, we **mark all records for that `CreditID` as `IsOut = 1`** (excluded), and we then insert the next oldest valid `CreditHistory` record that hasn't been processed yet.
   
3. **Marking Invalid Records**: 
   - The **`UPDATE` statement** marks the `CreditID` as "out" (`IsOut = 1`) if any related `CreditHistoryID` is missing from the `#finalist`.
   
4. **Inserting Next Valid Record**: 
   - If a `CreditID` is invalid (some related records are missing), we **insert the next oldest cancelled record** into the `#finalist`, which hasn't been processed yet. The `INSERT` ensures we continuously fill `#finalist` with records until all related records are included.
   
5. **Exiting the Loop**: 
   - The loop continues until all records in `#finalist` are valid (i.e., no records are marked as `IsOut = 0`).
   - The `IF NOT EXISTS` condition ensures that once all records are valid, we **exit the loop**.

6. **Deleting Records**: 
   - Once the loop ends, we proceed with the deletion of all related records:
     - **Delete from `Credit_Counterparty`**: We delete records linking `CreditHistory` to `CounterpartyHistory`.
     - **Delete from `CounterpartyHistory`**: We delete the related `CounterpartyHistory` records.
     - **Delete from `CreditHistory`**: Finally, we delete the `CreditHistory` records.

7. **Cleanup**: After processing, we drop the `#finalist` table.

### **Performance Considerations**:

- **Efficiency**: This approach avoids cursors but still requires repeatedly checking for related records in the `#finalist` table. Ensure your tables are indexed, particularly on `CreditHistoryID`, `CreditID`, and foreign key relationships to improve performance.
- **Row Locking**: Depending on your data size, frequent updates and inserts into `#finalist` might cause contention. Monitor performance if you're working with large datasets.

### **Conclusion**:
This solution processes the data using a `WHILE` loop and avoids cursors, as you requested. It dynamically adjusts the finalists, checking and including the next valid record if necessary, ensuring all related `CreditHistory` records are properly considered before deletion.
