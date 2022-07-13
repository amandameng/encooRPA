//代码执行入口，请勿修改或删除
public void Run()
{
    //在这里编写您的代码
    抓取失败订单数据表 = 实际增量订单数据表.Clone();
    // 实际增量订单数据表 => 来自网站导出的订单列表
    foreach(DataRow dr in 实际增量订单数据表.Rows){
        string expOrderNumber = dr["order_number"].ToString();
        DateTime receivedDateTime = Convert.ToDateTime(dr["received_date_time"]);
        // 经调查，导出文件的的订单timestamp跟网站显示的日期有时间差。导出文件里的订单时间 + 夏令时+13，其他+14hours = 网站展示订单时间
        DateTime createdTimeFromFile = convertToLocalTime(receivedDateTime);
        string createdTimeFromFileTillMinutes = createdTimeFromFile.ToString("yyyy-MM-dd HH:mm"); // 精确到分钟，因为网页上显示的订单时间只到分钟
        // 增量订单结果数据表 => 来自实际保存成功的订单
        bool match = false;
        if(增量订单结果数据表!= null){
             match = findFailedOrder(增量订单结果数据表, expOrderNumber, createdTimeFromFileTillMinutes);
        }

        if(!match){
            match = findFailedOrder(existingOrdersDT, expOrderNumber, createdTimeFromFileTillMinutes);  // 从existing order中找
        }

        if(match == false){
            抓取失败订单数据表.ImportRow(dr);
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

public bool findFailedOrder(DataTable targetDT, string expOrderNumber, string createdTimeFromFileTillMinutes){
    bool match = false;
    foreach(DataRow resultDR in targetDT.Rows){
        string resultOrderNumber = resultDR["order_number"].ToString();
        // Console.WriteLine("---{0}", resultDR["create_date_time"]);
        DateTime resultCreatedTime = Convert.ToDateTime(resultDR["create_date_time"]);
        string resultCreatedTimeTillMinutes = resultCreatedTime.ToString("yyyy-MM-dd HH:mm"); // 精确到分钟，因为网页上显示的订单时间只到分钟
        // Console.WriteLine("{0}, {1}, {2}, {3}", expOrderNumber, resultOrderNumber, createdTimeFromFileTillMinutes, resultCreatedTimeTillMinutes);
        if(expOrderNumber == resultOrderNumber && createdTimeFromFileTillMinutes == resultCreatedTimeTillMinutes){
            match = true;
            break;
        }
    }
    return match;
}