//代码执行入口，请勿修改或删除
public void Run()
{    
    string date = DateTime.Now.ToShortDateString();
    subject = $"Wyeth RPA result {date} - No new order";
    //receivers = Account["Receiver"].ToString();
    
    savePath = "C:\\RPA\\Wyeth\\DuplicateResult.xlsx";
    
    StringBuilder contextSb = new StringBuilder();
    context = contextSb.ToString();
    
    //Gen AttachTable
    FillAttachTable(ref attachTable, duplicateTable);

}
static void FillAttachTable(ref DataTable dt, DataTable dupTable)
{
    foreach(DataRow row in dupTable.Rows)
    {
            dt.Rows.Add(row.ItemArray);
    }
    foreach(DataRow row in dt.Rows)
    {
        row["Result"] = "失败";
        row["Reson"] = "重复订单";
    }

}