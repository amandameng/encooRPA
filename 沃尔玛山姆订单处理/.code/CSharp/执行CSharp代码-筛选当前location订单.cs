//代码执行入口，请勿修改或删除
public void Run()
{
    //在这里编写您的代码
    List<string> locationList = curShipToDT.Rows.Cast<DataRow>().Select<DataRow, string>(dr => dr["Nestle_Plant_No"].ToString()).ToList();
    curOrdersDT = allOrdersDT.Clone();
  //  int i = 0;
    foreach(DataRow dr in allOrdersDT.Rows){
        string location = dr["Location"].ToString().Trim();
        string receivedDateStr = dr["Received Date"].ToString();
        // Console.WriteLine(" --{0}- ", dr["Document Number"].ToString());
        bool 时间有效 = timeValid(receivedDateStr);
        
        // 只取当前沃尔玛8大仓订单或者 不属于WM，SAM，SAM-Water的订单
        if((locationList.Contains(location) || !WMLocationsList.Contains(location)) && 时间有效){
            curOrdersDT.ImportRow(dr);
        }
       // i++;
    }
    
}
//在这里编写您的函数或者类'
public DateTime convertToLocalTime(DateTime sourceCSTdtime)
{
    
    TimeZoneInfo cstTimeZone = TimeZoneInfo.FindSystemTimeZoneById("Central Standard Time");
    DateTime dtime = TimeZoneInfo.ConvertTime(sourceCSTdtime, cstTimeZone, TimeZoneInfo.Local);
    return dtime;
}

public bool timeValid(string receivedDateStr)
{
    DateTime receivedDateTime = Convert.ToDateTime(receivedDateStr);
    // 经调查，导出文件的的订单timestamp跟网站显示的日期有时间差。导出文件里的订单时间 + （13后入式夏令时， 14hours其他） = 网站展示订单时间
    DateTime onSiteDateTime = convertToLocalTime(receivedDateTime); // 2022-1-26 3:04:47  => 2022-1-26 17:04:47
    DateTime receivedDate = Convert.ToDateTime(onSiteDateTime.ToString("yyyy-MM-dd"));
    DateTime 结束日期date = Convert.ToDateTime(结束日期);
    DateTime 开始日期date = Convert.ToDateTime(开始日期);        
    // onSiteDateTime <= 结束日期date
    
    // Console.WriteLine("receivedDate: {0}, onSiteDateTime: {1}, 结束日期date:{2}", receivedDate, onSiteDateTime, 结束日期date);
    
    bool 时间有效 = DateTime.Compare(receivedDate, 结束日期date) <= 0 && DateTime.Compare(receivedDate, 开始日期date) >= 0;
    return 时间有效;
}