const string devEmail = "mengfanling@encootech.com";
//代码执行入口，请勿修改或删除
public void Run()
{
    
    //在这里编写您的代码
    setAccount();
    // 邮箱数据检查
    checkMailSetting();
    
    // 设定订单抓取批次
    // 流程执行后需要更新订单采集批次
    if(orderFecthingRecordsDT.Rows.Count == 0){
        dtRow_ProjectSettings["订单采集批次"] = 1;
    }else{
        int times = Convert.ToInt32(orderFecthingRecordsDT.Rows[0]["times"]);
        dtRow_ProjectSettings["订单采集批次"] = times + 1;
    }
    string monthStr = DateTime.Now.ToString("yyyy-MM");
    string todayDateStr = DateTime.Now.ToString("yyyy-MM-dd");
    string todayDateStrNoDash = DateTime.Now.ToString("yyyyMMdd");
    string 订单批次 = dtRow_ProjectSettings["订单采集批次"].ToString();
    string 项目目录 = dtRow_ProjectSettings["项目目录"].ToString();
    
    dtRow_ProjectSettings["订单明细文件路径"] = Path.Combine(项目目录, monthStr, todayDateStr, string.Format("Metro_Tracker_{0}_{1}.xlsx", todayDateStr, 订单批次));
    dtRow_ProjectSettings["发件箱配置"] = 发件箱配置;

    initOrderExcelAndPdfRow(todayDateStr, todayDateStrNoDash, 订单批次);
    initOrderDate();
}
//在这里编写您的函数或者类

public void setAccount(){
    if(rpaAccountsDT == null || rpaAccountsDT.Rows.Count == 0){
        errorMessageList.Add(string.Format("{0}_{1}流程 账号未设置。<br/>请在低代平台Account Setting模块维护此信息", customer_name, flow_name));
        return;
    }
    DataRow rpaAccountDRow = rpaAccountsDT.Rows[0];
    dtRow_ProjectSettings["用户名"] = rpaAccountDRow["user_name"];
    dtRow_ProjectSettings["密码"] = rpaAccountDRow["password"];
    dtRow_ProjectSettings["登录网址"] = rpaAccountDRow["customer_login_url"];
    dtRow_ProjectSettings["流程异常接收邮件"]  = string.IsNullOrEmpty(rpaAccountDRow["flow_alert_receiver_email_address"].ToString()) ? devEmail : rpaAccountDRow["flow_alert_receiver_email_address"];
}

public void checkMailSetting(){
    if(mailSettingDT == null || mailSettingDT.Rows.Count == 0){
        errorMessageList.Add(string.Format("{0}_{1}流程 邮件接收人未设置。<br/>请在低代平台Mail Setting模块维护此信息", customer_name, flow_name));
        return;
    }

    foreach(string orderCat in orderCategoryArr){
        string 邮件接收人字段 = orderCat + "邮件接收人";
        string 邮件抄送人字段 = orderCat + "邮件抄送人";

        dtProjectSetting.Columns.Add(邮件接收人字段, typeof(string));
        dtProjectSetting.Columns.Add(邮件抄送人字段, typeof(string));
        
        string mailToAddress = "";
        string mailCcAddress = "";
    
        checkMail(mailSettingDT, orderCat, customer_name, ref mailToAddress, ref mailCcAddress);
        dtRow_ProjectSettings[邮件接收人字段] = mailToAddress;
        dtRow_ProjectSettings[邮件抄送人字段] = mailCcAddress;
    }
}


public void checkMail(DataTable mailSettingDT, string orderCategory, string customerName, ref string mailToAddress, ref string mailCcAddress){
    // Exception Order
    DataRow[] resultOrderRows= mailSettingDT.Select(String.Format("order_category = '{0}' and customer_name='{1}'", orderCategory, customerName));
    if(resultOrderRows.Length > 0){
        mailToAddress = resultOrderRows[0]["mail_receipt_address"].ToString();
        mailCcAddress = resultOrderRows[0]["mail_cc_address"].ToString();
        string[] exceptionOReceiptAddressArr = mailToAddress.Split(new string[]{";"}, StringSplitOptions.RemoveEmptyEntries);
        if(exceptionOReceiptAddressArr.Length == 0){
            errorMessageList.Add(string.Format("({0}) 邮件收件人不合法, 多个邮箱需要用英文分号(;)分隔。<br/>请在低代平台Mail Setting模块维护此信息", orderCategory));
        }else{
            mailToAddress = String.Join(";", exceptionOReceiptAddressArr);  // Exception Order Mail receiver
        }
        
    }else if(resultOrderRows.Length == 0){
        errorMessageList.Add(string.Format("{0}_{1}【{2}】 邮件收件人不存在!  请在低代平台Mail Setting模块维护此信息", customer_name, flow_name, orderCategory));
    }
}

public void initOrderExcelAndPdfRow(string todayDateStr, string todayDateStrNoDash, string 订单批次){
    string 项目目录 = dtRow_ProjectSettings["项目目录"].ToString();
    string monthStr = DateTime.Now.ToString("yyyy-MM");
    string excelPostfix = string.Format("{0}_{1}.xlsx", todayDateStr, 订单批次);
    string pdfPrefix = string.Format("{0}Metro-{1}", todayDateStrNoDash, 订单批次);

    foreach(DataRow dr in orderCatResultDT.Rows){
        string orderCat = dr["orderCat"].ToString();
        dtRow_ProjectSettings[orderCat + "文件路径"] = Path.Combine(项目目录, monthStr, todayDateStr, string.Format("{0}_{1}", dr["orderCatExcelFileName"].ToString(), excelPostfix));
        dtRow_ProjectSettings[orderCat + "订单PDF"] = Path.Combine(项目目录, monthStr, todayDateStr, string.Format("{0}{1}.pdf", pdfPrefix, dr["orderCatPDFFileName"].ToString()));
    }
}

public void initOrderDate(){
    DateTime orderEndDate = fetchvalidDate(订货结束日期);
    DateTime orderBeginDate;
    if(!string.IsNullOrEmpty(订货开始日期)){ // 订货开始日期 参数不为空，则使用参数
        orderBeginDate = fetchvalidDate(订货开始日期);
    }else{
        if(lastCapturedDateDT != null && lastCapturedDateDT.Rows.Count > 0){
            string maxCreatedTime = lastCapturedDateDT.Rows[0]["maxCreatedTime"].ToString();
            orderBeginDate = DateTime.Parse(maxCreatedTime).AddDays(-3);
        }else{
            DateTime startDate = DateTime.Now;
            var dayOfMonth = startDate.Day;
            orderBeginDate = startDate.AddDays(-(int)dayOfMonth + 1);
        }
    }

    dtRow_ProjectSettings["订货开始日期"] = orderBeginDate.ToString("yyyy-MM-dd");
    dtRow_ProjectSettings["订货结束日期"] = orderEndDate.ToString("yyyy-MM-dd");
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