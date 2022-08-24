//代码执行入口，请勿修改或删除
public void Run()
{
    //在这里编写您的代码
    keepTodayOrderInTrackers();
    delayOrdersDT = initDelayOrdersDT();
    
    foreach(DataRow dr in successTrakerDT.Rows){
        DataRow[] drs = todayAllOrdersDT.Select(string.Format("`wyeth_POID`='{0}'", dr["POID"].ToString()));
        if(drs.Length == 0){
            continue;
        }
        DataRow orderDR = drs[0];
        DataRow newDR = delayOrdersDT.NewRow();
        newDR["DC"] = orderDR["store_location"].ToString();
        newDR["POID"] = orderDR["wyeth_POID"];
        newDR["订货日期"] = DateTime.Parse(orderDR["order_date"].ToString()).ToString("yyyy-MM-dd");
        newDR["交货日期"] = DateTime.Parse(orderDR["must_arrived_by"].ToString()).ToString("yyyy-MM-dd");
        int rddGapDays = DiffDays(DateTime.Parse(orderDR["readDate"].ToString()), DateTime.Parse(orderDR["must_arrived_by"].ToString()));
        newDR["备注"] = rddGapDays >= 3 ? "" : "RDD < 3";
        newDR["读单日期"] = orderDR["readDate"];
        delayOrdersDT.Rows.Add(newDR);
    }
    delayOrdersDT = delayOrdersDT.DefaultView.ToTable(true, new string[]{"DC", "POID", "订货日期", "交货日期", "备注", "延期至", "读单日期"});
}
//在这里编写您的函数或者类

public DataTable initDelayOrdersDT(){
    DataTable resultDT = new DataTable();

    //Excel Header: DC	POID	订货日期	交货日期	备注	延期至	读单日期
   // DB Headers:  order_date, must_arrived_by, region, customer_name, wyeth_POID,  date_format(created_time, '%Y-%m-%d') as readDate
    resultDT.Columns.Add("DC", typeof(string));
    resultDT.Columns.Add("POID", typeof(string));
    resultDT.Columns.Add("订货日期", typeof(string));
    resultDT.Columns.Add("交货日期", typeof(string));
    resultDT.Columns.Add("备注", typeof(string));
    resultDT.Columns.Add("延期至", typeof(string));
    resultDT.Columns.Add("读单日期", typeof(string));
    return resultDT;
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


public void keepTodayOrderInTrackers(){
    for(int rowCount = successTrakerDT.Rows.Count-1; rowCount >=0; rowCount--){
        DataRow dr = successTrakerDT.Rows[rowCount];
        string orderCaptureDateStr = dr["order_capture_date"].ToString();
        DateTime orderCaptureDateUTC = DateTime.Parse(orderCaptureDateStr);
        DateTime orderCaptureDateLocal = convertToLocalTimeFromUTC(orderCaptureDateUTC);
        if(orderCaptureDateLocal.ToString("yyyy-MM-dd") != 延期表指定日期.ToString("yyyy-MM-dd")){
            successTrakerDT.Rows.Remove(dr);
        }
    }
}

public static DateTime convertToLocalTimeFromUTC(DateTime sourceUTCdtime)
{
    TimeZoneInfo cstTimeZone = TimeZoneInfo.FindSystemTimeZoneById("UTC");
    DateTime dtime = TimeZoneInfo.ConvertTime(sourceUTCdtime, cstTimeZone, TimeZoneInfo.Local);
    return dtime;
}