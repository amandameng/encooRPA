//代码执行入口，请勿修改或删除
public void Run()
{
    //在这里编写您的代码
    
    // 清空模板数据
    cleanOrderResultDT = 模板数据表.Clone();

    foreach(DataRow dr in cleanOrder联结查询数据表.Rows){
        DataRow cleanOrderRow = cleanOrderResultDT.NewRow();
        cleanOrderRow["渠道"] = "01 NKA";
        cleanOrderRow["读单当天日期"] = DateTime.Parse(dr["create_date"].ToString()).ToString("yyyy/MM/dd");
        cleanOrderRow["客户名称"] = GlobalVariable.VariableHelper.GetVariableValue("客户平台");
        cleanOrderRow["雀巢PO No"] = combinePONumber(dr);
        cleanOrderRow["客户系统PO No"] = dr["order_number"].ToString();

        cleanOrderRow["订单数量（单位CS)"] = dr["itemCount"]; // 这个订单里面的item汇总个数
        
        cleanOrderRow["Plant/区域"] = dr["nestle_plant_no"];
        // cleanOrderRow["交货地"] = dr["logistics_warehouse"]; //no need to apply
        
        cleanOrderRow["客户要求送货日"] = DateTime.Parse(dr["request_delivery_date"].ToString()).ToString("yyyy/MM/dd");
       
        cleanOrderResultDT.Rows.Add(cleanOrderRow);
    }
}
//在这里编写您的函数或者类

// 采购单号+订单类型缩写（越库-YK，昆山花桥1仓-HQ，存货-CH）订单中含有NPP产品需要再加上NPP，即为：采购单号+订单类型缩写+NPP
public string combinePONumber(DataRow dr){
    string poNumber = dr["order_number"].ToString();
    string 仓库地址 = dr["logistics_warehouse"].ToString(); // 对应订单里面的仓库地址
    string 仓库短码 = dr["order_type_short"].ToString(); // 对应 ship to sold to 表里面的字段
    string nppStr = dr["BU"].ToString(); // 客户-雀巢主产品表里面的字段
    string order_type = dr["order_type"].ToString();// order_type
    if(String.IsNullOrEmpty(仓库短码)){
       仓库短码 = "CH"; // 默认CH
    }
    if(order_type.Contains("越库")){
       仓库短码 = "YK"; 
    }
    return String.Format("{0}{1}{2}", poNumber, 仓库短码, nppStr.ToUpper() == "NPP" ? "NPP" : "");
}