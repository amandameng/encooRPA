//代码执行入口，请勿修改或删除
public void Run()
{
    //在这里编写您的代码
    DataTable 增量订单详情数据表 = 增量订单数据表.Clone();

    DataRow[] dataRows = 增量订单数据表.Select(String.Format("order_number='{0}'", 订单号));
    
    foreach(DataRow dr in dataRows){
        dr["order_type"] = 采购单类型;
        增量订单详情数据表.ImportRow(dr);
    }

   订单数据表 = 增量订单详情数据表.DefaultView.ToTable(true, new string[]{"order_number", "order_type", "order_create_time", "order_create_date", "order_expire_date", "logistics_warehouse"});
   
   订单详情数据表 = 增量订单详情数据表.DefaultView.ToTable(true, new string[]{"order_number", "line_number", "customer_product_code", "customer_product_name", "product_barcode", "case_uom", "Nestle_Case_Configuration", "unit_price", "discount_rate", "total_sales", "purchase_qty", "confirm_qty"});
}

//在这里编写您的函数或者类