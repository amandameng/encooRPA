//代码执行入口，请勿修改或删除
public void Run()
{
    //在这里编写您的代码
    
    if(抓取失败订单列表!=null && 抓取失败订单列表.Count > 0){
        validOrdersDT = 实际增量订单数据表.Clone();
        DataRow[] drs = 实际增量订单数据表.Select(string.Format("order_number not in ({0})", string.Join(", ", 抓取失败订单列表)));
        foreach(DataRow dr in drs){
           validOrdersDT.ImportRow(dr);
        }
    }else{
        validOrdersDT = 实际增量订单数据表.Copy();
    }
    
    // Rename column to Excel exported header
    validOrdersDT.Columns["order_number"].ColumnName = "Document Number";
    validOrdersDT.Columns["document_type"].ColumnName = "Document Type";
    validOrdersDT.Columns["received_date_time"].ColumnName = "Received Date";
    validOrdersDT.Columns["vendor_number"].ColumnName = "Vendor Number";
    validOrdersDT.Columns["location"].ColumnName = "Location";

    validOrdersDT = validOrdersDT.DefaultView.ToTable(true, new string[]{"Document Number", "Document Type", "Received Date", "Vendor Number", "Location"});
}
//在这里编写您的函数或者类