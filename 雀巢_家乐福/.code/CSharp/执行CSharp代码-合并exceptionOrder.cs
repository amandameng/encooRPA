//代码执行入口，请勿修改或删除
public void Run()
{
    //在这里编写您的代码
    
  //  exceptionOrder模板数据表
  //  已取消异常模板数据表
    //rdd异常模板数据表
   //  交期不准联结查询数据表
   // 异常订单模板数据表
    
    exceptionResultDT= exceptionOrder模板数据表.Clone();
    if(已取消异常模板数据表!=null && 已取消异常模板数据表.Rows.Count > 0){
       exceptionResultDT.Merge(已取消异常模板数据表, true, MissingSchemaAction.Add); 
    }
    
    if(rdd异常模板数据表!=null && rdd异常模板数据表.Rows.Count > 0){
       exceptionResultDT.Merge(rdd异常模板数据表, true, MissingSchemaAction.Add); 
    }
        
    if(交期不准异常模板数据表!=null && 交期不准异常模板数据表.Rows.Count > 0){
       exceptionResultDT.Merge(交期不准异常模板数据表, true, MissingSchemaAction.Add); 
    }
            
    if(异常订单模板数据表!=null && 异常订单模板数据表.Rows.Count > 0){
       exceptionResultDT.Merge(异常订单模板数据表, true, MissingSchemaAction.Add); 
    }
    string[] colNames = exceptionResultDT.Columns.Cast<DataColumn>().Select<DataColumn, string>(dcol => dcol.ColumnName.ToString()).ToArray();
    exceptionResultDT = exceptionResultDT.DefaultView.ToTable(true, colNames);
}
//在这里编写您的函数或者类