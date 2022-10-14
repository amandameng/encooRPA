//代码执行入口，请勿修改或删除
public void Run()
{
    //在这里编写您的代码
    flow_name = customer_name + "订单采集";
    // 区域 来自参数
    if(string.IsNullOrEmpty(区域)){
        rpaAccountsSql = string.Format("select * from rpa_accounts where customer_name='{0}' and flow_name='{1}'", customer_name, flow_name);
    }else{
        string[] 区域数组 = 区域.Split(new string[] { ",", "，" }, StringSplitOptions.RemoveEmptyEntries);
        List<string> 指定区域List = new List<string>{};
        foreach(string item in 区域数组){
            指定区域List.Add("'" + item + "'");
        }
        string 指定区域 = String.Join(",", 指定区域List);
        rpaAccountsSql = string.Format("select * from rpa_accounts where customer_name='{0}' and flow_name='{1}' and region in ({2})", customer_name, flow_name, 指定区域);
    }
    延期表指定日期 = fetchvalidDate(延期表日期);
    
    //string 指定日期
    mailSettingSql = string.Format("select * from mail_setting where customer_name='{0}' and flow_name='{1}'", customer_name, flow_name);
    orderFetchRecordsSql = string.Format("select * from order_fecthing_records where cur_date='{0}' and customer_name like '%{1}%' and fetch_status=1", DateTime.Now.ToString("yyyy-MM-dd"), customer_name);
    masterDataSql = string.Format("select customer_material_no, customer_product_name, customer_product_nps, wyeth_material_no, wyeth_product_name, size, wyeth_unit_price, wyeth_nps from material_master_data where customer_name='{0}' and ver=(select max(ver) max_version from material_master_data where customer_name='{0}')", customer_name);
    curSoldToShipToSql = string.Format("select dc_name as DC, dc_no as DC编号, store_location as 门店, ship_to as 'Ship to', sold_to as 'Sold to', wyeth_dc_no as 惠氏大仓账号, region, customer_pay_method as 支付方式, dms_account DMS账号, discount, rtmart_dc_category as 仓别 from ship_to_sold_to where customer_name like '{0}%'", customer_name);
    constraintsSql = string.Format("SELECT sku_code, comment FROM constraint_list where ver=(select max(ver) from constraint_list)");
    specialSkuSql = string.Format("SELECT sold_to, customer_sku_code, sku_code, comment, customer_name FROM special_products where customer_name like '{0}%' and ver=(select max(ver) max_version from special_products where customer_name like '{0}%')", customer_name);
    lastCaptureDateSql = string.Format("select max(order_date) maxCreatedTime, region from {0} where customer_name='{1}' group by region", dtRow_ProjectSettings["订单数据库表名"].ToString(), customer_name);
    todayOrdersSql = string.Format(@"SELECT distinct order_date, must_arrived_by, {0}.region, {0}.customer_name, wyeth_POID,  
                                                             date_format({0}.created_time, '%Y-%m-%d') as readDate,ship_to_sold_to.store_location FROM {0}
                                                             join ship_to_sold_to on ship_to_sold_to.dc_no = {0}.dc_no and ship_to_sold_to.customer_name = concat({0}.customer_name, {0}.region)
                                                             where {0}.customer_name='{1}' and date_format({0}.created_time, '%Y-%m-%d')>='{2}'  and {0}.order_type='订单' 
                                                             order by region asc, order_date", dtRow_ProjectSettings["订单数据库表名"].ToString(), customer_name, 延期表指定日期.AddDays(-30).ToString("yyyy-MM-dd"));

    successOrdersTrackerSQL = string.Format(@"SELECT *
                                                                            FROM tracker 
                                                                            where order_capture_date >= '{0}' and (ship_to_code in (select ship_to from ship_to_sold_to where customer_name = '{1}' or customer_name like '%{1}%'))
                                                                            and isSuccess = '成功' and POID is not null and POID !=''", 延期表指定日期.ToString("yyyy-MM-dd"), customer_name);

}
//在这里编写您的函数或者类

public DateTime fetchvalidDate(string dateStrParam){
     DateTime resultDate;
    
    if(String.IsNullOrEmpty(dateStrParam)){
        resultDate = DateTime.Now; // 参数不传，如果是开始时间，则今天时间 - 3, 否则是今天时间
    }else{       
        bool isValid = DateTime.TryParse(dateStrParam, out resultDate);
        if(!isValid){
            throw(new Exception("输入的日期格式不正确，请输入 yyyy-MM-dd格式的日期，比如：2022-04-11"));
        }
    }
    return resultDate;
}