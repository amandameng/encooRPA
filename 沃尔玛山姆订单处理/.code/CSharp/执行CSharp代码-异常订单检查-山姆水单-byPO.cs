//代码执行入口，请勿修改或删除
const string checkPriceOption = "检查价差";
const string checkCustomerCodeOption = "备注客户店内码";
const string checkProductSizeOption = "客户店内码的原单规格";
const string orderChangeQtyDescription = "订单修改产品数量";
const string itemQtyNotIntegerDesc = "订单不为整数";

const string 巴黎水产品码 = "980080954";
const string 圣培露产品码 = "980085511";
const string 山姆水mix产品码 = "981067796";
public string 产品主数据匹配描述 = "无法匹配雀巢主数据";

// 雀巢的箱规是24，山姆箱规是16
public const decimal nestlePackage = 24;
public const decimal samPackage = 16;
public int minSamQty = 6; // 最小下单数量是6，这样才能保证转换到雀巢数量时大于等于1 
const string issueSeparator = ";";
public List<string> samWaterSpecialCodeLs = new List<string>{山姆水mix产品码};

/*
判断规则：

Ø  一个山姆产品编码同时对应以下三个不同雀巢产品编码，当有小数时请向下取整，并需要检查山姆订单数量是否为“6”的倍数。

Ø  当山姆订单数量小于“6”时，不录单不填写ex2o；当订单数量大于“6”时，按照“6”的倍数规则录单，比如数量为10，录单数量仅录6，同时反馈exception；如数量为12，录单数量为12。

Ø  当数量不是“6”的倍数时，需反馈为exception，原因描述为：“产品数量不是“6”的倍数“

Ø  山姆订单中含产品981067796时，整单放入exception，并标记匹配的三个雀巢产品编码及数量
*/


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
        
        // 整托和起送量的判断在订单层面和item层面都要做检查
        
        // 山姆水修改的订单也要进EX2O, 沃尔玛和山姆的订单只进最新订单
        checkExistingEX2O(order_number);

        bool isNewOrder = CheckNewOrder(order_number);
        if(isNewOrder) cleanExceptionDRow["是否新单"] = "是";

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
            // EX2O是否包含当前订单，如果包含，则无需再次录单
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
            repeatedOrderException(curOrderRow, prevOrderRow, ref cleanExceptionDRow, ref 问题订单List, tmpOrderDT.Rows); 
        }

         /* 待拆分产品判断 
         981067796 （6个口味平分） 星巴克胶囊咖啡6盒装， 拆分比例12512003,12468696,12466195 =0.5：0.25：0.25，这个不是普通意义上的平分，因为雀巢跟山姆的箱规不一致，比如 山姆箱规16，雀巢箱规24，客户下单最小订单数量应为24箱
        */
        List<string> bulk_walfer_codes = new List<string>{};
        foreach(DataRow dr in bulkWalferConfigDT.Rows){
            string customer_product_code = dr["customer_product_code"].ToString();
            bulk_walfer_codes.Add(customer_product_code);
        }
                
        List<string> refItemExceptionList = new List<string>{};
        // By Item 检查exception, 检查整托和起送量
        foreach(DataRow dr in curOrderDocLinkRows){
            List<string> itemExceptionList = new List<string>{};

            string productCode = dr["customer_product_code"].ToString();
            DataRow byPOItemRow =  exceptionByPODT.NewRow();
            byPOItemRow.ItemArray = cleanExceptionDRow.ItemArray;
            byPOItemRow["Customer order Item"] = dr["line_number"];
            int quantity_ordered = toIntConvert(dr["quantity_ordered"]);
            if(quantity_ordered == 0){
                itemExceptionList.Add("产品数量为0");
            }

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
            
          //  if(!String.IsNullOrEmpty(byPOItemRow["Exception reason"].ToString())){ // 异常信息不为空
          //      exceptionByPODT.Rows.Add(byPOItemRow);  // by Item 的异常订单 
          //  }
        }
        List<string> finalItemExceptionList = itemExceptionUniq(refItemExceptionList);

        cleanExceptionDRow["Item Type"] = "Order";
        cleanExceptionDRow["order category"] = (问题订单List.Count > 0 || finalItemExceptionList.Count > 0) ? "exception" : "clean";
        string onlyItemException = (问题订单List.Count == 0 && finalItemExceptionList.Count > 0) ? "1" : "0";
        cleanExceptionDRow["onlyItemException"] = onlyItemException;
        cleanExceptionDRow["Exception category"] = string.Join("; ", 问题订单List.Union(finalItemExceptionList));
        cleanExceptionDRow["Exception reason"] = cleanExceptionDRow["Exception category"];

        exceptionByPODT.Rows.Add(cleanExceptionDRow);
    }
    buildByPODT();
    buildByPOAndItemDT();
    // Convert.ToInt32("asdas");
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


public void checkExistingEX2O(string order_number){
    if(existingEX2ODT!=null && orderJobHistoryDT!=null){
        bool inEX2O = existingEX2ODT.AsEnumerable().Cast<DataRow>().Any(dRow => dRow["PO_Number"].ToString().Contains(order_number));        
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

/*
山姆1：N产品，比例是 1：1：1, 假设山姆的产品Qty为180，那么需要衍生出3条产品行数据，每一行的Qty都是90, 45, 45. 于 2022/4/15改成  0.5：0.25：0.25
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
        decimal curQuantity_ordered = quantity_ordered * samPackage/nestlePackage * rationValue;
        DataRow[] qtyMappingRows = samQtyMappingDT.Select(string.Format("Sam_Product_Code='{0}' and Nestle_Product_Code='{1}'", productCode, 雀巢产品编码));
        
        decimal finalQtyD = fetchQty(quantity_ordered, qtyMappingRows[0], ref itemExceptionList, true);
        int finalQty = toIntConvert(Math.Floor(finalQtyD));
        
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


// 产品行检查折扣
public void specialProductCheck(DataRow dr, ref List<string> refItemExceptionList, ref DataRow byPOItemRow, DataRow[] prevOrderDocLinkRows, List<string> 问题订单list){
    List<string> itemExceptionList = new List<String>{};
    
    string remark = dr["Remark"].ToString();
    string remarkOption = dr["Remark_Option"].ToString(); // Remark_Option 1. 需检查以下客户店内码的原单规格与雀巢的产品规格是否一致 2. 检查价差  3.备注客户店内码
    string 沃尔玛产品编码 = dr["customer_product_code"].ToString();
    byPOItemRow["沃尔玛产品编码"] = 沃尔玛产品编码;
    byPOItemRow["雀巢产品编码"] = dr["Nestle_Material_No"];
    byPOItemRow["Material Description"] =  dr["Material_Description"];
    byPOItemRow["Nestle BU"] = dr["Nestle_BU"];
    byPOItemRow["雀巢数量"] = dr["quantity_ordered"];
    decimal nestleNPS = toDecimalConvert(dr["Nestle_NPS"].ToString());

    if(String.IsNullOrEmpty(byPOItemRow["雀巢产品编码"].ToString())){
        byPOItemRow["Item Type"] = "Item";
        itemExceptionList.Add(产品主数据匹配描述);
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
            remarkOption = remarkOption.Replace(";", "").Replace("；", "").Trim();
            if(!string.IsNullOrEmpty(remarkOption)){ // remarkOption 不为空，则也添加到异常描述里面
                byPOItemRow["Item Type"] = "Item";
                itemExceptionList.Add("特殊产品需检查订单," + remarkOption);
            }
        }
    }

    // 有remark说明已经map到主数据
    /*
    if(remark.Contains("不填充ex2o，只反馈问题订单")){
        byPOItemRow["Item Type"] = "Item";
        itemExceptionList.Add("未录单，水仓+干货产品");
    }*/
    /* 
      产品行折扣
      =Promotional Allowance/Extended Cost
      产品行的折扣率不等于各产品扣点[产品主数据有维护]（有一个产品不符合就是exception） ，反馈Exception，【不录单】
    */
    string 产品扣点 = dr["产品扣点"].ToString();
    if(!string.IsNullOrEmpty(产品扣点)){
        decimal productDiscountRate = Math.Round(fetchRateInDecimal(产品扣点), 4);
        //Console.WriteLine("oli_extended_cost");
        decimal oli_extended_cost = toDecimalConvert(dr["oli_extended_cost"]);
        if(oli_extended_cost == 0){
            oli_extended_cost = decimal.MaxValue;
        }
        //Console.WriteLine("产品行折扣");
        decimal 产品行折扣 = Math.Round(toDecimalConvert(dr["oli_allowance_total"])/oli_extended_cost, 4);
        if(Math.Abs(productDiscountRate) != Math.Abs(产品行折扣)){
            byPOItemRow["Item Type"] = "Item";
            itemExceptionList.Add(string.Format("产品行折扣率不等于扣点，产品行折扣: {0}, 雀巢产品扣点: {1}", 产品行折扣, productDiscountRate));
        }
    }
    
    // 数量按照比例转换
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
      山姆京东类型的订单如下单产品为氨糖奶粉，不录单作为exception order发给CSA取消
      Exception reason：不录单，山姆JD+氨糖奶粉
   
    if(remark.Contains("特殊产品") && remarkOption.Contains("订单类型Promotional Event为JD") && dr["promotional_event"].ToString().Contains("JD")){
        byPOItemRow["Item Type"] = "Item";
        itemExceptionList.Add("不录单，山姆JD+氨糖奶粉");
    }
    */
    
    /* 整托和起送量判断 */
    // WMDC = location
    //Console.WriteLine("Nestle_Plant_No:{0}, customer_product_code:{1}, order_number: {2}",  dr["Nestle_Plant_No"].ToString(), dr["customer_product_code"].ToString(), dr["order_number"].ToString());
    DataRow[] samWaterDeliveryDRs = samWaterDeliverySettingDT.Select(String.Format("WMDC='{0}' and Customer_Product_Code ='{1}'", dr["Nestle_Plant_No"].ToString(), dr["customer_product_code"].ToString()));
    //Console.WriteLine("samWaterDeliveryDRs.Length: {0}", samWaterDeliveryDRs.Length);
    if(samWaterDeliveryDRs.Length > 0){
        DataRow samWaterDeliveryDR = samWaterDeliveryDRs[0];
        packageAndDeliveryCheck(samWaterDeliveryDR, dr["quantity_ordered"].ToString(), ref refItemExceptionList);
    }

     //Console.WriteLine("问题订单List: {0}", string.Join("|", refItemExceptionList));
    // 如果整个订单数量修改的话， 需要记录单个item变化的部分
    string orderExceptionListStr = string.Join("|", 问题订单list);
    if(orderExceptionListStr.Contains(orderChangeQtyDescription) || orderExceptionListStr.Contains("Total Order Amount")){
        string customerProductCode = dr["customer_product_code"].ToString();
        //Console.WriteLine("customerProductCode: {0}", customerProductCode);
        
        DataRow prevItemRow = null;
        foreach(DataRow prevDr in prevOrderDocLinkRows){
            // Console.WriteLine("prevDr customerProductCode: {0}", prevDr["customer_product_code"].ToString());
            if(customerProductCode == prevDr["customer_product_code"].ToString()){ // 找到对应de 前一个order的item
                prevItemRow = prevDr;
                break;
            }
        }

        // 之前item不等于当前item对应的数量
        if(prevItemRow!=null && prevItemRow["quantity_ordered"].ToString() != dr["quantity_ordered"].ToString()){
            byPOItemRow["原订单数量"] = prevItemRow["quantity_ordered"];
            //Console.WriteLine("原订单数量: {0}, 修改后订单数量: {1}", prevItemRow["quantity_ordered"], dr["quantity_ordered"]);
            decimal finalPrevQuantity = qtyMappingRow!=null ? fetchQty(prevItemRow["quantity_ordered"], qtyMappingRow, ref itemExceptionList, false) : toDecimalConvert(prevItemRow["quantity_ordered"]);

            byPOItemRow["修改后订单数量"] = dr["quantity_ordered"];
            itemExceptionList.Add(orderChangeQtyDescription + $"，原订单雀巢数量：{toIntConvert(finalPrevQuantity)}， 改单后雀巢数量：{toIntConvert(final_quantity)}");
        }else{
            byPOItemRow["原订单数量"] = null;
        }
    }

    if(itemExceptionList.Count > 0){
        byPOItemRow["Exception reason"] = string.Join("; ", itemExceptionList);
        byPOItemRow["order category"] = "exception";
    } 
    refItemExceptionList = refItemExceptionList.Union(itemExceptionList).ToList();
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
                重复订单信息.Add(orderChangeQtyDescription);
                List<String> resultMabds = prevOrderRows.Cast<DataRow>().Select<DataRow, string>(dr => dr["total_units_ordered"].ToString()).ToList();
                cleanExceptionDRow["原订单数量"] = resultMabds[0];
            }else{
                Dictionary<string, string> webItemMapDic = new Dictionary<string, string>{{"ship_date", "起送日"}, {"promotional_event", "订单类型"}, {"allowance_percent", "仓租"}, {"total_line_items", "产品行数"}, {"total_order_amount_after_adjustments", "Total Order Amount (After Adjustments)"}};
                
                if(webItemMapDic.Keys.Contains(colName)){
                    string colNameDes = webItemMapDic[colName];
                    重复订单信息.Add(String.Format("订单修改{0}, 从({1})修改为({2})", colNameDes, prevItemStr, curItemStr));
                    // List<String> resultMABDs = prevOrderRows.Cast<DataRow>().Select<DataRow, string>(dr => dr[colName].ToString()).ToList();
                }

            }
        }
    }
    //Console.WriteLine("重复订单信息: {0}", String.Join("；", 重复订单信息));
    if(重复订单信息.Count > 0){
       问题订单List.Add("重复订单：" + String.Join("；", 重复订单信息));
    }
    else{
       问题订单List.Add("客户系统重复出单，订单无修改"); 
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
        // 问题订单List.Add("JD订单不处理");
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
      整托和起送量判断， 如果一个订单里面包含2条产品的话，就在订单层级判断，否则在item层级判断
    */
    int orderTotalLineItems = toIntConvert(totalLineItems);
    if(orderTotalLineItems > 1){
        DataRow[] samWaterDeliveryDRs = samWaterDeliverySettingDT.Select(string.Format("WMDC='{0}' and Customer_Product_Code ='{1}'", dr["Nestle_Plant_No"].ToString(), "980080954+980085511"));
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
        string 周几 = caculateWeekDay(MABDDate);
        int dayAdded = fetchNumber(requestDeliveryDate);
        
        if(!requestDeliveryDate.Contains(周几) || (DateTime.Compare(MABDDate, 读单日.AddDays(dayAdded)) < 0))
        {
            问题订单List.Add($"客户指定送货日不在行程日");
        }
    }else{
        string 周几 = caculateWeekDay(MABDDate);
        Console.WriteLine("周几: {0}", 周几);
        List<string> dayStringList = shipLocationDic(requestDeliveryDate);
        bool validDate = false;
        foreach(string item in dayStringList){
            //Console.WriteLine($"--- item:{item}");
            if(item.Contains("下周")){
                string weekDayStr = item.Replace("下", "");
                Console.WriteLine("weekDayStr: {0}", weekDayStr);
                DateTime 读单日日期 = Convert.ToDateTime(读单日.ToString("yyyy-MM-dd"));
                DateTime nexWeekDayDate = nextWeekDate(读单日日期, weekDayMapping(weekDayStr));
                Console.WriteLine("MABDDate: {0}, nexWeekDayDate: {1}", MABDDate, nexWeekDayDate);

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
        if(!validDate){
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


    decimal walmartDiscountRate = customerDiscountRateM();
    decimal 不含扣点 = (1-walmartDiscountRate);
   
    Decimal total_order_amount_after_adjustments = toDecimalConvert(dr["total_order_amount_after_adjustments"]);
    
    decimal sap_net_value = getSapNetValue(dr, allitemproductCodesList, curOrderDocLinkRows, 不含扣点);
    
     Console.WriteLine("po_number: {0}", po_number);
    Console.WriteLine("sap_net_value: {0}, total_order_amount_after_adjustments: {1}", sap_net_value, total_order_amount_after_adjustments);
    decimal gapValue = sap_net_value - total_order_amount_after_adjustments;

    // 980080954 巴黎水, 且价差超出（-5，2）范围当作异常订单反馈CSA。 
    if(prodCode == 巴黎水产品码){
        if(gapValue < -5 || gapValue > 2 ){
            问题订单List.Add($"价差检查, SAP NET Value: {Math.Round(sap_net_value, 2)}, 山姆订单扣点后未税金额: {Math.Round(total_order_amount_after_adjustments, 2)}, 价差为：{gapValue}");
        }
    }else if(prodCode == 圣培露产品码){
        // 雀巢价小于山姆价，每箱价（X列价格/订单箱数）差0.0052525元的差异可忽略
        // 解释：雀巢价大于山姆价，每箱价（X列价格/订单箱数）大于0.0052525元的差异为异常
        int intQty = toIntConvert(orderQty);
        if(intQty == 0){
            intQty = Int32.MaxValue;
        }
        decimal 每箱价差 = Math.Round(gapValue/intQty, 8);
        Console.WriteLine("每箱价差: {0}", 每箱价差);
        if(gapValue > 0m || Math.Abs(每箱价差) > 0.0052525m){
            问题订单List.Add($"价差检查, SAP NET Value: {Math.Round(sap_net_value, 2)}, 山姆订单扣点后未税金额: {Math.Round(total_order_amount_after_adjustments, 2)}, 价差为：{gapValue}, 每箱价差：{gapValue}/{intQty}={每箱价差}");
        }
    }

    /*  
    跨月订单， MABD为下个月
    当月订单的MABD显示在下个月的订单，判断为问题订单，备注原因跨月订单反馈
    */
    DateTime orderCreateDateTime = Convert.ToDateTime(dr["create_date_time"]);
    bool mabdInNextMonth = (MABDDate.ToString("yyyy-MM") == orderCreateDateTime.AddMonths(1).ToString("yyyy-MM"));
    
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
}


public int fetchNumber(string srcTxt){
    Regex 数字结尾正则 = new Regex(@"\d{1,}$");
    Match matchResult112 = 数字结尾正则.Match(srcTxt);
    string numberStr = matchResult112.Value;
    if(numberStr!= ""){
       return toIntConvert(numberStr); 
    }else{
        return 0;
    }
}


public void packageAndDeliveryCheck(DataRow samWaterDeliveryDR, string orderQty, ref List<string> 问题订单List){
    int startDeliveryCount = toIntConvert(samWaterDeliveryDR["Start_Delivery_Count"]);  // 起送量
    int packageMultiple = toIntConvert(samWaterDeliveryDR["Package_Multiple"]); // 整托送货倍数
    if(packageMultiple == 0){
        packageMultiple = int.MaxValue;
    }
    int orderQtyInt = toIntConvert(orderQty); // 订单总量
    
    decimal 托数 = Math.Round(orderQtyInt/Convert.ToDecimal(packageMultiple), 6);
    int 托数整数 = orderQtyInt/packageMultiple;
    //Console.WriteLine("托数: {0}, 托数整数: {1}", 托数, 托数整数);
    if(托数整数 != 托数){ // 不是整托，不满足整托送货
       问题订单List.Add($"不满足整托送货，托数：{托数}"); 
    }

    string deliveryMethod = samWaterDeliveryDR["Delivery_Method"].ToString(); // 物流配送方式
    string 物流方式 = deliveryMethod.Contains("直送") ? "直送仓" : "集货仓";
    //Console.WriteLine("orderQtyInt: {0}, startDeliveryCount: {1}", orderQtyInt, startDeliveryCount);
    if(orderQtyInt < startDeliveryCount){ // Exception reason：集货仓/直送仓数量未达到XXXX起送量
        问题订单List.Add($"{物流方式}数量未达到{startDeliveryCount}起送量");
    }
}

// 获取 order SAP net value
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
    
            string 产品扣点 = itemDR["产品扣点"].ToString();
            if(!string.IsNullOrEmpty(产品扣点) && customerProdCode == 巴黎水产品码 ){
                decimal productDiscountRate = Math.Round(fetchRateInDecimal(产品扣点), 6);
                不含扣点 = 不含扣点 - productDiscountRate;
            }
            
            decimal itemNestleNPS=0;
            if(!String.IsNullOrEmpty(itemDR["Nestle_NPS"].ToString())){
                itemNestleNPS = toDecimalConvert(itemDR["Nestle_NPS"]);
            }
            //Console.WriteLine("itemNestleNPS: {0}, itemQuantityOrdered: {1}", itemNestleNPS, itemQuantityOrdered);
            sap_net_value += itemNestleNPS * itemQuantityOrdered * 不含扣点;
        }
    }
 
    return sap_net_value;
}

public decimal customerDiscountRateM(){
    DataRowCollection etoConfigDrs = etoConfigDT.Rows;
    
    string EDLC百分比Str = etoConfigDrs[0]["EDLC百分比"].ToString().Trim();
    string RTV百分比Str = etoConfigDrs[0]["RTV百分比"].ToString().Trim();
    string 仓租百分比Str = etoConfigDrs[0]["仓租百分比"].ToString().Trim();
    
    
    decimal EDLC百分比 = string.IsNullOrEmpty(EDLC百分比Str) ? 0 : fetchRateInDecimal(EDLC百分比Str);;
    decimal RTV百分比 = string.IsNullOrEmpty(RTV百分比Str) ? 0 : fetchRateInDecimal(RTV百分比Str);
    decimal 仓租百分比 = string.IsNullOrEmpty(仓租百分比Str) ? 0 : fetchRateInDecimal(仓租百分比Str);
   //Console.WriteLine("EDLC百分比:{0}, RTV百分比:{1}, 仓租百分比:{2}", EDLC百分比, RTV百分比, 仓租百分比);
    return EDLC百分比 + RTV百分比 + 仓租百分比;
}

public decimal fetchRateInDecimal(string walmartDiscountRateStr)
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
                resutRate = toDecimalConvert(walmartDiscountRateStr);
            }
        }
    }
    catch (Exception e)
    {
        Console.WriteLine("walmartDiscountRateStr不合法： {0}", e.Message);
    }
    return resutRate;
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

public DateTime nextWeekDate(DateTime 参照日, int weekDay)
{
    DateTime 统计开始时间 = 参照日.AddDays(6);
    var dayOfWeek = 统计开始时间.DayOfWeek;
    DateTime nextWeekDay =  统计开始时间.AddDays(-(int)dayOfWeek + weekDay);
    return nextWeekDay;
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

// 山姆水，合包数量转换
public decimal convertedQty(decimal customerQty, decimal nestleRatio, ref string exceptionReason){
     decimal finalQty = customerQty;

    // 订单数量不是6的倍数，则再判断大于还是小于6，小于则不录单，大于的话，取整录单
    if(toIntConvert(customerQty/minSamQty) != customerQty/minSamQty){
        if(customerQty < minSamQty){
            exceptionReason = string.Format("产品数量不是“{0}”的倍数且小于“{1}”，不录单", minSamQty, minSamQty);
        }else{
            exceptionReason = string.Format("产品数量不是“{0}”的倍数", minSamQty);
            finalQty = (Math.Floor(customerQty/minSamQty) * minSamQty) * samPackage/nestlePackage * nestleRatio;
        }
    }else{
        finalQty = customerQty * samPackage/nestlePackage * nestleRatio;
    }
    return finalQty;
}

public decimal fetchQty(object originalQty, DataRow qtyMappingRow, ref List<string> itemExceptionList, bool intoException){
    decimal customerOrderQty = toDecimalConvert(originalQty);
    decimal finalQty = customerOrderQty;
    // string Not_Integer_Still_Into_EX2O = qtyMappingRow["Not_Integer_Still_Into_EX2O"].ToString();
    string customerProdCode = qtyMappingRow["Sam_Product_Code"].ToString();
    decimal Nestle_Qty_ratio = toDecimalConvert(qtyMappingRow["Nestle_Qty"]);
    //nestleQty_m = Math.Floor(nestleQty_m); // 换算后不为整数，则向下取整    
    // 当换算后不是“6”的倍数时，需反馈为exception，原因描述为：“产品数量不是“6”的倍数“，雀巢的箱规是24，山姆箱规是16，山姆的下单数量换算为雀巢的数量需要 a*16/24, 即a * 2/3, 然后再按照0.5：0.25：0.25的比列拆分
    if(samWaterSpecialCodeLs.Contains(customerProdCode)){
        string exceptionReason = string.Empty;
        finalQty = convertedQty(customerOrderQty, toDecimalConvert(qtyMappingRow["Nestle_Qty"]), ref exceptionReason);
        if(!string.IsNullOrEmpty(exceptionReason) && intoException){
            if(!itemExceptionList.Contains(exceptionReason)) itemExceptionList.Add(exceptionReason); 
        }
    }
    return finalQty;
}

public decimal fetchQty(object originalQty, string customerProdCode, string nestleProdCode, DataTable samQtyMappingDT){
    DataRow[] qtyMappingRows = samQtyMappingDT.Select(string.Format("Sam_Product_Code='{0}' and Nestle_Product_Code='{1}'", customerProdCode, nestleProdCode));
    decimal customerOrderQty = toDecimalConvert(originalQty);
    decimal finalQty = customerOrderQty;

    if(qtyMappingRows.Length > 0){
        DataRow qtyMappingRow = qtyMappingRows[0];  
        // string Not_Integer_Still_Into_EX2O = qtyMappingRow["Not_Integer_Still_Into_EX2O"].ToString();
         if(samWaterSpecialCodeLs.Contains(customerProdCode)){
            string exceptionReason = string.Empty;
            finalQty = convertedQty(customerOrderQty, toDecimalConvert(qtyMappingRow["Nestle_Qty"]), ref exceptionReason);
         }
    }
    return finalQty;
}



public static decimal toDecimalConvert(object srcValue){
    Decimal nestle_NPS = 0;
    try{
        nestle_NPS = Convert.ToDecimal(srcValue);
    }catch(Exception e){
        Console.WriteLine($"转换成decimal价格出错，{srcValue}");
    }
    return nestle_NPS;
}

public static int toIntConvert(object srcValue){
    int intValue = 0;
    try{
        intValue = Convert.ToInt32(srcValue);
    }catch(Exception e){
        Console.WriteLine($"转换成int32出错，{srcValue}");
    }
    return intValue;
}
