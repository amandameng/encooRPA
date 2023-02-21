//代码执行入口，请勿修改或删除
public void Run()
{
    //在这里编写您的代码
    foreach(DataRow dr in 上月至今分仓明细数据表.Rows){
        string 订单修改信息 = dr["订单修改信息"].ToString();
        string[] dataArr = 订单修改信息.Split(new string[]{";"}, StringSplitOptions.RemoveEmptyEntries);
        List<string> finalInfoList = new List<string>{};
        foreach(string item in dataArr){
            if(item.Contains("MABD") || item.Contains("修改产品数量") || item.Contains("VMI")){
                finalInfoList.Add(item);
            }
        }
        dr["订单修改信息"] = string.Join(";", finalInfoList);
    }
}
//在这里编写您的函数或者类