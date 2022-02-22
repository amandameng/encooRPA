//代码执行入口，请勿修改或删除
public void Run()
{
    //在这里编写您的代码
    沃尔玛IC订单压缩文件 = zippedFilePath(订单附件列表, "沃尔玛IC");
    
}
//在这里编写您的函数或者类

public string zippedFilePath(List<string> 订单文件列表, string type){
    string firstFile = 订单文件列表[0];
    string dirName = Path.GetDirectoryName(firstFile);
    string dirParent = Path.GetDirectoryName(dirName);
    string zipName = type + "_" + Path.GetFileNameWithoutExtension(dirName) + ".zip";
    string 订单压缩文件 = Path.Combine(dirParent, zipName);
    Console.WriteLine(订单压缩文件);
    return 订单压缩文件;
}