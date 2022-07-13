//代码执行入口，请勿修改或删除
public void Run()
{
    //在这里编写您的代码
    if(string.IsNullOrEmpty(taskId)){
        foreach(DataRow dr in downloadCenterDT.Rows){
            string 操作 = dr[0].ToString();
            string 导出状态 = dr[2].ToString();
            DateTime 处理时间 = DateTime.Parse(dr[5].ToString()); // 5 处理时间
            TimeSpan ts = 处理时间 - taskTime;
            if(操作.Contains("更新状态") &&导出状态 == "待处理" && ts < new TimeSpan(0,0,5)){
                taskId = dr[1].ToString();
                break;
            }
        }
    }else{
        DataRow[] drs = downloadCenterDT.Select(string.Format("`Column-1`='{0}'", taskId));
        最终状态 = drs[0]["Column-2"].ToString();
    }

    Console.WriteLine("{0}, {1}",taskId, 最终状态);
}
//在这里编写您的函数或者类