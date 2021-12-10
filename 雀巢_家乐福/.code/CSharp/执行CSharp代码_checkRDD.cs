//代码执行入口，请勿修改或删除
public void Run()
{
    //在这里编写您的代码
    rdd异常订单数据表 = 最近待预约订单数据表.Clone();
    foreach(DataRow dr in 最近待预约订单数据表.Rows){
      string 网站交货日期 = dr["交货日期"].ToString().Trim();
      string request_delivery_date = dr["request_delivery_date"].ToString().Trim();
      if(网站交货日期 != request_delivery_date){
        rdd异常订单数据表.ImportRow(dr);
      }
    }
}
//在这里编写您的函数或者类