//代码执行入口，请勿修改或删除
public void Run()
{
    //在这里编写您的代码
    List<string> documentLinkList = 增量订单数据表.Rows.Cast<DataRow>().Select<DataRow, string>(dr => "'" + dr["document_link"].ToString() + "'").ToList();
    List<string> orderNumberList = 实际增量订单数据表.Rows.Cast<DataRow>().Select<DataRow, string>(dr => dr["order_number"].ToString()).ToList();
    
    string docLinkStr = String.Join(",", documentLinkList);
    string orderNumberStr = String.Join(",", orderNumberList);

    string newOrdersFromDBSql = string.Format("select * from {0} where customer_name='{1}' and document_link in ({2}) order by order_number, create_date_time desc", dtRow_ProjectSettings["订单数据库表名"].ToString(), curCustomerName, docLinkStr);
    string relatedOrdersAndDocLinkSql = string.Format("select distinct order_number, document_link, created_time from {0} where customer_name='{1}' and document_link in ({2}) group by document_link order by order_number, create_date_time desc", dtRow_ProjectSettings["订单数据库表名"].ToString(), curCustomerName, docLinkStr);
    string relatedOrdersCountAndDocLinkSql = string.Format(@"select count(order_number) orders_count, order_number from
                                                                                        (select distinct order_number, document_link from {0} where customer_name='{1}' and document_link in ({2}) group by document_link order by order_number, create_date_time desc) s2
                                                                                        group by order_number", dtRow_ProjectSettings["订单数据库表名"].ToString(), curCustomerName, docLinkStr);
    string walmartOldOrderSql = string.Format("select * from walmart_exported_orders where order_number in ({0})", orderNumberStr);
    Console.WriteLine("----relatedOrdersCountAndDocLinkSql: {0}", relatedOrdersCountAndDocLinkSql);
    inSqlDic = new Dictionary<string, string>{{"dbOrdersDT", newOrdersFromDBSql}, {"ordersAndLinkDT", relatedOrdersAndDocLinkSql}, {"ordersCountDT", relatedOrdersCountAndDocLinkSql}, {"walmartOldOrderDT", walmartOldOrderSql}};
}
//在这里编写您的函数或者类