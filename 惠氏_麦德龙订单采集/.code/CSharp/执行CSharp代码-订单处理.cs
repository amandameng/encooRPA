public string exceptionSeperator = "|";

public enum ExceptionCategory
{
    RDD,
    产品主数据缺失,
    CPO直流,
    退货单,
    产品数量异常,
    产品规格错误,
    不录品
};

//代码执行入口，请勿修改或删除
public void Run()
{
    //在这里编写您的代码
    DataTable cleanOrderItemsMappedToWyethDT = orderItemsMappedToWyethDT.Copy();
    DataTable exceptionOrderWithItemsDT = orderItemsMappedToWyethDT.Clone();
        //Console.WriteLine("cleanOrderItemsMappedToWyethDT: {0}", cleanOrderItemsMappedToWyethDT.Rows.Count);

    exceptionOrderWithItemsDT.Columns.Add("异常分类", typeof(string));
    exceptionOrderWithItemsDT.Columns.Add("异常详细描述", typeof(string));

    List<string> exceptionOrderList = new List<string>{};
     /*
        产品编码mapping异常
       一个订单的一个产品mapping不上，对一整个订单是如何处理？是不是整单不录单 ： 是 
    */
    DataTable skuMappingExceptionDT = exceptionOrderWithItemsDT.Clone();
    addSkuException(cleanOrderItemsMappedToWyethDT, ref skuMappingExceptionDT, ref exceptionOrderList);

    /*
        产品数量为0异常
        一个订单的一个产品mapping不上，对一整个订单是如何处理？是不是整单不录单 ： 是 
    */
    DataTable QtyAndSalesExceptionDT = exceptionOrderWithItemsDT.Clone(); // 产品数量金额异常
    addQtyZeroException(cleanOrderItemsMappedToWyethDT, ref QtyAndSalesExceptionDT, ref exceptionOrderList);
     
     /*
        不录品异常
        一个订单的一个产品是”不录品“，对一整个订单是如何处理？是不是整单不录单 ： 是 
    */
    DataTable notIntoDMSExceptionDT = exceptionOrderWithItemsDT.Clone();
    addNotIntoDMSException(cleanOrderItemsMappedToWyethDT, ref notIntoDMSExceptionDT, ref exceptionOrderList);


   // 直流CPO，RDD，退货单
    DataTable CPOExceptionExceptionDT = exceptionOrderWithItemsDT.Clone();
    DataTable RDDExceptionDT = exceptionOrderWithItemsDT.Clone();
    DataTable 退货单ExceptionDT = exceptionOrderWithItemsDT.Clone();

    /*
        直流CPO异常 - 产品规格或者彩箱装异常
    */
    DataRow[] cpoDrs = orderItemsWithInvalidCPODT.Select("直流CPO异常 = 'Y'");
    foreach(DataRow dr in cpoDrs){
        addCPOException(cleanOrderItemsMappedToWyethDT, ref CPOExceptionExceptionDT, ExceptionCategory.CPO直流.ToString(), dr, ref exceptionOrderList);
    }

    if(!orderItemsWithInvalidCPODT.Columns.Contains("订单PrintPO")) orderItemsWithInvalidCPODT.Columns.Add("订单PrintPO", typeof(string));

    DataTable orderWithExceptionDT = orderItemsWithInvalidCPODT.DefaultView.ToTable(true, new string[]{"订单号", "直流CPO异常", "RDD异常", "业务类型", "cpo异常原因"});

    foreach(DataRow dr in orderWithExceptionDT.Rows){
        // RDD
        if(dr["RDD异常"].ToString() == "Y"){
            addToExceptionOrder(cleanOrderItemsMappedToWyethDT, ref RDDExceptionDT, ExceptionCategory.RDD.ToString(), dr, ref exceptionOrderList);
        }
        
        // 退货单
        if(dr["业务类型"].ToString() == "自营采购退单"){
            List<string> tmpExceptionList = new List<string>{};
            addToExceptionOrder(cleanOrderItemsMappedToWyethDT, ref 退货单ExceptionDT, ExceptionCategory.退货单.ToString(), dr, ref tmpExceptionList);
        }
    }

    // 遍历所有订单Items 更新订单printPO值
    foreach(DataRow dr in orderItemsWithInvalidCPODT.Rows){
        string orderNumber = dr["订单号"].ToString();
        // set PrintPO
        DataRow[] orderDrs = totalOrderListDT.Select(string.Format("订单号='{0}'", orderNumber));
        if(orderDrs.Length > 0) dr["订单PrintPO"] = orderDrs[0]["订单PrintPO"];
    }

    // 存在exception的订单需要从clean 移除
    if(exceptionOrderList.Count > 0){
        foreach(string orderNumber in exceptionOrderList){
            DataRow[] drs = cleanOrderItemsMappedToWyethDT.Select(string.Format("订单号='{0}'", orderNumber));
            foreach(DataRow dr in drs){
                cleanOrderItemsMappedToWyethDT.Rows.Remove(dr);
            }
        }
    }
    // clean订单里面排除退货单
    if(退货单ExceptionDT.Rows.Count > 0){
        foreach(DataRow 退货dr in 退货单ExceptionDT.Rows){
            string orderNumber = 退货dr["订单号"].ToString();
            DataRow[] drs = cleanOrderItemsMappedToWyethDT.Select(string.Format("订单号='{0}'", orderNumber));
            foreach(DataRow dr in drs){
                cleanOrderItemsMappedToWyethDT.Rows.Remove(dr);
            }
        }
    }

    // Exception 表头
    // RPA获取订单日期及时间	客户名称	客户订单日期及时间	客户订单计划到货日期	门店/大仓编号	客户订单号（POID）	订单类型/Event	客户产品编码	客户产品名称	客户产品规格	客户产品单位数量	客户产品箱数	客户产品单价	客户产品总价	扣点	实际扣点	客户订单总金额/折后订单总金额	客户订单状态（正常/取消）	惠氏客户Sold to	惠氏客户Ship to	惠氏客户名称	惠氏POID	惠氏产品编码	惠氏产品名称	惠氏产品规格	惠氏产品箱数	惠氏产品单价	惠氏产品箱价	惠氏订单总金额    折后订单总金额	产品备注1（紧缺品）	产品备注2（彩箱/整箱）	异常分类	异常详细描述	备注							
    
    // Metro Tracker 订单明细 表头
    // 供商编码	供商名称	门店/仓库编码	门店/仓库名称	订单号	订货日期	计划到货日期	麦德龙总部商品编码	商品名称	商品编码	国条	种类	供应商商品编号	订货量	订货单位	邮报编码	CPO单号	大仓编码	提单日期	T列门店/仓库编码（原）	订单日期	RDD	DC	Ship to	Sold To	PO number	产品描述	惠氏sku	彩箱装	紧缺品	箱数	惠氏箱价	总计	
    DataTable skuMappingDBExceptionDT= writeToExceptionDT(skuMappingExceptionDT);
    
    DataTable CPODBExceptionDT = writeToExceptionDT(CPOExceptionExceptionDT);

    DataTable RDDDBExceptionDT = writeToExceptionDT(RDDExceptionDT);
    
    DataTable 退货单DBExceptionDT = writeToExceptionDT(退货单ExceptionDT);

    DataTable QtyAndSalesDBExceptionDT = writeToExceptionDT(QtyAndSalesExceptionDT);
    DataTable notIntoDMSDBExceptionDT = writeToExceptionDT(notIntoDMSExceptionDT);

    // 合并异常订单
    finalExceptionDT.Merge(skuMappingDBExceptionDT);
    finalExceptionDT.Merge(CPODBExceptionDT);
    finalExceptionDT.Merge(RDDDBExceptionDT);
    // finalExceptionDT.Merge(退货单DBExceptionDT);
    finalExceptionDT.Merge(QtyAndSalesDBExceptionDT);
    finalExceptionDT.Merge(notIntoDMSDBExceptionDT); // 不录入DMS

    // DMS Tracker 表头
    // 大仓账号	大仓密码	"付款方式\\n(赊销/现金）"	读单日期	SoldToCode	ShipToCode	Customer Name	"POID\n（客户订单号）"	"产品名称\\n（惠氏SKU 代码）"	"数量\\n(箱）"
    List<string> cleanOrderNumberList = writeToDMSTracker(cleanOrderItemsMappedToWyethDT); // 输出 dmsTrackerDT

    string cleanOrderPrintURL = setPrintPOURL(cleanOrderNumberList);
    
    List <string> 退货单OrderList =  退货单DBExceptionDT.Rows.Cast<DataRow>().Select<DataRow, string>(dr => dr["客户订单号（POID）"].ToString()).ToList();
    string 退货单OrderListPrintURL = setPrintPOURL(退货单OrderList);
    
    // 将退货单从Exception订单中移除
    //printDT(finalExceptionDT);
    
    Console.WriteLine("cleanOrderItemsMappedToWyethDT: {0}, finalExceptionDT: {1}", cleanOrderItemsMappedToWyethDT.Rows.Count, finalExceptionDT.Rows.Count);
    
    //printDT(退货单DBExceptionDT);

    foreach(DataRow exceptionDR in 退货单DBExceptionDT.DefaultView.ToTable(true, new string[]{"客户订单号（POID）"}).Rows){
         string orderNumber = exceptionDR["客户订单号（POID）"].ToString();
         DataRow[] drs = finalExceptionDT.Select(string.Format("`客户订单号（POID）`='{0}'", orderNumber));
            foreach(DataRow dr in drs){
                finalExceptionDT.Rows.Remove(dr);
            }
    }
    
    //Convert.ToInt32("sdsds");
    exceptionOrderList = finalExceptionDT.Rows.Cast<DataRow>().Select<DataRow, string>(dr => dr["客户订单号（POID）"].ToString()).ToList();
    string exceptionOrderPrintURL = setPrintPOURL(exceptionOrderList);
    
    foreach(DataRow dr in orderCatResultDT.Rows){
        string printURL = string.Empty;
        DataTable orderRelatedDBDT = new DataTable();
        switch(dr["orderCat"].ToString()){
            case "DMS_Tracker" :
                printURL = cleanOrderPrintURL;
                orderRelatedDBDT= dmsTrackerDT;
                break; 
            case "Exception"  :
                printURL= exceptionOrderPrintURL;
                orderRelatedDBDT = MergeExceptionDTbyProductRow(finalExceptionDT);
                break; 
            case "退货单"  :
                printURL= 退货单OrderListPrintURL;
                orderRelatedDBDT = 退货单DBExceptionDT;
                break; 
            default : /* 可选的 */
               break; 
        }
        dr["orderCatPrintURL"]  = printURL;
        dr["orderRelatedDT"] = orderRelatedDBDT;
    }
    
    // Convert.ToInt32("sd");
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


//在这里编写您的函数或者类
public DataTable writeToExceptionDT(DataTable sourceExceptionOrderItemsDT){
    DataTable curExceptionDT = finalExceptionDT.Clone();
    
    foreach(DataRow dr in sourceExceptionOrderItemsDT.Rows){
        string orderNumber = dr["订单号"].ToString();
        // if(!orderNumberList.Contains(orderNumber)) orderNumberList.Add(orderNumber);
        DataRow newDR = curExceptionDT.NewRow();
        newDR["RPA获取订单日期及时间"] = dr["提单日期"];
        newDR["客户名称"] = customer_name;
        newDR["客户订单日期及时间"] = dr["订货日期"];
        newDR["客户订单计划到货日期"] = dr["RDD"];
        newDR["门店/大仓编号"] = dr["门店/仓库编码"];
        newDR["客户订单号（POID）"] = dr["订单号"];
        newDR["订单类型/Event"] = dr["订单类型"];
        newDR["客户产品编码"] = dr["麦德龙总部商品编码"];
        newDR["客户产品名称"] = dr["商品名称"];
        newDR["客户产品规格"] = dr["订货单位"];
        newDR["客户产品箱数"] = dr["订货量"];
        newDR["惠氏客户Sold to"] = dr["Sold To"];
        newDR["惠氏客户Ship to"] = dr["Ship to"];
        newDR["惠氏客户名称"] = dr["DC"];
        newDR["惠氏POID"] = dr["PO number"];
        newDR["惠氏产品编码"] = dr["惠氏sku"];
        newDR["惠氏产品名称"] = dr["产品描述"];
        DataRow[] mappingSkuDRs = materialMasterDataDT.Select(string.Format("customer_material_no='{0}'",  dr["麦德龙总部商品编码"].ToString()));
        if(mappingSkuDRs.Length != 0){
            newDR["惠氏产品规格"] = mappingSkuDRs[0]["size"];
        }
        newDR["惠氏产品箱数"] = dr["箱数"];
        newDR["惠氏产品箱价"] = dr["惠氏箱价"];
        newDR["惠氏订单总金额"] = dr["总计"];
        newDR["产品备注1（紧缺品）"] = dr["紧缺"];
        newDR["产品备注2（彩箱/整箱）"] = dr["彩箱装"];
        newDR["异常分类"] = dr["异常分类"];
        newDR["异常详细描述"] = dr["异常详细描述"];
        curExceptionDT.Rows.Add(newDR);
    }
   return curExceptionDT;
}

// 大仓账号	大仓密码	"付款方式（赊销/现金）"	读单日期	SoldToCode	ShipToCode	Customer Name	"POID（客户订单号）"	"产品名称（惠氏SKU 代码）"	"数量（箱）"
public List<string> writeToDMSTracker(DataTable cleanOrderItemsMappedToWyethDT){
    List<string> orderNumberList = new List<string>{};
    if(!dmsTrackerDT.Columns.Contains("客户订单号")) dmsTrackerDT.Columns.Add("客户订单号", typeof(string));
    foreach(DataRow dr in cleanOrderItemsMappedToWyethDT.Rows){
        string orderNumber = dr["订单号"].ToString();
        if(!orderNumberList.Contains(orderNumber)) orderNumberList.Add(orderNumber);
        
        string shipTo = dr["Ship to"].ToString();
        DataRow[] ststDRs = soldToShipToDT.Select(string.Format("`Ship to`='{0}'", shipTo));
        string DMS账号 = string.Empty;
        string 付款方式 = string.Empty;
        string 门店 = string.Empty;
        if(ststDRs.Length != 0){
            DMS账号 = ststDRs[0]["DMS账号"].ToString();
            付款方式 = ststDRs[0]["支付方式"].ToString();
            门店 = ststDRs[0]["门店"].ToString();
        }
        DataRow dmsTrackerDR = dmsTrackerDT.NewRow();
        dmsTrackerDR["大仓账号"] = DMS账号;
        // dmsTrackerDR["大仓密码"]
        dmsTrackerDR["付款方式（赊销/现金）"] = 付款方式;
        dmsTrackerDR["读单日期"] = dr["提单日期"];
        dmsTrackerDR["客户要求到货日期"] = dr["RDD"];
        dmsTrackerDR["SoldToCode"] = dr["Sold To"];
        dmsTrackerDR["ShipToCode"] = dr["Ship to"];
        dmsTrackerDR["Customer Name"] = 门店;
        dmsTrackerDR["POID（客户订单号）"] = dr["PO number"];
        dmsTrackerDR["产品名称（惠氏SKU 代码）"] = dr["惠氏sku"];
        dmsTrackerDR["数量（箱）"] = dr["箱数"];
        dmsTrackerDR["客户订单号"] = orderNumber;
        dmsTrackerDT.Rows.Add(dmsTrackerDR);
    }
    return orderNumberList;
}

public string setPrintPOURL(List<string> orderNumberList){
    string url = "https://mdlvc.wumart.com/#index/cxpomana/metro/vcBatchPrint:orderNos={0}&print=true";
    string printUrl = string.Empty;
    
    if(orderNumberList.Count > 0){
        DataRow[] drs = totalOrderListDT.Select(string.Format("订单号 in ({0})", string.Join(",", orderNumberList)));
        List<string> orderPrintPOs = drs.Cast<DataRow>().Select<DataRow, string>(dr => dr["订单PrintPO"].ToString()).ToList();
        printUrl = string.Format(url, string.Join(",", orderPrintPOs));
    }
    return printUrl;
}

public void addToExceptionOrder(DataTable cleanOrderItemsMappedToWyethDT, ref DataTable theExceptionDT, string exceptionType, DataRow dr, ref List<string> exceptionOrderList){
    string orderNumber = dr["订单号"].ToString();
    DataRow[] cleanItemsdrs = cleanOrderItemsMappedToWyethDT.Select(string.Format("订单号='{0}'", orderNumber));
    Console.WriteLine("cleanItemsdrs Length: {0}", cleanItemsdrs.Length);
    string exceptionCat = string.Empty;
    string exceptionDetail = string.Empty;
    
    Console.WriteLine("cleanItemsdrs length: {0}", cleanItemsdrs.Length);
    
    foreach(DataRow mappedDr in cleanItemsdrs){
        DataRow newDR = theExceptionDT.NewRow();
        List<Object> itemList = mappedDr.ItemArray.ToList();
        switch(exceptionType){
            case "RDD":
                exceptionCat = "RDD<3D";
                exceptionDetail = "订单计划到货日期在3天内，需确认是否录入订单或延单";
                break; 
            case "退货单":
                exceptionCat = "退货单";
                exceptionDetail = "自营采购退单";
                break;
            default : /* 可选的 */
               break; 
        }
             
        itemList.Add(exceptionCat);
        itemList.Add(exceptionDetail);
        newDR.ItemArray =itemList.ToArray();
        theExceptionDT.Rows.Add(newDR);
        
        if(!exceptionOrderList.Contains(orderNumber)) exceptionOrderList.Add(orderNumber);
    }
}

public void addSkuException(DataTable cleanOrderItemsMappedToWyethDT, ref DataTable skuMappingExceptionDT, ref List<string> exceptionOrderList){
    List<string> orderItemIssueList = new List<string>{};
    DataRow[] drs = cleanOrderItemsMappedToWyethDT.Select("惠氏sku='' or 惠氏sku is null");
    foreach(DataRow dr in drs){
        string orderNumber = dr["订单号"].ToString();
        if(!orderItemIssueList.Contains(orderNumber)) orderItemIssueList.Add(orderNumber); // 添加orderNumber, 保持数据唯一
    }
    
    foreach(string orderNumber in orderItemIssueList){
        DataRow[] cleanItemsdrs = cleanOrderItemsMappedToWyethDT.Select(string.Format("订单号='{0}'", orderNumber));
        foreach(DataRow dr in cleanItemsdrs){
            DataRow newDR = skuMappingExceptionDT.NewRow();
            List<Object> itemList = dr.ItemArray.ToList();
            if(string.IsNullOrEmpty(dr["惠氏sku"].ToString())){
                itemList.Add(ExceptionCategory.产品主数据缺失.ToString());
                itemList.Add("客户对应的惠氏产品主数据缺失，需提供惠氏产品主数据");
            }else{
                itemList.Add(null);
                itemList.Add(null);
            }

            newDR.ItemArray = itemList.ToArray();
            skuMappingExceptionDT.Rows.Add(newDR);
        }
    }
    exceptionOrderList = exceptionOrderList.Union(orderItemIssueList).ToList();
}

public void addQtyZeroException(DataTable cleanOrderItemsMappedToWyethDT, ref DataTable QtyAndSalesExceptionDT, ref List<string> exceptionOrderList){
    List<string> orderItemIssueList = new List<string>{};
    DataRow[] drs = cleanOrderItemsMappedToWyethDT.Select("订货量=0");
    foreach(DataRow dr in drs){
        string orderNumber = dr["订单号"].ToString();
        if(!orderItemIssueList.Contains(orderNumber)) orderItemIssueList.Add(orderNumber); // 添加orderNumber, 保持数据唯一
    }
    
    foreach(string orderNumber in orderItemIssueList){
        DataRow[] cleanItemsdrs = cleanOrderItemsMappedToWyethDT.Select(string.Format("订单号='{0}'", orderNumber));
        foreach(DataRow dr in cleanItemsdrs){
            DataRow newDR = QtyAndSalesExceptionDT.NewRow();
            List<Object> itemList = dr.ItemArray.ToList();
            if(dr["订货量"].ToString() == "0"){
                itemList.Add(ExceptionCategory.产品数量异常.ToString());
                itemList.Add("产品行订货量为0");
            }else{
                itemList.Add(null);
                itemList.Add(null);
            }

            newDR.ItemArray = itemList.ToArray();
            QtyAndSalesExceptionDT.Rows.Add(newDR);
        }
    }
    exceptionOrderList = exceptionOrderList.Union(orderItemIssueList).ToList();
}

public void addCPOException(DataTable cleanOrderItemsMappedToWyethDT, ref DataTable CPOExceptionExceptionDT, string exceptionType, DataRow dr, ref List<string> exceptionOrderList){
    string orderNumber = dr["订单号"].ToString();
    string customerProductCode = dr["麦德龙总部商品编码"].ToString();

    DataRow[] drs = cleanOrderItemsMappedToWyethDT.Select(string.Format("订单号='{0}' and 麦德龙总部商品编码='{1}'", orderNumber, customerProductCode));
    DataRow itemDR = drs[0];
    DataRow newDR = CPOExceptionExceptionDT.NewRow();
    List<Object> itemList = itemDR.ItemArray.ToList();
    itemList.Add(ExceptionCategory.产品规格错误.ToString());
    itemList.Add(dr["CPO异常原因"]);
    newDR.ItemArray = itemList.ToArray();
    CPOExceptionExceptionDT.Rows.Add(newDR);
    if(!exceptionOrderList.Contains(orderNumber)) exceptionOrderList.Add(orderNumber);
}

public void addNotIntoDMSException(DataTable cleanOrderItemsMappedToWyethDT, ref DataTable notIntoDMSExceptionDT, ref List<string> exceptionOrderList){
    List<string> orderItemIssueList = new List<string>{};
    
    foreach(DataRow dr in cleanOrderItemsMappedToWyethDT.Rows){
        string orderNumber = dr["订单号"].ToString();
        string 惠氏sku = dr["惠氏sku"].ToString();
        string 客户Sku = dr["麦德龙总部商品编码"].ToString();

        string comment = specialProductComment(惠氏sku, 客户Sku, specialListDT);
        if(comment.Contains("不录")){
            if(!orderItemIssueList.Contains(orderNumber)) orderItemIssueList.Add(orderNumber); // 添加orderNumber, 保持数据唯一
        }
    }
    
    foreach(string orderNumber in orderItemIssueList){
        DataRow[] cleanItemsdrs = cleanOrderItemsMappedToWyethDT.Select(string.Format("订单号='{0}'", orderNumber));
        foreach(DataRow dr in cleanItemsdrs){
            string 惠氏sku = dr["惠氏sku"].ToString();
            string 客户Sku = dr["麦德龙总部商品编码"].ToString();
            string comment = specialProductComment(惠氏sku, 客户Sku, specialListDT);

            DataRow newDR = notIntoDMSExceptionDT.NewRow();
            List<Object> itemList = dr.ItemArray.ToList();
            if(comment.Contains("不录")){
                itemList.Add(ExceptionCategory.不录品.ToString());
                itemList.Add("不录品，请确认该如何处理");
            }else{
                itemList.Add(null);
                itemList.Add(null);
            }

            newDR.ItemArray = itemList.ToArray();
            notIntoDMSExceptionDT.Rows.Add(newDR);
        }
    }
    exceptionOrderList = exceptionOrderList.Union(orderItemIssueList).ToList();
}

// 查询特殊品某个产品comment
public string specialProductComment(string 惠氏产品码, string 客户产品码, DataTable specialListDT){
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