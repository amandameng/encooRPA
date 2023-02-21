public void Run()
{
    //From File
    DirectoryInfo dictInfo = new DirectoryInfo(@"C:\RPA\Wyeth");
    
    FileInfo sheetFile = dictInfo.GetFiles("*.xlsx")[0];
    Console.WriteLine("READ FILE : " + sheetFile.FullName);
    
    //DataTable sheetData = GetDataFromExcelByConn(sheetFile);
    DataTable sheetData = SheetData4View;
    Console.WriteLine(sheetData.Rows.Count + " Records From File.");
    //Filte the data with Database info
    duplicateTable = FilteData(ref sheetData, trackerTable);
    Console.WriteLine(sheetData.Rows.Count +"Records After filte.");
    DataTable tableByAcc = GetTableOrderByAccount(sheetData);
    
    foreach (DataRow row in tableByAcc.Rows)
    {
        DataTable inTable = ((DataTable)row["Table"]);
        AdjustTableFormat(inTable);
    }
    
    //From Database
    constraintSet = TableToHashSet(ConstraintList);
    AddPasswordForTable(tableByAcc, Accounts);
    
    TrimDataTable(tableByAcc);
    TableByAccount = tableByAcc;
}

static DataTable GetDataFromExcelByConn(FileInfo fileInfo, bool hasTitle = true)
{
    DataTable dt = new DataTable(fileInfo.Name);
    var filePath = fileInfo.FullName;
    string fileType = fileInfo.Extension;
    if (string.IsNullOrEmpty(fileType)) return null;

    string strCon = string.Format(@"Provider={0};
                                                Extended Properties=""Excel {1}.0;HDR={2};IMEX=1;"";
                                                data source={3};",
                    (fileType == ".xls" ? "Microsoft.Jet.OLEDB.4.0" : @"Microsoft.ACE.OLEDB.12.0"), (fileType == ".xls" ? 8 : 12), (hasTitle ? "Yes" : "NO"), filePath);

    using (OleDbConnection myConn = new OleDbConnection(strCon))
    {
        myConn.Open();
        DataTable FromExcel = myConn.GetOleDbSchemaTable(System.Data.OleDb.OleDbSchemaGuid.Tables, new object[] { null, null, null, "TABLE" });
        string sheetNameStr = FromExcel.Rows[0]["TABLE_NAME"].ToString();
        string strCom = $"SELECT * FROM [{sheetNameStr}]";
        using (OleDbDataAdapter myCommand = new OleDbDataAdapter(strCom, myConn))
        {
            myCommand.Fill(dt);
        }
    }

    return dt;
}

static DataTable GetTableOrderByAccount(DataTable rawTable)
{
    DataTable TableByAccount = new DataTable();
    TableByAccount.Columns.Add("Account");
    TableByAccount.Columns.Add("Password");
    TableByAccount.Columns.Add("Receiver");
    TableByAccount.Columns.Add("FailedPicList", typeof(List<string>));
    TableByAccount.Columns.Add("Table", typeof(DataTable));

    foreach (DataRow Row in rawTable.Rows)
    {
        bool IsExist = false;
        foreach(DataRow accRow in TableByAccount.Rows)
        {
            if (accRow["Account"].Equals(Row["大仓账号"]))
            {
                IsExist = true;
            }
        }
        if (!IsExist)
        {
            DataRow talbeByAccRow = TableByAccount.NewRow();
            talbeByAccRow["Account"] = Row["大仓账号"];
            DataView view = new DataView(rawTable);
            view.RowFilter = "大仓账号 = '" + Row["大仓账号"].ToString() + "'";
            talbeByAccRow["Table"] = view.ToTable();
            TableByAccount.Rows.Add(talbeByAccRow);
        }
    }

    return TableByAccount;
}

static HashSet<string> TableToHashSet(DataTable table)
{
    HashSet<string> conSet = new HashSet<string>();
    Console.WriteLine($"{table.Rows.Count} from constraint tabll");
    foreach (DataRow row in table.Rows)
    {
        conSet.Add(row[0].ToString());
    }
    Console.WriteLine($"{conSet.Count} from conSet");
    return conSet;
}

static void AdjustTableFormat(DataTable dataTable)
{
    Console.WriteLine("Count of Table Col : " + dataTable.Columns.Count);

    dataTable.Columns.RemoveAt(0);
    dataTable.Columns.RemoveAt(0);
    dataTable.Columns[0].ColumnName = "Pay";
    dataTable.Columns[6].ColumnName = "POID";
    dataTable.Columns[7].ColumnName = "itemName";
    dataTable.Columns[8].ColumnName = "Nums";
    dataTable.Columns[1].ColumnName = "ReadDate";
    dataTable.Columns[3].ColumnName = "soldto";
    dataTable.Columns[4].ColumnName = "shipto";
    dataTable.Columns[5].ColumnName = "customerName";
    dataTable.Columns[2].ColumnName = "RDD";
    
    int desiredSize = 9;
    while (dataTable.Columns.Count > desiredSize)
    {
        dataTable.Columns.RemoveAt(desiredSize);
    }
    
    dataTable.Columns.Add("Amount");
    dataTable.Columns.Add("Result");
    dataTable.Columns.Add("Reson");
    dataTable.Columns.Add("DmsPo");
    dataTable.Columns.Add("isCon");
}

static void AddPasswordForTable(DataTable table, DataTable accountsTable)
{
    Dictionary<string,string> accounts = new Dictionary<string,string>();
    Dictionary<string,string> receivers = new Dictionary<string,string>();
    foreach (DataRow row in accountsTable.Rows)
    {
        if(!accounts.Keys.Contains(row[0].ToString()))
        {
            accounts.Add(row[0].ToString(), row[1].ToString());
        }
    }
    
    foreach(DataRow row in table.Rows)
    {
        try
        {
            row["Password"] = accounts[row["Account"].ToString()];
        }
        catch
        {
            row["Password"] = "";
        }
    }
    
    foreach (DataRow row in accountsTable.Rows)
    {
        if(!receivers.Keys.Contains(row[0].ToString()))
        {
            receivers.Add(row[0].ToString(), row[2].ToString());
        }
    }
    
    foreach(DataRow row in table.Rows)
    {
        try
        {
            row["Receiver"] = receivers[row["Account"].ToString()];
        }
        catch
        {
            row["Receiver"] = "";
        }
    }
}

static DataTable FilteData(ref DataTable dt, DataTable dataBase)
{
    DataTable dupTable = dt.Clone();
    for (int i = dt.Rows.Count - 1; i >= 0; i--)
    {
        string dtPOID = dt.Rows[i][8].ToString();
        string dtPay = dt.Rows[i][2].ToString();
        string dtName = dt.Rows[i][9].ToString();
        string dtNums = dt.Rows[i][10].ToString();
        
        StringBuilder selectStrSb = new StringBuilder();
        selectStrSb.Append("POID = '").Append(dtPOID).Append("' AND sku_code = '").Append(dtName).Append("' AND payment_method = '").Append(dtPay).Append("'AND quantity = '").Append(dtNums).Append("'");
        DataRow[] drArray = dataBase.Select(selectStrSb.ToString());
        if (drArray.Length > 0)
        {
            dupTable.Rows.Add(dt.Rows[i].ItemArray);
            dt.Rows.RemoveAt(i);
        }
    }
    return dupTable;
}

static void TrimDataTable(DataTable dt)
{
    Console.WriteLine($"{dt.Rows.Count} Before Trim");
    for (int i = dt.Rows.Count - 1; i >= 0; i--)
    {
        if(dt.Rows[i]["Account"].ToString().Trim().Equals(String.Empty))
        {
            dt.Rows.RemoveAt(i);
        }
    }
     Console.WriteLine($"{dt.Rows.Count} After Trim");
}