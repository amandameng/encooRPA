//代码执行入口，请勿修改或删除
public void Run()
{
    exceptionByPODT = byPO模板数据表.Clone();
    
    AddMoreColumns(); // Clean and Exceltion BY ORder and Item 全包括

    // 输出：exceptionByPODT
    
    checkBulkWaferException = etoConfigDT.Rows[0]["checkBulkWaferException"].ToString();
    
    IEnumerable<IGrouping<string, DataRow>> groupedOrders = 增量订单关联数据表.Rows.Cast<DataRow>().GroupBy<DataRow, string>(dr => dr["order_number"].ToString());//C# 对DataTable中的某列分组，groupedDRs中的Key是分组后的值
    
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
        
        // 整托和起送量的判断在订单层面和item层面都要做检查
        
        // By Order的异常判断， 检查整托和起送量
        handleExceptionRow(curOrderRow, ref cleanExceptionDRow, ref 问题订单List, curOrderDocLinkRows);
        
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
            foreach(DataRow dr in tmpOrderDT.Rows){
                Console.WriteLine(string.Join("--", dr.ItemArray));
            }
            
            repeatedOrderException(curOrderRow, prevOrderRow, ref cleanExceptionDRow, ref 问题订单List, tmpOrderDT.Rows); 
        }

        // By Item 检查exception, 检查整托和起送量
        foreach(DataRow dr in curOrderDocLinkRows){
            string productCode = dr["customer_product_code"].ToString();
            DataRow byPOItemRow =  exceptionByPODT.NewRow();
            byPOItemRow.ItemArray = cleanExceptionDRow.ItemArray;
            specialProductCheck(dr, ref 问题订单List, ref byPOItemRow, prevOrderDocLinkRows);
            if(!String.IsNullOrEmpty(byPOItemRow["Exception reason"].ToString())){ // 异常信息不为空
                exceptionByPODT.Rows.Add(byPOItemRow);  // by Item 的异常订单 
            }
        }

        cleanExceptionDRow["Item Type"] = "Order";
        cleanExceptionDRow["order category"] = (问题订单List.Count > 0) ? "exception" : "clean";
        cleanExceptionDRow["Exception category"] = string.Join("; ", 问题订单List);
        cleanExceptionDRow["Exception reason"] = cleanExceptionDRow["Exception category"];

        exceptionByPODT.Rows.Add(cleanExceptionDRow);
    }
    buildByPODT();
    buildByPOAndItemDT();
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
    DataRow[] poItemDRows = exceptionByPODT.Select("`order category`= 'exception'");
    foreach(DataRow dr in poItemDRows){
        poItemDT.ImportRow(dr);
    }
    /*
    DataTable poDT = exceptionByPODT.Clone();
    DataRow[] poDRows = exceptionByPODT.Select("`Item Type`='Order'");
    foreach(DataRow dr in poDRows){
        if(poItemDT.Select(string.Format("`PO No.` = '{0}'", dr["PO No."].ToString())).Length == 0){
            poItemDT.ImportRow(dr);
        }
    }
   */
    byPOorItem模板数据表 = poItemDT.DefaultView.ToTable(true, byPOorItem模板数据表.Columns.Cast<DataColumn>().Select<DataColumn, string>(dc => dc.ColumnName).ToArray());
}

// 产品行检查折扣
public void specialProductCheck(DataRow dr, ref List<string> 问题订单List, ref DataRow byPOItemRow, DataRow[] prevOrderDocLinkRows){
    List<string> itemExceptionList = new List<String>{};
    
    string remark = dr["Remark"].ToString();
    string remarkOption = dr["Remark_Option"].ToString(); // Remark_Option 1. 需检查以下客户店内码的原单规格与雀巢的产品规格是否一致 2. 检查价差  3.备注客户店内码
    byPOItemRow["沃尔玛产品编码"] = dr["customer_product_code"].ToString();
    byPOItemRow["雀巢产品编码"] = dr["Nestle_Material_No"];
    byPOItemRow["Material Description"] =  dr["Material_Description"];
    byPOItemRow["Nestle BU"] = dr["Nestle_BU"];
    byPOItemRow["雀巢数量"] = dr["quantity_ordered"];

    if(String.IsNullOrEmpty(byPOItemRow["雀巢产品编码"].ToString())){
        byPOItemRow["Item Type"] = "Item";
        itemExceptionList.Add("无法匹配雀巢主数据");
        return;
    }

    // 有remark说明已经map到主数据
    if(remark.Contains("不填充ex2o，只反馈问题订单")){
        byPOItemRow["Item Type"] = "Item";
        itemExceptionList.Add("未录单，水仓+干货产品");
    }
    /* 
      产品行折扣
      =Promotional Allowance/Extended Cost
      产品行的折扣率不等于各产品扣点[产品主数据有维护]（有一个产品不符合就是exception） ，反馈Exception，【不录单】
    */
    string 产品扣点 = dr["产品扣点"].ToString();
    if(string.IsNullOrEmpty(产品扣点)){
        decimal productDiscountRate = fetchRateInDecimal(产品扣点);
        decimal 产品行折扣 = Math.Round(Convert.ToDecimal(dr["oli_allowance_total"])/Convert.ToDecimal(dr["oli_extended_cost"]), 2);
        if(productDiscountRate != 产品行折扣){
            byPOItemRow["Item Type"] = "Item";
            itemExceptionList.Add(string.Format("产品行折扣率不等于扣点，产品行折扣: {0}, 雀巢产品扣点: {1}", 产品行折扣, productDiscountRate));
        }
    }
    
    /*
      山姆京东类型的订单如下单产品为氨糖奶粉，不录单作为exception order发给CSA取消
      Exception reason：不录单，山姆JD+氨糖奶粉
    */
    if(remark.Contains("特殊产品") && remarkOption.Contains("订单类型Promotional Event为JD")){
        byPOItemRow["Item Type"] = "Item";
        itemExceptionList.Add("不录单，山姆JD+氨糖奶粉");
    }
    
    
    /* 整托和起送量判断 */
    DataRow[] samWaterDeliveryDRs = samWaterDeliverySettingDT.Select(String.Format("WMDC='{0}' and Customer_Product_Code ='{1}'", dr["WMDC"].ToString(), dr["customer_product_code"].ToString()));
    if(samWaterDeliveryDRs.Length > 0){
        DataRow samWaterDeliveryDR = samWaterDeliveryDRs[0];
        packageAndDeliveryCheck(samWaterDeliveryDR, dr["quantity_ordered"].ToString(), ref 问题订单List);
    }

     Console.WriteLine("问题订单List: {0}", string.Join("|", 问题订单List));
    // 如果整个订单数量修改的话， 需要记录单个item变化的部分
    if(string.Join("|", 问题订单List).Contains("订单修改产品数量")){
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
    }
    // Console.WriteLine("Item Type: {0}", byPOItemRow["Item Type"].ToString());
    // Convert.ToInt32("a.b");
    if(itemExceptionList.Count > 0){
        byPOItemRow["Exception reason"] = string.Join("; ", itemExceptionList);
        byPOItemRow["order category"] = "exception";
    } 
    问题订单List = 问题订单List.Union(itemExceptionList).ToList();
}

public void repeatedOrderException(DataRow curOrderRow, DataRow previousOrderRow, ref DataRow cleanExceptionDRow, ref List<string> 问题订单List, DataRowCollection prevOrderRows ){
    DataColumnCollection columns = previousOrderRow.Table.Columns;
    DataColumnCollection ValidColumns = prevOrderRows[0].Table.Columns;
    List<string> 重复订单信息 = new List<string>{};

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
            }else if(colName == "total_units_ordered"){
                重复订单信息.Add("订单修改产品数量");
                List<String> resultMabds = prevOrderRows.Cast<DataRow>().Select<DataRow, string>(dr => dr["total_units_ordered"].ToString()).ToList();
                cleanExceptionDRow["原订单数量"] = resultMabds[0];
            }else{
                Dictionary<string, string> webItemMapDic = new Dictionary<string, string>{{"promotional_event", "订单类型"}, {"allowance_percent", "仓租"}, {"total_line_items", "产品行数"}, {"total_order_amount_after_adjustments", "Total Order Amount (After Adjustments)"}};
                
                if(webItemMapDic.Keys.Contains(colName)){
                    string colNameDes = webItemMapDic[colName];
                    重复订单信息.Add(String.Format("订单修改{0}, 从({1})修改为({2})", colNameDes, prevItemStr, curItemStr));
                    // List<String> resultMABDs = prevOrderRows.Cast<DataRow>().Select<DataRow, string>(dr => dr[colName].ToString()).ToList();
                }

            }
        }
    }
    Console.WriteLine("重复订单信息: {0}", String.Join("；", 重复订单信息));
    if(重复订单信息.Count > 0){
       问题订单List.Add("重复订单：" + String.Join("；", 重复订单信息));
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
    List<string> moreColumns = new List<string>{"Item Type", "Customer order Item", "原始MABD", "新MABD", "沃尔玛产品编码", "雀巢产品编码", "Material Description", "原单箱规", "原订单数量", "修改后订单数量", "雀巢数量", "雀巢价格", "沃尔玛价格", "Exception reason"};
    foreach(string colName in moreColumns){
        exceptionByPODT.Columns.Add(colName, typeof(string));
    }
}

// 整单异常检查
public void handleExceptionRow(DataRow dr, ref DataRow cleanExceptionDRow, ref List<string> 问题订单List, DataRow[] curOrderDocLinkRows){
    List<String> allitemproductCodesList = curOrderDocLinkRows.Cast<DataRow>().Select<DataRow, string>(dr => dr["customer_product_code"].ToString()).ToList();
    string prodCode = allitemproductCodesList[0];
    DateTime 读单日 = DateTime.Now; //Convert.ToDateTime(dr["ship_date"]);//DateTime.Now;
    string 读单当天日期 = 读单日.ToString("yyyy/MM/dd");  // 1
    string po_number = dr["order_number"].ToString();  // 2
    string wmdc = dr["WMDC"].ToString(); // 3
    string promotionalEvent = dr["promotional_event"].ToString(); // 4
    string orderType = dr["order_type"].ToString();
    bool isReq = promotionalEvent.Contains("REQ");
    bool isKMDC = (wmdc == "KMDC");
    string totalLineItems = dr["total_line_items"].ToString();
    string nestleBU = string.Empty;
    // nestleBU = dr["Nestle_BU"].ToString(); 山姆和山姆水，这一列先留空
    // JD 订单不处理
    if(promotionalEvent.Contains("JD")){
        问题订单List.Add("JD订单不处理");
    }else if(promotionalEvent == "CANCEL PO"){
        问题订单List.Add("Cancel PO");
    }

    /*
    是否为手工单：？未知
    */
    string 是否为手工单 = string.Empty;  // 6

    string 是否为稳定库存 = (orderType == "0020") ? "稳定" : ""; // 7
    string orderQty = dr["total_units_ordered"].ToString(); // 8
    DateTime 起送日日期 = Convert.ToDateTime(dr["ship_date"]);
    string 起送日 = 起送日日期.ToString("yyyy/MM/dd"); // 9
    string MABD = Convert.ToDateTime(dr["must_arrived_by"]).ToString("yyyy/MM/dd"); // 10
    DateTime MABDDate = Convert.ToDateTime(dr["must_arrived_by"]);
    
    string requestDeliveryDate = dr["Request_Delivery_Date"].ToString(); // ship to表里面获取的。周一/周四, 【7400 KMDC 下周一/下周三】特殊处理
    
    /*
    2.整张订单的折扣1.3%
    Handling
    
    整张订单的折扣不等于1.3%，就反馈Exception
    */
    string allowancePercent = dr["allowance_percent"].ToString();
    bool 整单折扣 = allowancePercent.Split(new string[]{","}, StringSplitOptions.RemoveEmptyEntries).Contains("1.3%");
    if(!整单折扣){
        问题订单List.Add("仓租不为1.3%订单");
    }
    
    /*
      整托和起送量判断， 如果一个订单里面包含2条产品的话，就在订单层级判断，否则在item层级判断
    */
    int orderTotalLineItems = Convert.ToInt32(totalLineItems);
    if(orderTotalLineItems > 1){
        DataRow[] samWaterDeliveryDRs = samWaterDeliverySettingDT.Select(string.Format("WMDC='{0}' and Customer_Product_Code ='{1}'", wmdc, "980080954+980085511"));
        if(samWaterDeliveryDRs.Length > 0){
            DataRow samWaterDeliveryDR = samWaterDeliveryDRs[0];
            packageAndDeliveryCheck(samWaterDeliveryDR, orderQty, ref 问题订单List);
        }
    }

    /*
    DC号    	  到货日
    4873  	  ≥读单当日+3
    4802      周一/周二/周三/周四/周五/周日 且 ≥读单当日+2）
    4817  	  下周一/下周五
    4819  	  下周一/下周四/下周六 
    4874  	  下周一/下周四/下周六
    4816  	  下周一/下周四/下周六
    */

    bool validShipDate = false;
    if(requestDeliveryDate.Contains("读单当日") && !requestDeliveryDate.Contains("周")){
        int dayAdded = fetchNumber(requestDeliveryDate);
        // Console.WriteLine($"dayAdded: {dayAdded.ToString()}, MABDDate: {MABDDate.ToString()}, 读单日.AddDays(dayAdded): {读单日.AddDays(dayAdded).ToString()}");
        if(DateTime.Compare(MABDDate, 读单日.AddDays(dayAdded)) < 0){
            问题订单List.Add($"客户指定送货日不在行程日");
        }
    }else if(requestDeliveryDate.Contains("读单当日") && requestDeliveryDate.Contains("周") && requestDeliveryDate.Contains("且")){
        string[] requestDeliveryDateArr = requestDeliveryDate.Split(new string[]{"且"}, StringSplitOptions.RemoveEmptyEntries);
        
        string weekCondition = requestDeliveryDateArr[0];
        string dayAddedCondition = requestDeliveryDateArr[1];
        string 周几 = CaculateWeekDay(MABDDate);
        int dayAdded = fetchNumber(requestDeliveryDate);
        
        if(!requestDeliveryDate.Contains(周几) || (DateTime.Compare(MABDDate, 读单日.AddDays(dayAdded)) < 0))
        {
            问题订单List.Add($"客户指定送货日不在行程日");
        }
    }else{
        string[] requestDeliveryDateArr = requestDeliveryDate.Split(new string[]{"/"}, StringSplitOptions.RemoveEmptyEntries);
        
        bool validMABDDay = ifValidMABDDay(读单日, requestDeliveryDateArr, MABDDate);
        if(!validMABDDay){
            问题订单List.Add($"客户指定送货日不在行程日");
        }
    }
    
    /* 价差检查 
    订单扣点后未税总金额检查
   （雀巢扣点后未税价-山姆扣点后未税价抓单的⑬栏位金额），读单表sheet-订单明细X列：
    录单后雀巢扣点后未税价（W列）-订单Total Order Amount (上面抓单的第13个内容）After Adjustments)
    雀巢扣点后未税价计算：
    SAP Net Value=雀巢总成本价（有源数据）- 折扣金额
                             =雀巢未税箱价*箱数-雀巢未税箱价*箱数*0.03867
    其中0.03867为订单总扣点
    山姆订单扣点后未税价：Total Order Amount (After Adjustments)在 抓单的⑬栏位
    超出范围录单后Exception反馈CSA
    
    价差可接受范围（X列）
    1、980080954 巴黎水：
    超出（-5，2）范围当作异常订单反馈CSA。
    2、980085511 圣培露：
    雀巢价小于山姆价，每箱价（X列价格/订单箱数）差0.0052525元的差异可忽略
    */
    
    string 巴黎水产品码 = "980080954";
    string 圣培露产品码 = "980085511";

    decimal walmartDiscountRate = customerDiscountRateM();
    decimal 不含扣点 = (1-walmartDiscountRate);
    Decimal total_order_amount_after_adjustments = Convert.ToDecimal(dr["total_order_amount_after_adjustments"]);
    
    decimal sap_net_value = getSapNetValue(dr, allitemproductCodesList, curOrderDocLinkRows, 不含扣点);
    decimal gapValue = sap_net_value - total_order_amount_after_adjustments;

    // 980080954 巴黎水, 且价差超出（-5，2）范围当作异常订单反馈CSA。 
    if(prodCode == 巴黎水产品码){
        if(gapValue < -5 || gapValue > 2 ){
            问题订单List.Add($"价差检查, SAP NET Value: {Math.Round(sap_net_value, 2)}, 山姆订单扣点后未税金额: {Math.Round(total_order_amount_after_adjustments, 2)}");
        }
    }else if(prodCode == 圣培露产品码){
        // 雀巢价小于山姆价，每箱价（X列价格/订单箱数）差0.0052525元的差异可忽略
        // 解释：雀巢价大 于山姆价，每箱价（X列价格/订单箱数）大于0.0052525元的差异为异常
        decimal 每箱价差 = Math.Round(gapValue/Convert.ToInt32(orderQty), 8);
        if(gapValue > 0m || Math.Abs(每箱价差) > 0.0052525m){
            问题订单List.Add($"价差检查, SAP NET Value: {Math.Round(sap_net_value, 2)}, 山姆订单扣点后未税金额: {Math.Round(total_order_amount_after_adjustments, 2)}, 每箱价差：{每箱价差}");
        }
    }

    /*  
    跨月订单， MABD为下个月
    当月订单的MABD显示在下个月的订单，判断为问题订单，备注原因跨月订单反馈
    */
    DateTime orderCreateDateTime = Convert.ToDateTime(dr["create_date_time"]);
    bool mabdInNextMonth = MABDDate.Month == (orderCreateDateTime.Month + 1);
    
    if(mabdInNextMonth){
        问题订单List.Add("跨月订单");
    }

    cleanExceptionDRow["Order Date"] = 读单当天日期;
    cleanExceptionDRow["PO No."] = po_number;
    cleanExceptionDRow["SAP PO"] = $"IBU{po_number}";
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
    cleanExceptionDRow["Customer order Item"] = dr["total_line_items"].ToString();    
}


public int fetchNumber(string srcTxt){
    Regex 数字结尾正则 = new Regex(@"\d{1,}$");
    Match matchResult112 = 数字结尾正则.Match(srcTxt);
    string numberStr = matchResult112.Value;
    if(numberStr!= ""){
       return Convert.ToInt32(numberStr); 
    }else{
        return 0;
    }
}

public bool ifValidMABDDay(DateTime 读单日, string[] requestDeliveryDateArr, DateTime MABDDate){
    int todayDayOfWeek = (int)读单日.DayOfWeek;
    int saturdayDay = (int)DayOfWeek.Saturday;
    bool validMABDDay = false;
    if(todayDayOfWeek <= 6 && todayDayOfWeek != 0){
        int gapToSaturday = saturdayDay - todayDayOfWeek;
        foreach(string weekDay in requestDeliveryDateArr){
            int gapToTargetDay = gapToSaturday;
            switch(weekDay)
            {
                case "下周一":
                    gapToTargetDay += 2;
                    break;
                case "下周二":
                    gapToTargetDay += 3;
                    break;
                case "下周三":
                    gapToTargetDay += 4;
                    break;
                case "下周四":
                    gapToTargetDay += 5;
                    break;
                case "下周五":
                    gapToTargetDay += 6;
                    break;
                case "下周六":
                    gapToTargetDay += 7;
                    break;
                case "下周日":
                    gapToTargetDay += 8;
                    break;
                default:
                   gapToTargetDay += 1;
                break;
            }
            if(DateTime.Compare(读单日.AddDays(gapToTargetDay), MABDDate) == 0){
                validMABDDay = true;
                break;
            }
        }
    }
    return validMABDDay;
}


public void packageAndDeliveryCheck(DataRow samWaterDeliveryDR, string orderQty, ref List<string> 问题订单List){
    int startDeliveryCount = Convert.ToInt32(samWaterDeliveryDR["Start_Delivery_Count"]);  // 起送量
    int packageMultiple = Convert.ToInt32(samWaterDeliveryDR["Package_Multiple"]); // 整托送货倍数
    int orderQtyInt = Convert.ToInt32(orderQty); // 订单总量
    string deliveryMethod = samWaterDeliveryDR["Delivery_Method"].ToString(); // 物流配送方式
    string 物流方式 = deliveryMethod.Contains("直送") ? "直送仓" : "集货仓";
    if(orderQtyInt < startDeliveryCount){ // Exception reason：集货仓/直送仓数量未达到XXXX起送量
        问题订单List.Add($"{物流方式}数量未达到{startDeliveryCount}起送量");
    }else{
        decimal 托数 = Math.Round(orderQtyInt/Convert.ToDecimal(packageMultiple), 6);
        int 托数整数 = orderQtyInt/packageMultiple;
        if(托数整数 != 托数){ // 不是整托，不满足整托送货
           问题订单List.Add($"不满足整托送货，托数：{托数}"); 
        }
    }
}

// 获取 order SAP net value
public decimal getSapNetValue(DataRow dr, List<string> allitemproductCodesList, DataRow[] curOrderDocLinkRows, decimal 不含扣点){
    Decimal sap_net_value = 0;
    // 如果不属于1对多的那些产品
    string location = dr["location"].ToString();
    string allitemproductCodesStr = string.Join(", ", allitemproductCodesList.ToArray());

    foreach(DataRow itemDR in curOrderDocLinkRows){
        string customerProdCode = itemDR["customer_product_code"].ToString();
        int itemQuantityOrdered = Convert.ToInt32(itemDR["quantity_ordered"]);
        decimal itemNestleNPS=0;
        if(!String.IsNullOrEmpty(itemDR["Nestle_NPS"].ToString())){
            itemNestleNPS = Convert.ToDecimal(itemDR["Nestle_NPS"]);
        }
        sap_net_value += itemNestleNPS * itemQuantityOrdered * 不含扣点;
    }
 
    return sap_net_value;
}

public decimal customerDiscountRateM(){
    DataRowCollection etoConfigDrs = etoConfigDT.Rows;
    string walmartDiscountRateStr = etoConfigDrs[0]["discount_rate"].ToString();
    decimal walmartDiscountRate = fetchRateInDecimal(walmartDiscountRateStr);
    return walmartDiscountRate;
}

public static decimal fetchRateInDecimal(string walmartDiscountRateStr)
{
    Regex 百分数正则 = new Regex(@"^\d+\.?\d{0,2}%$");
    Match matchResult = 百分数正则.Match(walmartDiscountRateStr);
    string 百分比 = matchResult.Value;
    decimal resutRate = 0;
    try
    {
        if (!string.IsNullOrEmpty(百分比))
        {
            resutRate = Convert.ToDecimal(百分比.Replace("%", "")) / 100m;
        }
        else
        {
            if (!walmartDiscountRateStr.Contains("%"))
            { // 不包含%
                resutRate = Convert.ToDecimal(walmartDiscountRateStr);
            }
        }
    }
    catch (Exception e)
    {
        Console.WriteLine("walmartDiscountRateStr不合法： {0}", e.Message);
    }
    return resutRate;
}

public string CaculateWeekDay(DateTime dtNow)
{
    var weekdays = new string[] { "周日", "周一", "周二", "周三", "周四", "周五", "周六" };
    return weekdays[(int)dtNow.DayOfWeek];
}
