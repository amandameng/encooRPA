//代码执行入口，请勿修改或删除
public void Run()
{
    //在这里编写您的代码
    setDeafultValueForJobHistoryDT(wmRTDetoResultDT);
}
//在这里编写您的函数或者类

public void setDeafultValueForJobHistoryDT(DataTable etoResultDT){
    if(etoResultDT!=null && etoResultDT.Rows.Count > 0){
        wmRTDorderJobHistoryDT = etoResultDT.DefaultView.ToTable(true, new string[]{"Customer Order Number", "Customer Order Date", "customer_name"});
        wmRTDorderJobHistoryDT.Columns.Add("report_type", typeof(string));
        wmRTDorderJobHistoryDT.Columns.Add("email_sent", typeof(int));
        wmRTDorderJobHistoryDT.Columns.Add("email_sent_time", typeof(string));
        foreach(DataRow dr in wmRTDorderJobHistoryDT.Rows){
            dr["report_type"] = "EX2O";
            dr["email_sent"] = 1;
            dr["email_sent_time"] = DateTime.Now.ToString();
        }
    }
}