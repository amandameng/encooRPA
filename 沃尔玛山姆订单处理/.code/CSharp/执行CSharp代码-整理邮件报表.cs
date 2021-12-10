//代码执行入口，请勿修改或删除
public void Run()
{
    foreach(var item in 沃尔玛山姆报表字典){
        string key = item.Key;
        string value = item.Value.ToString();
        if(key.Contains("EX2OFile")){
            ex2o附件.Add(value);
        }else if(key.Contains("CleanExceptionFile")){
            cleanException附件.Add(value);
        }else if(key.Contains("分仓明细表")){
            分仓明细表附件.Add(value);
        }
    }

    if(沃尔玛山姆报表字典.ContainsKey("沃尔玛订单pdf附件")){
        List<string> 沃尔玛订单pdf附件 = (List<string>) 沃尔玛山姆报表字典["沃尔玛订单pdf附件"];
        沃尔玛订单压缩文件 = zippedFilePath(沃尔玛订单pdf附件, "沃尔玛");
        订单pdf附件列表.Add(沃尔玛订单压缩文件);
    }
    
    if(沃尔玛山姆报表字典.ContainsKey("山姆订单pdf附件")){
        List<string> 山姆订单pdf附件 = (List<string>) 沃尔玛山姆报表字典["山姆订单pdf附件"];
        山姆订单压缩文件 = zippedFilePath(山姆订单pdf附件, "山姆");
        Console.WriteLine("山姆订单压缩文件: {0}", 山姆订单压缩文件);
        订单pdf附件列表.Add(山姆订单压缩文件);
    }
    
    if(沃尔玛山姆报表字典.ContainsKey("山姆水订单pdf附件")){
        List<string> 山姆水单pdf附件 = (List<string>) 沃尔玛山姆报表字典["山姆水订单pdf附件"];
        山姆水订单压缩文件 = zippedFilePath(山姆水单pdf附件, "山姆IB-Water");
        Console.WriteLine("山姆订单压缩文件: {0}", 山姆水订单压缩文件);
        订单pdf附件列表.Add(山姆水订单压缩文件);
    }
    
}

public string zippedFilePath(List<string> 订单文件列表, string type){
    string firstFile = 订单文件列表[0];
    string dirName = Path.GetDirectoryName(firstFile);
    string dirParent = Path.GetDirectoryName(dirName);
    string zipName = type + "_" + Path.GetFileNameWithoutExtension(dirName) + ".zip";
    string 订单压缩文件 = Path.Combine(dirParent, zipName);
    Console.WriteLine(订单压缩文件);
    return 订单压缩文件;
}