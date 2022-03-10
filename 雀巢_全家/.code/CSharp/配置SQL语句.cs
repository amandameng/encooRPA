//代码执行入口，请勿修改或删除
public void Run()
{
    #region 配置Clean SQL
    var datetime = DateTime.Now;
    List<string> cleanInsertlist = new List<string>{};
    string cleanInsertStr = "";
    if (CleanTable.Rows.Count != 0)
    {
        for(int i=CleanTable.Rows.Count-1; i>=0; i--){
            DataRow cleanItem = CleanTable.Rows[i];
            // 根据订单查询数据库数据，已经存在的订单不加clean
           // 已经属于excelToOrder的订单就是已处理过的
            bool isCleanOrderExist =  ex2oOrderTable.AsEnumerable().Cast<DataRow>().Any(s => s["PO_Number"].ToString().Trim() == cleanItem["雀巢PO_No"].ToString().Trim());  //cleanOrderTable.AsEnumerable().Cast<DataRow>().Any(s => s["雀巢PO_No"].ToString() == cleanItem["雀巢PO_No"].ToString());
            
            if(isCleanOrderExist){
                CleanTable.Rows.Remove(cleanItem);
                continue;
            }
            string itemValues = string.Format("('{0}','{1}','{2}','{3}','{4}','{5}','{6}','{7}','{8}','{9}')",
                                    cleanItem["渠道"],cleanItem["读单日期"],cleanItem["客户名称"],cleanItem["雀巢PO_No"],cleanItem["客户Po_No"],cleanItem["订单数量"],
                                    cleanItem["交货地"],cleanItem["要求送货日"],cleanItem["备注"],datetime);
            cleanInsertlist.Add(itemValues);
        }
        cleanInsertStr = string.Join(",", cleanInsertlist);

        if(!string.IsNullOrEmpty(cleanInsertStr)){
            cleanSQL = string.Format(@"INSERT INTO clean_order 
                        (
                            渠道,
                            读单日期,
                            客户名称,
                            雀巢PO_No,
                            客户Po_No,
                            订单数量,
                            交货地,
                            要求送货日,
                            备注,
                            created_time
                        )
                        VALUES {0};", cleanInsertStr);           
       }
 
    }
    #endregion
    
    #region 配置Excel To SQL
    string excelToInsertStr = "";
    string orderJobHistoryInsertStr = "";
    List<string> excelToInsertlist = new List<string>{};
    List<string> orderJobHistoryList = new List<string>{};
    if (ExcelToTable.Rows.Count != 0)
    {
        for(int i=ExcelToTable.Rows.Count-1; i>=0; i--){
            DataRow excelItem = ExcelToTable.Rows[i];
            bool isEx2oOrderExist = ex2oOrderTable.AsEnumerable().Cast<DataRow>().Any(s => s["PO_Number"].ToString().Trim() == excelItem["PONumber"].ToString().Trim());
            // 根据订单查询数据库数据，已经存在的订单不加excel to Order
            // Console.WriteLine("--isEx2oOrderExist--{0}", isEx2oOrderExist);
            if(isEx2oOrderExist){
                ExcelToTable.Rows.Remove(excelItem);
                continue;
            }
            DateTime rddDate =  DateTime.ParseExact(excelItem["DeliveryDate"].ToString(), "yyyyMMdd", null);
            string insertValues = string.Format("('{0}','{1}','{2}','{3}','{4}','{5}','{6}','{7}','{8}','{9}','{10}','{11}','{12}','{13}','{14}')",
                                    excelItem["OrderDate"],"全家", excelItem["OrderType"],excelItem["SalesOrg"],excelItem["DistributionChannel"],
                                    excelItem["SoldTo"],excelItem["ShipTo"], excelItem["POOrder"],excelItem["PONumber"],excelItem["MaterialCode"],
                                    excelItem["Qty"],excelItem["UoM"], datetime, excelItem["CustomerOrderNumber"], rddDate.ToString("yyyy-MM-dd"));

            string orderJobHistoryInsertValues = string.Format("('{0}','{1}','{2}','{3}','{4}','{5}','{6}')",
                                    "全家", excelItem["CustomerOrderNumber"], excelItem["OrderDate"], "EX2O", 1, datetime, datetime);

            excelToInsertlist.Add(insertValues);
            Console.WriteLine("----------------" + orderJobHistoryInsertValues);
            if(!orderJobHistoryList.Contains(orderJobHistoryInsertValues)){
                orderJobHistoryList.Add(orderJobHistoryInsertValues);
            }
        }
        orderJobHistoryInsertStr = string.Join(",", orderJobHistoryList);
        excelToInsertStr = string.Join(",", excelToInsertlist);
        if(!string.IsNullOrEmpty(excelToInsertStr)){
            excelSQL = string.Format(@"INSERT INTO excel_to_order 
                        (
                            Customer_Order_Date,
                            Customer_Name,
                            Sales_Order_Type,
                            Sales_Org,
                            Distribution_channel,
                            Sold_to,
                            Ship_to,
                            PO,
                            PO_Number,
                            SAP_Material,
                            Qty,
                            UoM,
                            created_time,
                            Customer_Order_Number,
                            Reqd_Del_Date
                        )
                        VALUES {0}", excelToInsertStr);           
       }

       if(!string.IsNullOrEmpty(orderJobHistoryInsertStr)){
           orderJobHistoryInsertSQL = string.Format(@"INSERT INTO order_job_history 
                        (
                            customer_name,
                            order_number,
                            customer_order_date,
                            report_type,
                            email_sent,
                            email_sent_time,
                            created_time
                        )
                        VALUES {0}", orderJobHistoryInsertStr);    
       }

       //Convert.ToInt32("as");
       //System.Console.WriteLine(excelSQL);
    }
    #endregion
    
    #region 配置Exception SQL
    string exceptionInsertStr = "";
    List<string> exceptionInsertlist = new List<string>{};

    if (ExceptionTable.Rows.Count != 0)
    {
        for(int i=ExceptionTable.Rows.Count-1; i>=0; i--){
            DataRow exceptionItem = ExceptionTable.Rows[i];
            bool isEx2OOrderExist = ex2oOrderTable.AsEnumerable().Cast<DataRow>().Any(s => s["PO_Number"].ToString().Trim() == exceptionItem["雀巢SAP_PO"].ToString().Trim()); // 已经进excel to Order 的订单就是处理过的
            bool isExceptionOrderExist = exceptionOrderTable.AsEnumerable().Cast<DataRow>().Any(s => s["SAP_PO"].ToString() == exceptionItem["雀巢SAP_PO"].ToString()); // 已经进exception 的订单就是处理过的
           // 根据订单查询数据库数据，已经存在的订单不加exception Order
            if(isEx2OOrderExist || isExceptionOrderExist){
                ExceptionTable.Rows.Remove(exceptionItem);
                continue;
            }
            string itemValues = string.Format("('{0}','{1}','{2}','{3}','{4}','{5}','{6}','{7}','{8}','{9}','{10}','{11}','{12}','{13}','{14}','{15}','{16}','{17}')",
                                    exceptionItem["渠道"],exceptionItem["客户名称"],exceptionItem["订单日期"],exceptionItem["客户PO"],exceptionItem["雀巢SAP_PO"],
                                    exceptionItem["客户产品代码"],exceptionItem["雀巢产品代码"],  exceptionItem["产品名称"],exceptionItem["BU"],
                                    exceptionItem["数量"],exceptionItem["客户价格"],exceptionItem["雀巢价格"],exceptionItem["客户要求送货日"],exceptionItem["问题分类"],
                                    exceptionItem["问题详情描述"],exceptionItem["交货地"],exceptionItem["雀巢箱规"],datetime);
            exceptionInsertlist.Add(itemValues);  
        }
        exceptionInsertStr = string.Join(",", exceptionInsertlist);
        if(!string.IsNullOrEmpty(exceptionInsertStr)){
            exceptionSQL = string.Format(@"INSERT INTO exception_order 
                        (
                            渠道,
                            客户名称,
                            订单日期,
                            客户PO,
                            SAP_PO,
                            客户产品码,
                            雀巢产品码,
                            产品名称,
                            Nestle_BU,
                            数量,
                            客户价格,
                            雀巢价格,
                            客户要求送货日,
                            问题分类,
                            问题详细描述,
                            交货地,
                            雀巢箱规,
                            created_time
                        )
                        VALUES {0}",exceptionInsertStr);            
        }

        
        //System.Console.WriteLine(exceptionSQL); 
    }
    #endregion
    
    #region 配置 Family_Mart_Orders SQL
    string orderInsertStr = "";
    List<string> orderInsertlist = new List<string>{};
    if (ordersTable.Rows.Count != 0)
    {
        for(int i=ordersTable.Rows.Count-1; i>=0; i--){
            DataRow orderItem = ordersTable.Rows[i];
            bool orderExist = existingOrdersTable.AsEnumerable().Cast<DataRow>().Any(s => s["OrderNumber"].ToString() == orderItem["OrderNumber"].ToString() && s["OrderDate"].ToString() == orderItem["OrderDate"].ToString());
           // 根据订单查询数据库数据，已经存在的订单不加原始 Order
            if(orderExist){
                ordersTable.Rows.Remove(orderItem);
                continue;
            }
            string itemValues = string.Format("('{0}','{1}','{2}','{3}','{4}','{5}','{6}','{7}','{8}')",
                                    orderItem["OrderNumber"], orderItem["OrderType"],orderItem["OrderDate"],orderItem["ShipDate"],orderItem["ShipAddress"], orderItem["WMDC"], orderItem["ShipFrom"],orderItem["TotalUnit"],datetime);
            orderInsertlist.Add(itemValues);
        }
        orderInsertStr = string.Join(",", orderInsertlist);
        if(!string.IsNullOrEmpty(orderInsertStr)){
            orderSQL = string.Format(@"INSERT INTO family_mart_orders 
            (
                OrderNumber,
                OrderType,
                OrderDate,
                ShipDate,
                ShipAddress,
                WMDC,
                ShipFrom,
                TotalUnit,
                CreatedTime
            )
            VALUES {0}",orderInsertStr); 
        }
 
    }

    #endregion
    
    #region 配置 Family_Mart_Orders_Items SQL
    string orderItemInsertStr = "";
    List<string> orderItemInsertlist = new List<string>{};
    if (orderItemTable.Rows.Count != 0)
    {
        for(int i=orderItemTable.Rows.Count-1; i>=0; i--){
            DataRow orderItem = orderItemTable.Rows[i];
            bool orderItemExist = existingOrdersTable.AsEnumerable().Cast<DataRow>().Any(s => s["OrderNumber"].ToString() == orderItem["OrderNumber"]);
           // 根据订单查询数据库数据，已经存在的订单Item不加原始 Order Item
            if(orderItemExist){
                orderItemTable.Rows.Remove(orderItem);
                continue;
            }
            string itemValues = string.Format("('{0}','{1}','{2}','{3}','{4}','{5}','{6}','{7}','{8}','{9}')",
                                    orderItem["OrderNumber"], orderItem["ProductNumber"],orderItem["ProductName"],orderItem["LineNumber"],orderItem["Batch"],orderItem["Unit"],orderItem["Quantity"],orderItem["TotalQuantity"],orderItem["Barcode"],datetime);
            orderItemInsertlist.Add(itemValues);
        }
        orderItemInsertStr = string.Join(",", orderItemInsertlist);
        if(!string.IsNullOrEmpty(orderItemInsertStr)){
            orderItemSQL = string.Format(@"INSERT INTO family_mart_orders_items 
            (
                OrderNumber,
                ProductNumber,
                ProductName,
                LineNumber,
                Batch,
                Unit,
                Quantity,
                TotalQuantity,
                Barcode,
                CreatedTime
            )
            VALUES {0}",orderItemInsertStr);   
        }
    }

    #endregion
}
//在这里编写您的函数或者类