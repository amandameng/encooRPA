//代码执行入口，请勿修改或删除
public void Run()
{
    //在这里编写您的代码
    string curProjectPath = Environment.GetEnvironmentVariable("CurrentProjectSourcePath");
    string 指定数据模型子流程 = System.IO.Path.Combine(curProjectPath, "数据模型",  数据模型+".xaml");
    if(!System.IO.File.Exists(指定数据模型子流程)){
        throw(new Exception(string.Format("指定数据模型子流程不存在：{0}", 数据模型)));
    }
    isDev = dev == "dev";
    
    if(!isDev){   // 生产环境
        string tmpFolder = Path.Combine(curProjectPath, "Temp");
        if(!Directory.Exists(tmpFolder)){
            Directory.CreateDirectory(tmpFolder);
        }
        下载文件路径 = Path.Combine(tmpFolder, 数据模型 + "_"+ DateTime.Now.ToString("yyyy-MM-dd-HH-mm-ss") + ".xlsx");
    }else{ // 测试环境
        下载文件路径 = 待处理文件路径;
    }
    
    switch(数据模型){
        case "产品主数据":
            数据库表名 = "material_master_data";
            break;
        case "特殊产品":
            数据库表名 = "special_products";
            break;
    }
 
}
//在这里编写您的函数或者类