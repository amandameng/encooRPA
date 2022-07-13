//代码执行入口，请勿修改或删除
public void Run()
{
    //在这里编写您的代码
    Dictionary<string, List<string>> 交货地点字典 = new Dictionary<string, List<string>>{};
    foreach(DataRow dr in shipToDT.Rows){
        string client_logistics_warehouse = dr["Customer_Logistics_Warehouse"].ToString();
        string request_delivery_date = dr["Request_Delivery_Date"].ToString(); //周 三/周 六
        string[] rddArr = request_delivery_date.Split(new string[]{"/"}, StringSplitOptions.RemoveEmptyEntries);
        
        List<string> dayStringList = new List<string> { };
        foreach(string day in rddArr)
        {
            string newDay = day.Replace(" ", "");
            dayStringList.Add(newDay);
        }
        交货地点字典[client_logistics_warehouse] = dayStringList;
    }
        
    交期不准订单数据表 = 增量订单数据表.Clone();
    
    string 地点名称列名 = 增量订单数据表.Columns.Contains("地点名称") ? "地点名称" : "Logistics_warehouse"; // 网页字段：地点名称， 数据库： Logistics_warehouse
    string 交货日期列名 = 增量订单数据表.Columns.Contains("交货日期") ? "交货日期" : "order_expire_date"; // 网页字段：交货日期， 数据库： request_delivery_date

    foreach(DataRow dr in 增量订单数据表.Rows){
        string 地点名称 = dr[地点名称列名].ToString();
        string 交货日期 = dr[交货日期列名].ToString();
       // Console.WriteLine("-----交货日期-----{0}", 交货日期);
        DateTime 交期 = DateTime.Parse(交货日期);
        string 周几 = CaculateWeekDay(交期);
        
        if(交货地点字典.ContainsKey(地点名称)){
            List<string> dayStringList = 交货地点字典[地点名称];
            // 交货日期不在定义的范围内时，标记为异常订单
            if(!dayStringList.Contains(周几)){
                交期不准订单数据表.ImportRow(dr);
            }
        }else{
            Console.WriteLine($"地点名称需要维护： {地点名称}");
        }
    } 
}
//在这里编写您的函数或者类

public string CaculateWeekDay(DateTime dtNow)
{
    var weekdays = new string[] { "周日", "周一", "周二", "周三", "周四", "周五", "周六" };
    return weekdays[(int)dtNow.DayOfWeek];
}