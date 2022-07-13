//代码执行入口，请勿修改或删除
public void Run()
{
    //在这里编写您的代码
    List<string> locationList = ((DataTable)dtRow_ModuleSettings["soldToShipToDT"]).Rows.Cast<DataRow>().Select<DataRow, string>(dr => dr["DC编号"].ToString()).ToList();

    curOrdersDT = allOrdersDT.Clone();
    // int i = 0;
    foreach(DataRow dr in allOrdersDT.Rows){
        string location = dr["Location"].ToString().Trim();
        string receivedDateStr = dr["Received Date"].ToString();
        // Console.WriteLine(" --{0}- ", dr["Document Number"].ToString());
        bool 时间有效 = timeValid(receivedDateStr);
        //Console.WriteLine(" -时间有效-{0}- location{1}", 时间有效, location);
        // 只取当前沃尔玛8大仓订单或者 不属于WM，SAM的订单（即门店单）
        // bool locationValid = islocationValid(locationList, WMLocationsList,location);
        bool locationValid = true;
        if(locationValid && 时间有效){
            curOrdersDT.ImportRow(dr);
        }
       // i++;
    }
    // Convert.ToInt32("sd");
}

//在这里编写您的函数或者类
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
    DateTime 结束日期date = Convert.ToDateTime(dtRow_ProjectSettings["结束日期"].ToString());
    DateTime 开始日期date = Convert.ToDateTime(dtRow_ProjectSettings["开始日期"].ToString());        
    // onSiteDateTime <= 结束日期date
    
    // Console.WriteLine("receivedDate: {0}, onSiteDateTime: {1}, 结束日期date:{2}，开始日期date：{3}", receivedDate, onSiteDateTime, 结束日期date, 开始日期date);
    // 转换为中国时区的日期跟开始结束日期比较
    bool 时间有效 = DateTime.Compare(receivedDate, 结束日期date) <= 0 && DateTime.Compare(receivedDate, 开始日期date) >= 0;
    return 时间有效;
}

public bool islocationValid(List<string> locationList, List<string> WMLocationsList, string location){
    if(((bool)dtRow_ModuleSettings["isWM"])){
        return (locationList.Contains(location) || !WMLocationsList.Contains(location));
    }else{
        return locationList.Contains(location);
    }
}