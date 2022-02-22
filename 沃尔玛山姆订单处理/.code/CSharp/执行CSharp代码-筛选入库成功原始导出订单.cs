//代码执行入口，请勿修改或删除
public void Run()
{
    //在这里编写您的代码
    
    Console.WriteLine($"待更新exportedOrders: {待更新exportedOrders.Rows.Count.ToString()}");
    // Rename column to Excel exported header
    待更新exportedOrders.Columns["order_number"].ColumnName = "Document Number";
    待更新exportedOrders.Columns["document_type"].ColumnName = "Document Type";
    待更新exportedOrders.Columns["received_date_time"].ColumnName = "Received Date";
    待更新exportedOrders.Columns["vendor_number"].ColumnName = "Vendor Number";
    待更新exportedOrders.Columns["location"].ColumnName = "Location";

    待更新exportedOrders = 待更新exportedOrders.DefaultView.ToTable(true, new string[]{"Document Number", "Document Type", "Received Date", "Vendor Number", "Location"});
}
//在这里编写您的函数或者类