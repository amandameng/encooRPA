//代码执行入口，请勿修改或删除
public void Run()
{
    //在这里编写您的代码    
    
    Console.WriteLine(DateTime.Parse("2021-11-06 00:00:00") <= DateTime.Parse("2021-11-01 15:45:45"));
    
    //IEnumerable<DataRow> rows = exceptionDT.AsEnumerable().Except(existingExceptionOrdersDT.AsEnumerable(), DataRowComparer.Default);
   // 增量异常订单数据表 = rows.CopyToDataTable();
}
//在这里编写您的函数或者类