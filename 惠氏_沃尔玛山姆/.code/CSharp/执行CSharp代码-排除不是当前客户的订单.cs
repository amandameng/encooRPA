//代码执行入口，请勿修改或删除
public void Run()
{
    //在这里编写您的代码
    if(增量订单结果数据表 != null && 增量订单结果数据表.Rows.Count > 0){
        for(int TotalRowCount = 增量订单结果数据表.Rows.Count-1; TotalRowCount >= 0; TotalRowCount--){
            DataRow dr = 增量订单结果数据表.Rows[TotalRowCount];
            if(dr["平台商"].ToString() != curCustomerName){
                增量订单结果数据表.Rows.Remove(dr);
            }
        }
    }
}
//在这里编写您的函数或者类