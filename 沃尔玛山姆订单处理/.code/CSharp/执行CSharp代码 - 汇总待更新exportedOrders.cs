//代码执行入口，请勿修改或删除
public void Run()
{
    if(实际增量订单数据表 == null){
        return;
    }
    DataTable validOrdersDT = excludeFailedOrder(实际增量订单数据表, 抓取失败订单数据表);
    //在这里编写您的代码
    if(待更新exportedOrders==null || 待更新exportedOrders.Rows.Count == 0){
        待更新exportedOrders = validOrdersDT;
    }else{
        待更新exportedOrders.Merge(validOrdersDT);
    }
}
//在这里编写您的函数或者类

public DataTable excludeFailedOrder(DataTable 实际增量订单数据表, DataTable 抓取失败订单数据表){
    DataTable validOrdersDT = new DataTable();
    if(抓取失败订单数据表!=null && 抓取失败订单数据表.Rows.Count > 0){
        validOrdersDT = 实际增量订单数据表.Clone();
        foreach(DataRow dr in 实际增量订单数据表.Rows){
            Console.WriteLine("--" + dr["order_number"].ToString() + "--" + dr["received_date_time"].ToString() + "--");
            foreach(DataRow dRow in 抓取失败订单数据表.Rows){
                Console.WriteLine("||" + string.Join("-", dRow.ItemArray) + "||");
            }
            DataRow[] drs = 抓取失败订单数据表.Select(string.Format("order_number = '{0}' and received_date_time='{1}'", dr["order_number"].ToString(), dr["received_date_time"].ToString()));
            if(drs.Length == 0){
                validOrdersDT.ImportRow(dr);
            }
        }
    }else{
        Console.WriteLine($"实际增量订单数据表: {实际增量订单数据表.Rows.Count.ToString()}");
        validOrdersDT = 实际增量订单数据表.Copy();
    }
    return validOrdersDT;
}