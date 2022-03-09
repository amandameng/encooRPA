//代码执行入口，请勿修改或删除
public void Run()
{
    if(rpaAccountsDT.Rows.Count == 0){
        errorMessageList.Add("家乐福渠道缺少账号信息，请通过<b>云扩低代平台添加账号密码<b/>等信息");
    }
    if(etoConfigDT.Rows.Count == 0){
        errorMessageList.Add("家乐福渠道缺少平台配置表，请通过<b>云扩低代平台添加账号密码<b/>等信息");
    }
    if(shipToDT.Rows.Count == 0){
        errorMessageList.Add("家乐福渠道没有SoldToShipTo配置表，请通过<b>云扩低代平台添加账号密码<b/>等信息");
    }
    if(errorMessageList.Count > 0){
        return;
    }
    int daysCount = Convert.ToInt32(etoConfigDT.Rows[0]["daysCount"].ToString());
    DateTime 开始日期_日期 = DateTime.Parse(结束日期).AddDays(-(daysCount == 0 ? 7 : daysCount));
    if(maxCreatedTimeDT!=null && maxCreatedTimeDT.Rows.Count > 0){ // 订单最近一次获取时间跟默认设定的时间比较，取两者之间较小的值
        DateTime maxCreatedTime = Convert.ToDateTime(maxCreatedTimeDT.Rows[0]["max_created_time"].ToString()).AddDays(-1);
        if(DateTime.Compare(maxCreatedTime, 开始日期_日期) < 0){
            开始日期_日期 = maxCreatedTime;
        }
    }
    开始日期 = 开始日期_日期.ToString("yyyy-MM-dd");
    账号 = rpaAccountsDT.Rows[0]["User Name"].ToString();
    密码 = rpaAccountsDT.Rows[0]["Password"].ToString();
    网站 = rpaAccountsDT.Rows[0]["Customer Login URL"].ToString();
    flowAlertReceiverEmail = rpaAccountsDT.Rows[0]["Flow Alert Receiver Email Address"].ToString();
    
    etoReceiver = checkMail(mailSettingDT, "Excel To Order", 客户平台);
    cleanOrderReceiver = checkMail(mailSettingDT, "Clean Order", 客户平台);
    exceptionOrderReceiver = checkMail(mailSettingDT, "Exception Order", 客户平台);
}
//在这里编写您的函数或者类


public string checkMail(DataTable mailSettingDT, string orderCategory, string customer_Name){
    // Exception Order
    DataRow[] resultOrderRows= mailSettingDT.Select(String.Format("Order_Category = '{0}' and Customer_Name='{1}'", orderCategory, customer_Name));
    string orderReportReceiver = String.Empty;
    if(resultOrderRows.Length > 0){
        orderReportReceiver = resultOrderRows[0]["Mail_Receipt_Address"].ToString(); // 只获取第一个地址
        string[] exceptionOReceiptAddressArr = orderReportReceiver.Split(new string[]{"/"}, StringSplitOptions.RemoveEmptyEntries);
        if(exceptionOReceiptAddressArr.Length == 0){
            errorMessageList.Add(string.Format("({0}) 报表收件人不合法, 多个邮箱需要用'/'分隔。<br/>请在低代平台Mail Setting模块维护此信息", orderCategory));
        }else{
            orderReportReceiver = String.Join(";", exceptionOReceiptAddressArr);  // Exception Order Mail receiver
        }
        
    }else if(resultOrderRows.Length == 0){
        errorMessageList.Add(string.Format("({0}) 报表收件人不存在!  请在低代平台Mail Setting模块维护此信息", orderCategory));
    }
    return orderReportReceiver;
}