//代码执行入口，请勿修改或删除
public void Run()
{
    //在这里编写您的代码
    validTodayOrdersDT = successOrdersTrackerDT.Clone(); // 今天抓取的成功录入DMS的订单
    DataTable cxzOrdersDT = successOrdersTrackerDT.Clone();
    DataTable materialMasterDataDT = (DataTable)dtRow_ModuleSettings["materialMasterDataDT"];

    fetchTodayOrders(ref validTodayOrdersDT, ref cxzOrdersDT);
    // 山姆彩箱装数据整理
    processSamCXZDT(cxzOrdersDT, materialMasterDataDT);
    
    // 预约信息表整理
    processBookingInfoDT(validTodayOrdersDT);

}

// 生成【山姆彩箱装明细数据表】
public void processSamCXZDT(DataTable cxzOrdersDT, DataTable materialMasterDataDT){
     if(isSam && cxzOrdersDT.Rows.Count > 0){
        cxzDetailsDT = initCxzDetailsDT();
        
        foreach(DataRow dr in cxzOrdersDT.Rows){
            string poid = dr["POID"].ToString();
            string wyethSkuCode = dr["sku_code"].ToString();
            string 产品名称 = string.Empty;
            int 订货数量 = 0;
            decimal 直供单价 = 0m;          
            decimal 客户单价 = 0m;
            int size = 0;
            string customerSku = string.Empty;
            // 主数据表查询惠氏产品码
            DataRow[] materialMasterDrs = materialMasterDataDT.Select(string.Format("wyeth_material_no='{0}'", wyethSkuCode));
            if(materialMasterDrs.Length > 0){
                DataRow materialMasterDr = materialMasterDrs[0];
                customerSku = materialMasterDr["customer_material_no"].ToString();
                int qty = toIntConvert(dr["quantity"]);
                size = toIntConvert(materialMasterDr["size"]);
                if(size != 0){
                    decimal wyethNps = toDecimalConvert(materialMasterDr["wyeth_nps"]);
                    订货数量 = qty * size;
                    直供单价 = Math.Round(wyethNps/size, 2);
                }
            }
            // 原始订单表查询改单
            // 收集订单附件
            DataRow[] rawOrderWithFilesDRs =  recent30DaysLatestOrdersDT.Select(string.Format("wyeth_poid like '{0}%' and file_path<>'' and file_path is not null", poid));
            if(rawOrderWithFilesDRs.Length > 0){
                string filePath = rawOrderWithFilesDRs[0]["file_path"].ToString();
                if(!string.IsNullOrEmpty(filePath)  && File.Exists(filePath) && !cxzOrderFilesList.Contains(filePath)) cxzOrderFilesList.Add(filePath);
            }
                
            // 查询指定产品行
            DataRow[] rawOrderDrs = recent30DaysLatestOrdersDT.Select(string.Format("wyeth_poid='{0}' and product_code='{1}'", poid, customerSku));
            if(rawOrderDrs.Length > 0){
                DataRow rawOrderDr = rawOrderDrs[0];
                产品名称 = rawOrderDr["item_description"].ToString();
                if(size != 0){
                    客户单价 = Math.Round(toDecimalConvert(rawOrderDr["cost"])/size, 2);
                }              
            }
            DataRow cxzDetailDR = cxzDetailsDT.NewRow();
            cxzDetailDR["合同编号"] = dr["dms_po"];
            cxzDetailDR["POID"] = poid;
            cxzDetailDR["经销商SD代码"] = dr["dacang_account"];
            cxzDetailDR["经销商简称"] = dr["customer_name"];
            cxzDetailDR["SoldToCode"] = dr["sold_to_code"];
            cxzDetailDR["ShipToCode"] = dr["ship_to_code"];
            cxzDetailDR["付款方式"] = dr["payment_method"];
            cxzDetailDR["产品名称"] = 产品名称;
            cxzDetailDR["产品SAP代码"] = wyethSkuCode;
            cxzDetailDR["订货数量"] = 订货数量;
            cxzDetailDR["订货箱数"] = dr["quantity"];
            cxzDetailDR["直供单价"] = 直供单价;  // 惠氏单价
            cxzDetailDR["客户单价"] =  客户单价; // 查原始订单，当天最后一次修改过的订单  select * from walmart_orders where wyeth_poid = dr["POID"] and create_date_time = (select max(create_date_time) from walmart_orders where wyeth_poid = dr["POID"] and date_format(created_time, "%Y-%m-%d") = '2022-05-26' )
            cxzDetailsDT.Rows.Add(cxzDetailDR);
        }
    }
}

// 初始化山姆彩箱装明细表
public DataTable initCxzDetailsDT(){
    DataTable cxzDetailsTempDT = new DataTable();
    string[] columns = new string[]{"合同编号", "SAP订单编号", "发票号", "POID", "订单日期", "经销商SD代码", "经销商简称", "SoldToCode", "ShipToCode", "付款方式", "产品名称", "产品SAP代码", "订货数量", "订货箱数", "直供单价", "客户单价"};
    List<string> strCols = new List<string>{"POID", "SoldToCode", "ShipToCode", "产品SAP代码"};
    foreach(string colName in columns){
        if(strCols.Contains(colName)){
            DataColumn dcol = new DataColumn(colName, typeof(string));
            cxzDetailsTempDT.Columns.Add(dcol);

        }else{
            DataColumn dcol = new DataColumn(colName, typeof(object));
            // 设置初始值为当前时间
            if(colName == "订单日期"){
                dcol.DefaultValue = 指定日期.ToString("yyyy/MM/dd");
            }
            cxzDetailsTempDT.Columns.Add(dcol);
        }
    }
    return cxzDetailsTempDT;
}

/// <summary>
/// 筛选出【指定日期】的订单
/// </summary>
/// <param name="validTodayOrdersDT"></param>
/// <param name="cxzOrdersDT"></param>
public void fetchTodayOrders(ref DataTable validTodayOrdersDT, ref DataTable cxzOrdersDT){
    string todayStr = 指定日期.ToString("yyyy-MM-dd");
    foreach(DataRow dr in successOrdersTrackerDT.Rows){
        string orderCaptureDateStr = dr["order_capture_date"].ToString();
        DateTime orderCaptureDateUTC = DateTime.Parse(orderCaptureDateStr);
        DateTime orderCaptureDateLocal = convertToLocalTimeFromUTC(orderCaptureDateUTC);
        if(orderCaptureDateUTC.ToString("yyyy-MM-dd") == todayStr){
            validTodayOrdersDT.ImportRow(dr);
            if(dr["POID"].ToString().Contains("-cxz")){
                cxzOrdersDT.ImportRow(dr);
            }
        }
    }
}

// 初始化预约信息表
public DataTable initBookingInfoDT(){
    DataTable bookingInfoTempDT = new DataTable();
    string[] columns = new string[]{"收单日期", "POID", "经销商简称","Type", "经销商代码", "Event", "订单日期", "起运日期", "取消日期", "备注", "Sum of 订单箱数"}; //, "Sum of Total Order Amount"};
    List<string> strCols = new List<string>{"POID"};
    foreach(string colName in columns){
        if(strCols.Contains(colName)){
            DataColumn dcol = new DataColumn(colName, typeof(string));
            bookingInfoTempDT.Columns.Add(dcol);
        }else{
            DataColumn dcol = new DataColumn(colName, typeof(object));
            // 设置初始值为当前时间
            if(colName == "收单日期"){
                dcol.DefaultValue = 指定日期.ToString("MM/dd/yyyy");
            }
            bookingInfoTempDT.Columns.Add(dcol);
        }
    }
    return bookingInfoTempDT;
}

// 生成【预约信息表数据表】
public void processBookingInfoDT(DataTable validTodayOrdersDT){
    bookingInfoDT = initBookingInfoDT();
    
    IEnumerable<IGrouping<string, DataRow>> groupedOrders = validTodayOrdersDT.Rows.Cast<DataRow>().GroupBy<DataRow, string>(dr => dr["POID"].ToString()); //C# 对DataTable中的某列分组，groupedDRs中的Key是分组后的值
    foreach (var itemGroup in groupedOrders)
    {
        string poid = itemGroup.Key;
        DataRow[] orderDRows = itemGroup.ToArray();
        DataRow orderDRow = orderDRows[0];
        int 订单箱数 = 0;
        string orderType = string.Empty;
        string promotionalEvent = string.Empty;
        string dcNo = string.Empty;
        string 订单日期 = string.Empty;
        string 起运日期 = string.Empty;
        string 取消日期 = string.Empty;
        string 备注 = string.Empty;
        decimal totalOrderAmount = 0m;
        foreach(DataRow dr in orderDRows){
            int qty = toIntConvert(dr["quantity"]);
            订单箱数 = 订单箱数 + qty;
        }
        // 收集订单附件
        DataRow[] rawOrderWithFilesDRs =  recent30DaysLatestOrdersDT.Select(string.Format("wyeth_poid like '{0}%' and file_path<>'' and file_path is not null", poid));
        
        if(rawOrderWithFilesDRs.Length > 0){
            string filePath = rawOrderWithFilesDRs[0]["file_path"].ToString();
            if(!string.IsNullOrEmpty(filePath)  && File.Exists(filePath) && !bookingInfoOrderFilesList.Contains(filePath)) bookingInfoOrderFilesList.Add(filePath);
        }

        // 原始订单表查询订单
        DataRow[] rawOrderDrs = recent30DaysLatestOrdersDT.Select(string.Format("wyeth_poid like '{0}%' and total_order_amount_after_adjustments is not null", poid));
        if(rawOrderDrs.Length > 0){
            DataRow rawOrderDr = rawOrderDrs[0];
            DateTime rddDate = DateTime.Parse(rawOrderDr["must_arrived_by"].ToString());
            orderType = rawOrderDr["order_type"].ToString();
            promotionalEvent = rawOrderDr["promotional_event"].ToString();
            dcNo = rawOrderDr["location"].ToString();
            if(promotionalEvent.Contains("JD")) 备注 = "JD单";
            订单日期 = DateTime.Parse(rawOrderDr["create_date"].ToString()).ToString("MM/dd/yyyy");
            起运日期 = DateTime.Parse(rawOrderDr["ship_date"].ToString()).ToString("MM/dd/yyyy");
            取消日期 = rddDate.ToString("MM/dd/yyyy");
            totalOrderAmount = toDecimalConvert(rawOrderDr["total_order_amount_after_adjustments"].ToString());
            Int32 rddDays = DiffDays(指定日期, rddDate);
            if(rddDays < 3){
                if(string.IsNullOrEmpty(备注)){
                    备注 = "RDD < 3";
                }else{
                    备注 = 备注 + "|" + "RDD < 3";
                }
            }
        }

        DataRow bookingInfoDR = bookingInfoDT.NewRow();
        bookingInfoDR["POID"] = poid;
        bookingInfoDR["经销商简称"] = orderDRow["customer_name"];
        bookingInfoDR["Type"] = orderType;
        bookingInfoDR["经销商代码"] = dcNo;
        bookingInfoDR["Event"] = promotionalEvent;
        bookingInfoDR["订单日期"] = 订单日期;
        bookingInfoDR["起运日期"] = 起运日期;
        bookingInfoDR["取消日期"] = 取消日期;
        bookingInfoDR["备注"] = 备注;
        bookingInfoDR["Sum of 订单箱数"] = 订单箱数;
        // bookingInfoDR["Sum of Total Order Amount"] = totalOrderAmount;
        bookingInfoDT.Rows.Add(bookingInfoDR);  
    }
}

// 通用方法
public int toIntConvert(object value){
    int result = 0;
    Int32.TryParse(value.ToString(), out result);
    return result;
}

public decimal toDecimalConvert(object value){
    decimal result = 0m;
    Decimal.TryParse(value.ToString(), out result);
    return result;
}

public static DateTime convertToLocalTimeFromUTC(DateTime sourceUTCdtime)
{
    TimeZoneInfo cstTimeZone = TimeZoneInfo.FindSystemTimeZoneById("UTC");
    DateTime dtime = TimeZoneInfo.ConvertTime(sourceUTCdtime, cstTimeZone, TimeZoneInfo.Local);
    return dtime;
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