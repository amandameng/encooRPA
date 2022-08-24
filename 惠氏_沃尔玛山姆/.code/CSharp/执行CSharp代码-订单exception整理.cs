public enum ExceptionCategory
{
    RDD,
    产品主数据缺失,
    产品数量或者金额异常,
    门店订单,
    客户订单改单,
    客户percent异常,
    订单价格差异,
    不录单event,
    不录品
};
public string exceptionSeperator = "|";
public string exceptionContactSumbol = "；";

//代码执行入口，请勿修改或删除
public void Run()
{
    //在这里编写您的代码
    // dbOrdersDT 当前订单相关的所有历史订单
    // orderAndLinkDT order_number, document_link
    // ordersCountDT, orders_count, order_number

    if(!orderItemsMappedToWyethDT.Columns.Contains("订单价差")) orderItemsMappedToWyethDT.Columns.Add("订单价差", typeof(string)); // Exception 模板需要展示价差
    DataTable exceptionOrderWithItemsDT = orderItemsMappedToWyethDT.Clone();  // Excel 表头：  大区	收单日期	客户PO	DocID	经销商代码	订单日期	起运日期	取消日期	Type	  Event	Total Order Amount	备注	其他备注	行号	沃尔玛编码	订单箱数	沃尔玛单价/箱 Cost	Sold To	整箱	紧缺	POID	经销商简称	惠氏产品描述	惠氏编码	惠氏总价	惠氏折扣价	惠氏单价/箱	单价价差/箱
    exceptionOrderWithItemsDT.Columns.Add("异常分类", typeof(string));
    exceptionOrderWithItemsDT.Columns.Add("异常详细描述", typeof(string));

    // Exception
    DataTable RDDExceptionDT = exceptionOrderWithItemsDT.Clone();
    DataTable shopOrderExceptionDT = exceptionOrderWithItemsDT.Clone();
    DataTable percentExceptionDT = exceptionOrderWithItemsDT.Clone();
    DataTable skuMappingExceptionDT = exceptionOrderWithItemsDT.Clone();
    DataTable qtyAndSalesExceptionDT = exceptionOrderWithItemsDT.Clone();
    DataTable salesGapExceptionDT = exceptionOrderWithItemsDT.Clone();
    DataTable repeatedOrderExceptionDT = exceptionOrderWithItemsDT.Clone();
    DataTable notIntoDMSOrderExceptionDT = exceptionOrderWithItemsDT.Clone();
    DataTable notIntoDMSProductsExceptionDT = exceptionOrderWithItemsDT.Clone();

    DataTable orderNumbersDT = orderItemsMappedToWyethDT.DefaultView.ToTable(true, new string[]{"客户PO"});
    List<string> exceptionOrderList = new List<string>{}; // 存放异常订单号

// 异常订单判断
#region
    // 遍历订单号
    foreach(DataRow dr in orderNumbersDT.Rows){
        string orderNumber = dr["客户PO"].ToString();
        DataRow[] orderCountDrs = ordersCountDT.Select(string.Format("order_number = '{0}'", orderNumber));
        int orderCount = Convert.ToInt32(orderCountDrs[0]["orders_count"]);
        
        DataRow[] orderandLinkDrs = orderAndLinkDT.Select(string.Format("order_number = '{0}'", orderNumber));
        string documentLink = orderandLinkDrs[0]["document_link"].ToString();
        DataRow[] preverDocLinkRows=null;
        bool hasRepeatedOrders = false;
        DataRow[] curOrderDocLinkRows  = null;
        // orderCount > 1 说明是重复订单
        List<string> repeatedOrderExceptionList = new List<string>{};
        if(orderCount > 1){
            hasRepeatedOrders = true;
            string prevDocumentLink = orderandLinkDrs[1]["document_link"].ToString(); // orderandLinkDrs[1] 第二个doclink
            preverDocLinkRows = dbOrdersDT.Select(string.Format("order_number = '{0}' and document_link='{1}'", orderNumber, prevDocumentLink)); // 重复订单的上一次 DataRow[]
            curOrderDocLinkRows = dbOrdersDT.Select(string.Format("order_number = '{0}' and document_link='{1}'", orderNumber, documentLink)); // 当前最新订单的DataRow[]，from DB
            orderExceptionCheck(orderNumber, documentLink, ref repeatedOrderExceptionList, curOrderDocLinkRows, preverDocLinkRows); // 当前需要检查的字段为 【订单总数量，RDD， Promotional Event】
            compareOrderItem(curOrderDocLinkRows, preverDocLinkRows, ref repeatedOrderExceptionList);
        }

// 查看订单级别异常
#region
        DataRow rowHasAmount=null;
        bool hasSkuMappingIssue = false;
        bool hasQtyIssue = false;
        bool notIntoDMSProducts = false;
        decimal totalWyethAmount = 0;

        DataRow[] curOrderDocLinkSheetRows = orderItemsMappedToWyethDT.Select(string.Format("客户PO='{0}' and DocID='{1}'", orderNumber, documentLink)); // 当前最新订单的DataRow[]
        foreach(DataRow curDR in curOrderDocLinkSheetRows){
            if(String.IsNullOrEmpty(curDR["惠氏编码"].ToString())){
                hasSkuMappingIssue = true;
            }

            if(curDR["订单箱数"].ToString() == "0"){
                hasQtyIssue = true;
            }

            if(!string.IsNullOrEmpty(curDR["Total Order Amount"].ToString())){
                rowHasAmount = curDR;
            }

            string comment = specialProductComment(curDR["惠氏编码"].ToString(), curDR[curCustomerName + "编码"].ToString(), (DataTable)dtRow_ModuleSettings["specialListDT"]);
            Console.WriteLine("-----comment: {0}-----", comment);
            if(comment.Contains("不录")){
                notIntoDMSProducts = true;
            }
            totalWyethAmount += toDecimalConvert(curDR["惠氏折扣价"]);  // 惠氏折扣价汇总
        }
        
        // Convert.ToInt32("sdsd");
        decimal customerTotalOrderAmount = rowHasAmount!=null ? Convert.ToDecimal(rowHasAmount["Total Order Amount"]) : 0;
        decimal totalSalesGap = customerTotalOrderAmount - totalWyethAmount; // 沃尔玛 - 惠氏
        
#endregion
        // 遍历当前order Items
        foreach(DataRow curDR in curOrderDocLinkSheetRows){
            string exceptionType = string.Empty;
            string exceptionDetail = string.Empty;
            // RDD
            bool rddValid = rddDateValid(curDR["收单日期"].ToString(), curDR["取消日期"].ToString(), 3);
            if(!rddValid){
                addToExceptionOrder(orderNumber, ref RDDExceptionDT, ExceptionCategory.RDD.ToString(), exceptionDetail, curDR, ref exceptionOrderList);
            }

            // 指定的event不录单
            bool ifIntoDMS = checkEvent(curDR["Event"].ToString());
            if(!ifIntoDMS){
                addToExceptionOrder(orderNumber, ref notIntoDMSOrderExceptionDT, ExceptionCategory.不录单event.ToString(), exceptionDetail, curDR, ref exceptionOrderList);
            }

            // 门店订单
            string location = curDR["经销商代码"].ToString();
            bool shipTo门店 = !WMLocationsList.Contains(location);
            if(shipTo门店){
                addToExceptionOrder(orderNumber, ref shopOrderExceptionDT, ExceptionCategory.门店订单.ToString(), exceptionDetail, curDR, ref exceptionOrderList);
            }

            // 客户percent异常
            if(curDR["allowance_percent"].ToString().Trim() != dtRow_ModuleSettings["折扣率"].ToString()){
                addToExceptionOrder(orderNumber, ref percentExceptionDT, ExceptionCategory.客户percent异常.ToString(), exceptionDetail, curDR, ref exceptionOrderList);
            }

            // 产品主数据缺失
            if(hasSkuMappingIssue){
                exceptionType = string.Empty;
                if(String.IsNullOrEmpty(curDR["惠氏编码"].ToString())){
                     exceptionType = ExceptionCategory.产品主数据缺失.ToString();
                }
                addToExceptionOrder(orderNumber, ref skuMappingExceptionDT, exceptionType, exceptionDetail, curDR, ref exceptionOrderList);
            }

            // 产品数量为0
            if(hasQtyIssue){
                exceptionType = string.Empty;
                if(curDR["订单箱数"].ToString() == "0"){
                    exceptionType = ExceptionCategory.产品数量或者金额异常.ToString();
                }
                addToExceptionOrder(orderNumber, ref qtyAndSalesExceptionDT, exceptionType, "产品行订货量为0", curDR, ref exceptionOrderList);
            }

            // 订单包含不录品
            if(notIntoDMSProducts){
                exceptionType = string.Empty;
                string comment = specialProductComment(curDR["惠氏编码"].ToString(), curDR[curCustomerName + "编码"].ToString(), (DataTable)dtRow_ModuleSettings["specialListDT"]);
                if(comment.Contains("不录")){
                    exceptionType = ExceptionCategory.不录品.ToString();
                }
                addToExceptionOrder(orderNumber, ref notIntoDMSProductsExceptionDT, exceptionType, exceptionDetail, curDR, ref exceptionOrderList);
            }
            
           string exceptionComment = string.Empty;                
            // 总价有价差
            if(Math.Abs(totalSalesGap) > 15){
                /*decimal 单价价差 = Convert.ToDecimal(curDR["单价价差/箱"]);
                exceptionComment = "订单价差：" + totalSalesGap;
                if(Math.Abs(单价价差) > 0.01m){
                    exceptionComment = exceptionComment + exceptionContactSumbol + "价差超过0.01RMB，单价价差：" + 单价价差;
                }else{
                    exceptionComment = exceptionComment + exceptionContactSumbol + "价差介于0.01RMB，单价价差：" + 单价价差;
                }*/
                curDR["订单价差"] = totalSalesGap;
                addToExceptionOrder(orderNumber, ref salesGapExceptionDT, ExceptionCategory.订单价格差异.ToString(), exceptionDetail, curDR, ref exceptionOrderList);
            }

            // 客户订单改单
            if(hasRepeatedOrders){
                // 订单级别的异常信息
                DataRow prevDRow=null;
                exceptionComment = string.Empty;
                List<string> itemExceptionList = new List<string>{};
                foreach(DataRow prevDr in preverDocLinkRows){
                    if(prevDr["product_code"].ToString() == curDR[curCustomerName + "编码"].ToString()){
                        prevDRow = prevDr;
                        break;
                    }
                }
                // 筛选出数据库中指定产品行，为了和前一单做比较
                DataRow curDBDR = null;
                foreach(DataRow curItemDR in curOrderDocLinkRows){
                    if(curItemDR["product_code"].ToString() ==curDR[curCustomerName + "编码"].ToString()){
                        curDBDR = curItemDR;
                        break;
                    }
                }

               if(prevDRow!=null) repeatedItemCheck(prevDRow, curDBDR, ref itemExceptionList);
                if(repeatedOrderExceptionList.Count > 0){
                    itemExceptionList = itemExceptionList.Union(repeatedOrderExceptionList).ToList();
                }

                if(itemExceptionList.Count > 0){
                    exceptionComment = string.Join(exceptionSeperator, itemExceptionList);
                }else{
                    exceptionComment = "订单无修改";
                }
                addToExceptionOrder(orderNumber, ref repeatedOrderExceptionDT, ExceptionCategory.客户订单改单.ToString(), exceptionComment, curDR, ref exceptionOrderList);
            }
        }
    }
#endregion
    //汇总分类Exception
    DataTable RDDDBExceptionDT= writeToExceptionDT(RDDExceptionDT);
    DataTable shopOrderDBExceptionDT= writeToExceptionDT(shopOrderExceptionDT);
    DataTable percentDBExceptionDT= writeToExceptionDT(percentExceptionDT);
    DataTable skuMappingDBExceptionDT= writeToExceptionDT(skuMappingExceptionDT);
    DataTable qtyAndSalesDBExceptionDT= writeToExceptionDT(qtyAndSalesExceptionDT);
    DataTable salesGapDBExceptionDT= writeToExceptionDT(salesGapExceptionDT);
    DataTable repeatedOrderDBExceptionDT= writeToExceptionDT(repeatedOrderExceptionDT);
    DataTable notIntoDMSOrderDBExceptionDT= writeToExceptionDT(notIntoDMSOrderExceptionDT);
    DataTable notIntoDMSProductsDBExceptionDT= writeToExceptionDT(notIntoDMSProductsExceptionDT);

    // 合并异常订单
    finalExceptionDT.Merge(skuMappingDBExceptionDT);
    finalExceptionDT.Merge(shopOrderDBExceptionDT);
    finalExceptionDT.Merge(RDDDBExceptionDT);
    finalExceptionDT.Merge(percentDBExceptionDT);
    finalExceptionDT.Merge(qtyAndSalesDBExceptionDT); 
    finalExceptionDT.Merge(salesGapDBExceptionDT); 
    finalExceptionDT.Merge(repeatedOrderDBExceptionDT);
    finalExceptionDT.Merge(notIntoDMSOrderDBExceptionDT);
    finalExceptionDT.Merge(notIntoDMSProductsDBExceptionDT);


// Clean Order 整理（排除异常订单）
#region
    DataTable cleanOrdersDT = orderItemsMappedToWyethDT.Copy();
    for(int index=cleanOrdersDT.Rows.Count-1; index >= 0; index--){
        DataRow dr = cleanOrdersDT.Rows[index];
        string orderNumber = dr["客户PO"].ToString();
        // orderItemsMappedToWyethDT.Select("是否新单='Y'");
        
        // 1、异常分类除了重复订单还有其他异常, 不是clean order
        // 2、异常分类只有重复订单，并且不是新单的的，也不是clean order
        DataRow[] orderExceptionDRs = finalExceptionDT.Select(string.Format("`客户订单号（POID）`='{0}' and 异常分类 <> '{1}'", orderNumber,  ExceptionCategory.客户订单改单.ToString())); // 整个异常订单查找此订单的异常
        string 是否新单 =  dr["是否新单"].ToString();
        if(orderExceptionDRs.Length > 0 || (orderExceptionDRs.Length == 0 && 是否新单 == "N")){
            cleanOrdersDT.Rows.Remove(dr);
        }
        // 3、异常分类只有重复订单，并且是新单的，exception订单需要移除此单
        // Console.WriteLine("orderExceptionDRs Length: {0}, 新单：{1}", orderExceptionDRs.Length, 是否新单 );
        if(orderExceptionDRs.Length == 0 && 是否新单 == "Y"){
            // Console.WriteLine("---{0}", repeatedOrderDBExceptionDT.Rows.Count);
            removeRepeatCleanOrdersFromException(ref repeatedOrderDBExceptionDT, ref finalExceptionDT, orderNumber);
        }
    }

#endregion
    
    writeToDMSTracker(cleanOrdersDT);  // 生成dmsTrackerDT数据表

    // Exception 订单分类
    
    orderCatResultDT = (DataTable)dtRow_ModuleSettings["orderCatResultDT"];
    foreach(DataRow dr in orderCatResultDT.Rows){
        DataTable orderRelatedDBDT = new DataTable();
        switch(dr["orderCat"].ToString()){
            case "DMS_Tracker" :
                orderRelatedDBDT= dmsTrackerDT;
                break; 
            case "Exception"  :
                orderRelatedDBDT = MergeExceptionDTbyProductRow(finalExceptionDT); // finalExceptionDT 需要根据产品行合并下
                break;
            default :
               break; 
        }
        dr["orderRelatedDT"] = orderRelatedDBDT;
    }

    // orderItemsMappedToWyethDT.Columns.Remove("订单价差"); // remove 辅助列
}
//在这里编写您的函数或者类

public void removeRepeatCleanOrdersFromException(ref DataTable repeatedOrderDBExceptionDT, ref DataTable finalExceptionDT, string orderNumber){
    for(int j=repeatedOrderDBExceptionDT.Rows.Count-1; j >= 0; j--){
            DataRow dRow = repeatedOrderDBExceptionDT.Rows[j];
            // Console.WriteLine("客户订单号（POID） : {0}, orderNumber：{1}", dRow["客户订单号（POID）"], orderNumber );
            if(dRow["客户订单号（POID）"].ToString() == orderNumber){
                repeatedOrderDBExceptionDT.Rows.Remove(dRow);
            }
        }

    for(int j=finalExceptionDT.Rows.Count-1; j >= 0; j--){
        DataRow dRow = finalExceptionDT.Rows[j];
        // Console.WriteLine("客户订单号（POID） : {0}, orderNumber：{1}", dRow["客户订单号（POID）"], orderNumber );
        if(dRow["客户订单号（POID）"].ToString() == orderNumber){
            finalExceptionDT.Rows.Remove(dRow);
        }
    }
}

public void addToExceptionOrder(string orderNumber, ref DataTable theExceptionDT, string exceptionType, string exceptionDetailParam, DataRow curDR, ref List<string> exceptionOrderList){
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
            case "门店订单":
                exceptionCat = "门店订单不处理";
                exceptionDetail = "门店订单待确认如何处理";
                break; 
            case "产品主数据缺失":
                exceptionDetail = "客户对应的惠氏产品主数据缺失，需提供惠氏产品主数据";
                break;
            case "客户percent异常":
                exceptionDetail = string.Format("客户改Percent，{0}{1}，有变更需邮件告知facing请改为常规固定折扣", curCustomerName, dtRow_ModuleSettings["折扣率"].ToString());
                break;
            case "不录单event":
                exceptionDetail = "此Event不录单";
                break;
            case "订单价格差异":
                exceptionDetail = "客户订单产品与惠氏产品存在价格差异，需确认是否录入订单并跟进价差问题";
                break;
            case "不录品":
                exceptionDetail = "不录品，请确认该如何处理";
                break;
            default : /* 可选的 */
               break; 
        }

    itemList.Add(exceptionCat);
    itemList.Add(exceptionDetail);
    newExceptionDR.ItemArray =itemList.ToArray();
    theExceptionDT.Rows.Add(newExceptionDR);
    if(!exceptionOrderList.Contains(orderNumber)) exceptionOrderList.Add(orderNumber);
}

public DataTable writeToExceptionDT(DataTable sourceExceptionOrderItemsDT){  // , ref List<string> orderNumberList
    DataTable curExceptionDT = finalExceptionDT.Clone();
    
    foreach(DataRow dr in sourceExceptionOrderItemsDT.Rows){
        // string orderNumber = dr["客户PO"].ToString();
       // if(!orderNumberList.Contains(orderNumber)) orderNumberList.Add(orderNumber);
        DataRow newDR = curExceptionDT.NewRow();
        newDR["RPA获取订单日期及时间"] = dr["收单日期"];
        newDR["客户名称"] = dr["大区"];
        newDR["客户订单日期及时间"] = dr["订单日期"];
        newDR["客户订单计划到货日期"] = dr["取消日期"];
        newDR["门店/大仓编号"] = dr["经销商代码"];
        newDR["客户订单号（POID）"] = dr["客户PO"];
        newDR["订单类型/Event"] = dr["Event"];
        newDR["客户产品编码"] = dr[curCustomerName + "编码"];
        newDR["客户产品名称"] = dr["item_description"];
        newDR["客户产品箱数"] = dr["订单箱数"];
        decimal 客户单价 = toDecimalConvert(dr[curCustomerName + "单价/箱 Cost"]);
        newDR["客户产品单价"] = 客户单价;
        newDR["客户产品总价"] = Convert.ToInt32(dr["订单箱数"]) * 客户单价;
        newDR["扣点"] = dr["allowance_percent"];
        newDR["实际扣点"] = dr["allowance_total"];
        newDR["客户订单总金额"] = dr["Total Order Amount"];
        newDR["惠氏客户Sold to"] = dr["Sold To"];
        newDR["惠氏客户Ship to"] = dr["Ship to"];
        newDR["惠氏客户名称"] = dr["经销商简称"];
        newDR["惠氏POID"] = dr["POID"];
        newDR["惠氏产品编码"] = dr["惠氏编码"];
        newDR["惠氏产品名称"] = dr["惠氏产品描述"];
        newDR["惠氏产品箱数"] = dr["订单箱数"];
        newDR["惠氏产品箱价"] = dr["惠氏单价/箱"];
        newDR["惠氏订单总金额"] = dr["惠氏总价"];
        newDR["折后订单总金额"] = dr["惠氏折扣价"];
        newDR["产品备注1（紧缺品）"] = dr["紧缺"];
        newDR["产品备注2（彩箱/整箱）"] = dr["整箱"];
        newDR["异常分类"] = dr["异常分类"];
        newDR["异常详细描述"] = dr["异常详细描述"];
        newDR["产品箱价价差(未税）"] = 客户单价 - toDecimalConvert(dr["惠氏单价/箱"]);
        newDR["惠氏订单总金额价差 (未税）"] = dr["订单价差"];
        curExceptionDT.Rows.Add(newDR);
    }
   return curExceptionDT;
}

/// <summary>
/// must arrived at减去抓单日期需要>= 3天
/// </summary>
/// <param name="orderDateStr"></param>
/// <param name="rddDateStr"> 导出的日期格式是 yyyy/MM/dd </param>
///<param name="dayAdded"> 日期查 </param>
/// <returns></returns>
public bool rddDateValid(string orderDateStr, string rddDateStr, int dayAdded){
    // DateTime rddDate = DateTime.ParseExact(rddDateStr, "yyyy-MM-dd", null);
    DateTime rddDate = DateTime.Parse(rddDateStr);
    DateTime orderDate = DateTime.Parse(orderDateStr);
    if(DateTime.Compare(rddDate, orderDate.AddDays(dayAdded)) < 0){
        return false;
    }
    return true;
}

/// <summary>
/// 检查重复订单里面有哪些重复项
/// </summary>
/// <param name="orderNumber"></param>
/// <param name="documentLink"></param>
/// <param name="exceptionList"></param>
/// <param name="curOrderDocLinkRows"></param>
public void orderExceptionCheck(string orderNumber, string documentLink, ref List<string> exceptionList, DataRow[] curOrderDocLinkRows, DataRow[] preverDocLinkRows){
    DataRow curRow = curOrderDocLinkRows[0];
    DataRow prevRow = preverDocLinkRows[0];
    
    Dictionary<string, string> orderLevelColumnMapping = new Dictionary<string, string>{{"total_units_ordered", "总数量"}, {"order_type", "Order Type"}, {"ship_date", "Ship Date"}, {"must_arrived_by", "RDD"}, {"promotional_event", "Promotional Event"}, {"total_order_amount_after_adjustments", "订单总金额"}};  
    
    foreach(var colMap in orderLevelColumnMapping){
        string dbCol = colMap.Key;
        string dbDescription = colMap.Value;
        
        string prevValue = prevRow[dbCol].ToString();
        string curValue = curRow[dbCol].ToString();
        if(prevValue != curValue){
            string exceptionComment = string.Format("{0}由{1}修改为{2}", dbDescription, prevValue, curValue);

            switch(dbCol){
                case "total_units_ordered":
                    int qtyGap = Convert.ToInt32(curValue) - Convert.ToInt32(prevValue);
                    if(qtyGap > 0){
                        exceptionComment = string.Format("客户订单已改数量（增量），需NBS补录入");
                    }else{
                        exceptionComment = string.Format("客户订单已改数量（减量），需确认是否已发货");
                    }
                    break;
                case "must_arrived_by":
                    string prevDayStr = DateTime.Parse(prevValue).ToString("yyyy/MM/dd");
                    string curDayStr = DateTime.Parse(curValue).ToString("yyyy/MM/dd");
                    exceptionComment = $"RDD由{prevDayStr}改为{curDayStr}，可不用邮件告知Facing";
                    break; 
                case "promotional_event":
                    exceptionComment = string.Format(@"Promotional Event由{0}改为{1}", prevValue, curValue);
                    break;
                default : /* 可选的 */
                break; 
            }
            exceptionList.Add(exceptionComment);
        }
    }
}

// 按照【客户产品码】比较订单产品行，输出新增或者删除的产品行
public void compareOrderItem(DataRow[] curOrderDocLinkRows, DataRow[] prevOrderDocLinkRows, ref List<string> exceptionList){
    string[] currentProductCodeArr = curOrderDocLinkRows.Cast<DataRow>().Select<DataRow, string>(dr => dr["product_code"].ToString()).ToArray();
    string[] previousProductCodeArr = prevOrderDocLinkRows.Cast<DataRow>().Select<DataRow, string>(dr => dr["product_code"].ToString()).ToArray();
    string[] 新增产品码数组 = currentProductCodeArr.Except(previousProductCodeArr).ToArray();
    string[] 删除产品码数组 = previousProductCodeArr.Except(currentProductCodeArr).ToArray();
    if(新增产品码数组.Length > 0){
        foreach(string productCode in 新增产品码数组){
            foreach(DataRow dr in curOrderDocLinkRows){
                if(productCode == dr["product_code"].ToString()){
                    exceptionList.Add(string.Format("新增产品行【客户产品码：{0}，Line Item: {1}】", dr["product_code"], dr["line_number"]));
                }
            }
        }
    }
    
    if(删除产品码数组.Length > 0){
        foreach(string productCode in 删除产品码数组){
            foreach(DataRow dr in prevOrderDocLinkRows){
                if(productCode == dr["product_code"].ToString()){
                     exceptionList.Add(string.Format("删除产品行【客户产品码：{0}，Line Item: {1}】", dr["product_code"], dr["line_number"]));
                }
            }
        }
    }
}

/// <summary>
/// 重复订单的产品行重复项确认
/// </summary>
/// <param name="prevDRow"></param>
/// <param name="curDR"></param>
/// <param name="itemExceptionList"></param>
public void repeatedItemCheck(DataRow prevDRow, DataRow curDR, ref List<string> itemExceptionList){
    Dictionary<string, string> itemLevelColumnMapping = new Dictionary<string, string>{{"quantity_ordered", "Quantity"}, {"extended_cost", "Extended Cost"}, {"tax_percent", "Tax Percent"}};
    
    foreach(var colMap in itemLevelColumnMapping){
        string dbCol = colMap.Key;
        string dbDescription = colMap.Value;
        // Console.WriteLine(dbCol);
        string prevValue = prevDRow[dbCol].ToString();
        string curValue = curDR[dbCol].ToString();
        if(prevValue != curValue){
            string exceptionComment = string.Format("产品行修改{0}，修改前为：{1}", dbDescription, prevValue);
            itemExceptionList.Add(exceptionComment);
        }
    }
}
/// <summary>
/// clean order 整理成DMS Tracker格式
/// </summary>
/// <param name="cleanOrderItemsMappedToWyethDT"></param>
/// <returns></returns>
public List<string> writeToDMSTracker(DataTable cleanOrderItemsMappedToWyethDT){
    List<string> orderNumberList = new List<string>{};
    if(!dmsTrackerDT.Columns.Contains("客户订单号")) dmsTrackerDT.Columns.Add("客户订单号", typeof(string));
    if(!dmsTrackerDT.Columns.Contains("order document link")) dmsTrackerDT.Columns.Add("order document link", typeof(string));
    dmsTrackerDT = dmsTrackerDT.Clone();
    dmsTrackerDT.Columns["POID（客户订单号）"].DataType = typeof(string); // 修改列类型为string
    foreach(DataRow dr in cleanOrderItemsMappedToWyethDT.Rows){
        string orderNumber = dr["客户PO"].ToString();
        if(!orderNumberList.Contains(orderNumber)) orderNumberList.Add(orderNumber);
        
        string 经销商代码 = dr["经销商代码"].ToString();
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
        Console.WriteLine("POID: {0}, 惠氏编码: {1}", dr["POID"].ToString(), dr["惠氏编码"].ToString());
        
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
            dmsTrackerDR["读单日期"] = dr["收单日期"];
            dmsTrackerDR["客户要求到货日期"] = dr["取消日期"];
            dmsTrackerDR["SoldToCode"] = dr["Sold To"];
            dmsTrackerDR["ShipToCode"] = dr["Ship to"];
            dmsTrackerDR["Customer Name"] = 门店;
            dmsTrackerDR["POID（客户订单号）"] = dr["POID"].ToString();
            dmsTrackerDR["产品名称（惠氏SKU 代码）"] = dr["惠氏编码"];
            dmsTrackerDR["数量（箱）"] = dr["订单箱数"];
            dmsTrackerDR["客户订单号"] = orderNumber;
            dmsTrackerDR["order document link"] = dr["DocID"];
            dmsTrackerDT.Rows.Add(dmsTrackerDR);
        }
    }
    
    // Convert.ToInt32("2dasd");
    
    return orderNumberList;
}


public int toIntConvert(object value){
    int result = 0;
    Int32.TryParse(value.ToString(), out result);
    return result;
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
            if(!string.IsNullOrEmpty(exceptionDR["异常分类"].ToString())){  // 异常分类不为空才加入list
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
/// 根据customer preference DT的设置，检查此event是否录单
/// </summary>
/// <param name="eventValue"></param>
/// <returns></returns>
public bool checkEvent(string eventValue){
    bool intoDMS = true;
    DataTable customerPreferenceDT = (DataTable)dtRow_ModuleSettings["customerPreferenceDT"];
    if(customerPreferenceDT.Rows.Count > 0){
        string orderEventNotIntoDMS = customerPreferenceDT.Rows[0]["order_event_not_into_DMS"].ToString();  // 分号分隔
        List<string> eventList = orderEventNotIntoDMS.Split(new string[]{";"}, StringSplitOptions.RemoveEmptyEntries).ToList();
        if(eventList.Contains(eventValue)){
            intoDMS = false;
        }
    }
    return intoDMS;
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