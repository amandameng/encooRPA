//代码执行入口，请勿修改或删除
public void Run()
{
    //在这里编写您的代码
    string[] addedColumns = new string[]{"提单日期", "门店/仓库编码（原）", "订单日期", "RDD", "DC", "Ship to", "Sold To", "PO number", "产品描述", "惠氏sku", "彩箱装", "紧缺", "箱数", "惠氏箱价", "总计"};
    foreach(string dCol in addedColumns){
        var col = new DataColumn(dCol, typeof(object));
        if(dCol == "提单日期"){
            col.DefaultValue = DateTime.Now.ToString("yyyy/MM/dd");
        }
        orderItemsIntoSheetDT.Columns.Add(col);
    }
}
 
//在这里编写您的函数或者类