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
    彩箱装品检查
};
public string exceptionSeperator = "|";
public string exceptionContactSumbol = "；";

public void Run()
{
    //在这里编写您的代码
    // newOrdersTmpDT    只有order_number, dc_no
    // origOrdersFromSheetDT     订单详情
    
    dmsTrackerDT.Columns["POID（客户订单号）"].DataType = typeof(string);
    finalExceptionDT.Columns["惠氏POID"].DataType = typeof(string);

    initNewOrdersDT(); // 订单行遍历处理后输出 newOrdersDT
    
    DataTable exceptionsDT = newOrdersDT.Clone();
    exceptionsDT.Columns.Add("异常分类", typeof(string));
    exceptionsDT.Columns.Add("异常详细描述", typeof(string));
    
    // 新增的订单 => Exception 订单模板
    origOrdersMappingToExceptionOrders(ref exceptionsDT);

    // 删单 => Exception 订单模板
    deletedOrdersMappingToExceptionOrders(ref exceptionsDT);
    
    // printDT(exceptionsDT);
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
                Console.WriteLine("orderNumber: {0}", orderNumber);

        DataRow[] orderDRs = origOrdersFromSheetDT.Select(string.Format("采购单号 = '{0}'", orderNumber));  // 根据订单号筛选产品行详情
        // 处理订单每一产品行
        // 以下三个值是订单级别汇总
        decimal 客户订单总金额 = 0m;
        decimal 惠氏订单总金额 = 0m;
        decimal 惠氏订单总折扣价 = 0m;
        bool hasPriceException = false;
        bool hasSkuMappingException = false;
        bool hasSizeException = false;
        bool hasRDDException = false;
        bool 存在不录品 = false;
        bool 彩箱装品 = false;

        List<DataRow> origFullOrderList = new List<DataRow>{};
        // 遍历产品行开始
        foreach(DataRow orderItemDR in orderDRs){
            DataRow newOrderDR = newOrdersDT.NewRow();
            // 构建newOrderDR
            rowMappedToNewOrderRow(orderDetailsCols, orderItemDR, ref newOrderDR); // 渲染到新版订单模板上
            // SKU Mapping 不上
            if(string.IsNullOrEmpty(newOrderDR["惠氏编码"].ToString())){
                if(!hasSkuMappingException){ // 如果为假，则赋值为真，此单仅为真
                    hasSkuMappingException = true;
                }
            }

            // 规格差异, 即 惠氏订购箱数 不为整数
            if(toIntConvert(newOrderDR["惠氏订购箱数"]) != toDecimalConvert(newOrderDR["惠氏订购箱数"])){
                 if(!hasSizeException){ // 如果为假，则赋值为真，此单仅为真
                    hasSizeException = true;
                }
            }

            // 价格差异
            // 单价价差必须等于0为clean 订单
            Console.WriteLine("单价价差: {0}", newOrderDR["单价价差"]);
            if(toDecimalConvert(newOrderDR["单价价差"]) != 0m){ //  || toDecimalConvert(newOrderDR["总价价差"]) > 15m
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
           if(newOrderDR["彩箱装品"].ToString().Contains("不录入系统")){
               if(!存在不录品){ // 如果为假，则赋值为真，此单仅为真
                    存在不录品 = true;
                }
           }
           
            // 彩箱装
           if(newOrderDR["彩箱装品"].ToString().Contains("彩箱装")){
               if(!彩箱装品){ // 如果为假，则赋值为真，此单仅为真
                    彩箱装品 = true;
                }
           }
                       
            客户订单总金额 += toDecimalConvert(newOrderDR["大润发总价"]);
            惠氏订单总金额 += toDecimalConvert(newOrderDR["惠氏总价"]);
            惠氏订单总折扣价 += toDecimalConvert(newOrderDR["Net Value"]); 
            origFullOrderList.Add(newOrderDR);
        }
        
        // 遍历产品行结束
        Console.WriteLine("-------hasPriceException: {0}", hasPriceException);
        // 再次遍历产品行，设置订单总价，以及判断异常
        foreach(DataRow origFullOrderDR in origFullOrderList){
            origFullOrderDR["大润发订单总金额"] = 客户订单总金额;
            origFullOrderDR["惠氏订单总金额"] = 惠氏订单总金额;
            origFullOrderDR["惠氏订单总折扣价"] = 惠氏订单总折扣价;
            string exceptionType = string.Empty;
            string exceptionDetail = string.Empty;
            
            if(hasSkuMappingException){
                if(!string.IsNullOrEmpty(origFullOrderDR["惠氏编码"].ToString())){  // 惠氏编码 不为空
                    // exception为空
                    addToExceptionOrder(ref exceptionsDT, exceptionType, string.Empty, origFullOrderDR);
                }else{
                    addToExceptionOrder(ref exceptionsDT, ExceptionCategory.产品主数据缺失.ToString(), exceptionDetail, origFullOrderDR);
                }
            }
            
            if(hasSizeException){
                if(origFullOrderDR["规格差异"].ToString() == "0"){
                    // exception为空
                    addToExceptionOrder(ref exceptionsDT, exceptionType, string.Empty, origFullOrderDR);
                }else{
                    addToExceptionOrder(ref exceptionsDT, ExceptionCategory.产品规格差异.ToString(), exceptionDetail, origFullOrderDR);
                }
            }
            
            if(hasRDDException){
                if(toIntConvert(origFullOrderDR["RDD issue"]) >= 3){
                    // exception为空
                    addToExceptionOrder(ref exceptionsDT, exceptionType, string.Empty, origFullOrderDR);
                }else{
                    addToExceptionOrder(ref exceptionsDT, ExceptionCategory.RDD.ToString(), exceptionDetail, origFullOrderDR);
                }
            }
            Console.WriteLine("hasPriceException: {0}, 单价价差: {1}", hasPriceException, origFullOrderDR["单价价差"]);
            if(hasPriceException){
                if(toDecimalConvert(origFullOrderDR["单价价差"]) == 0m){
                    addToExceptionOrder(ref exceptionsDT, exceptionType, string.Empty, origFullOrderDR);
                }else{
                    // Console.WriteLine("2222");
                    exceptionDetail = $"客户订单产品与惠氏产品存在价格差异，需确认是否录入订单并跟进价差问题";
                    addToExceptionOrder(ref exceptionsDT, ExceptionCategory.订单价格差异.ToString(), exceptionDetail, origFullOrderDR);
                }
            }

            if(存在不录品){
                if(!origFullOrderDR["彩箱装品"].ToString().Contains("不录入系统")){
                    addToExceptionOrder(ref exceptionsDT, exceptionType, string.Empty, origFullOrderDR);
                }
                else{
                    addToExceptionOrder(ref exceptionsDT, ExceptionCategory.不录入系统.ToString(), exceptionDetail, origFullOrderDR);
                }
            }
            
            
            if(彩箱装品){
                if(!origFullOrderDR["彩箱装品"].ToString().Contains("彩箱装")){
                    addToExceptionOrder(ref exceptionsDT, exceptionType, string.Empty, origFullOrderDR);
                }
                else{
                    addToExceptionOrder(ref exceptionsDT, ExceptionCategory.彩箱装品检查.ToString(), exceptionDetail, origFullOrderDR);
                }
            }

            newOrdersDT.Rows.Add(origFullOrderDR);
        }
    }
}

public void deletedOrdersMappingToExceptionOrders(ref DataTable exceptionsDT){
    if(deletedOrderDT == null || deletedOrderDT.Rows.Count == 0){
        return;
    }
    
    foreach(DataRow dr in deletedOrderDT.Rows){
        string orderNumber = dr["order_number"].ToString();
        DataRow newOrderDR = newOrdersDT.NewRow();
        deletedDRowMappedToNewOrderRow(dr, ref newOrderDR); // 渲染到新版订单模板上
        string exceptionDetail = string.Empty;
        addToExceptionOrder(ref exceptionsDT, ExceptionCategory.客户订单删单.ToString(), exceptionDetail, newOrderDR);    
    }
}


public void initNewOrdersDT(){
    newOrdersDT = origOrdersFromSheetDT.Clone();
    // 惠氏编码	产品名称	紧缺品	规格	单价	箱价	订购箱数	惠氏总价	大润发总价	单价价差	RTM-惠氏价差	仓别	Ship to	Net Value	DC	惠氏订单编号	彩箱装品

    string[] addedColumns = new string[]{"订单号", "读单日期",  "惠氏编码", "惠氏产品名称", "紧缺", "惠氏规格", "惠氏单价", "惠氏箱价", "惠氏订购箱数", "惠氏总价", "大润发总价", "单价价差", "总价价差", "仓别", "ship to", "Net Value", "DC", "惠氏订单编号",  "彩箱装品", "sold to", "大仓号", "扣点", "RDD issue","大润发订单总金额","惠氏订单总金额", "惠氏订单总折扣价","客户名称", "区域", "惠氏客户名称"};
    List<string> objectColumns = new List<string>{"惠氏订购箱数", "惠氏单价", "大润发总价", "惠氏总价", "惠氏箱价", "惠氏规格", "Net Value", "单价价差", "总价价差", "RDD issue", "大润发订单总金额", "惠氏订单总金额", "惠氏订单总折扣价"};
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
    newOrdersDT.Columns["订单号"].SetOrdinal(1);
    newOrdersDT.Columns["读单日期"].SetOrdinal(2);
}

public void splitDCInfo(string 门店, ref string dcNo){
    dcNo = 门店.Substring(0, 4);
}

public string specialProductComment(string 惠氏产品码){
    string comment = string.Empty;
    DataTable specialProductsDT = (DataTable)dtRow_ModuleSettings["specialListDT"];
    // printDT(specialProductsDT);

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

public void getShipTo(string dcNo, ref string shipTo, ref string soldTo, ref string 扣点, ref string 仓别, ref string stst门店){
    DataTable soldToShipToDT = (DataTable)dtRow_ModuleSettings["soldToShipToDT"];
    DataRow[] drs = soldToShipToDT.Select(string.Format("`DC编号` = '{0}'", dcNo));
    if(drs.Length > 0){
        shipTo = drs[0]["Ship to"].ToString();
        soldTo = drs[0]["Sold to"].ToString();
        扣点 = drs[0]["discount"].ToString();
        仓别 = drs[0]["仓别"].ToString();
        stst门店 = drs[0]["门店"].ToString();
    }
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
        string 仓别 = string.Empty;
        string stst门店 = string.Empty;
        decimal 惠氏箱价 = 0m;

        splitDCInfo(orderItemDR["门店"].ToString(), ref dcNo); // 拆分dc_no 和 dc_name
        string customerSku = orderItemDR["货号"].ToString();
        getShipTo(dcNo, ref shipTo, ref soldTo, ref 扣点, ref 仓别, ref stst门店); // 给 shipTo 赋值，给扣点赋值

        // 将order 详情的每项赋值给新数据表行
        // 下载的订单表给当前数据表赋值
        foreach(DataColumn dcol in orderDetailsCols){
            string colName = dcol.ColumnName;
            newOrderDR[colName] = orderItemDR[dcol.ColumnName];
        }
        // "惠氏箱价", "惠氏订购箱数", "惠氏总价", "大润发总价", "单价价差", "总价价差", "仓别", "ship to", "sold to", "Net Value", "DC", "惠氏订单编号",  "彩箱装品", "RDD issue","大润发订单总金额"
        newOrderDR["订单号"] = string.Format("{0}.{1}", dcNo.Substring(1,3), orderItemDR["采购单号"]);
        newOrderDR["DC"] = 仓别 + "大润发";
        newOrderDR["大仓号"] = dcNo;
        newOrderDR["ship to"] = shipTo;
        newOrderDR["sold to"] = soldTo;
        newOrderDR["惠氏客户名称"] = stst门店;
        DateTime rddDate =  DateTime.Parse(orderItemDR["预计到货日"].ToString());
        DataRow wyethSKUMappingRow = getWyethMappingRow(customerSku);
        if(wyethSKUMappingRow != null){
            惠氏单价 = toDecimalConvert(wyethSKUMappingRow["wyeth_unit_price"]);
            惠氏规格 = toIntConvert(wyethSKUMappingRow["size"]);
            惠氏编码 = wyethSKUMappingRow["wyeth_material_no"].ToString();
            惠氏产品名称 = wyethSKUMappingRow["customer_product_name"].ToString();
            紧缺 = getConstraintProduct(惠氏编码.ToString());
            惠氏箱价 =  toDecimalConvert(wyethSKUMappingRow["wyeth_nps"]);
        }
        decimal 订货量 = toDecimalConvert(orderItemDR["订购数量"]);

        newOrderDR["惠氏产品名称"] = 惠氏产品名称;
        newOrderDR["惠氏编码"] = 惠氏编码;
        newOrderDR["紧缺"] = 紧缺;
        newOrderDR["惠氏单价"] = 惠氏单价;
        newOrderDR["惠氏箱价"] = 惠氏箱价;
        newOrderDR["惠氏规格"] = 惠氏规格;
        decimal 惠氏订购箱数 = 惠氏规格 != 0 ? 订货量/惠氏规格 : toDecimalConvert(orderItemDR["订购箱数"]);
        newOrderDR["惠氏订购箱数"] = Math.Round(惠氏订购箱数, 2);
        decimal 惠氏总价 = Math.Round(订货量 * 惠氏单价, 2);
        newOrderDR["惠氏总价"] = 惠氏总价;
        decimal 买价 = toDecimalConvert(orderItemDR["买价"]);

        decimal 客户产品行总金额 =  Math.Round(买价 * 订货量, 2);
        newOrderDR["大润发总价"] = 客户产品行总金额 ;
        newOrderDR["扣点"] = 扣点;

        decimal 扣点值 = fetchRateInDecimal(扣点);
        decimal 系统折扣价 = Math.Round(惠氏总价 * (1-扣点值), 2);
        newOrderDR["Net Value"] = 系统折扣价;
        // newOrderDR["规格差异"] = 物美规格 - 惠氏规格;
        newOrderDR["单价价差"] = 买价 - 惠氏单价;
        newOrderDR["总价价差"] = 客户产品行总金额 - 惠氏总价;
        int rddGapDays = DiffDays(DateTime.Parse(newOrderDR["读单日期"].ToString()), rddDate);
        newOrderDR["RDD issue"] = rddGapDays;
        string 惠氏订单编号 = newOrderDR["订单号"].ToString();

        string comment = specialProductComment(惠氏编码, customerSku, (DataTable)dtRow_ModuleSettings["specialListDT"]);
        Console.WriteLine("comment: {0}", comment);
        if(!string.IsNullOrEmpty(comment)){
            if(comment.Contains("有机彩箱装")){
                惠氏订单编号 = 惠氏订单编号 + "-cxz1";
            }else if(comment.Contains("铂臻彩箱装")){
                惠氏订单编号 = 惠氏订单编号 + "-cxz2";
            }
        }
        newOrderDR["惠氏订单编号"] = 惠氏订单编号;
        newOrderDR["彩箱装品"] = comment;

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
        string 仓别 = string.Empty;
        decimal 惠氏箱价 = 0m;
        string stst门店 = string.Empty;
        dcNo = orderItemDRFromDB["dc_no"].ToString();
        string customerSku = orderItemDRFromDB["product_code"].ToString();
        getShipTo(dcNo, ref shipTo, ref soldTo, ref 扣点, ref 仓别, ref stst门店); // 给 shipTo 赋值，给扣点赋值

        // 将order 详情的每项赋值给新数据表行
        // 下载的订单表给当前数据表赋值
        Dictionary<string, string> ordersColumnMapping = new  Dictionary<string, string>{
            {"store_location", "门店"},
            {"order_number", "采购单号"},
            {"order_type", "采购单类型"},
            {"product_code", "货号"},
            {"product_name", "品名"},
            {"size", "规格"},
            {"order_qty", "订购数量"},
            {"uom", "订购单位"},
            {"order_cases", "订购箱数"},
            {"price", "买价"},
            {"promotional_periods", "促销期数"},
            {"order_date", "创单日期"},
            {"received_qty","已收货数量"},
            {"must_arrived_by", "预计到货日"},
            {"actual_received_at","实际收货日"}
        };

        foreach(var kv in ordersColumnMapping){
            string colName = kv.Key;
            newOrderDR[kv.Value] = orderItemDRFromDB[ kv.Key];
        }
        // "惠氏箱价", "惠氏订购箱数", "惠氏总价", "大润发总价", "单价价差", "总价价差", "仓别", "ship to", "sold to", "Net Value", "DC", "惠氏订单编号",  "彩箱装品", "RDD issue","大润发订单总金额"
        newOrderDR["订单号"] = orderItemDRFromDB["order_number"];
        newOrderDR["DC"] = 仓别 + "大润发";
        newOrderDR["大仓号"] = dcNo;
        newOrderDR["ship to"] = shipTo;
        newOrderDR["sold to"] = soldTo;
        newOrderDR["惠氏客户名称"] = stst门店;

        DateTime rddDate =  DateTime.Parse(orderItemDRFromDB["must_arrived_by"].ToString());
        DataRow wyethSKUMappingRow = getWyethMappingRow(customerSku);
        if(wyethSKUMappingRow != null){
            惠氏单价 = toDecimalConvert(wyethSKUMappingRow["wyeth_unit_price"]);
            惠氏规格 = toIntConvert(wyethSKUMappingRow["size"]);
            惠氏编码 = wyethSKUMappingRow["wyeth_material_no"].ToString();
            惠氏产品名称 = wyethSKUMappingRow["customer_product_name"].ToString();
            紧缺 = getConstraintProduct(惠氏编码.ToString());
            惠氏箱价 =  toDecimalConvert(wyethSKUMappingRow["wyeth_nps"]);
        }
        decimal 订货量 = toDecimalConvert(orderItemDRFromDB["order_qty"]);

        newOrderDR["惠氏产品名称"] = 惠氏产品名称;
        newOrderDR["惠氏编码"] = 惠氏编码;
        newOrderDR["紧缺"] = 紧缺;
        newOrderDR["惠氏单价"] = 惠氏单价;
        newOrderDR["惠氏箱价"] = 惠氏箱价;
        newOrderDR["惠氏规格"] = 惠氏规格;
        decimal 惠氏订购箱数 = 惠氏规格 != 0 ? 订货量/惠氏规格 : toDecimalConvert(orderItemDRFromDB["order_cases"]);
        newOrderDR["惠氏订购箱数"] = Math.Round(惠氏订购箱数, 2);
        decimal 惠氏总价 = Math.Round(订货量 * 惠氏单价, 2);
        newOrderDR["惠氏总价"] = 惠氏总价;
        decimal 买价 = toDecimalConvert(orderItemDRFromDB["price"]);

        decimal 客户产品行总金额 =  Math.Round(买价 * 订货量, 2);
        newOrderDR["大润发总价"] = 客户产品行总金额 ;
        newOrderDR["扣点"] = 扣点;

        decimal 扣点值 = fetchRateInDecimal(扣点);
        decimal 系统折扣价 = Math.Round(惠氏总价 * (1-扣点值), 2);
        newOrderDR["Net Value"] = 系统折扣价;
        // newOrderDR["规格差异"] = 物美规格 - 惠氏规格;
        newOrderDR["单价价差"] = Math.Round(买价 - 惠氏单价, 2);
        newOrderDR["总价价差"] = 客户产品行总金额 - 惠氏总价;
        int rddGapDays = DiffDays(DateTime.Parse(newOrderDR["读单日期"].ToString()), rddDate);
        newOrderDR["RDD issue"] = rddGapDays;
        string comment = specialProductComment(惠氏编码, customerSku, (DataTable)dtRow_ModuleSettings["specialListDT"]);
        newOrderDR["惠氏订单编号"] = orderItemDRFromDB["wyeth_POID"];
        newOrderDR["彩箱装品"] = comment;
        newOrderDR["客户名称"] = dtRow_ModuleSettings["customer_name"];
        newOrderDR["区域"] = dtRow_ModuleSettings["区域"];
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
            case "彩箱装品检查":
                exceptionDetail = string.Format("彩箱装品，请确认该如何处理");
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
        newDR["客户订单日期及时间"] = dr["创单日期"];
        newDR["客户订单计划到货日期"] = dr["预计到货日"];
        newDR["门店/大仓编号"] = dr["大仓号"];
        newDR["客户订单号（POID）"] = dr["采购单号"];
        // newDR["订单类型/Event"] = dr["订单类型"]; // 默认是ZNBA，网页上展示的是直流CPO和直送CPO，没从网页抓取，所以只取导出的excel文件中的值
        newDR["客户产品编码"] = dr["货号"];
        newDR["客户产品名称"] = dr["品名"];
        newDR["客户产品箱数"] = dr["订购箱数"];
        newDR["客户产品单位数量"] = dr["订购数量"];
        newDR["客户产品单价"] = dr["买价"];
        newDR["客户产品总价"] = dr["大润发总价"];
        newDR["扣点"] = dr["扣点"];
        // newDR["实际扣点"] = dr["allowance_total"];
        newDR["客户订单总金额"] = dr["大润发订单总金额"]; // 需要原始订单整理期间填充
        newDR["惠氏客户Sold to"] = dr["sold to"];
        newDR["惠氏客户Ship to"] = dr["ship to"];
        newDR["惠氏客户名称"] = dr["惠氏客户名称"];
        newDR["惠氏POID"] = dr["惠氏订单编号"];
        newDR["惠氏产品编码"] = dr["惠氏编码"];
        newDR["惠氏产品名称"] = dr["惠氏产品名称"];
        newDR["惠氏产品箱数"] = dr["惠氏订购箱数"];
        newDR["惠氏产品单价"] = dr["惠氏单价"];
        newDR["惠氏产品箱价"] = Math.Round(toDecimalConvert(dr["惠氏箱价"]), 2);
        newDR["惠氏订单总金额"] = dr["惠氏总价"]; // 需要原始订单整理期间填充
        newDR["折后订单总金额"] = dr["Net Value"]; // 需要原始订单整理期间填充
        newDR["产品备注1（紧缺品）"] = dr["紧缺"];
        // newDR["产品备注2（彩箱/整箱）"] = dr["整箱"];
        newDR["产品单价价差(未税）"] = dr["单价价差"];
        newDR["惠氏订单总金额价差 (未税）"] = dr["总价价差"];
        newDR["异常分类"] = dr["异常分类"];
        newDR["异常详细描述"] = dr["异常详细描述"];
        newDR["客户产品规格"] = dr["规格"];
        newDR["惠氏产品规格"] = dr["惠氏规格"];
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
        string orderNumber = dr["采购单号"].ToString();
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
        DataRow[] existingDRs = dmsTrackerDT.Select(string.Format("`POID（客户订单号）`='{0}' and `产品名称（惠氏SKU 代码）`='{1}'", dr["采购单号"].ToString(), dr["惠氏编码"].ToString()));
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
            dmsTrackerDR["客户要求到货日期"] = dr["预计到货日"];
            dmsTrackerDR["SoldToCode"] = dr["sold To"];
            dmsTrackerDR["ShipToCode"] = dr["ship to"];
            dmsTrackerDR["Customer Name"] = 门店;
            dmsTrackerDR["POID（客户订单号）"] = dr["惠氏订单编号"].ToString();
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
        string orderNumber = dr["采购单号"].ToString();
        string customerSku = dr["货号"].ToString();
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
        Console.WriteLine("客户订单号（POID）: {0}, 客户产品编码: {1}", orderNumber, customerSku);
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

/// <summary>
/// 辅助打印数据表方法
/// </summary>
/// <param name="theDT"></param>
public void printDT(DataTable theDT){
    DataColumnCollection dcols = theDT.Columns;
    foreach(DataRow dr in theDT.Rows){
        foreach(DataColumn dc in dcols){
            Console.WriteLine("column:{0}, value:{1}", dc.ColumnName, dr[dc.ColumnName]);
        }
        Console.WriteLine("---------------------------------------");
    }
}