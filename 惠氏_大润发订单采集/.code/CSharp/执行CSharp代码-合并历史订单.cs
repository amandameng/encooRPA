//代码执行入口，请勿修改或删除
public void Run()
{
    //在这里编写您的代码
    DataTable 历史全部订单DT = new DataTable();;
    if(历史待收订单DT != null){
        历史全部订单DT = 历史待收订单DT;
    }
    
    if(历史全部订单DT != null && 历史全部订单DT.Rows.Count > 0){
        if(历史已收订单DT !=null){
            历史全部订单DT.Merge(历史已收订单DT);
        }
    }else{
         if(历史已收订单DT !=null){
            历史全部订单DT = 历史已收订单DT;
        }
    }
    if(历史全部订单DT!= null && 历史全部订单DT.Rows.Count > 0){
        历史全部订单UniqDT = 历史全部订单DT.DefaultView.ToTable(true, new string[]{"采购单号"});
    }
    
    deletedOrdersQuery = String.Format(@"select * from {0}
                 where not exists
                 (select order_number from {1}
                 where {0}.order_number = {1}.order_number) and order_date >= subdate(now(), {2}) and region='{3}'
                 and not exists(select customer_order_number from exception_order where exception_order.customer_order_number=rtmart_orders.order_number and customer_name='{4}' and exception_category like '%删单%')", 
    dtRow_ProjectSettings["订单数据库表名"].ToString(), dtRow_ProjectSettings["删单临时表"].ToString(), Convert.ToInt32(dtRow_ProjectSettings["删单检查天数"].ToString()), dtRow_ModuleSettings["区域"].ToString(), dtRow_ModuleSettings["customer_name"].ToString());

    Console.WriteLine("newOrdersQuery: {0}", deletedOrdersQuery);
}
//在这里编写您的函数或者类