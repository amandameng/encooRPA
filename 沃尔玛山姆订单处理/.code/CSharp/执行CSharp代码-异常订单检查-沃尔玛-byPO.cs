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
    DataRowCollection etoConfigDrs = etoConfigDT.Rows;
    checkBulkWaferException = etoConfigDrs[0]["checkBulkWaferException"].ToString();
    
    // 获取个渠道扣点比例，这边应该有3个rate
    getWMDiscountRate(etoConfigDrs);

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
        DataRow 分仓行 = 分仓明细数据表.NewRow();
        // Order 不重复的时候，判断order异常信息
        
        
        // 判断ship To 门店订单
        string location = curOrderRow["location"].ToString();
        // WM locations 不包含当前订单的location 则是门店订单异常
        bool shipTo门店 = !WMLocationsList.Contains(location);        
        
        // By Order的异常判断
        handleExceptionRow(curOrderRow, ref cleanExceptionDRow, ref 问题订单List, curOrderDocLinkRows, shipTo门店);
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
            // 产品行增减的判断
            compareOrderItem(curOrderDocLinkRows, prevOrderDocLinkRows, cleanExceptionDRow);
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
        // POS REPLEN 的订单不进分仓明细表, 2021-12-27 by mengfanling
        if(分仓行["订单类型（Promotional Event）"].ToString().Trim() != "POS REPLEN"){
           分仓明细数据表.Rows.Add(分仓行);
        }
        
        /*
          特殊产品需要检查订单
          价差订单
        */

         /* 散威化产品判断 */
        List<string> bulk_walfer_codes = new List<string>{};
        foreach(DataRow dr in bulkWalferConfigDT.Rows){
            string customer_product_code = dr["customer_product_code"].ToString();
            bulk_walfer_codes.Add(customer_product_code);
        }
        
        // By Item, 全部异常汇总
        List<string> refItemExceptionList = new List<string>{};

        // 如果问题订单包含POS REPLEN，则不用判断检查价差和送货日检查等问题
         string 问题订单字符 = string.Join(";", 问题订单List);
        if(!问题订单字符.Contains("POS REPLEN")){
            decimal sapNetValue = 0;
            decimal total_order_amount_after_adjustments = toDecimalConvert(curOrderDocLinkRows[0]["total_order_amount_after_adjustments"]);
            bool 散威化订单 = false;
            foreach(DataRow dr in curOrderDocLinkRows){
                string productCode = dr["customer_product_code"].ToString();
                DataRow byPOItemRow =  exceptionByPODT.NewRow();
                byPOItemRow.ItemArray = cleanExceptionDRow.ItemArray;
                
                int quantity_ordered = toIntConvert(dr["quantity_ordered"]);
                if(quantity_ordered == 0){
                    refItemExceptionList.Add("产品数量为0");
                }
                // 散威化价差， 最后看整单价差
                if(bulk_walfer_codes.Contains(productCode)){
                    散威化订单 = true;
                    cleanExceptionDRow["Customer order Item"] = dr["line_number"];
                    checkBulkWalferPrice(dr, ref refItemExceptionList, ref sapNetValue, cleanExceptionDRow);
                }else{
                    byPOItemRow["Customer order Item"] = dr["line_number"];
                    specialProductCheck(dr, 问题订单List, ref refItemExceptionList, ref byPOItemRow);
                    if(!String.IsNullOrEmpty(byPOItemRow["Exception reason"].ToString())){ // 存在异常信息
                        exceptionByPODT.Rows.Add(byPOItemRow);  // by Item 的异常订单 
                    }
                }
                
                // 如果整个订单数量修改的话， 需要记录单个item变化的部分
                string orderExceptionListStr = string.Join("|", 问题订单List);
                if(orderExceptionListStr.Contains("订单修改产品数量") || orderExceptionListStr.Contains("Total Order Amount")){
                    string customerProductCode = dr["customer_product_code"].ToString();
                    //Console.WriteLine("customerProductCode: {0}", customerProductCode);
                    
                    DataRow prevItemRow = null;
                    foreach(DataRow prevDr in prevOrderDocLinkRows){
                        //Console.WriteLine("prevDr customerProductCode: {0}", prevDr["customer_product_code"].ToString());
                        if(customerProductCode == prevDr["customer_product_code"].ToString()){ // 找到对应的前一个order的item
                            prevItemRow = prevDr;
                            break;
                        }
                    }

                    if(prevItemRow!=null){
                        // 之前item数量不等于当前item对应的数量
                        if(prevItemRow["quantity_ordered"].ToString() != dr["quantity_ordered"].ToString()){
                            byPOItemRow["原订单数量"] = prevItemRow["quantity_ordered"];
                            byPOItemRow["修改后订单数量"] = dr["quantity_ordered"];
                            refItemExceptionList.Add("订单修改产品数量");
                        }
                        // 之前item扣点不等于当前item扣点
                        if(prevItemRow["oli_allowance_percent"].ToString() != dr["oli_allowance_percent"].ToString()){
                            refItemExceptionList.Add($"订单修改扣点, 原折扣为{prevItemRow["oli_allowance_percent"]}，现折扣为{dr["oli_allowance_percent"]}");
                        }
                    }
                }
                
            }
            // 判断 sapNetValue
           // Console.WriteLine("sapNetValue: {0}, total_order_amount_after_adjustments: {1}", sapNetValue, total_order_amount_after_adjustments);
        
            // 只有散威化需要检查价差。总价满足价差条件的话，依次将里面的item加入exception Row
            if(散威化订单 && Math.Abs(sapNetValue - total_order_amount_after_adjustments) > 5){
                问题订单List.Add(string.Format("价格差异，sapNetValue: {0}, total_order_amount_after_adjustments: {1}", Math.Round(sapNetValue, 2), Math.Round(total_order_amount_after_adjustments, 2)));
            }
        }
        
        //Console.WriteLine(string.Join("@", 问题订单List));
        
        cleanExceptionDRow["Item Type"] = "Order";
        cleanExceptionDRow["order category"] = (问题订单List.Count > 0 || refItemExceptionList.Count > 0) ? "exception" : "clean";
        string onlyItemException = (问题订单List.Count == 0 && refItemExceptionList.Count > 0) ? "1" : "0";
        cleanExceptionDRow["onlyItemException"] = onlyItemException;
        cleanExceptionDRow["Exception category"] = string.Join("; ", 问题订单List.Union(refItemExceptionList));
        cleanExceptionDRow["Exception reason"] = cleanExceptionDRow["Exception category"];

        exceptionByPODT.Rows.Add(cleanExceptionDRow);
    }
    
    buildByPODT();
    buildByPOAndItemDT();
    // Convert.ToInt16("请问CDC");
}

public void compareOrderItem(DataRow[] curOrderDocLinkRows, DataRow[] prevOrderDocLinkRows, DataRow cleanExceptionDRow){
    string[] currentProductCodeArr = curOrderDocLinkRows.Cast<DataRow>().Select<DataRow, string>(dr => dr["customer_product_code"].ToString()).ToArray();
    string[] previousProductCodeArr = prevOrderDocLinkRows.Cast<DataRow>().Select<DataRow, string>(dr => dr["customer_product_code"].ToString()).ToArray();
    string[] 新增产品码数组 = currentProductCodeArr.Except(previousProductCodeArr).ToArray();
    string[] 删除产品码数组 = previousProductCodeArr.Except(currentProductCodeArr).ToArray();
    if(新增产品码数组.Length > 0){
        foreach(string productCode in 新增产品码数组){
            foreach(DataRow dr in curOrderDocLinkRows){
                if(productCode == dr["customer_product_code"].ToString()){
                    // Order Date	PO No.	Customer order Item   沃尔玛产品编码	雀巢产品编码	Material Description	Nestle BU		原订单数量	修改后订单数量	雀巢数量 Exception category  
                   //  byPOItemRow["Item Type"] = "Item";
                  //  byPOItemRow["order category"] = "exception";
                    DataRow itemExceptionRow =  exceptionByPODT.NewRow();
                    itemExceptionRow.ItemArray = cleanExceptionDRow.ItemArray;
                    initExceptionItemRow(ref itemExceptionRow, dr);
                    
                    itemExceptionRow["原订单数量"] = 0;
                    itemExceptionRow["修改后订单数量"] = dr["quantity_ordered"];
                    itemExceptionRow["雀巢数量"] = dr["quantity_ordered"];
                    itemExceptionRow["Exception reason"] = "订单修改产品数量,新增产品行";
                    exceptionByPODT.Rows.Add(itemExceptionRow);
                }
            }
        }
    }
    
    if(删除产品码数组.Length > 0){
        foreach(string productCode in 删除产品码数组){
            foreach(DataRow dr in prevOrderDocLinkRows){
                if(productCode == dr["customer_product_code"].ToString()){
                    // Order Date	PO No.	Customer order Item   沃尔玛产品编码	雀巢产品编码	Material Description	Nestle BU		原订单数量	修改后订单数量	雀巢数量 Exception category  
                   //  byPOItemRow["Item Type"] = "Item";
                  //  byPOItemRow["order category"] = "exception";
                    DataRow itemExceptionRow =  exceptionByPODT.NewRow();
                    itemExceptionRow.ItemArray = cleanExceptionDRow.ItemArray;
                    initExceptionItemRow(ref itemExceptionRow, dr);
                    
                    itemExceptionRow["原订单数量"] = dr["quantity_ordered"];
                    itemExceptionRow["修改后订单数量"] =0 ;
                   itemExceptionRow["Exception reason"] = "订单修改产品数量,删除产品行";

                    // itemExceptionRow["雀巢数量"] = dr["quantity_ordered"];
                    exceptionByPODT.Rows.Add(itemExceptionRow);
                }
            }
        }
    }

}

public void initExceptionItemRow(ref DataRow itemExceptionRow, DataRow dr){
    itemExceptionRow["Item Type"] = "Item";
    itemExceptionRow["order category"] = "exception";
    itemExceptionRow["Exception category"] = "订单修改产品数量";
    itemExceptionRow["Customer order Item"] = dr["line_number"];
    itemExceptionRow["沃尔玛产品编码"] = dr["customer_product_code"];
    itemExceptionRow["雀巢产品编码"] = dr["Nestle_Material_No"];
    itemExceptionRow["Material Description"] = dr["Material_Description"];
    itemExceptionRow["Nestle BU"] = dr["Nestle_BU"];    
}

public void setNotIntoEx2O(string order_number){
    if(existingEX2ODT!=null && orderJobHistoryDT!=null){
        // bool inEX2O = existingEX2ODT.AsEnumerable().Cast<DataRow>().Any(dRow => dRow["Customer_Order_Number"].ToString() == order_number);
        bool inEX2O = existingEX2ODT.AsEnumerable().Cast<DataRow>().Any(dRow => dRow["PO_Number"].ToString().Contains(order_number));

        
        bool inOrderJobHistory = orderJobHistoryDT.AsEnumerable().Cast<DataRow>().Any(dRow => dRow["order_number"].ToString() == order_number);
        // bool inOrderJobHistory = orderJobHistoryDT.AsEnumerable().Cast<DataRow>().Any(dRow => dRow["PO_Number"].ToString().Contains(order_number));

        if(inEX2O && inOrderJobHistory){
            不录单订单列表.Add(order_number);
        }
    }
}

public void getWMDiscountRate(DataRowCollection etoConfigDrs){
    string walmartDiscountRateStr = etoConfigDrs[0]["discount_rate"].ToString();
    string EDLC百分比字符串 = etoConfigDrs[0]["EDLC百分比"].ToString();
    string RTV百分比字符串 = etoConfigDrs[0]["RTV百分比"].ToString();
    string 仓租百分比字符串 = etoConfigDrs[0]["仓租百分比"].ToString();
    walmartDiscountRate = fetchRateInDecimal(walmartDiscountRateStr);
    EDLC百分比 = fetchRateInDecimal(EDLC百分比字符串);
    RTV百分比 = fetchRateInDecimal(RTV百分比字符串);
    仓租百分比 = fetchRateInDecimal(仓租百分比字符串);
}

public decimal sapNetValueFormular(decimal nestleNPS){
    decimal sapTmp = nestleNPS * (1 - EDLC百分比 - RTV百分比);
    decimal sapNetValue = sapTmp * (1 - 仓租百分比);
    return sapNetValue;
}

public void addExceptionRow(DataRow cleanExceptionDRow){
    string exceptionMessage = "无法mapping雀巢主数据";
    DataRow byPOItemRow =  exceptionByPODT.NewRow();
    byPOItemRow.ItemArray = cleanExceptionDRow.ItemArray;
    byPOItemRow["Exception category"] = exceptionMessage;
    byPOItemRow["Exception reason"] = exceptionMessage;
    exceptionByPODT.Rows.Add(byPOItemRow);
}

// 散威化价差异常
public DataRow priceExceptionRow(DataRow dr, string 雀巢产品编码, Decimal nestle_NPS, DataRow cleanExceptionDRow, int quantity_ordered){
    DataRow byPOItemRow =  exceptionByPODT.NewRow();
    string remarkOption = dr["Remark_Option"].ToString();
    string exceptionMessage = "价差订单";
    byPOItemRow.ItemArray = cleanExceptionDRow.ItemArray;
    byPOItemRow["Item Type"] = "Item";
    byPOItemRow["Exception category"] = exceptionMessage;
    byPOItemRow["Exception reason"] = exceptionMessage;
    byPOItemRow["雀巢产品编码"] = 雀巢产品编码;
    byPOItemRow["雀巢数量"] = quantity_ordered;
    byPOItemRow["雀巢价格"] = nestle_NPS;
    byPOItemRow["沃尔玛产品编码"] = dr["customer_product_code"].ToString();
    byPOItemRow["Material Description"] =  dr["Material_Description"];
    byPOItemRow["Nestle BU"] = dr["Nestle_BU"];
    byPOItemRow["沃尔玛价格"] = Math.Round(toDecimalConvert(dr["cost"]), 2);
    byPOItemRow["Item Type"] = "Item";
    byPOItemRow["order category"] = "exception";
    return byPOItemRow;
}

public void checkBulkWalferPrice(DataRow dr, ref List<string> refItemExceptionList, ref decimal sapNetValue, DataRow cleanExceptionDRow){
    string productCode = dr["customer_product_code"].ToString();
    int quantity_ordered = toIntConvert(dr["quantity_ordered"]);
    
    // 散威化配置表获取对应条目
    DataRow bulkWalferProduct = bulkWalferConfigDT.Select("customer_product_code='" +productCode + "'")[0];
    int boundry_value = toIntConvert(bulkWalferProduct["boundry_value"]);
    string exceptionMessage = String.Empty;
    // 获取excel_to_order_config表中沃尔玛对应的扣点

    if(quantity_ordered < boundry_value){
        string 雀巢产品编码 = bulkWalferProduct["nestle_code_default"].ToString();
        
        DataRow[] bulkProductDrs = oneToNNestleProductsDT.Select(String.Format("Nestle_Plant_No='{0}' and Nestle_Material_No='{1}'", dr["location"].ToString(), 雀巢产品编码));
        if(bulkProductDrs.Length == 0){
            addExceptionRow(cleanExceptionDRow);
           return;
        }

        decimal nestle_NPS = toDecimalConvert(bulkProductDrs[0]["Nestle_NPS"]);
        string matefial_description = bulkProductDrs[0]["Material_Description"].ToString();
        if(dr["Remark_Option"].ToString().Contains("检查价差")){
            DataRow byPOItemRow = priceExceptionRow(dr, 雀巢产品编码, nestle_NPS, cleanExceptionDRow, quantity_ordered);
            byPOItemRow["Material Description"] = matefial_description;
            exceptionByPODT.Rows.Add(byPOItemRow);
        }
        sapNetValue += sapNetValueFormular(nestle_NPS * quantity_ordered);
    }else{
        /* 
        customer_product_code  boundry_value  nestle_code_default  nestle_code_allocation  allocation_ratio  flavor_description  bulk_walfer_type
        021402419  10  12458252  12458252,12458087,12458320  7：2：1  巧克力：牛奶：花生  散威化
        021779181  10  12458252  12458252,12458087,12458320  7：2：1  巧克力：牛奶：花生  散威化
        */
        string nestleCodeAllocation = bulkWalferProduct["nestle_code_allocation"].ToString();
        string allocationRatio = bulkWalferProduct["allocation_ratio"].ToString();
        string[] nestleCodeArr = nestleCodeAllocation.Split(new string[]{","}, StringSplitOptions.RemoveEmptyEntries);
        string[] allocationRatioArr = allocationRatio.Split(new string[]{"："}, StringSplitOptions.RemoveEmptyEntries); // 注意： 是中文冒号

        // 按比例分割Qty
        int[] qtyArr = splitQtyByRatio(allocationRatioArr, quantity_ordered);

        for(int i=0; i< nestleCodeArr.Length; i++){
            string 雀巢产品编码 = nestleCodeArr[i];

            DataRow[] bulkProductDrs = oneToNNestleProductsDT.Select(String.Format("Nestle_Plant_No='{0}' and Nestle_Material_No='{1}'", dr["location"].ToString(), 雀巢产品编码));
            if(bulkProductDrs.Length == 0){
               addExceptionRow(cleanExceptionDRow);
               continue;
            }
            decimal nestle_NPS = toDecimalConvert(bulkProductDrs[0]["Nestle_NPS"]);
            string matefial_description = bulkProductDrs[0]["Material_Description"].ToString();

            int curQuantity_ordered = qtyArr[i];
            sapNetValue += sapNetValueFormular(nestle_NPS * curQuantity_ordered);
            if(dr["Remark_Option"].ToString().Contains("检查价差")){
                DataRow byPOItemRow = priceExceptionRow(dr, 雀巢产品编码, nestle_NPS, cleanExceptionDRow, curQuantity_ordered);
                byPOItemRow["Material Description"] = matefial_description;
                exceptionByPODT.Rows.Add(byPOItemRow);
            }
        } 
    }
    
    
}

public static decimal fetchRateInDecimal(string walmartDiscountRateStr)
{
    Regex 百分数正则 = new Regex(@"\d+(\.\d+)?%");
    Match matchResult = 百分数正则.Match(walmartDiscountRateStr);
    string 百分比 = matchResult.Value;
    decimal resutRate = 0;
    try
    {
        if (!string.IsNullOrEmpty(百分比))
        {
            resutRate = toDecimalConvert(百分比.Replace("%", "")) / 100m;
        }
    }
    catch (Exception e)
    {
        Console.WriteLine("walmartDiscountRateStr不合法： {0}", e.Message);
    }
    return resutRate;
}

public int[] splitQtyByRatio(string[] allocationRatioArr, int quantity_ordered){
    decimal total_ratio = 0.0m;
    foreach(string ratio in allocationRatioArr){
        total_ratio = total_ratio + toDecimalConvert(ratio);
    }
    if(total_ratio == 0){
        total_ratio = Decimal.MaxValue; // 如果 total ratio不合法，则给个最大值
    }    
    List<int> initQtyList = new List<int> {};
    int totalRequltQty = 0;
    foreach(string ratioStr in allocationRatioArr){
        int rationValue = toIntConvert(ratioStr);
        int curQuantity_ordered = toIntConvert(Math.Round(quantity_ordered * (rationValue/total_ratio)));
        totalRequltQty += curQuantity_ordered;
        initQtyList.Add(curQuantity_ordered);
    }
    if(quantity_ordered > totalRequltQty){
        int firstQty = initQtyList[0];
        initQtyList[0] = firstQty + (quantity_ordered - totalRequltQty);
    }
    return initQtyList.ToArray();
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
    
    DataRow[] poExceptionDRows = exceptionByPODT.Select("`order category`= 'exception' and `onlyItemException` <> '1'");
    
    // Console.WriteLine(poExceptionDRows.Length);
    foreach(DataRow dr in poExceptionDRows){
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

// 特殊产品by Item 检查
public void specialProductCheck(DataRow dr, List<string> 问题订单List,  ref List<string> refItemExceptionList, ref DataRow byPOItemRow){
    List<string> itemExceptionList = new List<String>{};
    
    string remark = dr["Remark"].ToString();
    string remarkOption = dr["Remark_Option"].ToString(); // Remark_Option 1. 需检查以下客户店内码的原单规格与雀巢的产品规格是否一致 2. 检查价差  3.备注客户店内码
    //价差订单
    //特殊产品需检查订单   停止出货产品/箱规检查
    string 沃尔玛产品编码 = dr["customer_product_code"].ToString();
    byPOItemRow["沃尔玛产品编码"] = 沃尔玛产品编码;
    byPOItemRow["雀巢产品编码"] = dr["Nestle_Material_No"];
    byPOItemRow["Material Description"] =  dr["Material_Description"];
    byPOItemRow["Nestle BU"] = dr["Nestle_BU"];
    decimal quantity_ordered = toDecimalConvert(dr["quantity_ordered"]);
    byPOItemRow["雀巢数量"] = dr["quantity_ordered"];
    decimal nestleNPS = toDecimalConvert(dr["Nestle_NPS"].ToString());
    //sapNetValue += sapNetValueFormular(nestleNPS*quantity_ordered); // 注释掉，不这样算价差
    // 有remark说明已经map到主数据
    if(remark.Contains("特殊产品")){
        string pack = dr["pack"].ToString(); // 客户网站是 12 / 12
        if(pack.Contains("/")){
           string[] packArr = pack.Split(new string[]{"/"}, StringSplitOptions.RemoveEmptyEntries);
           pack = packArr[0];
        }
        bool 检查价差 = remarkOption.Contains("检查价差");
        if(检查价差){
            bool skipCheckPrice = false;
            decimal cost = Math.Round(toDecimalConvert(dr["cost"]), 2); // 沃尔玛价格
            DataRow[] skipPriceCheckDRs = 沃尔玛跳过产品检查数据表.Select(string.Format("customer_product_code='{0}' and customer_name='{1}'", 沃尔玛产品编码, curCustomerName));
           // Console.WriteLine("沃尔玛产品编码：{0}, 沃尔玛价格:{1}", 沃尔玛产品编码, dr["cost"].ToString());
            if(skipPriceCheckDRs.Length > 0){
                DataRow skipPriceCheckDR = skipPriceCheckDRs[0];
                // 沃尔玛价格等于设定值的话，不反馈价差
               // Console.WriteLine("设定的价格：{0}", skipPriceCheckDR["customer_price"].ToString());
                decimal setPrice = Math.Round(toDecimalConvert(skipPriceCheckDR["customer_price"]), 2);
                if(setPrice == cost){
                   skipCheckPrice = true;
                }
            }
            
            if(!skipCheckPrice && cost != nestleNPS){
               // Console.WriteLine("--价差订单---");
                byPOItemRow["沃尔玛价格"] = cost;
                byPOItemRow["雀巢价格"] = nestleNPS;
                byPOItemRow["原单箱规"] = pack;
                byPOItemRow["Item Type"] = "Item";
                itemExceptionList.Add("价差订单");
            }
        }

        bool 备注客户店内码 = remarkOption.Contains("备注客户店内码");
        if(备注客户店内码){
             byPOItemRow["Item Type"] = "Item";
             itemExceptionList.Add("特殊产品需检查订单," + remarkOption);
        }

        bool 客户店内码的原单规格 = remarkOption.Contains("客户店内码的原单规格");
        if(客户店内码的原单规格){
            byPOItemRow["原单箱规"] = pack;
            byPOItemRow["Item Type"] = "Item";
            itemExceptionList.Add("特殊产品需检查订单," + remarkOption);
        }
        
        if(!检查价差 && !备注客户店内码 && !客户店内码的原单规格 && !string.IsNullOrEmpty(remarkOption)){ // remarkOption 不为空，则也添加到异常描述里面
            byPOItemRow["Item Type"] = "Item";
            itemExceptionList.Add("特殊产品需检查订单," + remarkOption);
        }
    }else{
        if(String.IsNullOrEmpty(byPOItemRow["雀巢产品编码"].ToString())){
            byPOItemRow["Item Type"] = "Item";
            itemExceptionList.Add("无法匹配雀巢主数据");
        }
    }

    // 汇总exception信息
    if(itemExceptionList.Count > 0){
        byPOItemRow["Exception reason"] = string.Join("; ", itemExceptionList);
        byPOItemRow["order category"] = "exception";
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
                Dictionary<string, string> webItemMapDic = new Dictionary<string, string>{{"ship_date", "起送日"}, {"promotional_event", "订单类型"}, {"allowance_percent", "仓租"}, {"total_line_items", "产品行数"}, {"total_order_amount_after_adjustments", "Total Order Amount (After Adjustments)"}};
                
                if(webItemMapDic.Keys.Contains(colName)){
                    string colNameDes = webItemMapDic[colName];
                    重复订单信息.Add(String.Format("订单修改{0}, 从({1})修改为({2})", colNameDes, prevItemStr, curItemStr));
                    List<String> resultMABDs = prevOrderRows.Cast<DataRow>().Select<DataRow, string>(dr => dr[colName].ToString()).ToList();
                    分仓行信息.Add(String.Format("原{0}为{1}", colNameDes, String.Join(",", resultMABDs)));
                }

            }
        }
    }
    //Console.WriteLine("重复订单信息: {0}", String.Join("；", 重复订单信息));
    if(重复订单信息.Count > 0){
       问题订单List.Add("重复订单：" + String.Join("；", 重复订单信息));
    }else{
       问题订单List.Add("客户系统重复出单，订单无修改"); 
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
public void initExceptionRow(DataRow dr, ref DataRow cleanExceptionDRow){
    DateTime timeNow =  DateTime.Now;
    string 读单当天日期 = timeNow.ToString("yyyy/MM/dd");  // 1
    string po_number = dr["order_number"].ToString();  // 2
    string wmdc = dr["WMDC"].ToString(); // 3
    string promotionalEvent = dr["promotional_event"].ToString(); // 4
    string orderType = dr["order_type"].ToString();
    string orderQty = dr["total_units_ordered"].ToString(); // 8
    string 起送日 = Convert.ToDateTime(dr["ship_date"]).ToString("yyyy/MM/dd"); // 9
    
    cleanExceptionDRow["Order Date"] = 读单当天日期;
    cleanExceptionDRow["PO No."] = po_number;
    cleanExceptionDRow["SAP PO"] = po_number;
    cleanExceptionDRow["仓号"] = dr["Nestle_Plant_No"].ToString();
    cleanExceptionDRow["WMDC"] = wmdc;
    cleanExceptionDRow["Order type（Promotional Event）"] = promotionalEvent;

    // 是否为手工单	是否为稳定库存	Order qty	起送日	MABD	[order category]（现在还判断不了，因为现在是by itemde 计算方式）	[Exception category] 等item确定了才能最终确定
    string 是否为稳定库存 = (orderType == "0020") ? "稳定" : ""; // 7
    cleanExceptionDRow["是否为稳定库存"] = 是否为稳定库存;
    cleanExceptionDRow["Order qty"] = orderQty;
    cleanExceptionDRow["起送日"] = 起送日;
}

public void handleExceptionRow(DataRow dr, ref DataRow cleanExceptionDRow, ref List<string> 问题订单List, DataRow[] curOrderDocLinkRows, bool shipTo门店){
    List<String> allitemInstructionsList = curOrderDocLinkRows.Cast<DataRow>().Select<DataRow, string>(dr => Regex.Replace(dr["item_instructions"].ToString(), @"\s+", " ")).ToList();

    DateTime timeNow =  DateTime.Now; //orderCreateDateTime;
    string wmdc = dr["WMDC"].ToString(); // 3
    bool isKMDC = (wmdc == "KMDC");
    Console.WriteLine("=promotional_event=={0}===", dr["promotional_event"].ToString());
    string promotionalEvent = dr["promotional_event"].ToString().Trim(); // 4
    bool isReq = promotionalEvent.Contains("REQ");
    string nestleBU = string.Empty;
    string MABD = Convert.ToDateTime(dr["must_arrived_by"]).ToString("yyyy/MM/dd"); // 10
    DateTime MABDDate = Convert.ToDateTime(dr["must_arrived_by"]);
    cleanExceptionDRow["MABD"] = MABD;
    cleanExceptionDRow["新MABD"] = MABD;   

    initExceptionRow(dr, ref cleanExceptionDRow);
    
    string 是否为手工单 = string.Empty;  // 6
    if(shipTo门店)
    {
        问题订单List.Add("Ship to为门店订单");
    }
    else{
        if(promotionalEvent == "STOF4"){
            问题订单List.Add("STOF4订单");
        }else if(isReq){
            问题订单List.Add("REQ订单");
            nestleBU = dr["Nestle_BU"].ToString(); // 5, 有可能匹配不上BU
        }else if(promotionalEvent == "CANCEL PO"){
            问题订单List.Add("Cancel PO");
        }
        cleanExceptionDRow["Nestle BU"] = nestleBU;
        /*
        5. 是否为手工单：通过订单沃尔玛产品编码来判断：
        当沃尔玛产品编码 = 21779181  普通散威，在F列填入：散威化
        当沃尔玛产品编码含21402419  吉林散威，在F列填入：散威化-吉林X件
        */
        manualOrderCheck(curOrderDocLinkRows, ref 是否为手工单);
        // row 赋值
        cleanExceptionDRow["是否为手工单"] = 是否为手工单;
    
        /* 
        新增暂时检查点（不定期取消此检查点）：
        散威化订单录单后反馈Exception
        Exception　reason：散威化请确认是否出单
        */
        if(是否为手工单.Contains("散威化") && checkBulkWaferException == "1"){
            问题订单List.Add("散威化请确认是否出单");
        }

        if(string.IsNullOrEmpty(dr["Sold_to_Code"].ToString())){
            问题订单List.Add("Sold to 为空");
        }

        if(string.IsNullOrEmpty(dr["Ship_to_Code"].ToString())){
            问题订单List.Add("Ship to 为空");
        }
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
        
        bool skipNext = false;
        /*
        2.问题订单的判断及登记
        问题订单，SAP会自动屏蔽，不会进入Idoc和SAP，需反馈给Facing加折扣重新推单
        判断条件：
        1.订单产品明细部分没有4.0% EDLC 0.7% Not RTV的折扣信息，只有13%税率和1.3%的仓佣
        2.沃尔玛订单Promotional Event字段为POS REPLEN
        
        满足判断条件其中一个即为问题订单，一般会同时出现，将订单号，没有折扣
        */
        // string allitemInstructions = dr["all_item_instructions"].ToString();
        bool 无折扣 = allitemInstructionsList.Contains("4.0% EDLC 0.7% Not RTV"); // 4.0% EDLC 0.7% Not RTV
        if(promotionalEvent == "POS REPLEN"){
            问题订单List.Add("问题订单,POS REPLEN");
            if(!不录单订单列表.Contains(dr["order_number"].ToString())){
                不录单订单列表.Add(dr["order_number"].ToString());
            }
            skipNext = true;
        }
        if(!无折扣){
            问题订单List.Add("问题订单,无折扣");
            if(!不录单订单列表.Contains(dr["order_number"].ToString())){
                不录单订单列表.Add(dr["order_number"].ToString());
            }
        }
        
        if(skipNext){
            return;
        }
        /*
          客户指定送货日不在行程日
          1. Promotional Event为REQ时 
          2. ship to地址为7400KMDC时
          1和2满足任意一个，都需要检查，1和2都不含的都不用检查行程
        */
        if(isReq || isKMDC){
            string 周几 = caculateWeekDay(MABDDate);
            List<string> dayStringList = shipLocationDic(requestDeliveryDate);
            bool validDate = false;
            foreach(string item in dayStringList){
                //Console.WriteLine($"--- item:{item}");
                if(item.Contains("下周")){
                    string weekDayStr = item.Replace("下", "");
                    DateTime dataNow = Convert.ToDateTime(timeNow.ToString("yyyy-MM-dd"));
                    DateTime nexWeekDayDate = nextWeekDate(dataNow, weekDayMapping(weekDayStr));
                    if(周几 == weekDayStr && DateTime.Compare(MABDDate, nexWeekDayDate) >= 0){
                        validDate = true;
                        break; 
                    }
                }else{
                    if(dayStringList.Contains(周几)){
                        validDate = true;
                        break;
                    }
                }
            }
            //Console.WriteLine($"周几: {周几}");
            if(!validDate){
                问题订单List.Add("客户指定送货日不在行程日");
            }
        }
        
        /*  
        跨月订单， MABD为下个月
        除KM DC(昆明DC)之外的七个大仓当月订单的MABD显示在下个月的订单，判断为问题订单，备注原因跨月订单反馈
        */
        DateTime orderCreateDateTime = Convert.ToDateTime(dr["create_date_time"]);
        bool mabdInNextMonth = (MABDDate.ToString("yyyy-MM") == orderCreateDateTime.AddMonths(1).ToString("yyyy-MM"));
       // Console.WriteLine("mabdInNextMonth: {0}， isKMDC: {1}", mabdInNextMonth, isKMDC);
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
        
        if(isKMDC){
            // 如果时KMDC，并且客户指定送货日在行程日 再去判断指定送货日是否能满足
            if(!问题订单List.Contains("客户指定送货日不在行程日")){
                // 周五17:30之后 到 下周二17：30之间的订单，送货日 最早需要在下周一
                // 周二17：30到周五17：30读到的单，送货日 最早需要下周三
                int todayDayOfWeek = (int)timeNow.DayOfWeek;
                
                //周二17：30到周五17：30读到的单，送货日 最早需要下周三
                // 先判断dayofWeek是否是周二到周五
            
                if(todayDayOfWeek >= 2 && todayDayOfWeek <= 5){ //  周二当天17:30前读到的订单已经包含在这里面
                    bool invalidRDD = beforeFriday17Judge(timeNow, MABDDate);
                    if(invalidRDD){
                       问题订单List.Add("客户指定送货日无法满足"); 
                    }
                }else if(todayDayOfWeek == 1 ){ // 周二17:30前读到的订单，最早下周一可满足；即订单MABD≥下周一为Clean，否则为Exception
                    bool invalidRDD = beforeTuesday17Judge(timeNow, MABDDate);
                    if(invalidRDD){
                        问题订单List.Add("客户指定送货日无法满足");
                    }
                }
            }
        }
        else{ // 中午12:00前读到的订单，最早可以满足Day+1的送货日, 即订单MABD≥读单当天+1为Clean，否则为Exception，属于无法满足的送货日。
            DateTime noonTime = DateTime.Parse(timeNow.ToString("yyyy-MM-dd 12:00:00"));
            DateTime timeNowDate = DateTime.Parse(timeNow.ToString("yyyy-MM-dd"));
            // 如果是周五得话，day + 1 需要多加两天，考虑周六日
            if((int)timeNow.DayOfWeek == 5){
                timeNowDate = timeNowDate.AddDays(2);
            }
            bool beforeNoonNotValid = DateTime.Compare(timeNow, noonTime) <= 0 && MABDDate < timeNowDate.AddDays(1);  // 12点前 and MABD < T+1
            bool afterNoonNotValid = DateTime.Compare(timeNow, noonTime) == 1 && MABDDate < timeNowDate.AddDays(2);  // 12点后 and MABD < T+2
        
            if(beforeNoonNotValid || afterNoonNotValid){
                问题订单List.Add("客户指定送货日无法满足");
            }
        }
    }
}
//在这里编写您的函数或者类

public void manualOrderCheck(DataRow[] curOrderDocLinkRows, ref string 是否为手工单){
    bool 包含吉林散威化 = false;
    string 吉林散威化描述 = string.Empty;
    foreach(DataRow bulkWalferDR in bulkWalferConfigDT.Rows){
        string 散威化产品码 = bulkWalferDR["customer_product_code"].ToString();
        string bulk_walfer_type = bulkWalferDR["bulk_walfer_type"].ToString().Trim();
        foreach(DataRow itemRow in curOrderDocLinkRows){
            string prodCode = itemRow["customer_product_code"].ToString();
            string itemQty = itemRow["quantity_ordered"].ToString();
           // Console.WriteLine("bulk_walfer_type: {0}, 散威化产品码: {1}， prodCode: {2}", bulk_walfer_type, 散威化产品码, prodCode);
            if(prodCode == 散威化产品码){
                if(bulk_walfer_type == "散威化"){
                    是否为手工单 = "散威化";
                }else if(bulk_walfer_type == "散威化-吉林"){
                    包含吉林散威化 = true;
                    吉林散威化描述 = string.Format("{0}{1}件", bulk_walfer_type, itemQty);;
                }
            } 
        }
    }
   // Console.WriteLine("包含吉林散威化: {0}, 吉林散威化描述: {1}", 包含吉林散威化, 吉林散威化描述);
    if(包含吉林散威化){
        是否为手工单 = 吉林散威化描述;
    }
}

public bool beforeTuesday17Judge(DateTime timeNow, DateTime MABDDate){
    int todayDayOfWeek = (int)timeNow.DayOfWeek;
    int dayOfTuesday = (int)DayOfWeek.Tuesday;
    int gap = todayDayOfWeek - dayOfTuesday; // 当天到周二的相距天数
    DateTime tuesdayDate = timeNow.AddDays(-gap);
    tuesdayDate = DateTime.Parse(tuesdayDate.ToString("yyyy-MM-dd 18:30:00")); // 
    
    if(timeNow <= tuesdayDate){ //周二17:30前读到的订单
        DateTime nextMonday = tuesdayDate.AddDays(6); // 下周一
        int result = String.Compare(MABDDate.ToString("d"), nextMonday.ToString("d")); // yyyy-MM-dd
        if( result == -1){ // 送货日小于下周一，记录 exception
           // Console.WriteLine("周二17:30前读到的订单，最早下周一可满足");
            return true;
        }
    }
    return false;
}

public bool beforeFriday17Judge(DateTime timeNow, DateTime MABDDate){
    int todayDayOfWeek = (int)timeNow.DayOfWeek;
    int dayOfTuesday = (int)DayOfWeek.Tuesday;
    int gap = todayDayOfWeek - dayOfTuesday; // 当天到周二的相距天数
    DateTime tuesdayDate = timeNow.AddDays(-gap);
    DateTime fridayDate = tuesdayDate.AddDays(3);
    
    tuesdayDate = DateTime.Parse(tuesdayDate.ToString("yyyy-MM-dd 18:30:00"));
    fridayDate = DateTime.Parse(fridayDate.ToString("yyyy-MM-dd 18:30:00")); // ? 这个时间待确认，是否可以改成23：59：59？
    if(timeNow > tuesdayDate && timeNow <= fridayDate){ // 如果是周二17：30到周五17：30读到的单
        DateTime nextWednesday = tuesdayDate.AddDays(8); // 下周三
        int result = String.Compare(MABDDate.ToString("d"), nextWednesday.ToString("d")); // yyyy-MM-dd
        if( result == -1){ // 送货日小于下周三，记录 exception
           // Console.WriteLine("周二18:00到周五18:00读到的订单，最早下周三可满足");
            return true;
        }
    }else if(timeNow <= tuesdayDate){ // 周二当天，小于17：30
        return beforeTuesday17Judge(timeNow, MABDDate);
    }
    else if(timeNow > fridayDate){ // 周五当天， 大于17：30
        // TODO
    }
    return false;
}


public string caculateWeekDay(DateTime dtNow)
{
    var weekdays = new string[] { "周日", "周一", "周二", "周三", "周四", "周五", "周六" };
    return weekdays[(int)dtNow.DayOfWeek];
}


public int weekDayMapping(string weekStr)
{
    string[] weekdays = new string[] { "周日", "周一", "周二", "周三", "周四", "周五", "周六" };
    int index = 0;
    foreach (string weekDay in weekdays)
    {
        if (weekStr == weekDay)
        {
            return index;
        }
        index += 1;
    }
    return -1;
}

public List<string> shipLocationDic(string requestDeliveryDate){
   string[] rddArr = requestDeliveryDate.Split(new string[]{"/"}, StringSplitOptions.RemoveEmptyEntries);
  
    List<string> dayStringList = new List<string> { };
    foreach(string day in rddArr)
    {
        string newDay = day.Replace(" ", "");
        dayStringList.Add(newDay);
    }
    return dayStringList;
}

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
    分仓行["起送日"] = Convert.ToDateTime(cleanExceptionDRow["起送日"]).ToString("yyyy/MM/dd");
    分仓行["MABD"] = Convert.ToDateTime(cleanExceptionDRow["MABD"]).ToString("yyyy/MM/dd");
    分仓行["客户名称"] = curCustomerName;
}

public static decimal toDecimalConvert(object srcValue){
    Decimal nestle_NPS = 0;
    try{
        nestle_NPS = Convert.ToDecimal(srcValue);
    }catch(Exception e){
       // Console.WriteLine($"转换成decimal价格出错，{srcValue}");
    }
    return nestle_NPS;
}

public static int toIntConvert(object srcValue){
    int intValue = 0;
    try{
        intValue = Convert.ToInt32(srcValue);
    }catch(Exception e){
       // Console.WriteLine($"转换成int32出错，{srcValue}");
    }
    return intValue;
}

public DateTime nextWeekDate(DateTime 参照日, int weekDay)
{
    DateTime 统计开始时间 = 参照日.AddDays(6);
    var dayOfWeek = 统计开始时间.DayOfWeek;
    DateTime nextWeekDay =  统计开始时间.AddDays(-(int)dayOfWeek + weekDay);
    return nextWeekDay;
}