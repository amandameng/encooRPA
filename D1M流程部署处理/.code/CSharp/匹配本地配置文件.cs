//代码执行入口，请勿修改或删除
public void Run()
{
    FileInfo[] allFiles = new FileInfo[]{};
    DirectoryInfo di = new DirectoryInfo(流程配置目录);
    allFiles = di.GetFiles("*.xls*", SearchOption.TopDirectoryOnly);
    Console.WriteLine(流程配置目录);
    
    Console.WriteLine("remote流程配置路径: {0}， allFiles：{1}", remote流程配置路径, allFiles.Length);
    // string rowConfigFileName = Path.GetFileNameWithoutExtension(remote流程配置路径); // 一直报非法路径错误
    foreach(var configFile in allFiles)
    {
        string curFileNameWithoutExtension = Path.GetFileName(configFile.FullName);         
        if(remote流程配置路径.EndsWith(curFileNameWithoutExtension)){
            本地配置文件 = configFile.FullName;
            break;
        }
    }
    Console.WriteLine(本地配置文件);
}
//在这里编写您的函数或者类