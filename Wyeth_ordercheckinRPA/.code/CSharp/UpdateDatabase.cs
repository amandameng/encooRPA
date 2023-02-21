//代码执行入口，请勿修改或删除
public void Run()
{
    DataTable resDt = new DataTable();
    resDt.Columns.Add("Account");
    resDt.Columns.Add("Pay");
    resDt.Columns.Add("POID");
    resDt.Columns.Add("itemName");
    resDt.Columns.Add("Nums");
    resDt.Columns.Add("ReadDate");
    resDt.Columns.Add("soldto");
    resDt.Columns.Add("shipto");
    resDt.Columns.Add("customerName");
    resDt.Columns.Add("DmsPo");
    resDt.Columns.Add("RDD");
    resDt.Columns.Add("isSuccess");
    foreach(DataRow accRow in TableByAccount.Rows)
    {
        DataTable dt = (DataTable)accRow["Table"];
        foreach(DataRow row in dt.Rows)
        {
            resDt.Rows.Add(new string[] {accRow["Account"].ToString(),
                                                            row["Pay"].ToString(),
                                                            row["POID"].ToString(),
                                                            row["itemName"].ToString(),
                                                            row["Nums"].ToString(),
                                                            DateTime.Now.ToUniversalTime().ToString(),
                                                            row["soldto"].ToString(),
                                                            row["shipto"].ToString(),
                                                            row["customerName"].ToString(),
                                                            row["DmsPo"].ToString(),
                                                            row["RDD"].ToString(),
                                                            row["Result"].ToString()
            });
        }
    }
    
    foreach(DataRow row in resDt.Rows)
    {
        Console.WriteLine("Account : " + row["Account"].ToString());
    }
    
    CheckIn(resDt, "Server=1.15.243.91;Database=vicode_wyeth;Uid=root;Password=encooVicode@123456;Allow User Variables=True");
}

private void CheckIn(DataTable inTable, string conn)
{
    DataTable dt = GetDataTableLayout("tracker", conn);
    
    foreach(DataRow dtRow in inTable.Rows)
    {
        DataRow row = dt.NewRow();
        
        row["dacang_account"] = dtRow["Account"];
        row["payment_method"] = dtRow["Pay"];
        row["order_capture_date"] = dtRow["ReadDate"];
        row["POID"] = dtRow["POID"];
        row["sku_code"] = dtRow["itemName"];
        row["quantity"] = dtRow["Nums"];
        row["sold_to_code"] = dtRow["soldto"];
        row["ship_to_code"] = dtRow["shipto"];
        row["customer_name"] = dtRow["customerName"];
        row["dms_po"] = dtRow["DmsPo"];
        
        DateTime rdd = new DateTime();
        if (!string.IsNullOrWhiteSpace(dtRow["RDD"].ToString()) && DateTime.TryParse(dtRow["RDD"].ToString(), out rdd))
        {
            row["RDD"] = rdd;
        }

        row["isSuccess"] = dtRow["isSuccess"];
        
        dt.Rows.Add(row);
    }

    UpdateDataBase(dt, "tracker", conn);
}

private DataTable GetDataTableLayout(string tableName, string connectionString)
    {
        DataTable table = new DataTable();

        using (MySqlConnection connection = new MySqlConnection(connectionString))
        {
            connection.Open();
            string query = $"SELECT * FROM " + tableName + " limit 0";
            using (MySqlDataAdapter adapter = new MySqlDataAdapter(query, connection))
            {
                adapter.Fill(table);
            };
        }

        return table;
    }
    
        public static void UpdateDataBase(DataTable dataTable, string tableName, string conn)
        {
            using (MySqlConnection connection = new MySqlConnection(conn))
            {
                connection.Open();

                using (MySqlTransaction tran = connection.BeginTransaction(IsolationLevel.Serializable))
                {
                    using (MySqlCommand cmd = new MySqlCommand())
                    {
                        cmd.Connection = connection;
                        cmd.Transaction = tran;
                        cmd.CommandText = $"SELECT * FROM " + tableName + " limit 0";

                        using (MySqlDataAdapter adapter = new MySqlDataAdapter(cmd))
                        {
                            adapter.UpdateBatchSize = 10000;
                            using (MySqlCommandBuilder cb = new MySqlCommandBuilder(adapter))
                            {
                                cb.SetAllValues = true;
                                adapter.Update(dataTable);
                                tran.Commit();
                            }
                        };
                    }
                }
                connection.Close();
            }
        }