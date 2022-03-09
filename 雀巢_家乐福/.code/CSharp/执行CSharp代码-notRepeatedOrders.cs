//代码执行入口，请勿修改或删除
public void Run()
{
    //在这里编写您的代码 
    增量异常订单数据表 = exceptionResultDT.Clone();
    foreach(DataRow dr in exceptionResultDT.Rows){
        string order_number = dr["客户PO"].ToString();
        string product_code = dr["客户产品代码"].ToString();
        string 问题分类 = dr["问题分类"].ToString().Trim();
        string 价差 = dr["价差"].ToString().Trim();
        DataRow[] searchedResults = existingExceptionOrders.Select(String.Format("客户PO='{0}' and 客户产品码='{1}' and 问题分类='{2}' and 价差='{3}'", order_number, product_code, 问题分类, 价差));
        if(searchedResults.Length == 0){
            增量异常订单数据表.ImportRow(dr);
        }
    }
    //IEnumerable<DataRow> rows = exceptionResultDT.AsEnumerable().Except(existingExceptionOrders.AsEnumerable());
    //增量异常订单数据表 = rows.CopyToDataTable();
}
//在这里编写您的函数或者类