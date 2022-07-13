//代码执行入口，请勿修改或删除
public void Run()
{
    //在这里编写您的代码
    List<string> 采购单号list = new List<string>{};
    foreach(DataRow dr in srcDataTable.Rows){
        采购单号list.Add(dr[srcCol].ToString());
    }
    string[] 采购单号数组 = 采购单号list.ToArray();

    List<string> 数据库采购单号list = new List<string>{};
    string[] 数据库采购单号数组 = new string[]{};

    if(targetDT!=null){
        foreach(DataRow dr in targetDT.Rows){
        数据库采购单号list.Add(dr[targetOrderCol].ToString());
        }
        数据库采购单号数组 = 数据库采购单号list.ToArray(); 
     }

    string[] 新增订单 = 采购单号数组.Except(数据库采购单号数组).ToArray();// 去除 srcDataTable 中 targetDT中的数据
    增量订单数据表 = srcDataTable.Clone();
    
    Console.WriteLine(新增订单.Length);
    if(新增订单.Length > 0){
        DataRow[] 新增订单行 = srcDataTable.Select(String.Format("`{0}` in ({1})", srcCol, String.Join(",", 新增订单) ));
        foreach(DataRow dr in 新增订单行){
            增量订单数据表.Rows.Add(dr.ItemArray);
        }
    }
}
//在这里编写您的函数或者类


