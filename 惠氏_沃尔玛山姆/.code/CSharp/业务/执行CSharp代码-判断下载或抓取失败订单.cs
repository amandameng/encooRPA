//代码执行入口，请勿修改或删除
public void Run()
{
    //在这里编写您的代码
    if(下载失败订单 != null || 抓取失败订单数据表 != null){
        
        异常订单邮件正文 = @"Dears all, <br/> 以下为抓取或者下载失败订单：<br/>";
        if(抓取失败订单数据表!=null && 抓取失败订单数据表.Rows.Count > 0){
            hasDownloadExceptionOrder = true;
            // 抓取失败订单数据表 = getUniqList(抓取失败订单数据表);
            List<string> 失败订单list = new List<string>{};
            foreach(DataRow dr in 抓取失败订单数据表.Rows){
                失败订单list.Add(string.Format("订单号：{0}，订单创建时间：{1}", dr["order_number"].ToString(), dr["received_date_time"].ToString()));
            }
            异常订单邮件正文 += string.Format("<b>抓取失败订单：</b><br/>{0} <br/>", string.Join("<br/>", 失败订单list));
        }
        if(下载失败订单!=null && 下载失败订单.Count > 0){
            hasDownloadExceptionOrder = true;
            下载失败订单 = getUniqList(下载失败订单);
            异常订单邮件正文 += string.Format("<br/><b>下载失败订单：</b><br/>{0} <br/>", string.Join("<br/>", 下载失败订单));
        }
        异常订单邮件标题 = string.Format("！！！【{0}】下载异常订单", curCustomerName);
        Console.WriteLine("异常订单邮件正文：{0}", 异常订单邮件正文);
    }
}
//在这里编写您的函数或者类

public List<string> getUniqList(List<string> 原始订单列表){
    int listCount = 原始订单列表.Count;
    List<string> resultList = new List<string>{};
    for(int i=listCount-1; i>=0; i--){
        string item = 原始订单列表[i].ToString();
        if(!resultList.Contains(item)){
            resultList.Add(Path.GetFileName(item)); // 只展示file name
        }
    }
    return resultList;
}