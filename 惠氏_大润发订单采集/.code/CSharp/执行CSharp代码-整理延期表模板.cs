//代码执行入口，请勿修改或删除
public void Run()
{
    //在这里编写您的代码
    delayOrdersDT = initDelayOrdersDT();
    foreach(DataRow dr in todayAllOrdersDT.Rows){
        DataRow newDR = delayOrdersDT.NewRow();
        newDR["DC"] = dr["region"].ToString() + dr["customer_name"].ToString();
        newDR["POID"] = dr["wyeth_POID"];
        newDR["订货日期"] = DateTime.Parse(dr["order_date"].ToString()).ToString("yyyy-MM-dd");
        newDR["交货日期"] = DateTime.Parse(dr["must_arrived_by"].ToString()).ToString("yyyy-MM-dd");
        int rddGapDays = DiffDays(DateTime.Parse(dr["readDate"].ToString()), DateTime.Parse(dr["must_arrived_by"].ToString()));
        newDR["备注"] = rddGapDays >= 3 ? "" : "RDD < 3";
        newDR["读单日期"] = dr["readDate"];
        delayOrdersDT.Rows.Add(newDR);
    }
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
