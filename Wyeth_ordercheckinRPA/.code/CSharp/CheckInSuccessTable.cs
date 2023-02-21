//代码执行入口，请勿修改或删除
public void Run()
{
    /*
    DataRow successRow = successTable.NewRow();
    
    successRow["Pay"] = order.Rows[0]["Pay"];
    successRow["POID"] = order.Rows[0]["POID"];
    successRow["Amount"] = orderAmount;
    successRow["Time"] = DateTime.Now.ToString();
    successRow["Account"] = "newAccount";
    
    successTable.Rows.Add(successRow);
    */
    
    foreach (DataRow itemRow in order.Rows)
    {
        indexAmountDic.Add(itemRow["Result"].ToString(), orderAmount);
        indexDmsPoDic.Add(itemRow["Result"].ToString(), dmspo);
    }    
}
//在这里编写您的函数或者类