//代码执行入口，请勿修改或删除
public void Run()
{
    initGlobalVariable();
   
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

    dtProjectSetting.Columns.Add("项目目录", typeof(string));
    dtProjectSetting.Columns.Add("数据库连接", typeof(string));
    dtProjectSetting.Columns.Add("订单数据库表名", typeof(string));
    dtProjectSetting.Columns.Add("clean订单数据库表名", typeof(string));
    dtProjectSetting.Columns.Add("tracker数据库表名", typeof(string));
    dtProjectSetting.Columns.Add("dev_email", typeof(string));
    dtProjectSetting.Columns.Add("指定日期", typeof(string));
    dtProjectSetting.Columns.Add("发件箱配置", typeof(string));
    

    // 相关模板文件
    dtProjectSetting.Columns.Add("订单明细模板文件", typeof(string));
    dtProjectSetting.Columns.Add("预约信息表模板文件", typeof(string));

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
   
    dtRow_ProjectSettings["订单数据库表名"] = "walmart_orders";
    dtRow_ProjectSettings["clean订单数据库表名"] = "clean_order_tracker";
    dtRow_ProjectSettings["tracker数据库表名"] = "tracker";
    dtRow_ProjectSettings["dev_email"] = "mengfanling@encootech.com";
    dtRow_ProjectSettings["指定日期"] = fetchSpecificDay();
    dtRow_ProjectSettings["发件箱配置"] = 发件箱配置;
}

public string fetchSpecificDay(){
    DateTime curDate = DateTime.Now;
    if(!string.IsNullOrEmpty(指定日期)){ // 订货开始日期 参数不为空，则使用参数
        curDate = fetchvalidDate(指定日期);
    }
    return curDate.ToString("yyyy-MM-dd");
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