//代码执行入口，请勿修改或删除
public void Run()
{
    //在这里编写您的代码
    下载失败订单 = new List<string>{};
    增量订单数据表 = new DataTable();
    增量订单数据表.Columns.Add("order_number", typeof(string));
    增量订单数据表.Columns.Add("location", typeof(string));
    增量订单数据表.Columns.Add("document_link", typeof(string));

    string order_numbers_str = string.Join(", ", 实际增量订单数据表.Rows.Cast<DataRow>().Select<DataRow, string>(dr => dr["order_number"].ToString()).ToList());
    existingOrderSql = string.Format("select * from {0} where order_number in ({1})", dtRow_ProjectSettings["订单数据库表名"].ToString(), order_numbers_str);
    noRepeatedOrdersDT = 实际增量订单数据表.DefaultView.ToTable(true, new string[]{"order_number"});
}
//在这里编写您的函数或者类