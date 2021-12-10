//代码执行入口，请勿修改或删除
public void Run()
{
    //在这里编写您的代码
   DataRow excelToOrderDrow = mailSettingDT.Select("Order_Category= 'Excel_To_Order'")[0];
   DateTime today = DateTime.Now;
    
   if(excelToOrderDrow != null){
       string mailReceiptDate = excelToOrderDrow["Mail_Receipt_Date"].ToString();
       string mailReceiptTime = excelToOrderDrow["Mail_Receipt_Time"].ToString();
       string mailReceiptAddress = excelToOrderDrow["mailReceiptAddress"].ToString();
       
       bool dayMatch = isWeekdayTouched(mailReceiptDate, today);
       if(dayMatch){
          // bool timeMatch = isTimeTouched()
       }

       
       string[] mailReceiptTimeArr = mailReceiptTime.Split(new string[]{"/"}, StringSplitOptions.RemoveEmptyEntries);
       if(!String.IsNullOrEmpty(mailReceiptAddress)){
          string[] mailReceiptAddressArr = mailReceiptAddress.Split(new string[]{"/"}, StringSplitOptions.RemoveEmptyEntries);
          receipentMailAddress = String.Join(";", mailReceiptAddressArr);
       }

   }
}
//在这里编写您的函数或者类

public string CaculateWeekDay(DateTime dtNow)
{
    var weekdays = new string[] { "周日", "周一", "周二", "周三", "周四", "周五", "周六" };
    return weekdays[(int)dtNow.DayOfWeek];
}

public bool isWeekdayTouched(string mailReceiptDate, DateTime theDate){
    string[] mailReceiptDateArr = mailReceiptDate.Split(new string[]{"/"}, StringSplitOptions.RemoveEmptyEntries); // weekday
    List<string> dayStringList = new List<string> { };
    foreach(string day in mailReceiptDateArr)
    {
        string newDay = day.Replace(" ", "");
        dayStringList.Add(newDay);
    }
    string dayOfWeek = CaculateWeekDay(theDate);
    if(dayStringList.Contains(dayOfWeek)){
        return true;
    }
    return false; 
}

public bool isTimeTouched(string mailReceiptTime, DateTime theDate){
   
}