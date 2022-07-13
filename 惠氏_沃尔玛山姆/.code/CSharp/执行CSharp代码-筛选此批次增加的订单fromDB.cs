//代码执行入口，请勿修改或删除
public void Run()
{
    //在这里编写您的代码
    DataTable newOrdersFromDBDT = fetchCurrentAddedOrdersDT(dbOrdersDT);
    // Console.WriteLine("newOrdersFromDBDT: {0}", newOrdersFromDBDT.Rows.Count);
    newOrdersIntoSheetDT = new DataTable();
    initSheetDT(ref newOrdersIntoSheetDT);

    DataTable firstOrderLinkDT = fetchFirstOrderLinkDT(newOrdersFromDBDT);
    
    foreach(DataRow dr in newOrdersFromDBDT.Rows){
        DataRow sheetRow = newOrdersIntoSheetDT.NewRow();
        string orderNumber = dr["order_number"].ToString();
        string documentLink = dr["document_link"].ToString();
        //Console.WriteLine("orderNumber: {0}, documentLink: {1}", orderNumber, documentLink);
        // 如果当前订单是同一批次抓取出来的，只保留最近的一次
        DataRow[] drs = firstOrderLinkDT.Select(string.Format("order_number='{0}' and document_link='{1}'", orderNumber, documentLink));
        if(drs.Length == 0){
            continue;
        }
        
        DataRow[] oldOrderDrs = walmartOldOrderDT.Select(string.Format("order_number='{0}'", orderNumber)); // 历史 walmart_exported_orders 未查到，则是新单
        string 是否新单 = oldOrderDrs.Length == 0 ? "Y" : "N";
        sheetRow["客户PO"] = dr["order_number"];
        sheetRow["DocID"] = dr["document_link"];
        sheetRow["经销商代码"] = dr["location"];
        sheetRow["订单日期"] = DateTime.Parse(dr["create_date"].ToString()).ToString("yyyy/MM/dd");
        sheetRow["起运日期"] = DateTime.Parse(dr["ship_date"].ToString()).ToString("yyyy/MM/dd");
        sheetRow["取消日期"] = DateTime.Parse(dr["must_arrived_by"].ToString()).ToString("yyyy/MM/dd");
        sheetRow["Type"] = dr["order_type"];
        //Console.WriteLine("--event:{0}---", dr["promotional_event"]);
        sheetRow["Event"] = dr["promotional_event"];
        if(dr["line_number"].ToString() == "001"){
            sheetRow["Total Order Amount"] = Math.Round(Convert.ToDouble(dr["total_order_amount_after_adjustments"]), 2);
        }
        if(dr["promotional_event"].ToString().Contains("JD")){
            sheetRow["备注"] = "JD单";
        }
        sheetRow["行号"] = dr["line_number"];
        sheetRow[curCustomerName + "编码"] = dr["product_code"];
        sheetRow["订单箱数"] = dr["quantity_ordered"];
        sheetRow[curCustomerName + "单价/箱 Cost"] = dr["cost"];
        sheetRow["allowance_percent"] = dr["allowance_percent"];
        sheetRow["allowance_total"] = dr["allowance_total"];
        sheetRow["是否新单"] = 是否新单;
        sheetRow["item_description"] = dr["item_description"];
        sheetRow["POID"] = dr["wyeth_poid"];
        newOrdersIntoSheetDT.Rows.Add(sheetRow);
    }

    // Convert.ToInt32("sdsd");
}
//在这里编写您的函数或者类
public DateTime convertToLocalTime(DateTime sourceCSTdtime)
{
    TimeZoneInfo cstTimeZone = TimeZoneInfo.FindSystemTimeZoneById("Central Standard Time");
    DateTime dtime = TimeZoneInfo.ConvertTime(sourceCSTdtime, cstTimeZone, TimeZoneInfo.Local);
    return dtime;
}

public DataTable fetchFirstOrderLinkDT(DataTable newOrdersFromDBDT){
    DataTable distinctOrderLinkDT = newOrdersFromDBDT.DefaultView.ToTable(true, new string[]{"order_number", "document_link"});
    
    
    DataTable firstOrderLinkDT = distinctOrderLinkDT.Clone();
    foreach(DataRow dr in newOrdersFromDBDT.Rows){
        // Console.WriteLine("----------{0}------", string.Join("@", dr.ItemArray));
        string orderNumber = dr["order_number"].ToString();
        DataRow[] drs = firstOrderLinkDT.Select(string.Format("order_number='{0}'", orderNumber));
        if(drs.Length == 0){ // 如果订单号不存在与表中，则导入一行
            firstOrderLinkDT.ImportRow(dr);
        }
    }
    return firstOrderLinkDT;
}

/// <summary>
/// 获取当前批次的新增订单（from DB）
/// </summary>
/// <param name="dbOrdersDT"></param>
/// <param name="实际增量订单数据表"></param>
/// <returns></returns>
public DataTable fetchCurrentAddedOrdersDT(DataTable dbOrdersDT){
    DataTable newOrdersFromDBDT = dbOrdersDT.Clone();

    // 按照接收时间正序排序
    DataView dv = 实际增量订单数据表.DefaultView;
    dv.Sort = "received_date_time";
    实际增量订单数据表 = dv.ToTable();
    
    
    // 这边对实际增量订单数据表进行倒序处理，所以前面对实际增量订单数据表进行了正序处理
    for(int maxRow = 实际增量订单数据表.Rows.Count-1; maxRow >=0; maxRow--){
        DataRow dr = 实际增量订单数据表.Rows[maxRow];
        string receivedDateStr = dr["received_date_time"].ToString();
        string orderNumber = dr["order_number"].ToString();
        
        // Console.WriteLine("---receivedDateStr: {0}----", receivedDateStr);
        
        DateTime receivedDateTime = Convert.ToDateTime(receivedDateStr);
        DateTime onSiteDateTime = convertToLocalTime(receivedDateTime);
        string timeTillMinutes = onSiteDateTime.ToString("yyyy-MM-dd HH:mm:00");
        DataRow[] matchedDrs = dbOrdersDT.Select(string.Format("order_number = '{0}' and create_date_time='{1}'", orderNumber, timeTillMinutes));
        if(matchedDrs.Length > 0){
            foreach(DataRow matchedDR in  matchedDrs){
                newOrdersFromDBDT.ImportRow(matchedDR);
            }
        }else{
            实际增量订单数据表.Rows.Remove(dr);
        }
    }
    return newOrdersFromDBDT;
}

public void initSheetDT(ref DataTable newOrdersIntoSheetDT){
    string[] columnsArr = new string[]{"大区", "收单日期", "客户PO", "DocID", "经销商代码", "订单日期", "起运日期", "取消日期", "Type", "Event", "Total Order Amount", "备注", "其他备注", "行号", curCustomerName + "编码", "订单箱数", curCustomerName + "单价/箱 Cost", "Sold To", "整箱", "紧缺", "POID", "经销商简称", "惠氏产品描述", "惠氏编码", "惠氏总价", "惠氏折扣价", "惠氏单价/箱", "单价价差/箱", "Ship To", "allowance_percent","allowance_total", "是否新单", "item_description"};
    string[] stringColArr = new string[]{"客户PO", "行号", curCustomerName + "编码", "经销商代码", "allowance_percent", "POID"};
    foreach(string col in columnsArr){
        DataColumn dcol;
        if(stringColArr.Contains(col)){
            dcol  = new DataColumn(col, typeof(string));
        }else{
            dcol  = new DataColumn(col, typeof(object));
        }

        if(col == "大区"){
            dcol.DefaultValue = curCustomerName;
        }
        
        if(col == "收单日期"){
            dcol.DefaultValue = DateTime.Now.ToString("yyyy/MM/dd");
        }

        newOrdersIntoSheetDT.Columns.Add(dcol);
    }
}