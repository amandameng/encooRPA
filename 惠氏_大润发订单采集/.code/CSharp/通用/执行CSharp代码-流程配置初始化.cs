//代码执行入口，请勿修改或删除
public void Run()
{
    initFilePath();  

    initGlobalVariable();
    checkValidDate(创单开始日期, "开始日期");
    checkValidDate(创单结束日期, "结束日期");
   
    // 初始化文件夹
}

public void checkValidDate(string 参数日期, string 日期类型){
     DateTime resultDate;

    if(!String.IsNullOrEmpty(参数日期)){
        bool isValid = DateTime.TryParse(参数日期, out resultDate);
        if(!isValid){
            throw(new Exception(string.Format("【{0}】格式不正确，请输入 yyyy-MM-dd格式的日期，比如：2021-10-14", 日期类型)));
        }
    }
}

// 手动上传文件的解压路径
public void initFilePath(){
    string curProjectPath = Environment.GetEnvironmentVariable("CurrentProjectSourcePath");
    tempFolder = Path.Combine(curProjectPath, "Temp");
    if(!Directory.Exists(tempFolder)){
        Directory.CreateDirectory(tempFolder);
    }
}
//代码执行入口，请勿修改或删除
public void initGlobalVariable()
{

    //在这里编写您的代码
    dtProjectSetting = new DataTable();

    dtProjectSetting.Columns.Add("数据库连接", typeof(string));
    dtProjectSetting.Columns.Add("开始日期", typeof(string));
    dtProjectSetting.Columns.Add("结束日期", typeof(string));
    dtProjectSetting.Columns.Add("项目目录", typeof(string));
    dtProjectSetting.Columns.Add("订单数据库表名", typeof(string));
    dtProjectSetting.Columns.Add("订单列表数据表名", typeof(string));  // 订单列表对应的数据库表名
    dtProjectSetting.Columns.Add("订单列表临时数据表名", typeof(string));  // 订单列表对应的临时数据库表名
    dtProjectSetting.Columns.Add("异常订单数据库表名", typeof(string));
    dtProjectSetting.Columns.Add("clean订单数据库表名", typeof(string));
    dtProjectSetting.Columns.Add("dev_email", typeof(string));
    dtProjectSetting.Columns.Add("发件箱配置", typeof(string));    
 
    // 文件上传订单HTML和Excel的zip文件  -- TODO
    dtProjectSetting.Columns.Add("上传文件解压缩路径", typeof(string));

    // 相关模板文件
    dtProjectSetting.Columns.Add("订单明细模板文件", typeof(string));
    dtProjectSetting.Columns.Add("DMS_Tracker模板文件", typeof(string));
    dtProjectSetting.Columns.Add("exception_order模板文件", typeof(string));
    dtProjectSetting.Columns.Add("创单开始日期", typeof(string));
    dtProjectSetting.Columns.Add("创单结束日期", typeof(string));
    dtProjectSetting.Columns.Add("删单临时表", typeof(string));
    dtProjectSetting.Columns.Add("待收订单iFrameSrc", typeof(string));
    dtProjectSetting.Columns.Add("已收订单iFRameSrc", typeof(string));
    dtProjectSetting.Columns.Add("删单检查天数", typeof(int));
    dtProjectSetting.Columns.Add("供应链网址", typeof(string));
    dtProjectSetting.Columns.Add("是否退单", typeof(bool));
    dtProjectSetting.Columns.Add("退货明细文件路径", typeof(string));
    dtProjectSetting.Columns.Add("延期表文件路径", typeof(string));

    dtProjectSetting.Columns.Add("退单邮件接收人", typeof(string));
    dtProjectSetting.Columns.Add("退单邮件抄送人", typeof(string));
    
    dtProjectSetting.Columns.Add("延期表邮件接收人", typeof(string));
    dtProjectSetting.Columns.Add("延期表邮件抄送人", typeof(string));

    dtRow_ProjectSettings = dtProjectSetting.NewRow();
    
    // 数据库连接 参数如果存在的话，，否则抛错
    if(!string.IsNullOrEmpty(数据库连接)){
       dtRow_ProjectSettings["数据库连接"] = 数据库连接;
    }else{
        throw(new Exception("数据库连接不存在，请设置值之后再运行"));
    }

    dtRow_ProjectSettings["项目目录"] = @"C:\RPA工作目录\惠氏_大润发";
    dtRow_ProjectSettings["订单明细模板文件"] = Path.Combine(dtRow_ProjectSettings["项目目录"].ToString(), @"模板文件\rtmart-tracker-template.xlsx") ;
    dtRow_ProjectSettings["DMS_Tracker模板文件"] = Path.Combine(dtRow_ProjectSettings["项目目录"].ToString(), @"模板文件\DMS_tracker_template.xlsx") ;
    dtRow_ProjectSettings["exception_order模板文件"] = Path.Combine(dtRow_ProjectSettings["项目目录"].ToString(), @"模板文件\exception_order_template.xlsx") ;

    dtRow_ProjectSettings["订单数据库表名"] = "rtmart_orders";
    dtRow_ProjectSettings["订单列表临时数据表名"] = "rtmart_exported_orders_tmp";
    dtRow_ProjectSettings["异常订单数据库表名"] = "exception_order";
    dtRow_ProjectSettings["clean订单数据库表名"] = "clean_order_tracker";
    dtRow_ProjectSettings["删单临时表"] = "rtmart_deleted_orders_tmp";

    dtRow_ProjectSettings["dev_email"] = "mengfanling@encootech.com";
    dtRow_ProjectSettings["发件箱配置"] = 发件箱配置;
    dtRow_ProjectSettings["待收订单iFrameSrc"] = "https://rtsupchain-portal.rt-mart.com/supchain/orders/planOrderMain*";
    dtRow_ProjectSettings["已收订单iFRameSrc"] = "https://rtsupchain-portal.rt-mart.com/supchain/orders/recptOrderMain*";
    dtRow_ProjectSettings["供应链网址"] = "https://rt-supplier.rt-mart.com/pages/portal/supply_chain";
    dtRow_ProjectSettings["删单检查天数"] = 20;
    dtRow_ProjectSettings["是否退单"] = 是否退单;
    dtRow_ProjectSettings["退货明细文件路径"] = Path.Combine(tempFolder, string.Format("{0}退货明细_{1}.xlsx", customer_name, DateTime.Today.ToString("yyyy-MM-dd")));
    dtRow_ProjectSettings["延期表文件路径"] = Path.Combine(tempFolder, string.Format("{0}延期表_{1}.xlsx", customer_name, DateTime.Today.ToString("yyyy-MM-dd")));
}

public void initOrderDate(){
    DateTime orderEndDate = fetchvalidDate(创单结束日期);
    DateTime orderBeginDate;
    if(!string.IsNullOrEmpty(创单开始日期)){ // 订货开始日期 参数不为空，则使用参数
        orderBeginDate = fetchvalidDate(创单开始日期);
    }else{
        if(lastCapturedDateDT != null && lastCapturedDateDT.Rows.Count > 0){
            string maxCreatedTime = lastCapturedDateDT.Rows[0]["maxCreatedTime"].ToString();
            orderBeginDate = DateTime.Parse(maxCreatedTime);
        }else{
            DateTime startDate = DateTime.Now;
            var dayOfMonth = startDate.Day;
            orderBeginDate = startDate.AddDays(-(int)dayOfMonth + 1);
        }
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