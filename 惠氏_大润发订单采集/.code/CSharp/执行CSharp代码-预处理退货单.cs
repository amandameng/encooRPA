//代码执行入口，请勿修改或删除
public void Run()
{
    //在这里编写您的代码
    List<string> orderList = returnOrdersDT.Rows.Cast<DataRow>().Select<DataRow, string>(dr => dr["采购单号"].ToString()).ToList();
    
    string orderNumStr = string.Join(",", orderList);
    
    existingReturnOrdersSql = string.Format("SELECT order_number FROM {0} where order_number in ({1}) and order_type = '退单'", dtRow_ProjectSettings["订单数据库表名"], orderNumStr);
}
//在这里编写您的函数或者类