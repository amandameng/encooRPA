//代码执行入口，请勿修改或删除
// public string[] orderCategoryArr = new string[]{"DMS_Tracker", "skuException", "RDDException", "无新增订单", "产品数量或金额异常", "价差异常", "门店订单", "重复订单", "客户percent异常"};
public string[] orderCategoryArr = new string[]{"DMS_Tracker", "Exception", "无新增订单"};

public void Run()
{
    //在这里编写您的代码
    DataTable dt_ModuleSetting = new DataTable();
        
    dt_ModuleSetting.Columns.Add("订单明细文件路径", typeof(string));
    dt_ModuleSetting.Columns.Add("用户名", typeof(string));
    dt_ModuleSetting.Columns.Add("密码", typeof(string));
    dt_ModuleSetting.Columns.Add("登录网址", typeof(string));
    dt_ModuleSetting.Columns.Add("流程异常接收邮件", typeof(string));
    dt_ModuleSetting.Columns.Add("订单采集批次", typeof(string));
    dt_ModuleSetting.Columns.Add("当前Listing文件", typeof(string));
    dt_ModuleSetting.Columns.Add("isWM", typeof(bool));
    dt_ModuleSetting.Columns.Add("isSam", typeof(bool));
    dt_ModuleSetting.Columns.Add("流程初始数据导入", typeof(bool));
    dt_ModuleSetting.Columns.Add("pdfFolder", typeof(string));
    dt_ModuleSetting.Columns.Add("折扣率", typeof(string));
    dt_ModuleSetting.Columns.Add("orderFecthingRecordsDT", typeof(object));

    dtRow_ModuleSettings = dt_ModuleSetting.NewRow(); // 外部定义
    dtRow_ModuleSettings["isWM"] = customer_name == "沃尔玛";
    dtRow_ModuleSettings["isSam"] = customer_name == "山姆";
    dtRow_ModuleSettings["折扣率"] = (customer_name == "沃尔玛" ? "1.6%" : "0.8%");
    // 当第一次运行时需要设置为 True，确保导入第一批初始数据
    dtRow_ModuleSettings["流程初始数据导入"] = false;  // 用于流程第一次运行时，能够导入对应客户的初始历史数据，而不至于第一次运行就下载所有订单。此参数应该搭配开始时间（设置到listing历史订单之前，确保所有历史订单不用再做处理）

    initExceptionFileName(ref dt_ModuleSetting);
    initRequiredDT(ref dt_ModuleSetting);

    setAccount();
    // 邮箱数据检查
    checkMailSetting(ref dt_ModuleSetting);
    
    // 设定订单抓取批次
    // 流程执行后需要更新订单采集批次
    if(orderFecthingRecordsDT.Rows.Count == 0){
        dtRow_ModuleSettings["订单采集批次"] = 1;
    }else{
        int times = Convert.ToInt32(orderFecthingRecordsDT.Rows[0]["times"]);
        dtRow_ModuleSettings["订单采集批次"] = times + 1;
    }
    dtRow_ModuleSettings["orderFecthingRecordsDT"] = orderFecthingRecordsDT;
    string monthStr = DateTime.Now.ToString("yyyy-MM");
    string todayDateStr = DateTime.Now.ToString("yyyy-MM-dd");
    string todayDateStrNoDash = DateTime.Now.ToString("yyyyMMdd");
    string 订单批次 = dtRow_ModuleSettings["订单采集批次"].ToString();
    string 项目目录 = dtRow_ProjectSettings["项目目录"].ToString();
    string 平台目录 = Path.Combine(项目目录, customer_name);
    string 当天订单文件夹 = Path.Combine(平台目录, monthStr, todayDateStr);
    dtRow_ModuleSettings["当前Listing文件"] = Path.Combine(tempFolder, String.Format("listing_{0}_{1}.xls", todayDateStr, 订单批次));
    dtRow_ModuleSettings["订单明细文件路径"] = Path.Combine(当天订单文件夹, string.Format("{0}_Tracker_{1}_{2}.xlsx", customer_name, todayDateStr, 订单批次));

    dtRow_ModuleSettings["pdfFolder"] =  Path.Combine(当天订单文件夹, string.Format("pdf_{0}_{1}", todayDateStr, 订单批次));
    initOrderExcelRow(todayDateStr, todayDateStrNoDash, 订单批次);
    initOrderDate();
    
    
}
//在这里编写您的函数或者类
public void initExceptionFileName(ref DataTable dt_ModuleSetting)
{
    
    DataTable orderCatResultDT = initOrderCatResultDT();
    
    foreach(string orderCat in orderCategoryArr){
        dt_ModuleSetting.Columns.Add(orderCat + "文件路径", typeof(string));
        // dtProjectSetting.Columns.Add(orderCat + "订单PDF", typeof(string)); // TODO, 因为订单PDF不再按照异常类型单独汇总，所以这边暂时没有用
    }
    dt_ModuleSetting.Columns.Add("orderCatResultDT", typeof(object));
    
    dtRow_ModuleSettings["orderCatResultDT"] = orderCatResultDT; // 存放异常订单结果数据表
}

public void initRequiredDT(ref DataTable dt_ModuleSetting)
{
    dt_ModuleSetting.Columns.Add("rpaAccountsDT", typeof(object)); // rpaAccountDT
    dt_ModuleSetting.Columns.Add("mailSettingDT", typeof(object)); // mailSetting
    dt_ModuleSetting.Columns.Add("materialMasterDataDT", typeof(object)); // materialMasterDataDT
    dt_ModuleSetting.Columns.Add("soldToShipToDT", typeof(object)); // shipToSoldToDT
    dt_ModuleSetting.Columns.Add("constraintListDT", typeof(object)); // constraintListDT
    dt_ModuleSetting.Columns.Add("specialListDT", typeof(object)); // specialListDT
    dt_ModuleSetting.Columns.Add("allWMSoldToShipToDT", typeof(object)); // specialListDT
    dt_ModuleSetting.Columns.Add("customerPreferenceDT", typeof(object)); // customerPreferenceDT

    dtRow_ModuleSettings["rpaAccountsDT"] = rpaAccountsDT;
    dtRow_ModuleSettings["mailSettingDT"] = mailSettingDT;
    dtRow_ModuleSettings["materialMasterDataDT"] = materialMasterDataDT;
    dtRow_ModuleSettings["soldToShipToDT"] = soldToShipToDT;
    dtRow_ModuleSettings["constraintListDT"] = constraintListDT;
    dtRow_ModuleSettings["specialListDT"] = specialListDT;
    dtRow_ModuleSettings["allWMSoldToShipToDT"] = allWMSoldToShipToDT;
    dtRow_ModuleSettings["customerPreferenceDT"] = customerPreferenceDT;
}

public DataTable initOrderCatResultDT(){
    DataTable orderCatResultDT = new DataTable();
    orderCatResultDT.Columns.Add("orderCat", typeof(string));
    orderCatResultDT.Columns.Add("orderCatExcelFileName", typeof(string));
    orderCatResultDT.Columns.Add("orderCatPDFFileName", typeof(string));
    orderCatResultDT.Columns.Add("mailSubject", typeof(string));

    orderCatResultDT.Columns.Add("orderCatPrintURL", typeof(string));
    orderCatResultDT.Columns.Add("orderRelatedDT", typeof(DataTable));
    
    //     orderCategoryArr = new string[]{"DMS_Tracker", "skuException", "RDDException", "无新增订单", "产品数量或金额异常", "价差异常", "门店订单", "重复订单", "客户percent异常"};

    orderCatResultDT.Rows.Add(new Object[]{ "DMS_Tracker",  "Clean_Order", "", "Order to DMS"});
    orderCatResultDT.Rows.Add(new Object[]{ "Exception",  "Exception_Order", "-Exception", "Exception Order"});
    /*orderCatResultDT.Rows.Add(new Object[]{ "RDDException",  "RDD_Exception", "-RDDException", "RDD Exception订单"});
    orderCatResultDT.Rows.Add(new Object[]{ "产品数量或金额异常",  "产品数量金额异常_Exception", "-数量金额Exception", "产品行数量或金额为0"});
    orderCatResultDT.Rows.Add(new Object[]{ "价差异常",  "价差异常_Exception", "-价差异常Exception", "价差异常"});
    orderCatResultDT.Rows.Add(new Object[]{ "门店订单",  "门店订单_Exception", "-门店订单Exception", "门店订单"});
    orderCatResultDT.Rows.Add(new Object[]{ "重复订单",  "重复订单_Exception", "-重复订单Exception", "重复订单"});
    orderCatResultDT.Rows.Add(new Object[]{ "客户percent异常",  "客户percent_Exception", "-客户percentException", "客户percent异常"});
    */
    return orderCatResultDT;
}

public void setAccount(){
    if(rpaAccountsDT == null || rpaAccountsDT.Rows.Count == 0){
        errorMessageList.Add(string.Format("{0}_{1}流程 账号未设置。<br/>请在低代平台Account Setting模块维护此信息", customer_name, flow_name));
        return;
    }
    DataRow rpaAccountDRow = rpaAccountsDT.Rows[0];
    dtRow_ModuleSettings["用户名"] = rpaAccountDRow["user_name"];
    dtRow_ModuleSettings["密码"] = rpaAccountDRow["password"];
    dtRow_ModuleSettings["登录网址"] = rpaAccountDRow["customer_login_url"];
    dtRow_ModuleSettings["流程异常接收邮件"] = string.IsNullOrEmpty(rpaAccountDRow["flow_alert_receiver_email_address"].ToString()) ? dtRow_ProjectSettings["dev_email"].ToString() : rpaAccountDRow["flow_alert_receiver_email_address"];
}

public void checkMailSetting(ref DataTable dt_ModuleSetting){
    if(mailSettingDT == null || mailSettingDT.Rows.Count == 0){
        errorMessageList.Add(string.Format("{0}_{1}流程 邮件接收人未设置。<br/>请在低代平台Mail Setting模块维护此信息", customer_name, flow_name));
        return;
    }

    foreach(string orderCat in orderCategoryArr){
        string 邮件接收人字段 = orderCat + "邮件接收人";
        string 邮件抄送人字段 = orderCat + "邮件抄送人";

        dt_ModuleSetting.Columns.Add(邮件接收人字段, typeof(string));
        dt_ModuleSetting.Columns.Add(邮件抄送人字段, typeof(string));
        
        string mailToAddress = "";
        string mailCcAddress = "";
    
        checkMail(mailSettingDT, orderCat, customer_name, ref mailToAddress, ref mailCcAddress);
        dtRow_ModuleSettings[邮件接收人字段] = mailToAddress;
        dtRow_ModuleSettings[邮件抄送人字段] = mailCcAddress;
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

public void initOrderExcelRow(string todayDateStr, string todayDateStrNoDash, string 订单批次){
    string 项目目录 = dtRow_ProjectSettings["项目目录"].ToString();
    string 平台目录 = Path.Combine(项目目录, customer_name);

    string monthStr = DateTime.Now.ToString("yyyy-MM");
    string excelPostfix = string.Format("{0}{1}_{2}.xlsx", customer_name, todayDateStr, 订单批次);
    // string pdfPrefix = string.Format("{0}{1}-{2}", todayDateStrNoDash, 订单批次);

    foreach(DataRow dr in ((DataTable)dtRow_ModuleSettings["orderCatResultDT"]).Rows){
        string orderCat = dr["orderCat"].ToString();
        dtRow_ModuleSettings[orderCat + "文件路径"] = Path.Combine(平台目录, monthStr, todayDateStr, string.Format("{0}_{1}", dr["orderCatExcelFileName"].ToString(), excelPostfix));
        // dtRow_ProjectSettings[orderCat + "订单PDF"] = Path.Combine(项目目录, monthStr, todayDateStr, string.Format("{0}{1}.pdf", pdfPrefix, dr["orderCatPDFFileName"].ToString()));
    }
}

public void initOrderDate(){
    DateTime orderEndDate = fetchvalidDate(结束日期);
    DateTime orderBeginDate;
    if(!string.IsNullOrEmpty(开始日期)){ // 订货开始日期 参数不为空，则使用参数
        orderBeginDate = fetchvalidDate(开始日期);
    }else{
        if(lastCapturedDateDT != null && lastCapturedDateDT.Rows.Count > 0){
            string maxCreatedTime = lastCapturedDateDT.Rows[0]["maxCreatedTime"].ToString();
            if(!string.IsNullOrEmpty(maxCreatedTime)){
                orderBeginDate = DateTime.Parse(maxCreatedTime);
            }else{
                orderBeginDate = getFirstDayOfMonth();
            }
        }else{
            orderBeginDate = getFirstDayOfMonth();
        }
    }

    if(DateTime.Parse(orderBeginDate.ToString("yyyy-MM-dd")) >= DateTime.Parse(orderEndDate.ToString("yyyy-MM-dd"))){
        orderBeginDate = orderEndDate.AddDays(-7);
    }
    dtRow_ProjectSettings["开始日期"] = orderBeginDate.ToString("yyyy-MM-dd");
    dtRow_ProjectSettings["结束日期"] = orderEndDate.ToString("yyyy-MM-dd");
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

public DateTime getFirstDayOfMonth(){
    DateTime startDate = DateTime.Now;
    var dayOfMonth = startDate.Day;
    DateTime orderBeginDate = startDate.AddDays(-(int)dayOfMonth + 1);
    return orderBeginDate;
}