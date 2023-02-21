//代码执行入口，请勿修改或删除
public string[] 录单仓库 = new string[] {"4816", "4874"};
const string checkPriceOption = "检查价差";
const string checkCustomerCodeOption = "备注客户店内码";
const string checkProductSizeOption = "客户店内码的原单规格";
const string orderChangeQtyDescription = "订单修改产品数量";
const string itemQtyNotIntegerDesc = "订单数量转换后不为整数";
const string issueSeparator = ";";
public List<string> nestleSpecialCoffeCodeLs = new List<string>{"981047661", "981061720"};
public int nestleSpecialCoffeCodeMultiple = 5;

public string 产品主数据匹配描述 = "无法匹配雀巢主数据";

public void Run()
{
    exceptionByPODT = byPO模板数据表.Clone();
    AddMoreColumns(); // Clean and Exceltion BY ORder and Item 全包括
    if(增量订单关联数据表 == null || 增量订单关联数据表.Rows.Count == 0){
        return;
    }
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
        DataRow 分仓行 = 分仓明细数据表.NewRow();
        // Order 不重复的时候，判断order异常信息
        
        // EX2O是否包含当前订单，如果包含，则无需再次录单
        checkExistingEX2O(order_number);

        bool isNewOrder = CheckNewOrder(order_number);
        if(isNewOrder) cleanExceptionDRow["是否新单"] = "是";

        // 判断ship To 门店订单
        string location = curOrderRow["location"].ToString();
        // WM locations 不包含当前订单的location 则是门店订单异常
        bool shipTo门店 = !WMLocationsList.Contains(location);

        // By Order的异常判断
        handleExceptionRow(curOrderRow, ref cleanExceptionDRow, ref 问题订单List, curOrderDocLinkRows, shipTo门店);
        if(shipTo门店){
            cleanExceptionDRow["Item Type"] = "Order";
            cleanExceptionDRow["order category"] = (问题订单List.Count > 0) ? "exception" : "clean";
            cleanExceptionDRow["Exception category"] = string.Join(issueSeparator, 问题订单List);
            cleanExceptionDRow["Exception reason"] = cleanExceptionDRow["Exception category"];
    
            exceptionByPODT.Rows.Add(cleanExceptionDRow);
            continue; // 继续下一条
        }
        // 设置分仓明细表每行的信息
        setRowValueForDC(ref 分仓行, cleanExceptionDRow);
        
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
            //foreach(DataRow dr in tmpOrderDT.Rows){
           //     Console.WriteLine(string.Join("--", dr.ItemArray));
           // }
            
            repeatedOrderException(curOrderRow, prevOrderRow, ref cleanExceptionDRow, ref 问题订单List, ref 分仓行, tmpOrderDT.Rows); 
        }
    
        
         /* 待拆分产品判断 
         980064918 （6个口味平分） 星巴克胶囊咖啡6盒装， 拆分比例 6200071：6200571：6200671 = 1：1：1，这个不是普通意义上的平分，因为雀巢跟山姆的箱规不一致，比如 山姆箱规是60，雀巢的是20，所以这个是1份分成雀巢的3份, 后调整为0.7
        */
        List<string> bulk_walfer_codes = new List<string>{};
        foreach(DataRow dr in bulkWalferConfigDT.Rows){
            string customer_product_code = dr["customer_product_code"].ToString();
            bulk_walfer_codes.Add(customer_product_code);
        }
        // By Item 检查exception
        List<string> refItemExceptionList = new List<string>{};
        int final_line_item = 1;
        foreach(DataRow dr in curOrderDocLinkRows){
            List<string> itemExceptionList = new List<string>{};
            string productCode = dr["customer_product_code"].ToString();
            DataRow byPOItemRow =  exceptionByPODT.NewRow();
            byPOItemRow.ItemArray = cleanExceptionDRow.ItemArray;
            
            int quantity_ordered = toIntConvert(dr["quantity_ordered"]);
            if(quantity_ordered == 0){
                itemExceptionList.Add("产品数量为0");
            }
           
            byPOItemRow["Customer order Item"] = dr["line_number"];
            specialProductCheck(dr, ref itemExceptionList, ref byPOItemRow, prevOrderDocLinkRows, 问题订单List);
            
            bool multipleHasException = false;
            // 如果是1对多产品，检查下数量是否为整数
             if(bulk_walfer_codes.Contains(productCode)){
                multipleHasException = checkMultipleProducts(dr, itemExceptionList, cleanExceptionDRow);
            }

            // 汇总exception信息
            if(itemExceptionList.Count > 0){
                byPOItemRow["Exception reason"] = string.Join(issueSeparator, itemExceptionList);
                byPOItemRow["order category"] = "exception";
                Console.WriteLine(string.Join("**", byPOItemRow.ItemArray));
                if(!multipleHasException) exceptionByPODT.Rows.Add(byPOItemRow);  // by Item 的异常订单
                refItemExceptionList = refItemExceptionList.Union(itemExceptionList).ToList();
            }
        }

        List<string> finalItemExceptionList = itemExceptionUniq(refItemExceptionList);
        
        cleanExceptionDRow["Item Type"] = "Order";
        cleanExceptionDRow["order category"] = (问题订单List.Count > 0 || finalItemExceptionList.Count > 0) ? "exception" : "clean";
        string onlyItemException = (问题订单List.Count == 0 && finalItemExceptionList.Count > 0) ? "1" : "0"; // 订单无异常，ITEM有异常，就不需要把item异常再加一遍进最终的异常输出表里面去，因为再下面108行已经合并了两种类型的异常
        cleanExceptionDRow["onlyItemException"] = onlyItemException;
        cleanExceptionDRow["Exception category"] = string.Join("; ", 问题订单List.Union(finalItemExceptionList));
        cleanExceptionDRow["Exception reason"] = cleanExceptionDRow["Exception category"];

        exceptionByPODT.Rows.Add(cleanExceptionDRow);
        
        // POS REPLEN 的订单不进分仓明细表, 2021-12-27 by mengfanling, 只有沃尔玛的POS REPLEN不进分仓明细
        // if(分仓行["订单类型（Promotional Event）"].ToString() != "POS REPLEN"){
        if(cleanExceptionDRow["Exception category"].ToString().ToUpper().Contains("CVP")){
            分仓行["订单修改信息"] = string.IsNullOrEmpty(分仓行["订单修改信息"].ToString()) ? cleanExceptionDRow["Exception category"] : (分仓行["订单修改信息"].ToString() + "; " + cleanExceptionDRow["Exception category"].ToString());
        }
        分仓明细数据表.Rows.Add(分仓行);
       // }
    }
    
    buildByPODT();
    buildByPOAndItemDT();
    // Convert.ToInt32("a.b");
}

public List<string> itemExceptionUniq(List<string> refItemExceptionList){
    string allItemsExceptionStr = string.Join(issueSeparator, refItemExceptionList);
    string[] allItemsExceptionArr = allItemsExceptionStr.Split(new string[]{ issueSeparator }, StringSplitOptions.RemoveEmptyEntries);
    List<string> finalItemExceptionList = new List<string>{};
    foreach(string item in allItemsExceptionArr){
        if(!finalItemExceptionList.Contains(item)){
            finalItemExceptionList.Add(item);
        }
    }
    return finalItemExceptionList;
}

// TODO, 雀巢数量转换后
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
                    itemExceptionRow["雀巢数量"] = fetchQty( dr["quantity_ordered"], dr["customer_product_code"].ToString(), dr["Nestle_Material_No"].ToString(), samQtyMappingDT);
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
    itemExceptionRow["Exception category"] = orderChangeQtyDescription;
    itemExceptionRow["Customer order Item"] = dr["line_number"];
    itemExceptionRow["沃尔玛产品编码"] = dr["customer_product_code"];
    itemExceptionRow["雀巢产品编码"] = dr["Nestle_Material_No"];
    itemExceptionRow["Material Description"] = dr["Material_Description"];
    itemExceptionRow["Nestle BU"] = dr["Nestle_BU"];    
}


public void checkExistingEX2O(string order_number){
    if(existingEX2ODT!=null && orderJobHistoryDT!=null){
        bool inEX2O = existingEX2ODT.AsEnumerable().Cast<DataRow>().Any(dRow => dRow["PO_Number"].ToString().Contains(order_number));
        // bool inEX2O = existingEX2ODT.AsEnumerable().Cast<DataRow>().Any(dRow => dRow["Customer_Order_Number"].ToString() == order_number);

        bool inOrderJobHistory = orderJobHistoryDT.AsEnumerable().Cast<DataRow>().Any(dRow => dRow["order_number"].ToString() == order_number);
        if(inEX2O && inOrderJobHistory){
            不录单订单列表.Add(order_number);
        }
    }
}

// 检查是否新单
public bool CheckNewOrder(string order_number){
    if(exportedOrdersDT!=null){
        bool isOldOrder = exportedOrdersDT.AsEnumerable().Cast<DataRow>().Any(dRow => dRow["order_number"].ToString() == order_number);
        return !isOldOrder;
    }
    return true;
}

/*
山姆1：N产品，比例是 1：1：1, 假设山姆的产品Qty为180，那么需要衍生出3条产品行数据，每一行的Qty都是180. 于 2022/4/15改成  0.7：0.7：0.7
*/
public bool checkMultipleProducts(DataRow dr, List<string> itemExceptionList, DataRow cleanExceptionDRow){
    string productCode = dr["customer_product_code"].ToString();
    int quantity_ordered = toIntConvert(dr["quantity_ordered"]);
    bool addToException = false;
    DataRow bulkWalferProduct = bulkWalferConfigDT.Select("customer_product_code='" + productCode + "'")[0];
    string nestleCodeAllocation = bulkWalferProduct["nestle_code_allocation"].ToString();
    string allocationRatio = bulkWalferProduct["allocation_ratio"].ToString();
    string[] nestleCodeArr = nestleCodeAllocation.Split(new string[]{",", "，"}, StringSplitOptions.RemoveEmptyEntries);
    string[] allocationRatioArr = allocationRatio.Split(new string[]{"：", ":"}, StringSplitOptions.RemoveEmptyEntries); // 注意： 是中文冒号
    for(int i=0; i< allocationRatioArr.Length; i++){
        string ratioStr = allocationRatioArr[0];
        string 雀巢产品编码 = nestleCodeArr[i];
        DataRow[] bulkProductDrs = oneToNNestleProductsDT.Select(String.Format("Nestle_Plant_No='{0}' and Nestle_Material_No='{1}'", dr["location"].ToString(), 雀巢产品编码));
        if(bulkProductDrs.Length == 0){
           addExceptionRow(cleanExceptionDRow);
           addToException = true;
           continue;
        }
        string material_description = bulkProductDrs[0]["Material_Description"].ToString();
        string remark = bulkProductDrs[0]["Remark"].ToString();
        string remarkOption = bulkProductDrs[0]["Remark_Option"].ToString();

        decimal rationValue = toDecimalConvert(ratioStr);
        decimal curQuantity_ordered = quantity_ordered * rationValue;
        
        decimal finalQtyD = fetchQty(quantity_ordered, productCode, 雀巢产品编码, samQtyMappingDT);
        int finalQty = toIntConvert(Math.Floor(finalQtyD));
        
        // int finalQty = Convert.ToInt32(Math.Floor(curQuantity_ordered));
        
        Console.WriteLine("quantity_ordered:{0}, curQuantity_ordered: {1}, finalQty: {2}", quantity_ordered, curQuantity_ordered, finalQty);
        if(curQuantity_ordered != toIntConvert(curQuantity_ordered)){
            DataRow byPOItemRow =  exceptionByPODT.NewRow();
            byPOItemRow.ItemArray = cleanExceptionDRow.ItemArray;
            initExceptionItemRow(ref byPOItemRow, dr);
            byPOItemRow["修改后订单数量"] = null;
            byPOItemRow["雀巢产品编码"] = 雀巢产品编码;
            byPOItemRow["沃尔玛产品编码"] = productCode;
            if(i==0){
                byPOItemRow["原订单数量"] = quantity_ordered;
            }else{
                byPOItemRow["原订单数量"] = null;
            }
            byPOItemRow["雀巢数量"] = finalQty;
            byPOItemRow["Material Description"] = material_description;
            if(!itemExceptionList.Contains(itemQtyNotIntegerDesc)) itemExceptionList.Add(itemQtyNotIntegerDesc);
            byPOItemRow["Exception reason"] = string.Join(issueSeparator, itemExceptionList);
            exceptionByPODT.Rows.Add(byPOItemRow);
            addToException = true;
        }
        
        if(remark.Contains("特殊产品")){
            remarkOption = remarkOption.Replace(issueSeparator, "").Replace("；", "").Trim();
            if(!string.IsNullOrEmpty(remarkOption) && !remarkOption.Contains("JD氨糖")){ // remarkOption 不为空，则也添加到异常描述里面
                DataRow byPOItemRow =  exceptionByPODT.NewRow();
                byPOItemRow.ItemArray = cleanExceptionDRow.ItemArray;
                initExceptionItemRow(ref byPOItemRow, dr);
                byPOItemRow["雀巢数量"] = finalQty;
                byPOItemRow["雀巢产品编码"] = 雀巢产品编码;
                byPOItemRow["Material Description"] = material_description;
                byPOItemRow["Item Type"] = "Item";
                if(!itemExceptionList.Contains("特殊产品需检查订单," + remarkOption)) itemExceptionList.Add("特殊产品需检查订单," + remarkOption);
                byPOItemRow["Exception reason"] = string.Join(issueSeparator, itemExceptionList);
                exceptionByPODT.Rows.Add(byPOItemRow);
                addToException = true;
            }
        }
    }
    return addToException;
}

public void addExceptionRow(DataRow cleanExceptionDRow){
    string exceptionMessage = 产品主数据匹配描述;
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

// 产品行检查折扣
public void specialProductCheck(DataRow dr, ref List<string> refItemExceptionList, ref DataRow byPOItemRow, DataRow[] prevOrderDocLinkRows, List<string> 问题订单list){
    List<string> itemExceptionList = new List<String>{};
    
    string remark = dr["Remark"].ToString();
    string remarkOption = dr["Remark_Option"].ToString(); // Remark_Option 1. 需检查以下客户店内码的原单规格与雀巢的产品规格是否一致 2. 检查价差  3.备注客户店内码
    string 沃尔玛产品编码 = dr["customer_product_code"].ToString();
    string location = dr["location"].ToString();
    byPOItemRow["沃尔玛产品编码"] = 沃尔玛产品编码;
    byPOItemRow["雀巢产品编码"] = dr["Nestle_Material_No"];
    byPOItemRow["Material Description"] =  dr["Material_Description"];
    byPOItemRow["Nestle BU"] = dr["Nestle_BU"];
    decimal nestleNPS = toDecimalConvert(dr["Nestle_NPS"].ToString());
    Console.WriteLine("--------------雀巢产品编码{0}----", byPOItemRow["雀巢产品编码"]);

    if(String.IsNullOrEmpty(byPOItemRow["雀巢产品编码"].ToString())){
        byPOItemRow["Item Type"] = "Item";
        itemExceptionList.Add(产品主数据匹配描述);
        // return;
    }else{
        if(remark.Contains("特殊产品")){
            string pack = dr["pack"].ToString(); // 客户网站是 12 / 12
            if(pack.Contains("/")){
                string[] packArr = pack.Split(new string[]{"/"}, StringSplitOptions.RemoveEmptyEntries);
                pack = packArr[0];
            }
            bool 检查价差 = remarkOption.Contains(checkPriceOption);
            if(检查价差){
                bool skipCheckPrice = false;
                decimal cost = Math.Round(toDecimalConvert(dr["cost"]), 2); // 沃尔玛价格
                /*        于2022-04-15被MFL注释，因为客户现在不用跳过产品价格检查了
                
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
                */
                
                if(!skipCheckPrice && cost != nestleNPS){
                // Console.WriteLine("--价差订单---");
                    byPOItemRow["沃尔玛价格"] = cost;
                    byPOItemRow["雀巢价格"] = nestleNPS;
                    byPOItemRow["原单箱规"] = pack;
                    byPOItemRow["Item Type"] = "Item";
                    itemExceptionList.Add("价差订单");
                }
                remarkOption = remarkOption.Replace(checkPriceOption, "");
            }

            bool 备注客户店内码 = remarkOption.Contains(checkCustomerCodeOption);
            if(备注客户店内码){
                byPOItemRow["Item Type"] = "Item";
                itemExceptionList.Add("特殊产品需检查订单," + remarkOption);
                remarkOption = remarkOption.Replace(checkCustomerCodeOption, "");
            }

            bool 客户店内码的原单规格 = remarkOption.Contains(checkProductSizeOption);
            if(客户店内码的原单规格){
                byPOItemRow["原单箱规"] = pack;
                byPOItemRow["Item Type"] = "Item";
                itemExceptionList.Add("特殊产品需检查订单," + remarkOption);
                remarkOption = remarkOption.Replace(checkProductSizeOption, "");
            }

            remarkOption = remarkOption.Replace(issueSeparator, "").Replace("；", "").Trim();
            if(!string.IsNullOrEmpty(remarkOption) && !remarkOption.Contains("JD氨糖")){ // remarkOption 不为空，则也添加到异常描述里面
                byPOItemRow["Item Type"] = "Item";
                itemExceptionList.Add("特殊产品需检查订单," + remarkOption);
            }
        }
    }

    if(!录单仓库.Contains(location) && remark.Contains("不填充ex2o")){
        byPOItemRow["Item Type"] = "Item";
        itemExceptionList.Add("未录单，水仓+干货产品");
    }
    /* 
      产品行折扣
      =Promotional Allowance/Extended Cost
      产品行的折扣率不等于各产品扣点[产品主数据有维护]（有一个产品不符合就是exception） ，反馈Exception，同时也要放入excel to Order
    */
    string 产品扣点 = dr["产品扣点"].ToString();
    if(!string.IsNullOrEmpty(产品扣点)){
        decimal productDiscountRate = Math.Round(fetchRateInDecimal(产品扣点), 3);
        decimal oli_extended_cost = toDecimalConvert(dr["oli_extended_cost"]);
        if(oli_extended_cost == 0){
            oli_extended_cost = decimal.MaxValue;
        }
        // Console.WriteLine("oli_allowance_total:{0}, oli_extended_cost: {1}", dr["oli_allowance_total"].ToString(), oli_extended_cost);
        decimal 产品行折扣 = Math.Round(toDecimalConvert(dr["oli_allowance_total"])/oli_extended_cost, 3);
        if(Math.Abs(productDiscountRate) != Math.Abs(产品行折扣)){
            byPOItemRow["Item Type"] = "Item";
            byPOItemRow["雀巢数量"] = dr["quantity_ordered"];
           
            // Console.WriteLine(string.Format("产品行折扣率不等于扣点，产品行折扣: {0}, 产品扣点: {1}", Math.Abs(产品行折扣), productDiscountRate));
            itemExceptionList.Add(string.Format("产品行折扣率不等于扣点，产品行折扣: {0}, 产品扣点: {1}", Math.Abs(产品行折扣), productDiscountRate));
        }
    }
    
    // 山姆与雀巢产品数量对照
    /*
    由于部分产品山姆和雀巢的规格不一致，需要按照比例进行转换，会存在雀巢数量不为整数的情况：
    1, 山姆产品980070675（雀巢营养谷物迷你装）对应雀巢产品数量不为整数时，按照以下例子计算逻辑计算并录单。录单完毕后再作为exception反馈出来备注原订单数量是多少，录单后数量是多少
    
    例:
    当山姆原单数量为2507时（山姆一箱对应雀巢0.6箱），雀巢数量为 2507*0.6=1504.2，不为整数，因此需要自主换算山姆箱数，
    固定规则 : 按照山姆整层30箱为标准送货
    --2507/30=83.56（向下取整数83）
    --83*30=2490箱
      即按照山姆数量2490箱出单
    
    2, 其余产品换算雀巢数量不为整数的情况不录单反馈Exception
    */
    // 存在数据转换的话，就去检查箱数是否为整数
    decimal final_quantity = toDecimalConvert(dr["quantity_ordered"]);
    DataRow qtyMappingRow = null;
    if(samQtyMappingDT!=null){
        DataRow[] qtyMappingRows = samQtyMappingDT.Select(string.Format("Sam_Product_Code='{0}' and Nestle_Product_Code='{1}'", dr["customer_product_code"].ToString(), dr["Nestle_Material_No"].ToString()));
        if(qtyMappingRows.Length > 0){
            qtyMappingRow = qtyMappingRows[0];
        
            final_quantity = fetchQty(dr["quantity_ordered"], qtyMappingRow, ref itemExceptionList, true);
            byPOItemRow["雀巢数量"] = toIntConvert(Math.Floor(final_quantity));
            
            Decimal cost = toDecimalConvert(dr["cost"]);
            // Cost_Check_Value 设置了值
            if(!string.IsNullOrEmpty(qtyMappingRow["Cost_Check_Value"].ToString())){
                Decimal costCheckValue = toDecimalConvert(qtyMappingRow["Cost_Check_Value"]);
                if(costCheckValue != cost){ // 客户网站cost跟设定的不match，则不录单
                  itemExceptionList.Add($"{byPOItemRow["沃尔玛产品编码"]}成本为{Math.Round(cost, 2)}，与雀巢Cost{Math.Round(costCheckValue, 2)}不一致");
                }
            }
        }
    }
    
    /*
      山姆京东类型的订单如下单产品为氨糖奶粉(客户码：980086618)，不录单作为exception order发给CSA取消
      Exception reason：不录单，山姆JD+氨糖奶粉
    */
     if(remark.Contains("特殊产品") && remarkOption.Contains("氨糖")){ // 录单仓库.Contains(dr["WMDC"].ToString()) &&，条件于20220425去除
        byPOItemRow["Item Type"] = "Item";
        if(dr["promotional_event"].ToString().Contains("JD")){
            itemExceptionList.Add(remarkOption); // "不录单，山姆JD+氨糖奶粉"
        }else{
            itemExceptionList.Add("含氨糖奶粉");
        }
     }

     Console.WriteLine("--------------------问题订单List: {0}", string.Join("|", 问题订单list));
    // 如果整个订单数量修改的话， 需要记录单个item变化的部分
    string orderExceptionListStr = string.Join("|", 问题订单list);
    if(orderExceptionListStr.Contains(orderChangeQtyDescription) || orderExceptionListStr.Contains("Total Order Amount")){
        string customerProductCode = dr["customer_product_code"].ToString();
        //Console.WriteLine("customerProductCode: {0}", customerProductCode);
        
        DataRow prevItemRow = null;
        foreach(DataRow prevDr in prevOrderDocLinkRows){
            //Console.WriteLine("prevDr customerProductCode: {0}", prevDr["customer_product_code"].ToString());
            if(customerProductCode == prevDr["customer_product_code"].ToString()){ // 找到对应de 前一个order的item
                prevItemRow = prevDr;
                Console.WriteLine("---------原订单数量: {0}, 修改后订单数量: {1}", prevItemRow["quantity_ordered"], dr["quantity_ordered"]);
                break;
            }
        }
    
        // 之前item不等于当前item对应的数量
        if(prevItemRow!=null && prevItemRow["quantity_ordered"].ToString() != dr["quantity_ordered"].ToString()){
            byPOItemRow["原订单数量"] = prevItemRow["quantity_ordered"];
            decimal finalPrevQuantity = qtyMappingRow!=null ? fetchQty(prevItemRow["quantity_ordered"], qtyMappingRow, ref itemExceptionList, false) : toDecimalConvert(prevItemRow["quantity_ordered"]);
            
            
            byPOItemRow["修改后订单数量"] = dr["quantity_ordered"];
            itemExceptionList.Add($"订单修改产品数量 ，原订单雀巢数量：{toIntConvert(finalPrevQuantity)}， 改单后雀巢数量：{toIntConvert(final_quantity)}");
        }else{
            byPOItemRow["原订单数量"] = null;
        }
    }
    refItemExceptionList = refItemExceptionList.Union(itemExceptionList).ToList();

    if(itemExceptionList.Count > 0){
        byPOItemRow["Exception reason"] = string.Join("; ", itemExceptionList);
        byPOItemRow["order category"] = "exception";
    }
}

public decimal fetchQty(object originalQty, DataRow qtyMappingRow, ref List<string> itemExceptionList, bool intoException){
    decimal customerOrderQty = toDecimalConvert(originalQty);
    if(nestleSpecialCoffeCodeLs.Contains(qtyMappingRow["Sam_Product_Code"].ToString())){
        if(customerOrderQty/nestleSpecialCoffeCodeMultiple != Math.Floor(customerOrderQty/nestleSpecialCoffeCodeMultiple)){
            customerOrderQty = Math.Floor(customerOrderQty/nestleSpecialCoffeCodeMultiple) * nestleSpecialCoffeCodeMultiple;
        }
    }
    string Not_Integer_Still_Into_EX2O = qtyMappingRow["Not_Integer_Still_Into_EX2O"].ToString();
    decimal nestleQty_m = customerOrderQty * toDecimalConvert(qtyMappingRow["Nestle_Qty"]);
    int nestleQtyInt = toIntConvert(nestleQty_m);
    // 换算不为整数则，看产品设定是否录单,反馈exception
    if((nestleQtyInt != nestleQty_m)){
        if(Not_Integer_Still_Into_EX2O == "1"){
            int 山姆整层箱数 = 30;
            int 层数 = toIntConvert(Math.Floor(customerOrderQty/山姆整层箱数));   // TODO: Math.Floor 还是 Math.Round，进位还是去除小数
            decimal quantity_ordered = 层数 * 山姆整层箱数;
            nestleQtyInt = toIntConvert(quantity_ordered * toDecimalConvert(qtyMappingRow["Nestle_Qty"]));
            if(intoException) itemExceptionList.Add($"山姆订单箱数不为整数仍然录单"); // ，原订单雀巢数量：{customerOrderQty}， 改单后雀巢数量：{nestleQtyInt}
            return nestleQtyInt;
        }else{
            if(intoException) itemExceptionList.Add($"山姆订单箱数不为整数不录单");  // ，原订单雀巢数量：{customerOrderQty}， 雀巢对应数量：{nestleQty_m}
            return nestleQty_m;
        }
    }else{
        return nestleQty_m;
    }
}

public decimal fetchQty(object originalQty, string customerProdCode, string nestleProdCode, DataTable samQtyMappingDT){
    DataRow[] qtyMappingRows = samQtyMappingDT.Select(string.Format("Sam_Product_Code='{0}' and Nestle_Product_Code='{1}'", customerProdCode, nestleProdCode));
    decimal customerOrderQty = toDecimalConvert(originalQty);
    if(nestleSpecialCoffeCodeLs.Contains(customerProdCode)){
        if(customerOrderQty/nestleSpecialCoffeCodeMultiple != Math.Floor(customerOrderQty/nestleSpecialCoffeCodeMultiple)){
            customerOrderQty = Math.Floor(customerOrderQty/nestleSpecialCoffeCodeMultiple) * nestleSpecialCoffeCodeMultiple;
        }
    }
    if(qtyMappingRows.Length > 0){
        DataRow qtyMappingRow = qtyMappingRows[0];  
        
        string Not_Integer_Still_Into_EX2O = qtyMappingRow["Not_Integer_Still_Into_EX2O"].ToString();
        decimal nestleQty_m = customerOrderQty * toDecimalConvert(qtyMappingRow["Nestle_Qty"]);
        int nestleQtyInt = toIntConvert(nestleQty_m);
        // 换算不为整数则，看产品设定是否录单,反馈exception
        if((nestleQtyInt != nestleQty_m)){
            if(Not_Integer_Still_Into_EX2O == "1"){
                int 山姆整层箱数 = 30;
                int 层数 = toIntConvert(Math.Floor(customerOrderQty/山姆整层箱数));             // TODO: Math.Floor 还是 Math.Round，进位还是去除小数
                decimal quantity_ordered = 层数 * 山姆整层箱数;
                nestleQtyInt = toIntConvert(quantity_ordered * toDecimalConvert(qtyMappingRow["Nestle_Qty"]));
                return nestleQtyInt;
            }else{
                return nestleQty_m;
            }
        }else{
            return nestleQty_m;
        }
    }else{
        return customerOrderQty;
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
        else
        {
            if (!walmartDiscountRateStr.Contains("%"))
            { // 不包含%
                //Console.WriteLine($"walmartDiscountRateStr: {walmartDiscountRateStr}");
                resutRate = toDecimalConvert(walmartDiscountRateStr);
            }
        }
    }
    catch (Exception e)
    {
        //Console.WriteLine("walmartDiscountRateStr不合法： {0}", e.Message);
    }
    return resutRate;
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
                重复订单信息.Add(orderChangeQtyDescription);
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

public void handleExceptionRow(DataRow dr, ref DataRow cleanExceptionDRow, ref List<string> 问题订单List, DataRow[] curOrderDocLinkRows, bool shipTo门店){
    List<String> allitemproductCodesList = curOrderDocLinkRows.Cast<DataRow>().Select<DataRow, string>(dr => dr["customer_product_code"].ToString()).ToList();

    string po_number = dr["order_number"].ToString();  // 2
    string wmdc = dr["WMDC"].ToString(); // 3
    string promotionalEvent = dr["promotional_event"].ToString(); // 4
    string orderType = dr["order_type"].ToString();
    bool isReq = promotionalEvent.Contains("REQ");
    bool isKMDC = (wmdc == "KMDC");
    string totalLineItems = dr["total_line_items"].ToString();
    string nestleBU = string.Empty;
    string location = dr["location"].ToString();

    initExceptionRow(dr, ref cleanExceptionDRow);

     // 当前仓库不属于录单仓库，且有remark说明 “不填充ex2o”
    if(!录单仓库.Contains(location)){
        不录单订单列表.Add(po_number);
    }
    
    // nestleBU = dr["Nestle_BU"].ToString(); 山姆和山姆水这一列先留空
    if(shipTo门店)
    {
        问题订单List.Add("Ship to为门店订单");
        return;
    }

    if(promotionalEvent == "STOF4"){
        问题订单List.Add("STOF4订单");
    }else if(isReq){
        问题订单List.Add("REQ订单");
    }else if(promotionalEvent == "CANCEL PO"){
        问题订单List.Add("Cancel PO");
    }
    
    /*
    是否为手工单：通过订单产品编码来判断：
    山姆01：980064917，980063938，980064918，980064961，980070675，980086618
    山姆02：980066573，980056574，980061557
    */
    string 是否为手工单 = string.Empty;  // 6
    
    string prodCode = allitemproductCodesList[0];
    
    foreach(DataRow samQtyMappingRow in samQtyMappingDT.Rows){
        string 山姆产品码 = samQtyMappingRow["Sam_Product_Code"].ToString();
        
        if(prodCode == 山姆产品码){
            是否为手工单 = "山姆" + samQtyMappingRow["Distribution_Channel"].ToString();
            break;
        }
    }

    string 是否为稳定库存 = (orderType == "0020") ? "稳定" : ""; // 7
    string orderQty = dr["total_units_ordered"].ToString(); // 8
    DateTime 起送日日期 = Convert.ToDateTime(dr["ship_date"]);
    string 起送日 = 起送日日期.ToString("yyyy/MM/dd"); // 9
    string MABD = Convert.ToDateTime(dr["must_arrived_by"]).ToString("yyyy/MM/dd"); // 10
    DateTime MABDDate = Convert.ToDateTime(dr["must_arrived_by"]);
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

    /*
    客户指定送货日不在行程日
    山姆订单的MABD（订单上显示为Cancel Date标签⑨处）
    1.起送日（ship to date）≤送货日期≤MABD(cancel date)即可
    DC号     送货周期
    4874      周一/周四 
    4816      周一/周四
    在起送日和MABD的日期间，是有周一或周四即可，不符合则放入exception
    */
    if(!string.IsNullOrEmpty(requestDeliveryDate)){
        DateTime shipDateTmp = 起送日日期;
        bool validShipDate = false;
        while(DateTime.Compare(shipDateTmp, MABDDate) < 0){
            string 周几 = CaculateWeekDay(shipDateTmp);
            List<string> dayStringList = shipLocationDic(requestDeliveryDate); // requestDeliveryDate 是ship to 定义的山姆的送货日期
            if(dayStringList.Contains(周几)){
                validShipDate = true;
                break;
            }
            shipDateTmp = shipDateTmp.AddDays(1);
        }
        if(!validShipDate){
            问题订单List.Add("客户指定送货日不在行程日");
        }
    }

    
    /* 价差检查 
    不检查产品单品的价差，录单后检查订单扣点后未税总金额价差：
    订单扣点后未税总金额价差=山姆订单扣点后未税金额-雀巢扣点后未税金额(客户有数据准备)
    
    山姆订单扣点后未税金额：不用自己算，为原单上Total Order Amount (After Adjustments)金额（左图截图所示）
    
    可接受范围：-5≤订单总价差≤+5
    如果超出范围，则放入exception，如果在范围内则放入clean
    */
    
    decimal walmartDiscountRate = customerDiscountRateM();
    //Console.WriteLine($"walmartDiscountRate: {walmartDiscountRate}");
    decimal 不含扣点 = (1-walmartDiscountRate);
    Decimal total_order_amount_after_adjustments = toDecimalConvert(dr["total_order_amount_after_adjustments"]);
    
    decimal sap_net_value = getSapNetValue(dr, allitemproductCodesList, curOrderDocLinkRows, 不含扣点);
    decimal 实际价差 = sap_net_value - total_order_amount_after_adjustments;
    if(Math.Abs(实际价差) > 5){
        问题订单List.Add($"价差检查, SAP NET Value: {Math.Round(sap_net_value, 2)}, 山姆订单扣点后未税金额: {Math.Round(total_order_amount_after_adjustments, 2)}, 价差为：{实际价差}");
    }

    /*  
    跨月订单， MABD为下个月
    除KM DC(昆明DC)之外的七个大仓当月订单的MABD显示在下个月的订单，判断为问题订单，备注原因跨月订单反馈
    */
    DateTime orderCreateDateTime = Convert.ToDateTime(dr["create_date_time"]);
    bool mabdInNextMonth = (MABDDate.ToString("yyyy-MM") == orderCreateDateTime.AddMonths(1).ToString("yyyy-MM"));

    if(mabdInNextMonth){
        问题订单List.Add("跨月订单");
    }

    cleanExceptionDRow["Nestle BU"] = nestleBU;
    // 是否为手工单	是否为稳定库存	Order qty	起送日	MABD	[order category]（现在还判断不了，因为现在是by itemde 计算方式）	[Exception category] 等item确定了才能最终确定
    cleanExceptionDRow["是否为手工单"] = 是否为手工单;
    cleanExceptionDRow["MABD"] = MABD;
    cleanExceptionDRow["新MABD"] = MABD;   
    // cleanExceptionDRow["Customer order Item"] = dr["line_number"].ToString();    
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

public decimal getSapNetValue(DataRow dr, List<string> allitemproductCodesList, DataRow[] curOrderDocLinkRows, decimal 不含扣点){
    Decimal sap_net_value = 0;
    // 如果不属于1对多的那些产品
    string location = dr["location"].ToString();
    string allitemproductCodesStr = string.Join(", ", allitemproductCodesList.ToArray());
    DataRow[] oneToNDrs = oneToNNestleProductsDT.Select(string.Format("Customer_Name='{0}' and Nestle_Plant_No='{1}' and Customer_Material_No in ({2})", curCustomerName, location, allitemproductCodesStr));
    
    //Console.WriteLine("oneToNDrs.Length{0}", oneToNDrs.Length);
    if(oneToNDrs.Length > 0){ // 包含 1：n 产品码
        // 遍历order items
        foreach(DataRow itemDR in curOrderDocLinkRows){
            string customerProdCode = itemDR["customer_product_code"].ToString();
            
            List<DataRow> oneToNSndDrsList = new List<DataRow>{};
            foreach(DataRow oneToNDR in oneToNDrs){
                if(oneToNDR["Customer_Material_No"].ToString() == customerProdCode){
                    oneToNSndDrsList.Add(oneToNDR);
                }
            }
            DataRow[] oneToNSndDrs = oneToNSndDrsList.ToArray();
            decimal itemNestleNPS=0;
            // 如果调价不为空，则使用调价计算
            if(!String.IsNullOrEmpty(itemDR["Adjustive_Price"].ToString())){
                itemNestleNPS = toDecimalConvert(itemDR["Adjustive_Price"]);
            }else if(!String.IsNullOrEmpty(itemDR["Nestle_NPS"].ToString())){
                itemNestleNPS = toDecimalConvert(itemDR["Nestle_NPS"]);
            }
            int itemQuantityOrdered = toIntConvert(itemDR["quantity_ordered"]);
            // 此商品属于客户1：n雀巢产品
            if(oneToNSndDrs.Length > 0){
                foreach(DataRow oneToNProdDr in oneToNSndDrs){
                    decimal oneToNNestleNPS = 0; 
                    string customerProductCode = oneToNProdDr["Customer_Material_No"].ToString();
                    string nestleProdCode = oneToNProdDr["Nestle_Material_No"].ToString();
//Console.WriteLine("itemQuantityOrdered: {0}", itemQuantityOrdered);
                    decimal itemQuantity = fetchQty(itemQuantityOrdered, customerProductCode, nestleProdCode, samQtyMappingDT);
//Console.WriteLine("itemQuantity: {0}", itemQuantity);

                    if(!String.IsNullOrEmpty(oneToNProdDr["Adjustive_Price"].ToString())){
                        oneToNNestleNPS = toDecimalConvert(oneToNProdDr["Adjustive_Price"]);
                    }else{
                        oneToNNestleNPS = toDecimalConvert(oneToNProdDr["Nestle_NPS"]);
                    }
                     Console.WriteLine($"itemNestleNPS: {oneToNNestleNPS}, itemQuantity: {itemQuantity}, 不含扣点: {不含扣点}");
            
                    sap_net_value += oneToNNestleNPS * itemQuantity * 不含扣点;
                }
            }else{
                
                 string nestleProdCode = itemDR["Nestle_Material_No"].ToString();
                 decimal itemQuantity = fetchQty(itemQuantityOrdered, customerProdCode, nestleProdCode, samQtyMappingDT);
                sap_net_value += itemNestleNPS * itemQuantity * 不含扣点;
            }
        }
    }else{
        foreach(DataRow itemDR in curOrderDocLinkRows){
            string customerProdCode = itemDR["customer_product_code"].ToString();
            string nestleProdCode = itemDR["Nestle_Material_No"].ToString();

            int itemQuantityOrdered = toIntConvert(itemDR["quantity_ordered"]);
            decimal itemQuantity = fetchQty(itemQuantityOrdered, customerProdCode, nestleProdCode, samQtyMappingDT);
            decimal itemNestleNPS=0;
            if(!String.IsNullOrEmpty(itemDR["Adjustive_Price"].ToString())){
                itemNestleNPS = toDecimalConvert(itemDR["Adjustive_Price"]);
            }else if(!String.IsNullOrEmpty(itemDR["Nestle_NPS"].ToString())){
                itemNestleNPS = toDecimalConvert(itemDR["Nestle_NPS"]);
            }
            //Console.WriteLine($"333------itemNestleNPS: {itemNestleNPS}, itemQuantity: {itemQuantity}, 不含扣点: {不含扣点}");

            sap_net_value += itemNestleNPS * itemQuantity * 不含扣点;
        }
    }
    return sap_net_value;
}

public decimal customerDiscountRateM(){
    DataRowCollection etoConfigDrs = etoConfigDT.Rows;
    //string walmartDiscountRateStr = etoConfigDrs[0]["discount_rate"].ToString();
    //decimal walmartDiscountRate = fetchRateInDecimal(walmartDiscountRateStr);
    //string walmartDiscountRateStr = etoConfigDrs[0]["discount_rate"].ToString();
    //decimal walmartDiscountRate = fetchRateInDecimal(walmartDiscountRateStr);
    decimal EDLC百分比 = fetchRateInDecimal(etoConfigDrs[0]["EDLC百分比"].ToString());
    decimal RTV百分比 = fetchRateInDecimal(etoConfigDrs[0]["RTV百分比"].ToString());
    decimal 仓租百分比 = fetchRateInDecimal(etoConfigDrs[0]["仓租百分比"].ToString());
   
    return EDLC百分比 + RTV百分比 + 仓租百分比;
} 

public string CaculateWeekDay(DateTime dtNow)
{
    var weekdays = new string[] { "周日", "周一", "周二", "周三", "周四", "周五", "周六" };
    return weekdays[(int)dtNow.DayOfWeek];
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

    //Console.WriteLine(string.Join(" | ", cleanExceptionDRow.ItemArray));
    
    分仓行["读单当天日期"] = cleanExceptionDRow["Order Date"];
    分仓行["PO."] =  cleanExceptionDRow["PO No."];
    分仓行["WMDC"] =  cleanExceptionDRow["WMDC"];
    分仓行["订单类型（Promotional Event）"] = cleanExceptionDRow["Order type（Promotional Event）"];
    分仓行["是否为手工单"] = cleanExceptionDRow["是否为手工单"];
    分仓行["是否为稳定库存"] = cleanExceptionDRow["是否为稳定库存"];
    分仓行["数量"] = cleanExceptionDRow["Order qty"];
    分仓行["起送日"] = Convert.ToDateTime(cleanExceptionDRow["起送日"].ToString()).ToString("yyyy/MM/dd");
    分仓行["MABD"] = Convert.ToDateTime(cleanExceptionDRow["MABD"].ToString()).ToString("yyyy/MM/dd");
    分仓行["客户名称"] = curCustomerName;
}

public static decimal toDecimalConvert(object srcValue){
    Decimal nestle_NPS = 0;
    try{
        nestle_NPS = Convert.ToDecimal(srcValue);
    }catch(Exception e){
        //Console.WriteLine($"转换成decimal价格出错，{srcValue}");
    }
    return nestle_NPS;
}

public static int toIntConvert(object srcValue){
    int intValue = 0;
    try{
        intValue = Convert.ToInt32(srcValue);
    }catch(Exception e){
        //Console.WriteLine($"转换成int32出错，{srcValue}");
    }
    return intValue;
}


// 按比列重分配，山姆谷物
public int[] reAllocateQty(string[] allocationRatioArr, int quantity_ordered){
    List<int> initQtyList = new List<int> {};
    foreach(string ratioStr in allocationRatioArr){
        int rationValue = Convert.ToInt32(ratioStr);
        int curQuantity_ordered = quantity_ordered * rationValue;
        initQtyList.Add(curQuantity_ordered);
    }
    return initQtyList.ToArray();
}