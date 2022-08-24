//代码执行入口，请勿修改或删除
public void Run()
{
    string timeNowStr = DateTime.Now.ToString("yyyy-MM-dd");
    orderFolder = @"D:\全家\" + timeNowStr + @"\";
    orderFolderFromConsole = orderFolder + @"console\"; // 用来接收控制台下载的订单文件
    pdf订单文件 = Path.Combine(orderFolder, DateTime.Now.ToString("yyyyMMddHHmmss")+".pdf");
    downloadPath= Environment.GetFolderPath(Environment.SpecialFolder.UserProfile).ToString() + @"\Downloads\";
    if(账号信息表.Rows.Count > 0){
        userName = 账号信息表.Rows[0]["User Name"].ToString();
        password = 账号信息表.Rows[0]["Password"].ToString();
        webUrl = 账号信息表.Rows[0]["Customer Login URL"].ToString();
        flowAlertReceiverEmailAddress = 账号信息表.Rows[0]["Flow Alert Receiver Email Address"].ToString();
    }
    else{
        throw new Exception("账号信息获取失败");
    }

    //计算上一个工作日
    // 参数未设置的话
    
    if(maxCreatedTimeDT!=null && maxCreatedTimeDT.Rows.Count > 0){
        string maxCreatedTimeStr = Convert.ToString(maxCreatedTimeDT.Rows[0][0]);
        if(!string.IsNullOrEmpty(maxCreatedTimeStr)){
           lastWorkingDay = Convert.ToDateTime(maxCreatedTimeStr).AddDays(-1).ToString("yyyy/MM/dd");
        }
    }
    if(string.IsNullOrEmpty(lastWorkingDay)){
        DateTime now = DateTime.Now;
        if(now.DayOfWeek.ToString() == "Monday"){
            lastWorkingDay = now.AddDays(-3).ToString("yyyy/MM/dd");
        }
        else if(now.DayOfWeek.ToString() == "Sunday"){
            lastWorkingDay = now.AddDays(-2).ToString("yyyy/MM/dd");
        }
        else{
            lastWorkingDay = now.AddDays(-1).ToString("yyyy/MM/dd");
        }
    }
    if(!string.IsNullOrEmpty(订单结束日期)){ // 如果设置了结束时间，则选用此时间
        DateTime orderEndDate;
        DateTime.TryParse(订单结束日期, out orderEndDate);
        if(orderEndDate != null){
            orderEndDateStr = orderEndDate.ToString("yyyy/MM/dd");
        }else{
            throw new Exception("订单结束日期参数填写格式有误，正确的格式如2022/01/18");
        }
    }else{
       orderEndDateStr = DateTime.Now.AddDays(1).ToString("yyyy/MM/dd");  // MFL 于2022/2/10 x修改成默认结束日期为 day +1
    }
    
}
//在这里编写您的函数或者类
public void initFolder(){
    // orderFolder 不存在，则新建
    if(!Directory.Exists(orderFolder)){
        Directory.CreateDirectory(orderFolder);
    }
    // from控制台的文件夹，如果存在就删除重建
    if(Directory.Exists(orderFolderFromConsole)){
        Directory.Delete(orderFolderFromConsole, true);
    }
    Directory.CreateDirectory(orderFolderFromConsole);
}
