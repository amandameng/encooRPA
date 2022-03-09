//代码执行入口，请勿修改或删除
public void Run()
{
    //在这里编写您的代码
    DataTable dictinctOrderNumDT = allToDoEx2oDT.DefaultView.ToTable(true, new string[]{"Customer_Order_Number", "Customer_Name"});
    
    List<string> updateOrderJobHistoryList = new List<string>{};
    foreach(DataRow dr in dictinctOrderNumDT.Rows){
        int edi_sent = 1; // success
        
        string orderNumber = dr["Customer_Order_Number"].ToString();
        if(allExceptionOrdersDT != null && allExceptionOrdersDT.Rows.Count > 0){
            DataRow[] excepDrs = allExceptionOrdersDT.Select(string.Format("order_number='{0}'", orderNumber));
            if(excepDrs.Length > 0){
                edi_sent = -1; // pending 
                ediMessage = excepDrs[0]["message"].ToString();
            }
        }
        string customerName = dr["Customer_Name"].ToString();
        string updateSql = string.Empty;
        if(ediStatus){
            updateSql = string.Format("update order_job_history set edi_sent={0}, edi_send_message='{1}', edi_sent_time='{2}' where order_number='{3}' and customer_name='{4}' ", edi_sent, ediMessage, DateTime.Now, orderNumber, customerName);
        }
       /* 
        else{
            updateSql = string.Format("update order_job_history set edi_send_message='{0}', edi_sent_time='{1}' where order_number='{2}' and customer_name='{3}' ", ediMessage, DateTime.Now, orderNumber, customerName);
        }
        */
        if(!string.IsNullOrEmpty(updateSql)) updateOrderJobHistoryList.Add(updateSql);
    }
    updateOrderJobHistorySQL = string.Join(";", updateOrderJobHistoryList);
    
}
//在这里编写您的函数或者类