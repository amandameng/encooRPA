//代码执行入口，请勿修改或删除
public void Run()
{
    //在这里编写您的代码
    // 历史全部订单UniqDT
    // 删单异常数据表
    // 最近20天原始订单数据表
    // 存在DB，不存在历史订单的为删单，得考虑大仓号，订单号
    deletedOrderDT = 最近20天原始订单数据表.Clone();
    foreach(DataRow orderDR in 最近20天原始订单数据表.Rows){
        string dc_no = orderDR["dc_no"].ToString();
        string customer_order_number = orderDR["order_number"].ToString();
        Console.WriteLine(string.Format("采购单号 = '{0}' and 门店 like '{1}%'", customer_order_number, dc_no));
        DataRow[] portal20daysOrders = 历史全部订单UniqDT.Select(string.Format("采购单号 = '{0}' and 门店 like '{1}%'", customer_order_number, dc_no));
        DataRow[] exceptionDeletedDbOrders = 删单异常数据表.Select(string.Format("customer_order_number = '{0}' and dc_no = '{1}'", customer_order_number, dc_no));
        if(portal20daysOrders.Length == 0 && exceptionDeletedDbOrders.Length == 0){
            Console.WriteLine(string.Join(",", orderDR.ItemArray));
            deletedOrderDT.ImportRow(orderDR);
        }
    }
    // Convert.ToInt32("as");
}
//在这里编写您的函数或者类