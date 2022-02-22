//代码执行入口，请勿修改或删除
public void Run()
{
    //在这里编写您的代码
    int totalRows = 增量订单数据表.Rows.Count;
    for(int i=totalRows-1; i >= 0; i--){
        DataRow dr = 增量订单数据表.Rows[i];
        string location = dr["location"].ToString();
        if(!WMLocationsList.Contains(location)){
            // 增量订单数据表.Rows.Remove(dr);
            不录单订单列表.Add(dr["order_number"].ToString());
        }
    }
}
//在这里编写您的函数或者类