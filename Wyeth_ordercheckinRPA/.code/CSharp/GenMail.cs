//代码执行入口，请勿修改或删除
public void Run()
{    
    DataTable inTable = (DataTable)Account["Table"];

    string account = Account["Account"].ToString();
    savePath = $"C:\\RPA\\Wyeth\\{account}Result.xlsx";
    // 失败图片添加到附件
    List<string> failedPicList = System.Convert.IsDBNull(Account["FailedPicList"]) ? new List<string>{} : (List<string>)Account["FailedPicList"];
    if(failedPicList != null && failedPicList.Count > 0){
        foreach(string file in failedPicList){
            附件.Add(file);
        }
    }
    
    string date = DateTime.Now.ToShortDateString();
    string result = "全部成功";
    
    StringBuilder contextSb = new StringBuilder();
    contextSb.AppendLine("Please check the attachment for Wyeth RPA's execution result.");
    context = contextSb.ToString();
    
    //Gen AttachTable
    //FillAttachTable(ref attachTable, duplicateTable, Account);
    foreach (DataRow inRow in inTable.Rows)
    {
        DataRow attRow = attachTable.NewRow();
        
        attRow["Account"] = Account["Account"].ToString();
        attRow["Pay"] = inRow["Pay"];
        attRow["DateTime"] = inRow["ReadDate"];
        attRow["soldto"] = inRow["soldto"];
        attRow["shipto"] = inRow["shipto"];
        attRow["CustomerName"] = inRow["customerName"];
        attRow["POID"] = inRow["POID"];
        attRow["itemName"] = inRow["itemName"];
        attRow["Nums"] = inRow["Nums"];
        attRow["Result"] = inRow["Result"];
        attRow["Reson"] = inRow["Reson"];
        attRow["Amount"] = inRow["Amount"];
        attRow["DmsPo"] = inRow["DmsPo"];
        attRow["RDD"] = inRow["RDD"];
        
        if (constraintSet.Contains(inRow["itemName"].ToString()))
        {
            attRow["isCon"] = "是";
        }
        else
        {
            attRow["isCon"] = "否";
        }
        
        if(inRow["Result"].ToString().Equals("失败"))
        {
            result = "存在失败";
        }
        attachTable.Rows.Add(attRow);
    }
    
    subject = $"Wyeth RPA result {date} - {account} - {inTable.Rows[0]["customerName"].ToString()} - {result}";

}
static void FillAttachTable(ref DataTable dt, DataTable dupTable, DataRow Account)
{
    foreach(DataRow row in dupTable.Rows)
    {
        if (row[0].ToString().Equals(Account["Account"].ToString()))
        {
            dt.Rows.Add(row.ItemArray);
        }        
    }
    foreach(DataRow row in dt.Rows)
    {
        row["Result"] = "失败";
        row["Reson"] = "重复订单";
    }
}