//代码执行入口，请勿修改或删除
public void Run()
{
    //在这里编写您的代码
    skuMappingHeader = new string[]{"客户产品代码", "客户产品名称", "客户箱价", "惠氏产品代码", "惠氏产品名称", "规格", "惠氏单价/罐", "惠氏箱价"};
    
    // materialMasterDataDT 的列头 
    // customer_material_no, customer_product_name, customer_product_nps, wyeth_material_no, wyeth_product_name, size, wyeth_unit_price, wyeth_nps
        
    buildTrackeraAndRegionDT();

}
//在这里编写您的函数或者类

// DC	POID	订货日期	交货日期	货号	"QTY 罐数"	大润发实际总金额	Nestle CODE	SKU	直供价	直供总价	折扣价	备注

/// <summary>
/// 搭建退货单Tracker
/// </summary>
public void buildTrackeraAndRegionDT(){
     // Tracker Header
    // {"门店","采购单号","采购单类型","采购单状态","货号","品名","规格","订购数量","订购单位","订购箱数","单品开票价","促销期数","创单日期","已收货数量","预计到货日","实际收货日","取货日期","订单号","货号","罐数","单价","大润发实际总金额","华东整理日期","东北整理日期","华北整理日期"};
    // 今年退货单DT
    Dictionary<string, string> trackerHeaderDic = new Dictionary<string, string>{
        {"门店", "store_location"},
        {"采购单号", "order_number"},
        {"采购单类型", "order_type"},
        {"采购单状态", "order_status"},
        {"货号", "product_code"},
        {"品名", "product_name"},
        {"规格","size"},
        {"订购数量", "order_qty"},
        {"订购单位", "uom"},
        {"订购箱数","order_cases"},
        {"单品开票价", "price"},
        {"促销期数", "promotional_periods"},
        {"创单日期","order_date"},
        {"已收货数量", "received_qty"},
        {"预计到货日", "must_arrived_by"},
        {"实际收货日", "actual_received_at"},
        {"大润发实际总金额", "product_total_sales"}
    };
 
    DataTable 今年退货单DT = (DataTable)outReturnOrderDTDic["今年退货单DT"];
    yearReturnOrderTrackerDT = new DataTable();
    initReturnOrderExcelDT(ref yearReturnOrderTrackerDT, trackerHeaderDic);
    
    yearOrderRegionDT = new DataTable();
    initOrderRegionDT(ref yearOrderRegionDT);
    
    不重复退货单ListDT = 今年退货单DT.DefaultView.ToTable(true, new string[]{"region", "wyeth_POID", "created_time"});
    ReBuildListOrderDT(ref 不重复退货单ListDT);

    foreach(DataRow dr in 今年退货单DT.Rows){
        DataRow orderExcelRow = yearReturnOrderTrackerDT.NewRow();
        DataRow regionDR = yearOrderRegionDT.NewRow();
        
        foreach(var colKV in trackerHeaderDic){
            orderExcelRow[colKV.Key] = dr[colKV.Value];
        }
       // Tracker Row
        buildTrackerDR(ref orderExcelRow, dr);
        yearReturnOrderTrackerDT.Rows.Add(orderExcelRow);
        
        // Region Row
        buildRegionDR(ref regionDR, dr);
        yearOrderRegionDT.Rows.Add(regionDR);
    }
}

public void initReturnOrderExcelDT(ref DataTable targetDT, Dictionary<string, string> trackerHeaderDic){
    foreach(var colKV in trackerHeaderDic){
        targetDT.Columns.Add(colKV.Key, typeof(string));
    }
    string[] extraCols = {"取货日期","订单号","货号2","罐数","单价"};
    foreach(string col in extraCols){
        targetDT.Columns.Add(col, typeof(string));
    }
    // ,"华东整理日期","东北整理日期","华北整理日期"
    foreach(DataRow dr in rpaAccountsDT.Rows){
        targetDT.Columns.Add(dr["region"].ToString()+"整理日期", typeof(string));
    }
}


/// <summary>
/// DC	POID	订货日期	交货日期	货号	"QTY 罐数"	大润发实际总金额	Nestle CODE	SKU	直供价	直供总价	折扣价	备注

/// </summary>
public void getShipTo(string dcNo, ref string 扣点){
    DataRow[] drs = soldToShipToDT.Select(string.Format("`DC编号` = '{0}'", dcNo));
    if(drs.Length > 0){
        扣点 = drs[0]["discount"].ToString();
    }
}

public DataRow getWyethMappingRow(string customerSKU){
    DataRow[] drs = materialMasterDataDT.Select(string.Format("customer_material_no='{0}'", customerSKU));
    if(drs.Length > 0){
       return drs[0];
    }else{
        return null;
    }
}

public void initOrderRegionDT(ref DataTable targetDT){
    string[] columns = new string[]{"DC", "POID", "订货日期", "交货日期", "货号", "QTY 罐数", "大润发实际总金额", "Nestle CODE", "SKU", "直供价", "直供总价", "折扣价", "备注"};
    foreach(string col in columns){
        targetDT.Columns.Add(col, typeof(string));
    }
}

public void ReBuildListOrderDT(ref DataTable targetDT){
    string thisYearHeader = DateTime.Today.Year + "整理日期";
    // "region", "wyeth_POID", "created_time"
    targetDT.Columns["region"].ColumnName = "DC";
    targetDT.Columns["wyeth_POID"].ColumnName = "订单号";
    targetDT.Columns["created_time"].ColumnName = thisYearHeader;

    foreach(DataRow dr in targetDT.Rows){
        dr["DC"] = dr["DC"].ToString() + customer_name;
        dr[thisYearHeader] = DateTime.Parse(dr[thisYearHeader].ToString()).ToString("yyyy/MM/dd");
    }
}

/// <summary>
/// 整理Traker DataRow
/// </summary>
/// <param name="regionDR"></param>
/// <param name="dr"></param>
public void buildTrackerDR(ref DataRow orderExcelRow, DataRow dr){
    string region = dr["region"].ToString();
    orderExcelRow["订单号"] = dr["wyeth_POID"];
    orderExcelRow["货号2"] = orderExcelRow["货号"];
    orderExcelRow["罐数"] = orderExcelRow["已收货数量"];
    orderExcelRow["单价"] = orderExcelRow["单品开票价"];
    string 整理日期 = DateTime.Parse(dr["created_time"].ToString()).ToString("yyyy/MM/dd");
    orderExcelRow[region + "整理日期"] = 整理日期;
}

public void buildRegionDR(ref DataRow regionDR, DataRow dr){
    regionDR["DC"] = dr["region"].ToString() + "大润发";
    regionDR["POID"] = dr["wyeth_POID"];
    regionDR["订货日期"] = DateTime.Parse(dr["order_date"].ToString()).ToString("yyyy/MM/dd");
    regionDR["交货日期"] = DateTime.Parse(dr["must_arrived_by"].ToString()).ToString("yyyy/MM/dd");
    regionDR["货号"] = dr["product_code"];
    regionDR["QTY 罐数"] = dr["received_qty"];
    regionDR["大润发实际总金额"] =dr["product_total_sales"];
    DataRow wyethSKUMappingRow = getWyethMappingRow(dr["product_code"].ToString());
    
    decimal 惠氏单价 = 0m;
    string 惠氏编码 = string.Empty;
    string 惠氏产品名称 = string.Empty;
    string 仓别 = string.Empty;
    string 扣点 = string.Empty;
    decimal 惠氏箱价 = 0m;
    string dcNo = dr["dc_no"].ToString();
    getShipTo(dcNo, ref 扣点); // 给 shipTo 赋值，给扣点赋值
    decimal 扣点值 = fetchRateInDecimal(扣点);
    if(wyethSKUMappingRow != null){
        惠氏单价 = toDecimalConvert(wyethSKUMappingRow["wyeth_unit_price"]);
        惠氏编码 = wyethSKUMappingRow["wyeth_material_no"].ToString();
        惠氏产品名称 = wyethSKUMappingRow["customer_product_name"].ToString();
        惠氏箱价 =  toDecimalConvert(wyethSKUMappingRow["wyeth_nps"]);
    }
    regionDR["Nestle CODE"] = 惠氏编码;
    regionDR["SKU"] = 惠氏产品名称;
    regionDR["直供价"] = 惠氏单价;
    decimal 订货量 = toDecimalConvert(dr["received_qty"]);
    decimal 惠氏总价 = Math.Round(订货量 * 惠氏单价, 2);
    regionDR["直供总价"] = 惠氏总价;
    decimal 系统折扣价 = Math.Round(惠氏总价 * (1-扣点值), 2);
    regionDR["折扣价"] =系统折扣价;
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