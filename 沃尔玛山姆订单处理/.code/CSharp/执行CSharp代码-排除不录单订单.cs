//代码执行入口，请勿修改或删除
public void Run()
{
    //在这里编写您的代码
    ETO增量订单数据表 = 增量订单数据表.Copy();
    if(不录单订单列表!=null && 不录单订单列表.Count > 0){
        int totalRows = ETO增量订单数据表.Rows.Count;
        for(int i=totalRows-1; i >= 0; i--){
            DataRow dr = ETO增量订单数据表.Rows[i];
            string order_number = dr["order_number"].ToString();
            if(不录单订单列表.Contains(order_number)){
               ETO增量订单数据表.Rows.Remove(dr);
            }
        }
    }
    
}
//在这里编写您的函数或者类