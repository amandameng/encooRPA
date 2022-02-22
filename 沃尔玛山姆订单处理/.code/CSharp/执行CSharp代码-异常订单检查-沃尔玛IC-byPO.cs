//代码执行入口，请勿修改或删除
public void Run()
{
    exceptionByPODT = byPO模板数据表.Clone();
    
    AddMoreColumns(); // Clean and Exceltion BY ORder and Item 全包括

    // 输出：exceptionByPODT
    // 是否检查散威化
    /*
    新增暂时检查点（不定期取消此检查点）：
    散威化订单录单后反馈Exception
    */
    checkBulkWaferException = etoConfigDT.Rows[0]["checkBulkWaferException"].ToString();
    
    IEnumerable<IGrouping<string, DataRow>> groupedOrders = 增量订单关联数据表.Rows.Cast<DataRow>().GroupBy<DataRow, string>(dr => dr["order_number"].ToString());//C# 对DataTable中的某列分组，groupedDRs中的Key是分组后的值
    
    int 此批订单序号 = 订单序号;
    foreach (var itemGroup in groupedOrders)
    {
        string order_number = itemGroup.Key;
        DataRow[] orderRows = itemGroup.ToArray();
        
        IEnumerable<IGrouping<string, DataRow>> groupedOrderDocLinks = orderRows.Cast<DataRow>().GroupBy<DataRow, string>(dr => dr["document_link"].ToString());//C# 对DataTable中的某列分组，groupedDRs中的Key是分组后的值
        List<DataRow[]> groupedOrderDocLinksList = new List<DataRow[]>{};
        foreach(var orderDocLinkItemGroup in groupedOrderDocLinks){
            DataRow[] orderDocLinkItemDrs = orderDocLinkItemGroup.ToArray();
            groupedOrderDocLinksList.Add(orderDocLinkItemDrs);
        }

        List<string> 问题订单List = new List<string>{};
       
        // current Valid Order and Rows
        DataRow[] curOrderDocLinkRows = groupedOrderDocLinksList[0];
        DataRow curOrderRow = curOrderDocLinkRows[0]; // current Order one Row，包含order全部信息
        
        DataRow cleanExceptionDRow = exceptionByPODT.NewRow();  // by PO or Item 的 exception
        DataRow 分仓行 = 分仓明细数据表.NewRow();
        // Order 不重复的时候，判断order异常信息
        
        List<String> allitemInstructionsList = curOrderDocLinkRows.Cast<DataRow>().Select<DataRow, string>(dr => dr["item_instructions"].ToString()).ToList();
        List<String> allitemproductCodesList = curOrderDocLinkRows.Cast<DataRow>().Select<DataRow, string>(dr => dr["customer_product_code"].ToString()).ToList();

        // 判断ship To 门店订单
        string location = curOrderRow["location"].ToString();
        // WM locations 不包含当前订单的location 则是门店订单异常
        bool shipTo门店 = !WMLocationsList.Contains(location);        
        
        // By Order的异常判断
        handleExceptionRow(curOrderRow, ref cleanExceptionDRow, ref 问题订单List, allitemInstructionsList, allitemproductCodesList, shipTo门店, 此批订单序号);
        
        // 门店订单不判断item异常
        if(shipTo门店){
            cleanExceptionDRow["Item Type"] = "Order";
            cleanExceptionDRow["order category"] = (问题订单List.Count > 0) ? "exception" : "clean";
            cleanExceptionDRow["Exception category"] = string.Join("; ", 问题订单List);
            cleanExceptionDRow["Exception reason"] = cleanExceptionDRow["Exception category"];
    
            exceptionByPODT.Rows.Add(cleanExceptionDRow);
            continue; // 继续下一条
        }
        // 设置分仓明细表每行的信息
        setRowValueForDC(ref 分仓行, cleanExceptionDRow);

        // EX2O是否包含当前订单，如果包含，则无需再次录单
        setNotIntoEx2O(order_number);
        
        /*
          有重复订单， MABD， 产品数量修改，等其他任意信息修改都要抓取
        */
        int ordersLength = groupedOrderDocLinksList.Count;
        // Console.WriteLine(ordersLength);
        DataRow[] prevOrderDocLinkRows = new DataRow[]{};
        // 有重复订单， by PO 【检查MABD，订单修改产品数量，特殊产品，其他问题】，by item【价差检查，特殊产品】
        if(ordersLength > 1){
            prevOrderDocLinkRows = groupedOrderDocLinksList[1];
            DataRow prevOrderRow = prevOrderDocLinkRows[0]; // previous Order one Row，包含order全部信息
            DataTable tmpOrderDT = 增量订单关联数据表.Clone();
            
            foreach(DataRow[] drows in groupedOrderDocLinksList.Skip(1).Take(ordersLength-1)){
                foreach(DataRow dr in drows){
                    DataRow tmpRow = tmpOrderDT.NewRow();
                    tmpRow.ItemArray = dr.ItemArray;
                    tmpOrderDT.Rows.Add(tmpRow);
                } 
            }
            tmpOrderDT = tmpOrderDT.DefaultView.ToTable(true, new string[]{"order_number", "order_type", "ship_date", "must_arrived_by", "promotional_event", "location", "allowance_or_charge", "allowance_description", "allowance_percent", "allowance_total", "total_order_amount_after_adjustments", "total_line_items", "total_units_ordered"});
            
            repeatedOrderException(curOrderRow, prevOrderRow, ref cleanExceptionDRow, ref 问题订单List, ref 分仓行, tmpOrderDT.Rows); 
        }
        
        分仓明细数据表.Rows.Add(分仓行);
   
        /*
          特殊产品需要检查订单
          价差订单
        */

        // By Item
        List<string> refItemExceptionList = new List<string>{};

        foreach(DataRow dr in curOrderDocLinkRows){
            string productCode = dr["customer_product_code"].ToString();
            DataRow byPOItemRow =  exceptionByPODT.NewRow();
            byPOItemRow.ItemArray = cleanExceptionDRow.ItemArray;
            byPOItemRow["Customer order Item"] = dr["line_number"];
            specialProductCheck(dr, ref refItemExceptionList, ref byPOItemRow, prevOrderDocLinkRows);
            if(!String.IsNullOrEmpty(byPOItemRow["Exception reason"].ToString())){ // 异常信息不为空
                exceptionByPODT.Rows.Add(byPOItemRow);  // by Item 的异常订单 
            }
        }

        cleanExceptionDRow["Item Type"] = "Order";
        cleanExceptionDRow["order category"] = (问题订单List.Count > 0 || refItemExceptionList.Count > 0) ? "exception" : "clean";
        string onlyItemException = (问题订单List.Count == 0 && refItemExceptionList.Count > 0) ? "1" : "0";
        cleanExceptionDRow["onlyItemException"] = onlyItemException;
        cleanExceptionDRow["Exception category"] = string.Join("; ", 问题订单List.Union(refItemExceptionList));
        cleanExceptionDRow["Exception reason"] = cleanExceptionDRow["Exception category"];

        exceptionByPODT.Rows.Add(cleanExceptionDRow);
        此批订单序号 += 1;
    }
    
    buildByPODT();
    buildByPOAndItemDT();
}

public void setNotIntoEx2O(string order_number){
    if(existingEX2ODT!=null && orderJobHistoryDT!=null){
        // bool inEX2O = existingEX2ODT.AsEnumerable().Cast<DataRow>().Any(dRow => dRow["Customer_Order_Number"].ToString() == order_number);
        bool inEX2O = existingEX2ODT.AsEnumerable().Cast<DataRow>().Any(dRow => dRow["PO_Number"].ToString().Contains(order_number));

        
        bool inOrderJobHistory = orderJobHistoryDT.AsEnumerable().Cast<DataRow>().Any(dRow => dRow["order_number"].ToString() == order_number);

        if(inEX2O && inOrderJobHistory){
            不录单订单列表.Add(order_number);
        }
    }
}

public void addExceptionRow(DataRow cleanExceptionDRow){
    string exceptionMessage = "无法mapping雀巢主数据";
    DataRow byPOItemRow =  exceptionByPODT.NewRow();
    byPOItemRow.ItemArray = cleanExceptionDRow.ItemArray;
    byPOItemRow["Exception category"] = exceptionMessage;
    byPOItemRow["Exception reason"] = exceptionMessage;
    exceptionByPODT.Rows.Add(byPOItemRow);
}

public void buildByPODT(){
    DataTable poDT = exceptionByPODT.Clone();
    DataRow[] poDRows = exceptionByPODT.Select("`Item Type`='Order'");
    foreach(DataRow dr in poDRows){
        poDT.ImportRow(dr);
    }
    byPO模板数据表 = poDT.DefaultView.ToTable(true, byPO模板数据表.Columns.Cast<DataColumn>().Select<DataColumn, string>(dc => dc.ColumnName).ToArray());
}

public void buildByPOAndItemDT(){
    DataTable poItemDT = exceptionByPODT.Clone();
    DataRow[] poItemDRows = exceptionByPODT.Select("`order category`= 'exception' and `onlyItemException` <> '1'");
    foreach(DataRow dr in poItemDRows){
        poItemDT.ImportRow(dr);
    }
    byPOorItem模板数据表 = poItemDT.DefaultView.ToTable(true, byPOorItem模板数据表.Columns.Cast<DataColumn>().Select<DataColumn, string>(dc => dc.ColumnName).ToArray());
}

// 特殊产品by Item 检查
public void specialProductCheck(DataRow dr, ref List<string> refItemExceptionList, ref DataRow byPOItemRow, DataRow[] prevOrderDocLinkRows){
    List<string> itemExceptionList = new List<String>{};
    
    string remark = dr["Remark"].ToString();
    string remarkOption = dr["Remark_Option"].ToString(); // Remark_Option 1. 需检查以下客户店内码的原单规格与雀巢的产品规格是否一致 2. 检查价差  3.备注客户店内码
    //价差订单
    //特殊产品需检查订单   停止出货产品/箱规检查

    byPOItemRow["沃尔玛产品编码"] = dr["customer_product_code"].ToString();
    byPOItemRow["雀巢产品编码"] = dr["Nestle_Material_No"];
    byPOItemRow["Material Description"] =  dr["Material_Description"];
    byPOItemRow["Nestle BU"] = dr["Nestle_BU"];
    byPOItemRow["雀巢数量"] = dr["quantity_ordered"];

    /*
    // 有remark说明已经map到主数据
    if(remark.Contains("特殊产品")){
        if(remarkOption.Contains("检查价差")){
           int cost = Convert.ToInt32(Convert.ToDecimal(dr["cost"]));
           int nestleNPS = Convert.ToInt32(dr["Nestle_NPS"]);
           if(cost != nestleNPS){
               byPOItemRow["沃尔玛价格"] = cost;
               byPOItemRow["雀巢价格"] = nestleNPS;
               byPOItemRow["原单箱规"] = dr["pack"];
               byPOItemRow["Item Type"] = "Item";
               itemExceptionList.Add("价差订单");
           }
        }
        if(remarkOption.Contains("备注客户店内码")){
             byPOItemRow["Item Type"] = "Item";
             itemExceptionList.Add("特殊产品需检查订单,备注客户店内码");
        }
        if(remarkOption.Contains("客户店内码的原单规格")){
            byPOItemRow["原单箱规"] = dr["pack"];
            byPOItemRow["Item Type"] = "Item";
            itemExceptionList.Add("特殊产品需检查订单,客户店内码的原单规格");
        }
    }else{
        if(String.IsNullOrEmpty(byPOItemRow["雀巢产品编码"].ToString())){
            byPOItemRow["Item Type"] = "Item";
            itemExceptionList.Add("无法匹配雀巢主数据");
        }
    }
    */
    
    
   //  Console.WriteLine("refItemExceptionList: {0}", string.Join("|", refItemExceptionList));
    // 如果整个订单数量修改的话， 需要记录单个item变化的部分
    string orderExceptionListStr = string.Join("|", refItemExceptionList);
    if(orderExceptionListStr.Contains("订单修改产品数量") || orderExceptionListStr.Contains("Total Order Amount")){
        string customerProductCode = dr["customer_product_code"].ToString();
        Console.WriteLine("customerProductCode: {0}", customerProductCode);
        
        DataRow prevItemRow = null;
        foreach(DataRow prevDr in prevOrderDocLinkRows){
            Console.WriteLine("prevDr customerProductCode: {0}", prevDr["customer_product_code"].ToString());
            if(customerProductCode == prevDr["customer_product_code"].ToString()){ // 找到对应de 前一个order的item
                prevItemRow = prevDr;
                break;
            }
        }
    
        // 之前item不等于当前item对应的数量
        if(prevItemRow!=null && prevItemRow["quantity_ordered"].ToString() != dr["quantity_ordered"].ToString()){
            byPOItemRow["原订单数量"] = prevItemRow["quantity_ordered"];
            Console.WriteLine("原订单数量: {0}, 修改后订单数量: {1}", prevItemRow["quantity_ordered"], dr["quantity_ordered"]);
            
            byPOItemRow["修改后订单数量"] = dr["quantity_ordered"];
            itemExceptionList.Add("订单修改产品数量");
        }
        
        // 之前item扣点不等于当前item扣点
         if(prevItemRow!=null && prevItemRow["oli_allowance_percent"].ToString() != dr["oli_allowance_percent"].ToString()){
            
            itemExceptionList.Add($"订单修改扣点, 原折扣为{prevItemRow["oli_allowance_percent"]}，现折扣为{dr["oli_allowance_percent"]}");
        }
    }
    // Console.WriteLine("Item Type: {0}", byPOItemRow["Item Type"].ToString());
    // Convert.ToInt32("a.b");
    if(itemExceptionList.Count > 0){
        byPOItemRow["Exception reason"] = string.Join("; ", itemExceptionList);
        byPOItemRow["order category"] = "exception";
    }
    foreach(string itemStr in itemExceptionList){
        
    }
    refItemExceptionList = refItemExceptionList.Union(itemExceptionList).ToList();
}

public void repeatedOrderException(DataRow curOrderRow, DataRow previousOrderRow, ref DataRow cleanExceptionDRow, ref List<string> 问题订单List, ref DataRow 分仓行, DataRowCollection prevOrderRows ){
    DataColumnCollection columns = previousOrderRow.Table.Columns;
    DataColumnCollection ValidColumns = prevOrderRows[0].Table.Columns;
    List<string> 重复订单信息 = new List<string>{};
    List<string> 分仓行信息 = new List<string>{};

    object[] previousItemArr = previousOrderRow.ItemArray;
    object[] curItemArr = curOrderRow.ItemArray;
    DataTable itemChangeDT = initItemChangeDT();
    for(int i=0; i <= previousItemArr.Length-1; i++){
        string colName = columns[i].ColumnName;
        if(!ValidColumns.Contains(colName)){
            continue;
        }
        string prevItemStr = Convert.ToString(previousItemArr[i]);
        string curItemStr = Convert.ToString(curItemArr[i]);
        if(prevItemStr != curItemStr){
            DataRow itemChangeRow = itemChangeDT.NewRow();
            itemChangeRow["order_number"] = curOrderRow["order_number"];
            itemChangeRow["changedColumnName"] = colName;
            itemChangeRow["change_before"] = prevItemStr;
            itemChangeRow["change_after"] = curItemStr;
            itemChangeDT.Rows.Add(itemChangeRow);
            
            if(colName == "must_arrived_by"){
                重复订单信息.Add("订单修改MABD");
                List<String> resultMabds = prevOrderRows.Cast<DataRow>().Select<DataRow, string>(dr => Convert.ToDateTime(dr["must_arrived_by"]).ToString("yyyy/MM/dd")).ToList();
                cleanExceptionDRow["原始MABD"] = resultMabds[0];
                分仓行信息.Add(String.Format("原MABD为{0}", String.Join(",", resultMabds)));
            }else if(colName == "total_units_ordered"){
                重复订单信息.Add("订单修改产品数量");
                List<String> resultMabds = prevOrderRows.Cast<DataRow>().Select<DataRow, string>(dr => dr["total_units_ordered"].ToString()).ToList();
                cleanExceptionDRow["原订单数量"] = resultMabds[0];
                分仓行信息.Add(String.Format("原订单数量为{0}", String.Join(",", resultMabds)));
            }else{
                Dictionary<string, string> webItemMapDic = new Dictionary<string, string>{{"promotional_event", "订单类型"}, {"allowance_percent", "仓租"}, {"total_line_items", "产品行数"}, {"total_order_amount_after_adjustments", "Total Order Amount (After Adjustments)"}};
                
                if(webItemMapDic.Keys.Contains(colName)){
                    string colNameDes = webItemMapDic[colName];
                    重复订单信息.Add(String.Format("订单修改{0}, 从({1})修改为({2})", colNameDes, prevItemStr, curItemStr));
                    List<String> resultMABDs = prevOrderRows.Cast<DataRow>().Select<DataRow, string>(dr => dr[colName].ToString()).ToList();
                    分仓行信息.Add(String.Format("原{0}为{1}", colNameDes, String.Join(",", resultMABDs)));
                }

            }
        }
    }
    Console.WriteLine("重复订单信息: {0}", String.Join("；", 重复订单信息));
    if(重复订单信息.Count > 0){
       问题订单List.Add("重复订单：" + String.Join("；", 重复订单信息));
    }
    if(分仓行信息.Count > 0){
        分仓行["订单修改信息"] = String.Join("；", 分仓行信息);
    }
}
// 修改前后记录表
public DataTable initItemChangeDT(){
    DataTable itemChangeDT = new DataTable();
    itemChangeDT.Columns.Add("order_number", typeof(string));
    itemChangeDT.Columns.Add("changedColumnName", typeof(string));
    //itemChangeDT.Columns.Add("document_link_before", typeof(string));
    //itemChangeDT.Columns.Add("document_link_after", typeof(string));
    itemChangeDT.Columns.Add("change_before", typeof(string));
    itemChangeDT.Columns.Add("change_after", typeof(string));
    return itemChangeDT;
}

public void AddMoreColumns(){
    /*
      Customer order Item => 客户原始订单行数
      Exception level => Order 或者 Item
    */
    List<string> moreColumns = new List<string>{"Item Type", "Customer order Item", "原始MABD", "新MABD", "沃尔玛产品编码", "雀巢产品编码", "Material Description", "原单箱规", "原订单数量", "修改后订单数量", "雀巢数量", "雀巢价格", "沃尔玛价格", "Exception reason", "onlyItemException"};
    foreach(string colName in moreColumns){
        exceptionByPODT.Columns.Add(colName, typeof(string));
    }
    exceptionByPODT.Columns["onlyItemException"].DefaultValue = '0';
}

public void handleExceptionRow(DataRow dr, ref DataRow cleanExceptionDRow, ref List<string> 问题订单List, List<string> allitemInstructionsList, List<string> allitemproductCodesList, bool shipTo门店, int 此批订单序号){
    string 读单当天日期 = DateTime.Now.ToString("yyyy/MM/dd");  // 1
    string po_number = getPONumber(dr, 此批订单序号);  // 2
    string wmdc = dr["WMDC"].ToString(); // 3
    string promotionalEvent = dr["promotional_event"].ToString(); // 4
    string orderType = dr["order_type"].ToString();
    bool isReq = promotionalEvent.Contains("REQ");
    bool isKMDC = (wmdc == "KMDC");
    string totalLineItems = dr["total_line_items"].ToString();
    string nestleBU = string.Empty;
    
    string 是否为稳定库存 = (orderType == "0020") ? "稳定" : ""; // 7
    string orderQty = dr["total_units_ordered"].ToString(); // 8
    string 起送日 = Convert.ToDateTime(dr["ship_date"]).ToString("yyyy/MM/dd"); // 9
    string MABD = Convert.ToDateTime(dr["must_arrived_by"]).ToString("yyyy/MM/dd"); // 10
    DateTime MABDDate = Convert.ToDateTime(dr["must_arrived_by"]);
    string 是否为手工单 = string.Empty;  // 6
    
    if(shipTo门店)
    {
        问题订单List.Add("Ship to为门店订单");
    }
    else{
        if(promotionalEvent == "CANCEL PO"){
            问题订单List.Add("Cancel PO");
        }
    
        /*
        5. 是否为手工单：通过订单沃尔玛产品编码来判断：
        当沃尔玛产品编码 = 21779181  普通散威，在F列填入：散威化
        当沃尔玛产品编码含21402419  吉林散威，在F列填入：散威化-吉林X件
        */
        string prodCode = allitemproductCodesList[0];
        /*
        foreach(DataRow bulkWalferDR in bulkWalferConfigDT.Rows){
            string 散威化产品码 = bulkWalferDR["customer_product_code"].ToString();
            string bulk_walfer_type = bulkWalferDR["bulk_walfer_type"].ToString();
            if(prodCode == 散威化产品码){
                if(bulk_walfer_type == "散威化"){
                    是否为手工单 = "散威化";
                }else{
                    是否为手工单 = bulk_walfer_type;
                }
                break;
            }
        }
       */
        
        /* 
        新增暂时检查点（不定期取消此检查点）：
        散威化订单录单后反馈Exception
        Exception　reason：散威化请确认是否出单
        */
        /*
        if(是否为手工单.Contains("散威化") && checkBulkWaferException == "1"){
            问题订单List.Add("散威化请确认是否出单");
        }
        
        // Console.WriteLine("Request_Delivery_Date {0}", dr["Request_Delivery_Date"].ToString());
        
        string requestDeliveryDate = dr["Request_Delivery_Date"].ToString(); // ship to表里面获取的。周一/周四, 【7400 KMDC 下周一/下周三】特殊处理
        */
        /*
        2.整张订单的折扣1.3%
        Handling
        
        整张订单的折扣不等于1.3%，就反馈Exception
        */
        /*
        string allowancePercent = dr["allowance_percent"].ToString();
        bool 整单折扣 = allowancePercent.Split(new string[]{","}, StringSplitOptions.RemoveEmptyEntries).Contains("1.3%");
        if(!整单折扣){
            问题订单List.Add("仓租不为1.3%订单");
        }
        */
        /*
        2.问题订单的判断及登记
        问题订单，SAP会自动屏蔽，不会进入Idoc和SAP，需反馈给Facing加折扣重新推单
        判断条件：
        1.订单产品明细部分没有4.0% EDLC 0.7% Not RTV的折扣信息，只有13%税率和1.3%的仓佣
        2.沃尔玛订单Promotional Event字段为POS REPLEN
        
        满足判断条件其中一个即为问题订单，一般会同时出现，将订单号，没有折扣
        */
        // string allitemInstructions = dr["all_item_instructions"].ToString();
        /*
        bool 无折扣 = allitemInstructionsList.Contains("4.0% EDLC  0.7% Not RTV");
        if(promotionalEvent == "POS REPLEN"){
            问题订单List.Add("问题订单,POS REPLEN");
        }
        if(!无折扣){
            问题订单List.Add("问题订单,无折扣");
        }
        */
        /*
          客户指定送货日不在行程日
          1. Promotional Event为REQ时 
          2. ship to地址为7400KMDC时
          1和2满足任意一个，都需要检查，1和2都不含的都不用检查行程
        */
        /*
        if(isReq || isKMDC){
            string 周几 = CaculateWeekDay(MABDDate);
            List<string> dayStringList = shipLocationDic(requestDeliveryDate);
            
            if(!dayStringList.Contains(周几)){
                问题订单List.Add("客户指定送货日不在行程日");
            }
        }
        
        */
        /*  
        跨月订单， MABD为下个月
        除KM DC(昆明DC)之外的七个大仓当月订单的MABD显示在下个月的订单，判断为问题订单，备注原因跨月订单反馈
        */
        DateTime orderCreateDateTime = Convert.ToDateTime(dr["create_date_time"]);
        bool mabdInNextMonth = (MABDDate.ToString("yyyy-MM") == orderCreateDateTime.AddMonths(1).ToString("yyyy-MM"));
        
        if(!isKMDC && mabdInNextMonth){
            问题订单List.Add("跨月订单");
        }
        
        /*
        ！！！【以读单时间为准，不以订单时间为准】
        二，过期订单（判断订单是否在有效期内可以预约上物流）
        1.当KMDC满足MABD=送货行程时，还要判断是否能预约成功。
        周二17:30前读到的订单，最早下周一可满足；即订单MABD≥下周一为Clean，否则为Exception. 是否要改成23：59：59？
        周五17:30前读到的订单，最早下周三可满足；即订单MABD≥下周三为Clean，否则为Exception
        
        2.除开KMDC其他7个DC，中午12:00前读到的订单，最早可以满足Day+1的送货日
        即订单MABD≥读单当天+1为Clean，否则为Exception，属于无法满足的送货日。
        Exception reason：客户指定送货日无法满足
        */
        // 
        /*
        DateTime timeNow =  DateTime.Now; //orderCreateDateTime;
        if(wmdc == "KMDC"){
            // 周五17:30之后 到 下周二17：30之间的订单，送货日 最早需要在下周一
            // 周二17：30到周五17：30读到的单，送货日 最早需要下周三
            int todayDayOfWeek = (int)timeNow.DayOfWeek;
            
            //周二17：30到周五17：30读到的单，送货日 最早需要下周三
            // 先判断dayofWeek是否是周二到周五
        
            if(todayDayOfWeek >= 2 && todayDayOfWeek <= 5){ //  周二当天17:30前读到的订单已经包含在这里面
                bool invalidRDD = beforeFriday17Judge(timeNow, MABDDate);
                问题订单List.Add("客户指定送货日无法满足");
            }else if(todayDayOfWeek == 1 ){ // 周二17:30前读到的订单，最早下周一可满足；即订单MABD≥下周一为Clean，否则为Exception
                bool invalidRDD = beforeTuesday17Judge(timeNow, MABDDate);
                if(invalidRDD){
                    问题订单List.Add("客户指定送货日无法满足");
                }
            }
        }
        else{ // 中午12:00前读到的订单，最早可以满足Day+1的送货日, 即订单MABD≥读单当天+1为Clean，否则为Exception，属于无法满足的送货日。
            DateTime noonTime = DateTime.Parse(timeNow.ToString("yyyy-MM-dd 12:00:00"));
            DateTime timeNowDate = DateTime.Parse(timeNow.ToString("yyyy-MM-dd"));
            bool beforeNoonNotValid = DateTime.Compare(timeNow, noonTime) <= 0 && MABDDate < timeNowDate.AddDays(1);  // 12点前 and MABD < T+1
            bool afterNoonNotValid = DateTime.Compare(timeNow, noonTime) == 1 && MABDDate < timeNowDate.AddDays(2);  // 12点后 and MABD < T+2
        
            if(beforeNoonNotValid || afterNoonNotValid){
                问题订单List.Add("客户指定送货日无法满足");
            }
        }
        */
    }

    cleanExceptionDRow["Order Date"] = 读单当天日期;
    cleanExceptionDRow["PO No."] = dr["order_number"].ToString();
    cleanExceptionDRow["SAP PO"] = po_number;
    cleanExceptionDRow["仓号"] = dr["Nestle_Plant_No"].ToString();
    cleanExceptionDRow["WMDC"] = wmdc;
    cleanExceptionDRow["Order type（Promotional Event）"] = promotionalEvent;
    cleanExceptionDRow["Nestle BU"] = nestleBU;
    // 是否为手工单	是否为稳定库存	Order qty	起送日	MABD	[order category]（现在还判断不了，因为现在是by itemde 计算方式）	[Exception category] 等item确定了才能最终确定
    cleanExceptionDRow["是否为手工单"] = 是否为手工单;
    cleanExceptionDRow["是否为稳定库存"] = 是否为稳定库存;
    cleanExceptionDRow["Order qty"] = orderQty;
    cleanExceptionDRow["起送日"] = 起送日;
    cleanExceptionDRow["MABD"] = MABD;
    cleanExceptionDRow["新MABD"] = MABD;
}
//在这里编写您的函数或者类

// 设置分仓行值
public void setRowValueForDC(ref DataRow 分仓行, DataRow cleanExceptionDRow){
    // 读单当天日期	PO.	WMDC	订单类型（Promotional Event）	是否为手工单	是否为稳定库存	数量	起送日	MABD	订单修改信息

    分仓行["读单当天日期"] = cleanExceptionDRow["Order Date"];
    分仓行["PO."] =  cleanExceptionDRow["PO No."];
    分仓行["WMDC"] =  cleanExceptionDRow["WMDC"];
    分仓行["订单类型（Promotional Event）"] = cleanExceptionDRow["Order type（Promotional Event）"];
    分仓行["是否为手工单"] = cleanExceptionDRow["是否为手工单"];
    分仓行["是否为稳定库存"] = cleanExceptionDRow["是否为稳定库存"];
    分仓行["数量"] = cleanExceptionDRow["Order qty"];
    分仓行["起送日"] = Convert.ToDateTime(cleanExceptionDRow["起送日"]).ToString("yyyy/MM/dd");;
    分仓行["MABD"] = Convert.ToDateTime(cleanExceptionDRow["MABD"]).ToString("yyyy/MM/dd");;
}


public string getPONumber(DataRow dr, int 订单序号){
    string orderNumber = dr["order_number"].ToString();
    string mmdd = DateTime.Now.ToString("MMdd");
    string WMDC = dr["WMDC"].ToString();
    // 4001014040-DGPDC-1021-1
    return $"{orderNumber}-{WMDC}-{mmdd}-{订单序号}";
}