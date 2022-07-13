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
    initOrderDate();

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

public void initOrderDate(){
    DateTime orderEndDate = fetchvalidDate(订货结束日期);
    DateTime orderBeginDate;
    if(!string.IsNullOrEmpty(订货开始日期)){ // 订货开始日期 参数不为空，则使用参数
        orderBeginDate = fetchvalidDate(订货开始日期);
    }else{
        if(maxCreatedTimeDT!=null && maxCreatedTimeDT.Rows.Count > 0){
            string maxCreatedTime = maxCreatedTimeDT.Rows[0]["max_created_time"].ToString();
            orderBeginDate = DateTime.Parse(maxCreatedTime).AddDays(-1);
        }else{
            int daysCount = Convert.ToInt32(etoConfigDT.Rows[0]["daysCount"].ToString());
            orderBeginDate =orderEndDate.AddDays(-(daysCount == 0 ? 7 : daysCount));
        }
    }

    开始日期 = orderBeginDate.ToString("yyyy-MM-dd");
    结束日期 = orderEndDate.ToString("yyyy-MM-dd");
}

public DateTime fetchvalidDate(string dateStrParam){
     DateTime resultDate;
    
    if(String.IsNullOrEmpty(dateStrParam)){
        resultDate = DateTime.Now; // 参数不传，如果是开始时间，则今天时间 - 3, 否则是今天时间
    }else{       
        bool isValid = DateTime.TryParse(dateStrParam, out resultDate);
        if(!isValid){
            throw(new Exception("输入的日期格式不正确，请输入 yyyy-MM-dd格式的日期，比如：2022-04-11"));
        }
    }
    return resultDate;
}