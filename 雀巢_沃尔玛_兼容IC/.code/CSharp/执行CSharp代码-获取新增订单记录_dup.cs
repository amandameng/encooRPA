//代码执行入口，请勿修改或删除
public void Run()
{
    //在这里编写您的代码
    realValidOrders = curOrdersDT.Clone();
    foreach(DataRow dr in curOrdersDT.Rows){
        string order_number = dr["order_number"].ToString();
        string document_link = dr["order_link"].ToString();
        DateTime create_date_time = Convert.ToDateTime(dr["Date"].ToString());
        DataRow[] existingOrderDrs = existingOrdersDT.Select(string.Format("order_number='{0}' and document_link='{1}'", order_number, document_link));
        if(existingOrderDrs.Length == 0){
           realValidOrders.ImportRow(dr);
        }
    }
}
//在这里编写您的函数或者类