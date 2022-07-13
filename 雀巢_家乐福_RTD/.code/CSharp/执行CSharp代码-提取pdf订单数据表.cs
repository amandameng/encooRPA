//代码执行入口，请勿修改或删除
public void Run()
{
    //在这里编写您的代码
    来自pdf订单数据表 = 采购单数据表.Clone();
    if(!string.IsNullOrEmpty(console压缩文件路径) && 订单文件检查价差){
        foreach(Dictionary<string,string> item in 来自pdf采购单号类型list){
            string 订单号 = item["采购单号"].ToString();
             // 增量订单数据表 是读取的excel文件中的订单+详情数据
            DataRow[] dataRows = 采购单数据表.Select(String.Format("order_number='{0}'", 订单号));
    
            foreach(DataRow dr in dataRows){
                // dr["order_type"] = 采购单类型;
                来自pdf订单数据表.ImportRow(dr);
            }
            
        }
    }
    来自pdf订单数据表 = 来自pdf订单数据表.DefaultView.ToTable(true, new string[]{"order_number", "order_type", "order_create_time", "order_create_date", "order_expire_date", "logistics_warehouse"});
}
//在这里编写您的函数或者类