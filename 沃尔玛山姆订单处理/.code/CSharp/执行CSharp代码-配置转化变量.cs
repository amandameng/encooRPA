//代码执行入口，请勿修改或删除
public void Run()
{
    userName = rpaAccountsDT.Rows[0]["User Name"].ToString();
    userPassword = rpaAccountsDT.Rows[0]["Password"].ToString();
    loginUrl = rpaAccountsDT.Rows[0]["Customer Login URL"].ToString();
    flowAlertEmail = rpaAccountsDT.Rows[0]["Flow Alert Receiver Email Address"].ToString();
        
    if(etoConfigDT.Rows.Count == 0){
        errorMessageList.Add($"excel_to_order_config 表没有设置customer='{curCustomerName}'的数据");
        return;
    }
    int daysCount = Convert.ToInt32(etoConfigDT.Rows[0]["daysCount"].ToString());
    开始日期 = DateTime.Parse(结束日期).AddDays(-(daysCount == 0 ? 7 : daysCount)).ToString("yyyy-MM-dd");
    
    etoMailReceiver = checkMail(mailSettingDT, "Excel To Order", WM_Customer_name);
    cleanExceptionOrderMailReceiver = checkMail(mailSettingDT, "Clean And Exception Order", WM_Customer_name);

    // 只有山姆和沃尔玛需要分仓明细表, 山姆水和沃尔玛ICE CREAM 都不用
    //if(curCustomerName=="山姆" || curCustomerName=="沃尔玛"){
        分仓明细MailReceiver = checkMail(mailSettingDT, "分仓明细表", WM_Customer_name);
    // }
    
    DataRowCollection etoConfigDrs = etoConfigDT.Rows;
    if(etoConfigDrs.Count == 0){
        errorMessageList.Add($"excel_to_order_config 表没有设置customer='{curCustomerName}'的扣点数据，例：6%");
    }else{
        DataRow etoConfigDr = etoConfigDrs[0];
        string discount_rate = etoConfigDr["discount_rate"].ToString();
        if(!String.IsNullOrEmpty(discount_rate)){ // discount_rate 不为空
            Regex 百分数正则 = new Regex(@"^\d+\.?\d{0,2}%$");
            Match matchResult = 百分数正则.Match(discount_rate);
            string 百分比 = matchResult.Value;
            if(String.IsNullOrEmpty(discount_rate) || string.IsNullOrEmpty(百分比)){
                errorMessageList.Add($"excel_to_order_config 表customer='{curCustomerName}'的扣点数据不合规，例：6%");
            }
        }
        
    }
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