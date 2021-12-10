//代码执行入口，请勿修改或删除
public void Run()
{
    //在这里编写您的代码
    List<string> locationList = curShipToDT.Rows.Cast<DataRow>().Select<DataRow, string>(dr => dr["Nestle_Plant_No"].ToString()).ToList();
    curOrdersDT = allOrdersDT.Clone();
    foreach(DataRow dr in allOrdersDT.Rows){
        string location = dr["location"].ToString();
        string receivedDateStr = dr["Received Date"].ToString();
        DateTime receivedDateTime = Convert.ToDateTime(receivedDateStr);
        // 经调查，导出文件的的订单timestamp跟网站显示的日期有时间差。导出文件里的订单时间 + 14hours = 网站展示订单时间
        DateTime onSiteDateTime = Convert.ToDateTime(receivedDateTime.AddHours(14).ToString("yyyy-MM-dd"));
        
        DateTime 结束日期date = Convert.ToDateTime(结束日期);
        DateTime 开始日期date = Convert.ToDateTime(开始日期);        
        // onSiteDateTime <= 结束日期date
        
        // Console.WriteLine("receivedDateStr: {0}, onSiteDateTime: {1}, 结束日期date:{2}, Document Number: {3}", receivedDateStr, onSiteDateTime, 结束日期date, dr["Document Number"].ToString());
        
        bool 时间有效 = DateTime.Compare(onSiteDateTime, 结束日期date) <= 0 && DateTime.Compare(onSiteDateTime, 开始日期date) >= 0;
        
        // 只取当前山姆的订单
        if(locationList.Contains(location) && 时间有效){
            curOrdersDT.ImportRow(dr);
        }
    }
}
//在这里编写您的函数或者类