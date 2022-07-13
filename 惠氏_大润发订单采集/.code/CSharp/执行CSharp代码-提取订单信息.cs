//代码执行入口，请勿修改或删除
public void Run()
{
    //在这里编写您的代码
    uniqOrdersDTFromSheets = origOrdersFromSheetDT.DefaultView.ToTable(true, new string[]{"采购单号"});
    // 如果订单号为空，则抛出异常重试
    if(uniqOrdersDTFromSheets.Rows.Count > 0){
        foreach(DataRow dr in uniqOrdersDTFromSheets.Rows){
            string 采购单号 = dr["采购单号"].ToString().Trim();
            if(string.IsNullOrEmpty(采购单号)){
                throw new Exception("订单号为空，需要重新下载文件");
            }
        }
    }
    newOrdersQuery = String.Format(@"select order_number from {0}
                 where not exists
                 (select order_number from {1}
                 where {0}.order_number = {1}.order_number and {1}.order_date > subdate(now(), 180)
                 )", dtRow_ProjectSettings["订单列表临时数据表名"].ToString(), dtRow_ProjectSettings["订单数据库表名"].ToString());

    Console.WriteLine("newOrdersQuery: {0}", newOrdersQuery);

}
//在这里编写您的函数或者类