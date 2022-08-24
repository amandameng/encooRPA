//代码执行入口，请勿修改或删除
decimal tax = 0.13m;
public enum ExceptionCategory
{
    RDD,
    产品主数据缺失,
    产品规格差异,
    客户订单删单,
    订单价格差异,
    不录入系统,
    门店订单
};
public string exceptionSeperator = "|";
public string exceptionContactSumbol = "；";

public void Run()
{
    //在这里编写您的代码
    // newOrdersTmpDT    只有order_number, dc_no
    // origOrdersFromSheetDT     订单详情
    
    initNewOrdersDT(); // 订单行遍历处理后输出 newOrdersDT
    
    DataTable exceptionsDT = newOrdersDT.Clone();
    exceptionsDT.Columns.Add("异常分类", typeof(string));
    exceptionsDT.Columns.Add("异常详细描述", typeof(string));
    
    // 新增的订单 => Exception 订单模板
    origOrdersMappingToExceptionOrders(ref exceptionsDT);

    // 删单 => Exception 订单模板
    deletedOrdersMappingToExceptionOrders(ref exceptionsDT);
    
    // exception格式转换成 DB Mapiing  对应的字段
    DataTable exceptionsDBDT = writeToExceptionDT(exceptionsDT);
    // 合并exception
    uniqExceptionDT = MergeExceptionDTbyProductRow(exceptionsDBDT);
    // 收集exception订单号，用于批量查询导出pdf
    exceptionOrderList = uniqExceptionDT.Rows.Cast<DataRow>().Select<DataRow, string>(dr => dr["客户订单号（POID）"].ToString()).ToList();
    // getCleanOrder
    DataTable cleanOrdersDT = getCleanOrders(newOrdersDT, uniqExceptionDT);
    writeToDMSTracker(cleanOrdersDT);
    
    // Convert.ToInt32("as");

}
//在这里编写您的函数或者类

public void origOrdersMappingToExceptionOrders(ref DataTable exceptionsDT){
    DataColumnCollection orderDetailsCols = origOrdersFromSheetDT.Columns;
    
    foreach(DataRow dr in newOrdersTmpDT.Rows){
        string orderNumber = dr["order_number"].ToString();
        DataRow[] orderDRs = origOrdersFromSheetDT.Select(string.Format("订单号 = '{0}'", orderNumber));  // 根据订单号筛选产品行详情
        // 处理订单每一产品行
        // 以下三个值是订单级别汇总
        decimal 物美订单总金额 = 0m;
        decimal 惠氏订单总金额 = 0m;
        decimal 惠氏订单总折扣价 = 0m;
        bool hasPriceException = false;
        bool hasSkuMappingException = false;
        bool hasSizeException = false;
        bool hasRDDException = false;
        bool 存在不录品 = false;
        bool 门店订单 = false;

        List<DataRow> origFullOrderList = new List<DataRow>{};
        // 遍历产品行开始
        foreach(DataRow orderItemDR in orderDRs){
            DataRow newOrderDR = newOrdersDT.NewRow();
            rowMappedToNewOrderRow(orderDetailsCols, orderItemDR, ref newOrderDR); // 渲染到新版订单模板上
            
            // SKU Mapping 不上
            if(string.IsNullOrEmpty(newOrderDR["惠氏编码"].ToString())){
                if(!hasSkuMappingException){ // 如果为假，则赋值为真，此单仅为真
                    hasSkuMappingException = true;
                }
            }

            // 规格差异
            if(newOrderDR["规格差异"].ToString() != "0"){
                 if(!hasSizeException){ // 如果为假，则赋值为真，此单仅为真
                    hasSizeException = true;
                }
            }

            // 价格差异
            // 单价价差必须等于0 且 总价价差需要在15以内的为clean 订单
            if(toDecimalConvert(newOrderDR["单价价差"]) != 0m || toDecimalConvert(newOrderDR["总价价差"]) > 15m){
                if(!hasPriceException){ // 如果为假，则赋值为真，此单仅为真
                    hasPriceException = true;
                }
            }

            // RDD Check，Order Level
           if(toIntConvert(newOrderDR["RDD issue"]) < 3){
               if(!hasRDDException){ // 如果为假，则赋值为真，此单仅为真
                    hasRDDException = true;
                }
           }

           // 不录品
           if(newOrderDR["特殊品"].ToString().Contains("不录入系统")){
               if(!存在不录品){ // 如果为假，则赋值为真，此单仅为真
                    存在不录品 = true;
                }
           }
           
           // 门店订单，仓号不在指定范围内的
           if(isStoreDC(newOrderDR["大仓号"].ToString())){
               if(!门店订单){ // 如果为假，则赋值为真，此单仅为真
                    门店订单 = true;
                }
           }

                       
            物美订单总金额 += toDecimalConvert(newOrderDR["物美总价"]);
            惠氏订单总金额 += toDecimalConvert(newOrderDR["惠氏总价"]);
            惠氏订单总折扣价 += toDecimalConvert(newOrderDR["系统折扣价"]); 
            origFullOrderList.Add(newOrderDR);
        }
        // 遍历产品行结束

        // 再次遍历产品行，设置订单总价，以及判断异常
        foreach(DataRow origFullOrderDR in origFullOrderList){
            origFullOrderDR["物美订单总金额"] = 物美订单总金额;
            origFullOrderDR["惠氏订单总金额"] = 惠氏订单总金额;
            origFullOrderDR["惠氏订单总折扣价"] = 惠氏订单总折扣价;
            string exceptionType = string.Empty;
            string exceptionDetail = string.Empty;
            
            if(hasSkuMappingException){
                if(!string.IsNullOrEmpty(origFullOrderDR["惠氏编码"].ToString())){  // 惠氏编码 不为空
                    // exception为空
                    addToExceptionOrder(ref exceptionsDT, exceptionType, exceptionDetail, origFullOrderDR);
                }else{
                    addToExceptionOrder(ref exceptionsDT, ExceptionCategory.产品主数据缺失.ToString(), exceptionDetail, origFullOrderDR);
                }
            }
            
            if(hasSizeException){
                if(origFullOrderDR["规格差异"].ToString() == "0"){
                    // exception为空
                    addToExceptionOrder(ref exceptionsDT, exceptionType, exceptionDetail, origFullOrderDR);
                }else{
                    addToExceptionOrder(ref exceptionsDT, ExceptionCategory.产品规格差异.ToString(), exceptionDetail, origFullOrderDR);
                }
            }
            
            if(hasRDDException){
                addToExceptionOrder(ref exceptionsDT, ExceptionCategory.RDD.ToString(), exceptionDetail, origFullOrderDR); 
            }
            
            if(hasPriceException){
                if(toDecimalConvert(origFullOrderDR["单价价差"]) != 0m || toDecimalConvert(origFullOrderDR["总价价差"]) > 15m){
                    // exception为空
                    // exceptionDetail = $"客户订单产品与惠氏产品存在价格差异（未税单价差异：{origFullOrderDR["单价价差"]}，总价差异：{origFullOrderDR["总价价差"]}），需确认是否录入订单并跟进价差问题";
                    addToExceptionOrder(ref exceptionsDT, ExceptionCategory.订单价格差异.ToString(), exceptionDetail, origFullOrderDR);
                }else{
                    addToExceptionOrder(ref exceptionsDT, exceptionType, exceptionDetail, origFullOrderDR);
                }
            }

            if(存在不录品){
                if(!origFullOrderDR["特殊品"].ToString().Contains("不录入系统")){
                    // exception为空
                    addToExceptionOrder(ref exceptionsDT, exceptionType, exceptionDetail, origFullOrderDR);
                }else{
                    addToExceptionOrder(ref exceptionsDT, ExceptionCategory.不录入系统.ToString(), exceptionDetail, origFullOrderDR);
                }
            }
            
            if(门店订单){
                addToExceptionOrder(ref exceptionsDT, ExceptionCategory.门店订单.ToString(), exceptionDetail, origFullOrderDR);
            }

            newOrdersDT.Rows.Add(origFullOrderDR);
        }
    }
}

public void deletedOrdersMappingToExceptionOrders(ref DataTable exceptionsDT){
    if(deletedOrdersFromDBDT == null || deletedOrdersFromDBDT.Rows.Count == 0){
        return;
    }
    
    foreach(DataRow dr in deletedOrdersFromDBDT.Rows){
        string orderNumber = dr["order_number"].ToString();
        DataRow newOrderDR = newOrdersDT.NewRow();
        deletedDRowMappedToNewOrderRow(dr, ref newOrderDR); // 渲染到新版订单模板上
        string exceptionDetail = string.Empty;
        addToExceptionOrder(ref exceptionsDT, ExceptionCategory.客户订单删单.ToString(), exceptionDetail, newOrderDR);    
        // newOrdersDT.Rows.Add(newOrderDR);
    }
}


public void initNewOrdersDT(){
    newOrdersDT = origOrdersFromSheetDT.Clone();
    // 读单日期	POID	DC	ship to	订单日期	RDD	产品名称	惠氏编码	紧缺	订购箱数	物美未税单价	惠氏单价	物美总价	惠氏总价	物美规格	惠氏规格	扣点	系统折扣价	规格差异	单价价差	总价价差	RDD issue

    string[] addedColumns = new string[]{"大仓号", "大仓名", "读单日期", "POID", "DC", "ship to", "sold to", "实际订单日期", "RDD", "惠氏产品名称", "惠氏编码", "紧缺", "订购箱数", "物美未税单价", "惠氏单价", "物美总价", "惠氏总价", "物美规格", "惠氏规格", 
                                                                 "扣点", "系统折扣价", "规格差异", "单价价差", "总价价差", "特殊品","RDD issue", "物美订单总金额", "惠氏订单总金额", "惠氏订单总折扣价", "订单类型2", "客户名称", "区域"};
    List<string> objectColumns = new List<string>{"订购箱数",  "物美未税单价", "惠氏单价", "物美总价", "惠氏总价", "物美规格", "惠氏规格", "系统折扣价", "规格差异", "单价价差", "总价价差", "RDD issue", "物美订单总金额", "惠氏订单总金额", "惠氏订单总折扣价"};
    foreach(string colName in addedColumns){
        DataColumn dcol = null;
        if(objectColumns.Contains(colName)){
            dcol = new DataColumn(colName, typeof(object));
        }else{
            dcol = new DataColumn(colName, typeof(string));
        }
        
        if(colName == "读单日期"){
            dcol.DefaultValue = DateTime.Now.ToString("yyyy/MM/dd");
        }
       newOrdersDT.Columns.Add(dcol);
    }
}

public void splitDCInfo(string 订货地, ref string dcNo, ref string dcName){
    string[] addreddArr = 订货地.Split(new string[]{" "}, StringSplitOptions.RemoveEmptyEntries);
    if(addreddArr.Length == 2){
        dcNo = addreddArr[0];
        dcName = addreddArr[1];
    }
}

public string specialProductComment(string 惠氏产品码){
    string comment = string.Empty;
    DataTable specialProductsDT = (DataTable)dtRow_ModuleSettings["specialListDT"];
    DataRow[] drs = specialProductsDT.Select(string.Format("sku_code='{0}'", 惠氏产品码));
    if(drs.Length > 0){
        comment = drs[0]["comment"].ToString();
    }
    return comment;
}

// 查询特殊品某个产品comment
public string specialProductComment(string 惠氏产品码, string 客户产品码, DataTable specialListDT){
    // printDT(specialListDT);
    string comment = string.Empty;
    if(!string.IsNullOrEmpty(惠氏产品码)){
        DataRow[] matchedDrs = specialListDT.Select(string.Format("sku_code='{0}' and customer_sku_code='{1}'", 惠氏产品码, 客户产品码));
        if(matchedDrs.Length > 0){
            comment = matchedDrs[0]["comment"].ToString();
        }else{
            DataRow[] matchedDrs2 = specialListDT.Select(string.Format("sku_code='{0}'", 惠氏产品码));
            if(matchedDrs2.Length > 0){
                comment = matchedDrs2[0]["comment"].ToString();
            }
        }
    }
    return comment;
}

public void getShipTo(string dcNo, ref string shipTo, ref string soldTo, ref string 扣点){
    DataTable soldToShipToDT = (DataTable)dtRow_ModuleSettings["soldToShipToDT"];
    DataRow[] drs = soldToShipToDT.Select(string.Format("`DC编号` = '{0}'", dcNo));
    if(drs.Length > 0){
        shipTo = drs[0]["Ship to"].ToString();
        soldTo = drs[0]["Sold to"].ToString();
        扣点 = drs[0]["discount"].ToString();
    }
}

public bool isStoreDC(string dcNo){
    DataTable soldToShipToDT = (DataTable)dtRow_ModuleSettings["soldToShipToDT"];
    DataRow[] drs = soldToShipToDT.Select(string.Format("`DC编号` = '{0}'", dcNo));
    if(drs.Length == 0){
        return true;
    }
    return false;
}

public DataRow getWyethMappingRow(string customerSKU){
    DataTable materialMasterDataDT = (DataTable)dtRow_ModuleSettings["materialMasterDataDT"];
    DataRow[] drs = materialMasterDataDT.Select(string.Format("customer_material_no='{0}'", customerSKU));
    if(drs.Length > 0){
       return drs[0];
    }else{
        return null;
    }
}

public string getConstraintProduct(string wyethSku){
    string comment = string.Empty;
    DataTable constraintListDT= (DataTable)dtRow_ModuleSettings["constraintListDT"];
    if(string.IsNullOrEmpty(wyethSku)){ // if wyethSku 为空，return 空
        return comment;
    }
    DataRow[] drs = constraintListDT.Select(string.Format("sku_code='{0}'", wyethSku));
     if(drs.Length > 0){
       comment = drs[0]["comment"].ToString();
    }
     return comment;
}
/// <summary>
/// 将从下载的订单详情中的每一行转换成newOrderDR
/// </summary>
/// <param name="orderDetailsCols"></param>
/// <param name="orderItemDR"></param>
/// <param name="newOrderDR"></param>
public void rowMappedToNewOrderRow(DataColumnCollection orderDetailsCols, DataRow orderItemDR, ref DataRow newOrderDR){
        string dcNo = string.Empty;
        string dcName = string.Empty;
        string shipTo = string.Empty;
        string soldTo = string.Empty;
        string 扣点 = string.Empty;
        decimal 惠氏单价 = 0m;
        int 惠氏规格 = 0;
        string 惠氏编码 = string.Empty;
        string 惠氏产品名称 = string.Empty;
        string 紧缺 = string.Empty;
        splitDCInfo(orderItemDR["订货地"].ToString(), ref dcNo, ref dcName); // 拆分dc_no 和 dc_name
        string customerSku = orderItemDR["商品编号"].ToString();
        getShipTo(dcNo, ref shipTo,ref soldTo, ref 扣点); // 给 shipTo 赋值，给扣点赋值

        // 将order 详情的每项赋值给新数据表行
        // 下载的订单表给当前数据表赋值
        foreach(DataColumn dcol in orderDetailsCols){
            string colName = dcol.ColumnName;
            newOrderDR[colName] = orderItemDR[dcol.ColumnName];

        }
        newOrderDR["POID"] = orderItemDR["订单号"].ToString();
        newOrderDR["DC"] = dcName;
        newOrderDR["大仓号"] = dcNo;
        newOrderDR["大仓名"] = dcName;
        newOrderDR["ship to"] = shipTo;
        newOrderDR["sold to"] = soldTo;
        newOrderDR["实际订单日期"] = DateTime.ParseExact(orderItemDR["订单日期"].ToString(), "yyyyMMdd", null).ToString("yyyy/MM/dd");
        DateTime rddDate =  DateTime.ParseExact(orderItemDR["预计到货日"].ToString(), "yyyyMMdd", null);
        newOrderDR["RDD"] =rddDate.ToString("yyyy/MM/dd");
        DataRow wyethSKUMappingRow = getWyethMappingRow(customerSku);
        if(wyethSKUMappingRow != null){
            惠氏单价 = toDecimalConvert(wyethSKUMappingRow["wyeth_unit_price"]);
            惠氏规格 = toIntConvert(wyethSKUMappingRow["size"]);
            惠氏编码 = wyethSKUMappingRow["wyeth_material_no"].ToString();
            惠氏产品名称 = wyethSKUMappingRow["customer_product_name"].ToString();
            紧缺 = getConstraintProduct(惠氏编码.ToString());
        }

        newOrderDR["惠氏产品名称"] = 惠氏产品名称;
        newOrderDR["惠氏编码"] = 惠氏编码;
        newOrderDR["紧缺"] = 紧缺;
        newOrderDR["惠氏单价"] = 惠氏单价;
        newOrderDR["惠氏规格"] = 惠氏规格;
        newOrderDR["订购箱数"] = orderItemDR["订货量"];
        decimal 客户含税价格 = toDecimalConvert(orderItemDR["价格"]);
        decimal 物美未税单价 = Math.Round(客户含税价格/(1+tax), 4);
        newOrderDR["物美未税单价"] = 物美未税单价;
        decimal 订货量 = toDecimalConvert(orderItemDR["订货量"]);
        string 包装单位 = orderItemDR["包装单位"].ToString();
        string 物美规格Str = 包装单位.Replace("H", "");
        int 物美规格 = toIntConvert(物美规格Str);
        newOrderDR["物美规格"] = 物美规格;
        decimal 物美总价 =  Math.Round((客户含税价格/(1+tax)) * 物美规格 * 订货量, 2);
        newOrderDR["物美总价"] = 物美总价 ;
        newOrderDR["扣点"] = 扣点;
        decimal 惠氏总价 = Math.Round(订货量 * 惠氏单价 * 惠氏规格, 2);
        newOrderDR["惠氏总价"] = 惠氏总价;

        decimal 扣点值 = fetchRateInDecimal(扣点);
        decimal 系统折扣价 = Math.Round(订货量 * 惠氏单价 * 惠氏规格 * (1-扣点值), 2);
        newOrderDR["系统折扣价"] = 系统折扣价;
        newOrderDR["规格差异"] = 物美规格 - 惠氏规格;
        newOrderDR["单价价差"] = Math.Round(物美未税单价 - 惠氏单价, 2);
        newOrderDR["总价价差"] = 物美总价 - 惠氏总价;
        int rddGapDays = DiffDays(DateTime.Parse(newOrderDR["读单日期"].ToString()), rddDate);
        newOrderDR["RDD issue"] = rddGapDays;
        string comment = specialProductComment(惠氏编码, customerSku, (DataTable)dtRow_ModuleSettings["specialListDT"]);
        newOrderDR["特殊品"] = comment;
        // 筛选页面订单（ordersFromPagesDT），设置【订单类型2】
        DataRow[] orderDataRowsFromPage = ordersFromPagesDT.Select(string.Format("订单号='{0}'", orderItemDR["订单号"].ToString()));
        if(orderDataRowsFromPage.Length > 0){
            newOrderDR["订单类型2"] = orderDataRowsFromPage[0]["订单类型"];
        }
        newOrderDR["客户名称"] = dtRow_ModuleSettings["customer_name"];
        newOrderDR["区域"] = dtRow_ModuleSettings["区域"];
       
}

/// <summary>
/// 将从数据库查出来的原单（删单）转换成 newOrderDR
/// </summary>
/// <param name="orderItemDR">从DB查出来的一行</param>
/// <param name="newOrderDR"></param>
public void deletedDRowMappedToNewOrderRow(DataRow orderItemDRFromDB, ref DataRow newOrderDR){
        string dcNo = string.Empty;
        string dcName = string.Empty;
        string shipTo = string.Empty;
        string soldTo = string.Empty;
        string 扣点 = string.Empty;
        decimal 惠氏单价 = 0m;
        int 惠氏规格 = 0;
        string 惠氏编码 = string.Empty;
        string 惠氏产品名称 = string.Empty;
        string 紧缺 = string.Empty;
        
        string customerSku = orderItemDRFromDB["product_code"].ToString();
        dcNo = orderItemDRFromDB["dc_no"].ToString();
        getShipTo(dcNo, ref shipTo,ref soldTo, ref 扣点); // 给 shipTo 赋值，给扣点赋值

        // 将order 详情的每项赋值给新数据表行
        // 下载的订单表给当前数据表赋值

        newOrderDR["商品编号"] = orderItemDRFromDB["product_code"];
    
        newOrderDR["商品名称"] = orderItemDRFromDB["product_name"];
        newOrderDR["订货量"] = orderItemDRFromDB["order_qty"];
        newOrderDR["价格"] = orderItemDRFromDB["price"];
        newOrderDR["物美总价"] = orderItemDRFromDB["customer_product_notax_total"];
        newOrderDR["物美订单总金额"] = orderItemDRFromDB["customer_order_notax_price"];
        newOrderDR["POID"] = orderItemDRFromDB["order_number"];
        newOrderDR["DC"] = orderItemDRFromDB["order_location"];
        newOrderDR["大仓号"] = dcNo;
        newOrderDR["大仓名"] = orderItemDRFromDB["dc_name"];
        newOrderDR["ship to"] = shipTo;
        newOrderDR["sold to"] = soldTo;
        newOrderDR["实际订单日期"] = DateTime.Parse(orderItemDRFromDB["order_date"].ToString()).ToString("yyyy/MM/dd");
        DateTime rddDate =  DateTime.Parse(orderItemDRFromDB["estimated_arrived_at"].ToString());
        newOrderDR["RDD"] =rddDate.ToString("yyyy/MM/dd");
        DataRow wyethSKUMappingRow = getWyethMappingRow(customerSku);
        if(wyethSKUMappingRow != null){
            惠氏单价 = toDecimalConvert(wyethSKUMappingRow["wyeth_unit_price"]);
            惠氏规格 = toIntConvert(wyethSKUMappingRow["size"]);
            惠氏编码 = wyethSKUMappingRow["wyeth_material_no"].ToString();
            惠氏产品名称 = wyethSKUMappingRow["customer_product_name"].ToString();
            紧缺 = getConstraintProduct(惠氏编码.ToString());
        }

        newOrderDR["惠氏产品名称"] = 惠氏产品名称;
        newOrderDR["惠氏编码"] = 惠氏编码;
        newOrderDR["紧缺"] = 紧缺;
        newOrderDR["惠氏单价"] = 惠氏单价;
        newOrderDR["惠氏规格"] = 惠氏规格;
        newOrderDR["订购箱数"] = orderItemDRFromDB["order_qty"];
        decimal 客户含税价格 = toDecimalConvert(orderItemDRFromDB["price"]);
        decimal 物美未税单价 = Math.Round(客户含税价格/(1+tax), 4);
        newOrderDR["物美未税单价"] = 物美未税单价;
        decimal 订货量 = toDecimalConvert(orderItemDRFromDB["order_qty"]);
        string 包装单位 = orderItemDRFromDB["package_uom"].ToString();
        string 物美规格Str = 包装单位.Replace("H", "");
        int 物美规格 = toIntConvert(物美规格Str);
        newOrderDR["物美规格"] = 物美规格;
        decimal 物美总价 =  Math.Round((客户含税价格/(1+tax)) * 物美规格 * 订货量, 2);
        newOrderDR["物美总价"] = 物美总价 ;
        newOrderDR["扣点"] = 扣点;
        decimal 惠氏总价 = Math.Round(订货量 * 惠氏单价 * 惠氏规格, 2);
        newOrderDR["惠氏总价"] = 惠氏总价;

        decimal 扣点值 = fetchRateInDecimal(扣点);
        decimal 系统折扣价 = Math.Round(订货量 * 惠氏单价 * 惠氏规格 * (1-扣点值), 2);
        newOrderDR["系统折扣价"] = 系统折扣价;
        newOrderDR["规格差异"] = 物美规格 - 惠氏规格;
        newOrderDR["单价价差"] = Math.Round(物美未税单价 - 惠氏单价, 2);
        newOrderDR["总价价差"] = 物美总价 - 惠氏总价;
        int rddGapDays = DiffDays(DateTime.Today, rddDate);
        newOrderDR["RDD issue"] = rddGapDays;
        newOrderDR["订单类型2"] = orderItemDRFromDB["order_type2"];
}

    
public int toIntConvert(object srcValue){
    int intValue = 0;
    try{
        intValue = Convert.ToInt32(srcValue);
    }catch(Exception e){
       Console.WriteLine($"转换成int32出错，{srcValue}");
    }
    return intValue;
}

public decimal toDecimalConvert(object srcValue){
    Decimal nestle_NPS = 0;
    try{
        nestle_NPS = Convert.ToDecimal(srcValue);
    }catch(Exception e){
       Console.WriteLine($"转换成decimal价格出错，{srcValue}");
    }
    return nestle_NPS;
}

public decimal fetchRateInDecimal(string discountStr)
{
    Regex 百分数正则 = new Regex(@"\d+(\.\d+)?%");
    Match matchResult = 百分数正则.Match(discountStr);
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
            if (!discountStr.Contains("%"))
            { // 不包含%
                resutRate = toDecimalConvert(discountStr);
            }
        }
    }
    catch (Exception e)
    {
        Console.WriteLine("discountStr不合法： {0}", e.Message);
    }
    return resutRate;
}

/// <summary>
/// must arrived at减去抓单日期需要>= 3天
/// </summary>
/// <param name="startTime">读单日期</param>
/// <param name="endTime"> 导出的日期格式是 yyyy/MM/dd </param>
/// <returns></returns>
public int DiffDays(DateTime startTime, DateTime endTime)
{
    TimeSpan daysSpan = new TimeSpan(endTime.Ticks - startTime.Ticks);
    return daysSpan.Days;
}

public void addToExceptionOrder(ref DataTable theExceptionDT, string exceptionType, string exceptionDetailParam, DataRow curDR){
   string exceptionCat = string.Empty;
    string exceptionDetail = string.Empty;
    DataRow newExceptionDR = theExceptionDT.NewRow();
    List<Object> itemList = curDR.ItemArray.ToList();
    exceptionCat = exceptionType;
    exceptionDetail = exceptionDetailParam;

     switch(exceptionType){
            case "RDD":
                exceptionCat = "RDD<3D";
                exceptionDetail = "订单计划到货日期在3天内，需确认是否录入订单或延单";
                break;
            case "订单价格差异":
                exceptionDetail = "客户订单产品与惠氏产品存在价格差异，需确认是否录入订单并跟进价差问题";
                break; 
            case "产品主数据缺失":
                exceptionDetail = "客户对应的惠氏产品主数据缺失，需提供惠氏产品主数据";
                break;
            case "产品规格差异":
                exceptionDetail = string.Format("客户订单产品规格异常，需确认录入数量");
                break;
            case "客户订单删单":
                exceptionDetail = string.Format("客户订单删单，请确认该如何处理");
                break;
            case "不录入系统":
                exceptionDetail = string.Format("不录品，请确认该如何处理");
                break;
            case "门店订单":
                exceptionDetail = string.Format("门店订单，请确认该如何处理");
                break;
            default : /* 可选的 */
               break; 
        }

    itemList.Add(exceptionCat);
    itemList.Add(exceptionDetail);
    newExceptionDR.ItemArray = itemList.ToArray();
    theExceptionDT.Rows.Add(newExceptionDR);
}

/// <summary>
/// 
/// </summary>
/// <param name="sourceExceptionOrderItemsDT"></param>
/// <returns></returns>
public DataTable writeToExceptionDT(DataTable sourceExceptionOrderItemsDT){  // , ref List<string> orderNumberList
    DataTable curExceptionDT = finalExceptionDT.Clone();
    
    foreach(DataRow dr in sourceExceptionOrderItemsDT.Rows){
        // string orderNumber = dr["客户PO"].ToString();
       // if(!orderNumberList.Contains(orderNumber)) orderNumberList.Add(orderNumber);
        DataRow newDR = curExceptionDT.NewRow();
        newDR["RPA获取订单日期及时间"] = dr["读单日期"];
        newDR["客户名称"] = dtRow_ModuleSettings["customer_name"];
        newDR["客户订单日期及时间"] = dr["实际订单日期"];
        newDR["客户订单计划到货日期"] = dr["RDD"];
        newDR["门店/大仓编号"] = dr["大仓号"];
        newDR["客户订单号（POID）"] = dr["POID"];
        newDR["订单类型/Event"] = dr["订单类型"]; // 默认是ZNBA，网页上展示的是直流CPO和直送CPO，没从网页抓取，所以只取导出的excel文件中的值
        newDR["客户产品编码"] = dr["商品编号"];
        newDR["客户产品名称"] = dr["商品名称"];
        newDR["客户产品箱数"] = dr["订货量"];
        newDR["客户产品单价"] = dr["价格"];
        newDR["客户产品总价"] = dr["物美总价"];
        newDR["扣点"] = dr["扣点"];
        // newDR["实际扣点"] = dr["allowance_total"];
        newDR["客户订单总金额"] = dr["物美订单总金额"]; // 需要原始订单整理期间填充
        newDR["惠氏客户Sold to"] = dr["sold to"];
        newDR["惠氏客户Ship to"] = dr["ship to"];
        newDR["惠氏客户名称"] = dtRow_ModuleSettings["客户区域"].ToString();
        newDR["惠氏POID"] = dr["POID"];
        newDR["惠氏产品编码"] = dr["惠氏编码"];
        newDR["惠氏产品名称"] = dr["惠氏产品名称"];
        newDR["惠氏产品箱数"] = dr["订购箱数"];
        newDR["惠氏产品箱价"] = Math.Round(toDecimalConvert(dr["惠氏单价"]) * toDecimalConvert(dr["惠氏规格"]), 2);
        newDR["惠氏产品单价"] = Math.Round(toDecimalConvert(dr["惠氏单价"]), 2);

        newDR["惠氏订单总金额"] = dr["惠氏订单总金额"]; // 需要原始订单整理期间填充
        newDR["折后订单总金额"] = dr["惠氏订单总折扣价"]; // 需要原始订单整理期间填充
        newDR["产品备注1（紧缺品）"] = dr["紧缺"];
        // newDR["产品备注2（彩箱/整箱）"] = dr["整箱"];
        newDR["产品单价价差(未税）"] = dr["单价价差"];
        newDR["惠氏订单总金额价差 (未税）"] = dr["总价价差"];

        newDR["异常分类"] = dr["异常分类"];
        newDR["异常详细描述"] = dr["异常详细描述"];
        curExceptionDT.Rows.Add(newDR);
    }
   return curExceptionDT;
}


/// <summary>
/// clean order 整理成DMS Tracker格式
/// </summary>
/// <param name="cleanOrderItemsMappedToWyethDT"></param>
/// <returns></returns>
public List<string> writeToDMSTracker(DataTable cleanOrderItemsMappedToWyethDT){
    List<string> orderNumberList = new List<string>{};

    foreach(DataRow dr in cleanOrderItemsMappedToWyethDT.Rows){
        string orderNumber = dr["POID"].ToString();
        if(!orderNumberList.Contains(orderNumber)) orderNumberList.Add(orderNumber);
        
        string 经销商代码 = dr["大仓号"].ToString();
        DataTable soldToShipToDT = (DataTable)dtRow_ModuleSettings["soldToShipToDT"];
        DataRow[] ststDRs = soldToShipToDT.Select(string.Format("`DC编号`='{0}'", 经销商代码));
        string DMS账号 = string.Empty;
        string 付款方式 = string.Empty;
        string 门店 = string.Empty;
        if(ststDRs.Length != 0){
            DMS账号 = ststDRs[0]["DMS账号"].ToString();
            付款方式 = ststDRs[0]["支付方式"].ToString();
            门店 = ststDRs[0]["门店"].ToString();
        }
        // 一个订单匹配中的客户产品码匹配到多个惠氏产品码，这时候需要合并产品行数量
        DataRow[] existingDRs = dmsTrackerDT.Select(string.Format("`POID（客户订单号）`='{0}' and `产品名称（惠氏SKU 代码）`='{1}'", dr["POID"].ToString(), dr["惠氏编码"].ToString()));
        if(existingDRs.Length > 0){
            // 合并产品行数量
            foreach(DataRow existingDR in dmsTrackerDT.Rows){
                if(existingDR["POID（客户订单号）"].ToString() == dr["POID"].ToString() && existingDR["产品名称（惠氏SKU 代码）"].ToString() == dr["惠氏编码"].ToString()){
                    Console.WriteLine("{0}， {1}", existingDR["数量（箱）"], dr["订单箱数"]);
                    existingDR["数量（箱）"] = toIntConvert(existingDR["数量（箱）"]) + toIntConvert(dr["订单箱数"]);
                }
            }
        }else{
            DataRow dmsTrackerDR = dmsTrackerDT.NewRow();
            dmsTrackerDR["大仓账号"] = DMS账号;
            // dmsTrackerDR["大仓密码"]
            dmsTrackerDR["付款方式（赊销/现金）"] = 付款方式;
            dmsTrackerDR["读单日期"] = dr["读单日期"];
            dmsTrackerDR["客户要求到货日期"] = dr["RDD"];
            dmsTrackerDR["SoldToCode"] = dr["sold To"];
            dmsTrackerDR["ShipToCode"] = dr["ship to"];
            dmsTrackerDR["Customer Name"] = 门店;
            dmsTrackerDR["POID（客户订单号）"] = dr["POID"];
            dmsTrackerDR["产品名称（惠氏SKU 代码）"] = dr["惠氏编码"];
            dmsTrackerDR["数量（箱）"] = dr["订购箱数"];
            dmsTrackerDT.Rows.Add(dmsTrackerDR);
        }
    }
    // Convert.ToInt32("2dasd");
    return orderNumberList;
}

public DataTable getCleanOrders(DataTable allDT, DataTable exceptionDT){
    DataTable cleanOrdersDT = allDT.Clone();
    foreach(DataRow dr in allDT.Rows){
        string orderNumber = dr["订单号"].ToString();
        DataRow[] drs = exceptionDT.Select(string.Format("`客户订单号（POID）`='{0}'", orderNumber));
        if(drs.Length == 0){
            cleanOrdersDT.ImportRow(dr);
            if(!cleanOrderList.Contains(orderNumber)) {cleanOrderList.Add(orderNumber);}  // 收集clean 订单号，用于批量查询导出pdf
        }
    }
    return cleanOrdersDT;
}

/// <summary>
/// 根据产品行合并异常信息
/// </summary>
/// <param name="exceptionDT"></param>
/// <returns></returns>
public DataTable MergeExceptionDTbyProductRow(DataTable exceptionDT){
    DataTable distinctOrderItemDT = exceptionDT.DefaultView.ToTable(true, new string[]{"客户订单号（POID）", "客户产品编码"});
    DataTable mergedExceptionDT = exceptionDT.Clone();

    foreach(DataRow dr in distinctOrderItemDT.Rows){
        string orderNumber = dr["客户订单号（POID）"].ToString();
        string customerSku = dr["客户产品编码"].ToString();
        DataRow[] drs = exceptionDT.Select(string.Format("`客户订单号（POID）`='{0}' and 客户产品编码='{1}'", orderNumber, customerSku));
        
        List<string> exceptionCategoryList = new List<string>{};
        List<string> exceptionDetailList = new List<string>{};
        DataRow finalDataRow = drs[0];
        foreach(DataRow exceptionDR in drs){
            if(!string.IsNullOrEmpty(exceptionDR["异常分类"].ToString())){
                exceptionCategoryList.Add(exceptionDR["异常分类"].ToString());
                exceptionDetailList.Add(exceptionDR["异常详细描述"].ToString());
            }
        }
        finalDataRow["异常分类"] = string.Join(exceptionSeperator, exceptionCategoryList);
        finalDataRow["异常详细描述"] = string.Join(exceptionSeperator, exceptionDetailList);
        mergedExceptionDT.ImportRow(finalDataRow);
    }
    return mergedExceptionDT;
}