//代码执行入口，请勿修改或删除
public void Run()
{
    //在这里编写您的代码
    if(!validOrdersFromListingDT.Columns.Contains("localTime")){
      validOrdersFromListingDT.Columns.Add("localTime", typeof(string));
    }

    for(int i = validOrdersFromListingDT.Rows.Count-1; i>=0; i --){
        DataRow dr = validOrdersFromListingDT.Rows[i];
        string documentNumber = dr["Document Number"].ToString();
        string receivedDate = dr["Received Date"].ToString();
        DataRow[] drs = 实际增量订单数据表.Select(string.Format("`order_number` = '{0}' and `received_date_time` = '{1}'", documentNumber, receivedDate));
        if(drs.Length == 0){
            validOrdersFromListingDT.Rows.Remove(dr);
        }else{
            dr["localTime"] = convertToLocalTime(Convert.ToDateTime(receivedDate));
        }
    }
}

//在这里编写您的函数或者类
public DateTime convertToLocalTime(DateTime sourceCSTdtime)
{
    
    TimeZoneInfo cstTimeZone = TimeZoneInfo.FindSystemTimeZoneById("Central Standard Time");
    DateTime dtime = TimeZoneInfo.ConvertTime(sourceCSTdtime, cstTimeZone, TimeZoneInfo.Local);
    return dtime;
}