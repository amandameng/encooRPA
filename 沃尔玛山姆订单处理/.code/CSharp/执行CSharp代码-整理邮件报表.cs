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
            //分仓明细表附件.Add(value);
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
    
    mergeEX2ODT();
    setDeafultValueForJobHistoryDT(allEx2oDT);
}

public string zippedFilePath(List<string> 订单文件列表, string type){
    string mainFolder = @"C:\RPA工作目录\雀巢_沃尔玛\导出文件\订单pdf\雀巢沃尔玛订单";
    string dateStr = DateTime.Now.ToString("yyyy-MM-dd");
    string dirParent = Path.Combine(mainFolder, dateStr);
    string timeNowStr = DateTime.Now.ToString("yyyy-MM-dd-HH-mm-ss");
    string zipName = type + "_" + timeNowStr + ".zip";
    string 订单压缩文件 = Path.Combine(dirParent, zipName);
    Console.WriteLine(订单压缩文件);
    return 订单压缩文件;
}

public void mergeEX2ODT(){
    mergeOneDT(ref allEx2oDT, "沃尔玛EX2ODT");
    mergeOneDT(ref allEx2oDT, "山姆EX2ODT");
    mergeOneDT(ref allEx2oDT, "山姆水EX2ODT");
}

public void mergeOneDT(ref DataTable allEx2oDT, string EX2ODTName){
   if(沃尔玛山姆报表字典.ContainsKey(EX2ODTName)){
        DataTable 沃尔玛EX2ODT = (DataTable)沃尔玛山姆报表字典[EX2ODTName];
        if(allEx2oDT == null){
            if(沃尔玛EX2ODT!=null && 沃尔玛EX2ODT.Rows.Count > 0){
               allEx2oDT = 沃尔玛EX2ODT;
            }
        }else{
            if(沃尔玛EX2ODT!=null && 沃尔玛EX2ODT.Rows.Count > 0){
               allEx2oDT.Merge(沃尔玛EX2ODT);
            }
        }
    }
}

public void setDeafultValueForJobHistoryDT(DataTable allEx2oDT){
    if(allEx2oDT!=null && allEx2oDT.Rows.Count > 0){
        orderJobHistoryDT = allEx2oDT.DefaultView.ToTable(true, new string[]{"Customer Order Number", "Customer Order Date", "customer_name"});
        orderJobHistoryDT.Columns.Add("report_type", typeof(string));
        orderJobHistoryDT.Columns.Add("email_sent", typeof(int));
        orderJobHistoryDT.Columns.Add("email_sent_time", typeof(string));
        foreach(DataRow dr in orderJobHistoryDT.Rows){
            dr["report_type"] = "EX2O";
            dr["email_sent"] = 1;
            dr["email_sent_time"] = DateTime.Now.ToString();
        }        
    }
}