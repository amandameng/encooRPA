//代码执行入口，请勿修改或删除
public void Run()
{
    //在这里编写您的代码
    if(增量订单数据表!=null && 增量订单数据表.Rows.Count > 0){
        List<string> 订单编号列表 = new List<string>{};

        foreach(DataRow dr in 增量订单数据表.Rows){
            string 订单编号 = dr["order_number"].ToString();
            if(!订单编号列表.Contains(订单编号)){
                订单编号列表.Add(订单编号);
            }
        }
    
        string 订单编号字符串 = String.Join(",", 订单编号列表.ToArray());
        
        string srcSql = @"SELECT ods.*, stst.Request_Delivery_Date, stst.WMDC, ods.location Nestle_Plant_No,stst.Sold_to_Code, stst.Ship_to_Code, stst.daysSpent,
                                oli.line_number, oli.product_code customer_product_code, oli.item_instructions, oli.cost, oli.quantity_ordered, oli.pack,
                                oli.allowance_total oli_allowance_total , `oli`.`extended_cost` oli_extended_cost, `oli`.`allowance_percent` oli_allowance_percent,
                                mmd.Nestle_Material_No, mmd.Nestle_BU, mmd.Material_Description, mmd.Remark, mmd.Remark_Option, mmd.Nestle_NPS, mmd.Distribution_Channel,
                                mmd.Adjustive_Price, mmd.Discount_Rate as 产品扣点
                                from walmart_orders ods
                                join walmart_order_items oli on oli.order_number = ods.order_number and oli.document_link = ods.document_link
                                left join sold_to_ship_to stst on ods.location=stst.Nestle_Plant_No and stst.Customer_Name=ods.customer_name
                                left join material_master_data mmd on mmd.Customer_Material_No = oli.product_code and mmd.`Customer_Name` = ods.customer_name 
                                and mmd.`Nestle_Plant_No`= `stst`.`Nestle_Plant_No` and ods.location = mmd.Nestle_Plant_No
                                where ods.customer_name='{0}' and ods.order_number in ({1})
                                group by ods.order_number, ods.document_link, oli.product_code
                                order by ods.order_number, ods.create_date_time desc";
    
        selectSql = String.Format(srcSql, curCustomerName, 订单编号字符串);
        Console.WriteLine(selectSql);
    }
   
    
    // 一年内的excel to Order
    existingEX2OSql = String.Format("SELECT distinct PO_Number, Customer_Order_Number from excel_to_order where Customer_Name='{0}' and created_time > date_sub(CURDATE(), INTERVAL 360 DAY) limit 10000;", curCustomerName);
    noPriceCheckSql = string.Format("select * from walmart_skip_price_check where customer_name='{0}'", curCustomerName);
    orderJobHistorySql = string.Format("select * from order_job_history where customer_name='{0}' and email_sent=1 and report_type='{1}'", curCustomerName, "EX2O");
    exportedOrdersSql = string.Format("select distinct order_number from walmart_exported_orders where received_date_time > date_sub(CURDATE(), INTERVAL 180 DAY) limit 10000;");
}
//在这里编写您的函数或者类