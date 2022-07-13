//代码执行入口，请勿修改或删除
public void Run()
{
    //在这里编写您的代码
    
    List<string> headersFromFile = allDataDT.Columns.Cast<DataColumn>().Select<DataColumn, string>(dcol => dcol.ColumnName.ToString()).ToList();
    List<string> headersHasIssue = new List<string>{};
     foreach(string header in headersFromFile){
         Console.WriteLine("---{0}", header);
     }
    
    foreach(string header in defaultHeaders){
        Console.WriteLine(header);
        if(!headersFromFile.Contains(header)){
            Console.WriteLine("++++{0}", string.Join(",", headersFromFile));
            if(!headersHasIssue.Contains(header)) headersHasIssue.Add(header);
        }
    }
    if(headersHasIssue.Count > 0){
        throw(new Exception(string.Format("文件中缺少的必填列如下：{0}", string.Join(", ", headersHasIssue))));
    }
    
    sqlDic = new Dictionary<string, string>{{"当前最高版本号", string.Format("select max(ver) maxVersion from {0} where customer_name='{1}'", 数据库表名, 客户名称)}};
}
//在这里编写您的函数或者类