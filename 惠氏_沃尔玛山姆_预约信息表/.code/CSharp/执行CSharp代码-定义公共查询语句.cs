//代码执行入口，请勿修改或删除
public void Run()
{
    //在这里编写您的代码
    flow_name = customer_name + "订单采集";
    rpaAccountsSql = string.Format("select * from rpa_accounts where customer_name='{0}' and flow_name='{1}'", customer_name, flow_name);
    mailSettingSql = string.Format("select * from mail_setting where customer_name='{0}' and flow_name='{1}'", customer_name, flow_name);
    masterDataSql = string.Format("select customer_material_no, customer_product_name, wyeth_material_no, wyeth_product_name, wyeth_nps, size, wyeth_unit_price from material_master_data where customer_name='{0}' and ver=(select max(ver) max_version from material_master_data where customer_name='{0}')", customer_name);
    curSoldToShipToSql = string.Format("select dc_name as DC, dc_no as DC编号, store_location as 门店, ship_to as 'Ship to', sold_to as 'Sold to', wyeth_dc_no as 惠氏大仓账号, customer_pay_method as 支付方式, dms_account DMS账号 from ship_to_sold_to where customer_name='{0}'", customer_name);
    constraintsSql = string.Format("SELECT sku_code, comment FROM constraint_list where ver=(select max(ver) from constraint_list)");
    specialSkuSql = string.Format("SELECT sold_to, sku_code, comment FROM special_products where customer_name='{0}'", customer_name);
}
//在这里编写您的函数或者类