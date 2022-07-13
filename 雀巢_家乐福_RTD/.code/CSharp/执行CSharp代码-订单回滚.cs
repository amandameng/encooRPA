//代码执行入口，请勿修改或删除
public void Run()
{
    //在这里编写您的代码
    List<String> orders_number_List = new List<String> {};
    
    // define outside
    // List<String> deleteExcelToOrderSqlList = new List<String> {};

    foreach(DataRow dr in 增量订单单号数据表.Rows){
        string order_number = dr["order_number"].ToString();
        orders_number_List.Add(order_number);
        string deletEx2oSql = string.Format("delete from excel_to_order where Customer_Name = '{0}' and PO_Number like '{1}%'  and id> 0", 客户平台, order_number);
        deleteExcelToOrderSqlList.Add(deletEx2oSql);
    }
    string order_numners = String.Join(",", orders_number_List);
    
    
    deleteOrderSql = string.Format("delete from orders where customer_name = '{0}' and order_number in ({1}) ", 客户平台, order_numners);
    deleteOrderItemsSql = string.Format("delete from order_line_items where customer_name = '{0}' and order_number in ({1}) ", 客户平台, order_numners);
    
    deleteCleanOrdersSql = string.Format("delete from clean_order where 客户名称 = '{0}' and 客户Po_No in ({1}) ", 客户平台, order_numners);
    deleteExceptionOrdersSql = string.Format("delete from exception_order where 客户名称 = '{0}' and 客户PO in ({1}) ", 客户平台, order_numners);
    deleteOrderJobHistorySql = string.Format("delete from order_job_history where customer_name = '{0}' and 客户PO in ({1}) ", 客户平台, order_numners);
}
//在这里编写您的函数或者类