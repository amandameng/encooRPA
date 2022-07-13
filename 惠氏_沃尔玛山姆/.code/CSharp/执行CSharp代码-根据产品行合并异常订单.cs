//代码执行入口，请勿修改或删除
public void Run()
{
    //在这里编写您的代码
    DataTable distinctOrderItemDT = exceptionDT.DefaultView.ToTable(true, new string[]{"customer_order_number", "customer_sku"});
    mergedExceptionDT = exceptionDT.Clone();
    
    foreach(DataRow dr in distinctOrderItemDT.Rows){
        string orderNumber = dr["customer_order_number"].ToString();
        string customerSku = dr["customer_sku"].ToString();
        DataRow[] drs = exceptionDT.Select(string.Format("customer_order_number='{0}' and customer_sku='{1}'", orderNumber, customerSku));
        
        List<string> exceptionCategoryList = new List<string>{};
        List<string> exceptionDetailList = new List<string>{};
        DataRow finalDataRow = drs[0];
        foreach(DataRow exceptionDR in drs){
            exceptionCategoryList.Add(exceptionDR["exception_category"].ToString());
            exceptionDetailList.Add(exceptionDR["exception_detail"].ToString());
        }
        finalDataRow["exception_category"] = string.Join("|", exceptionCategoryList);
        finalDataRow["exception_detail"] = string.Join("|", exceptionDetailList);
        mergedExceptionDT.ImportRow(finalDataRow);
    }
    
    
}
//在这里编写您的函数或者类