//代码执行入口，请勿修改或删除
public void Run()
{
    //在这里编写您的代码
    if(File.Exists(eto_file_path)){
        报表文件字典["山姆EX2OFile"] = eto_file_path;
    }
    if(File.Exists(clean_order_file_path)){
        报表文件字典["山姆CleanExceptionFile"] = eto_file_path;
    }
    if(File.Exists(分仓明细表文件)){
        报表文件字典["山姆分仓明细表"] = eto_file_path;
    }
    
}
//在这里编写您的函数或者类