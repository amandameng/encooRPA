//代码执行入口，请勿修改或删除
public void Run()
{
    if (successTable != null && successTable.Rows.Count > 0)
    {
        foreach (DataRow row in successTable.Rows)
        {
            if (row["Account"].ToString().Equals("newAccount"))
            {
                row["Account"] = AccountRow["Account"];
            }
        }
        //successTable.Rows[successTable.Rows.Count - 1]["Account"] = AccountRow["Account"];
    }
    
    if (failedTable != null && failedTable.Rows.Count > 0)
    {
        foreach (DataRow row in failedTable.Rows)
        {
            if (row["Account"].ToString().Equals("newAccount"))
            {
                row["Account"] = AccountRow["Account"];
            }
        }
        //failedTable.Rows[failedTable.Rows.Count - 1]["Account"] = AccountRow["Account"];
    }
}
//在这里编写您的函数或者类