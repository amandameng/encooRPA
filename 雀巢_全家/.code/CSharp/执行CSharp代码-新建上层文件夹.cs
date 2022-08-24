//代码执行入口，请勿修改或删除
public void Run()
{
    //在这里编写您的代码
    string parentDir = Path.GetDirectoryName(filePath);
    // 上层文件夹不存在，则新建
    if(!Directory.Exists(parentDir)){
        Directory.CreateDirectory(parentDir);
    }
}
//在这里编写您的函数或者类