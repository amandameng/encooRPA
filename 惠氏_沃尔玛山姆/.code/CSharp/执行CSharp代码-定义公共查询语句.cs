//代码执行入口，请勿修改或删除
public void Run()
{
    //在这里编写您的代码
    flow_name = customer_name + "订单采集";
    rpaAccountsSql = string.Format("select * from rpa_accounts where customer_name='{0}' and flow_name='{1}'", customer_name, flow_name);
    mailSettingSql = string.Format("select * from mail_setting where customer_name='{0}' and flow_name='{1}'", customer_name, flow_name);
    orderFetchRecordsSql = string.Format("select * from order_fecthing_records where cur_date='{0}' and customer_name='{1}' and fetch_status=1", DateTime.Now.ToString("yyyy-MM-dd"), customer_name);
    masterDataSql = string.Format("select customer_material_no, customer_product_name, wyeth_material_no, wyeth_product_name, wyeth_nps, size, wyeth_unit_price from material_master_data where customer_name='{0}' and ver=(select max(ver) max_version from material_master_data where customer_name='{0}')", customer_name);
    curSoldToShipToSql = string.Format("select dc_name as DC, dc_no as DC编号, store_location as 门店, ship_to as 'Ship to', sold_to as 'Sold to', wyeth_dc_no as 惠氏大仓账号, customer_pay_method as 支付方式, dms_account DMS账号 from ship_to_sold_to where customer_name='{0}'", customer_name);
    wmSoldToShipToSql = string.Format("select dc_name as DC, dc_no as DC编号, store_location as 门店, ship_to as 'Ship to', sold_to as 'Sold to', wyeth_dc_no as 惠氏大仓账号, customer_pay_method as 支付方式, dms_account DMS账号 from ship_to_sold_to where top_customer_name='{0}'", "沃尔玛");
    constraintsSql = string.Format("SELECT sku_code, comment FROM constraint_list where ver=(select max(ver) from constraint_list)");
    specialSkuSql = string.Format("SELECT sold_to, sku_code, comment, customer_sku_code, dc_no FROM special_products where customer_name='{0}' and ver=(select max(ver) max_version from special_products where customer_name='{0}')", customer_name);
    lastCaptureDateSql = string.Format("select max(order_capture_date) maxCreatedTime from {0} where customer_name='{1}'", dtRow_ProjectSettings["clean订单数据库表名"].ToString(), customer_name);
    customerPreferenceSql = string.Format("select * from customer_preference_settings where customer_name='{0}'", customer_name);
}
//在这里编写您的函数或者类