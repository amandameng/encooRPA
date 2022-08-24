//代码执行入口，请勿修改或删除
const string 直流CPO订单类型="直流CPO";
public void Run()
{
    //在这里编写您的代码
    totalOrderListDT.Columns.Add("订单PrintPO", typeof(string));
    totalOrderListDT.Columns.Add("直流CPO异常", typeof(string));
    totalOrderListDT.Columns.Add("RDD异常", typeof(string));
  
    for(int i=totalOrderListDT.Rows.Count - 1; i>=0; i--){
        DataRow drFromList = totalOrderListDT.Rows[i];
        string orderNumber = drFromList["订单号"].ToString();
        DataRow[] matchedDRows = existingOrdersDT.Select(string.Format("order_number = '{0}'", orderNumber));
        // Console.WriteLine("----{0}", matchedDRows.Length);
        
        if(matchedDRows.Length >= 1){
            totalOrderListDT.Rows.Remove(drFromList); // 排除已经存在的订单
        }
        else{
            //  订单类型为 "供商直送"无需抓取
            if(无需抓取订单类型列表.Contains(drFromList["订单类型"].ToString())){
                totalOrderListDT.Rows.Remove(drFromList);
            }    
        }
    }

   // 网页抓取的订单 与 导出的订单合并
    mergeOrdersFromPage();
   // 提取订单明细相关列
    fetchValidOrderItemsColsDT();
}
//在这里编写您的函数或者类

/// <summary>
/// 合并订单和订单详情
/// </summary>
public void mergeOrdersFromPage(){
    string[] orderRelatedColumns = new string[]{"订单类型", "业务类型", "订单状态", "打印状态", "确认方式", "直流CPO异常", "CPO异常原因", "RDD异常" };
    foreach(string colName in orderRelatedColumns){
        totalOrderItemsDT.Columns.Add(colName, typeof(string));
    }
    orderItemsWithInvalidCPODT = totalOrderItemsDT.Clone();

    // 遍历订单
    // 1、判断直流CPO
    // 2、判断RDD, 计划到货日期减去抓单日期需要>= 3天
    foreach(DataRow orderRow in totalOrderListDT.Rows){
        string orderNumber = orderRow["订单号"].ToString();
        string 订单类型 = orderRow["订单类型"].ToString();
        string 抓单日期 = DateTime.Now.ToString("yyyy/MM/dd");
        string 计划到货日期 = orderRow["计划到货日期"].ToString();
        bool rddValid = rddDateValid(抓单日期, 计划到货日期, 3);
       
        DataRow[] matchedOrderItemDRows = totalOrderItemsDT.Select(string.Format("订单号 = '{0}'", orderNumber));
        bool isCPOOrder = (订单类型 == 直流CPO订单类型);
        bool validCPO = true;
        string cpo异常原因 = string.Empty;
       // 直流CPO 订单判断：
      // 1）彩箱装产品需要邮件至Fascing不予录单
      // 2）非彩箱装产品，规格非350g, 非12EA（H12）， 邮件至Fascing不予录单
      // TODO：彩箱装中其余正常产品是正常录单还是一整个订单block？ ：整单block

        foreach(DataRow itemRow in matchedOrderItemDRows){
            string 客户Sku = itemRow["麦德龙总部商品编码"].ToString();
            DataRow[] mappingSkuDRs = materialMasterDataDT.Select(string.Format("customer_material_no='{0}'",  客户Sku));
            string comment = string.Empty;
            if(mappingSkuDRs.Length > 0){
                string 惠氏sku = mappingSkuDRs[0]["wyeth_material_no"].ToString();
                comment = specialProductComment(惠氏sku, 客户Sku, specialListDT);
            }

            if(isCPOOrder){
                string 商品名称 = itemRow["商品名称"].ToString();
                string 订货单位 = itemRow["订货单位"].ToString();
                string 订货量 = itemRow["订货量"].ToString();
                if(comment.Contains("彩箱装")){
                    validCPO = false;
                    itemRow["cpo异常原因"] = "Metro CPO彩箱装产品"; 
                }else{
                    bool sizeValid = 商品名称.ToUpper().Contains("350G") && 订货单位=="H12";
                    if(!sizeValid){
                        validCPO = false;
                       itemRow["cpo异常原因"] = "Metro CPO非彩箱装产品规格异常"; 
                    }
                }
            }
            if(!rddValid){
                itemRow["RDD异常"] = "Y";
            }
            if(!validCPO){
                itemRow["直流CPO异常"] = "Y";
            }
            addMoreColumns(orderRow, itemRow, ref orderItemsWithInvalidCPODT);
        }
        
        if(!rddValid){
            orderRow["RDD异常"] = "Y";
        }
        if(!validCPO){
            orderRow["直流CPO异常"] = "Y";
        }
    }
}

public void addMoreColumns(DataRow orderRow, DataRow itemRow, ref DataTable orderItemsWithInvalidCPODT){
    DataRow newItemRow = orderItemsWithInvalidCPODT.NewRow();
    newItemRow.ItemArray = itemRow.ItemArray;
    newItemRow["订单类型"] = orderRow["订单类型"];
    newItemRow["业务类型"] = orderRow["业务类型"];
    newItemRow["订单状态"] = orderRow["订单状态"];
    newItemRow["打印状态"] = orderRow["打印状态"];
    newItemRow["确认方式"] = orderRow["确认方式"];
    orderItemsWithInvalidCPODT.Rows.Add(newItemRow);
}

/// <summary>
/// 提取订单明细相关列数据
/// </summary>
public void fetchValidOrderItemsColsDT(){
    string[] orderItemCols = new string[]{"供商编码", "供商名称", "门店/仓库编码", "门店/仓库名称", "订单号", "订货日期", "计划到货日期", "麦德龙总部商品编码", "商品名称", "商品编码", "国条", "种类", "供应商商品编号", "订货量", "订货单位", "邮报编码", "CPO单号", "大仓编码", "订单类型"};
    orderItemsIntoSheetDT = orderItemsWithInvalidCPODT.DefaultView.ToTable(true, orderItemCols);
}

/// <summary>
/// 计划到货日期减去抓单日期需要>= 3天
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