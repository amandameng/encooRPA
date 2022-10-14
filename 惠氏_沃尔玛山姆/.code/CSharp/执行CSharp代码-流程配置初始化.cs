//代码执行入口，请勿修改或删除
public void Run()
{
    initGlobalVariable();
    checkValidDate(开始日期, "开始日期");
    checkValidDate(结束日期, "结束日期");
   
    List<string> allModules = new List<String>{"沃尔玛", "山姆"};
    string[] 沃尔玛流程模块数组 = 沃尔玛流程模块.Split(new string[]{"|"}, StringSplitOptions.RemoveEmptyEntries);
    待运行模块 = new List<String>{};
    foreach(string 模块 in 沃尔玛流程模块数组){
        if(!allModules.Contains(模块)){
            throw(new Exception(string.Format("已经实现的客户为：{0}，指定的模块未实现：{1}", string.Join("|", allModules), 模块)));
        }else{
            待运行模块.Add(模块);
        }
    }
    // 初始化文件夹
    initFilePath();    
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
}

//代码执行入口，请勿修改或删除
public void initGlobalVariable()
{

    //在这里编写您的代码
    dtProjectSetting = new DataTable();

    dtProjectSetting.Columns.Add("数据库连接", typeof(string));
    dtProjectSetting.Columns.Add("订单跳转URL", typeof(string));
    dtProjectSetting.Columns.Add("开始日期", typeof(string));
    dtProjectSetting.Columns.Add("结束日期", typeof(string));
    dtProjectSetting.Columns.Add("项目目录", typeof(string));
    dtProjectSetting.Columns.Add("订单数据库表名", typeof(string));
    dtProjectSetting.Columns.Add("订单列表数据表名", typeof(string));  // 订单列表对应的数据库表名
    dtProjectSetting.Columns.Add("订单列表临时数据表名", typeof(string));  // 订单列表对应的临时数据库表名
    dtProjectSetting.Columns.Add("异常订单数据库表名", typeof(string));
    dtProjectSetting.Columns.Add("clean订单数据库表名", typeof(string));
    dtProjectSetting.Columns.Add("env", typeof(string));
    dtProjectSetting.Columns.Add("dev_email", typeof(string));
    dtProjectSetting.Columns.Add("发件箱配置", typeof(string));
    dtProjectSetting.Columns.Add("流程警示邮件", typeof(string));

    // 文件上传订单HTML和Excel的zip文件  -- TODO
    dtProjectSetting.Columns.Add("上传文件解压缩路径", typeof(string));

    // 相关模板文件
    dtProjectSetting.Columns.Add("订单明细模板文件", typeof(string));
    dtProjectSetting.Columns.Add("DMS_Tracker模板文件", typeof(string));
    dtProjectSetting.Columns.Add("exception_order模板文件", typeof(string));

    dtProjectSetting.Columns.Add("执行记录数据表", typeof(object));
    dtRow_ProjectSettings = dtProjectSetting.NewRow();
    
    // 数据库连接 参数如果存在的话，直接赋值，否则抛错
    if(!string.IsNullOrEmpty(数据库连接)){
       dtRow_ProjectSettings["数据库连接"] = 数据库连接;
    }else{
        throw(new Exception("数据库连接不存在，请设置值之后再运行"));
    }

    //dtRow_ProjectSettings["订单跳转URL"] = "https://mdlvc.wumart.com/#index/cxpomana/metro/vcOrderMana";
    dtRow_ProjectSettings["项目目录"] = @"C:\RPA工作目录\惠氏_沃尔玛山姆";
    dtRow_ProjectSettings["订单明细模板文件"] = Path.Combine(dtRow_ProjectSettings["项目目录"].ToString(), @"模板文件\WM-Tracker-Template.xlsx") ;
    dtRow_ProjectSettings["DMS_Tracker模板文件"] = Path.Combine(dtRow_ProjectSettings["项目目录"].ToString(), @"模板文件\DMS_tracker_template.xlsx") ;
    dtRow_ProjectSettings["exception_order模板文件"] = Path.Combine(dtRow_ProjectSettings["项目目录"].ToString(), @"模板文件\exception_order_template.xlsx") ;

    dtRow_ProjectSettings["订单数据库表名"] = "walmart_orders";
    dtRow_ProjectSettings["订单列表数据表名"] = "walmart_exported_orders";
    dtRow_ProjectSettings["订单列表临时数据表名"] = "walmart_exported_orders_tmp";
    dtRow_ProjectSettings["异常订单数据库表名"] = "exception_order";
    dtRow_ProjectSettings["clean订单数据库表名"] = "clean_order_tracker";
    dtRow_ProjectSettings["env"] = env;
    dtRow_ProjectSettings["dev_email"] = "mengfanling@encootech.com";
    dtRow_ProjectSettings["发件箱配置"] = 发件箱配置;
    
    dtRow_ProjectSettings["执行记录数据表"] = initRecordDT();
}

public DataTable initRecordDT(){
    DataTable recordDT = new DataTable();
    recordDT.Columns.Add("module_name", typeof(string));
    recordDT.Columns.Add("执行结果", typeof(bool));
    recordDT.Columns.Add("错误消息", typeof(string));
    return recordDT;
}