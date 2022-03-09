//代码执行入口，请勿修改或删除
public void Run()
{
    //在这里编写您的代码
    // 需要发送EDI的 excel To Order 信息
    List<string> ex2oCustomerList = new List<string>{};
    List<string> customerList = new List<string>{};
    foreach(DataRow dr in validEdiCustomerDT.Rows){
        string customer_name = dr["customer_name"].ToString();
        customerList.Add("'" + customer_name + "'");
        if(customer_name == "沃尔玛"){
            // 查找散威化产品码
            bulkProductsSql = "select group_concat(nestle_code_allocation) nestleCodes from nestle_bulk_walfer_config where Customer_Name='" + customer_name + "'";
            // 沃尔玛 包含散威化产品的订单
            excelToOrderQueryForWMSql = @"select distinct Customer_Order_Number from excel_to_order 
                                    join order_job_history
                                    on excel_to_order.Customer_Name = order_job_history.customer_name 
                                    and excel_to_order.Customer_Order_Number = order_job_history.order_number
                                    where excel_to_order.Customer_Name='" + customer_name + @"' 
                                    and SAP_Material in ({0}) 
                                    and order_job_history.edi_sent is null";

            ex2oCustomerList.Add("{0}");
        }else{
            ex2oCustomerList.Add(string.Format("excel_to_order.Customer_Name = '{0}'", customer_name));
        }
    }
    activeCustomerNames = string.Join(", ", customerList);

    
    string ex2OQueryStr = string.Join(" or ", ex2oCustomerList);
    
    excelToOrderQueryForAllSql = @"SELECT excel_to_order.* FROM excel_to_order
                                    join order_job_history
                                    on excel_to_order.Customer_Name = order_job_history.customer_name 
                                    and excel_to_order.Customer_Order_Number = order_job_history.order_number
                                    join (select min(created_time) min_created_time, Customer_Order_Number, Customer_Name from excel_to_order where excel_to_order.created_time >= subdate(curdate(),  " + gapDays + @") group by Customer_Order_Number) uniqEX2O
                                    on uniqEX2O.Customer_Order_Number = excel_to_order.Customer_Order_Number and uniqEX2O.min_created_time =excel_to_order.created_time
                                    where
                                    excel_to_order.created_time >= subdate(curdate(), " + gapDays + @")
                                    and ( " + ex2OQueryStr + @")
                                    and order_job_history.edi_sent is null";
    
    // 存在pending的订单文件
    if(!string.IsNullOrEmpty(pending订单文件)){
        pendingOrderJHSql = string.Format("select * from order_job_history where edi_sent={0} and customer_name in ({1})", -1, activeCustomerNames);
    }
}
//在这里编写您的函数或者类