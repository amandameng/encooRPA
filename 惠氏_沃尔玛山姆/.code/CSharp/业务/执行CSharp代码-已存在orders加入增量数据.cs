//代码执行入口，请勿修改或删除
public void Run()
{
    //在这里编写您的代码
    DataTable 新增订单数据表 = existingOrdersDT.Clone();
    新增订单数据表.PrimaryKey = null;
    新增订单数据表.Columns.Remove("created_time");
    新增订单数据表.Columns.Remove("id");
    
    foreach(DataColumn dc in 新增订单数据表.Columns){
        dc.DataType = typeof(string);
    }

    DataRow existingOrderRow = existingRows[0];
    int colIdIndex = existingOrderRow.Table.Columns.IndexOf("id");
    int createdTimeIdIndex = existingOrderRow.Table.Columns.IndexOf("created_time");
    List<object> itemArrList = existingOrderRow.ItemArray.ToList();
    // 先remove 大索引，后remove小索引
    itemArrList.RemoveAt(createdTimeIdIndex);
    itemArrList.RemoveAt(colIdIndex);
    新增订单数据表.Rows.Add(itemArrList.ToArray());

    if(增量订单结果数据表==null){
        增量订单结果数据表 = 新增订单数据表;
    }else{
       增量订单结果数据表.Merge(新增订单数据表, true, MissingSchemaAction.Add); 
    }
}
//在这里编写您的函数或者类