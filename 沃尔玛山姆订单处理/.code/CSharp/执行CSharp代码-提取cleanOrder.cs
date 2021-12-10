//代码执行入口，请勿修改或删除
public void Run()
{
    //在这里编写您的代码
    // List<string> 订单编号列表 = new List<string>{};
    if(String.IsNullOrEmpty(订单列名)){
        订单列名 = 目标订单数据表.Columns.Contains("采购单号") ? "采购单号" : "order_number"; // 数据库查出的是order_number, 网站下载的是采购单号
    }
     
    foreach(DataRow dr in 目标订单数据表.Rows){
        string 订单编号 = dr[订单列名].ToString();
        if(!订单编号列表.Contains(订单编号)){
            订单编号列表.Add(订单编号);
        }
    }

    string 订单编号字符串 = String.Join(",", 订单编号列表.ToArray());
        
    string rawSelectSql = @" SELECT ods.order_number, ods.order_type, ods.customer_name, 
                            round(sum(oli.confirm_qty/CAST(oli.package_size as signed)), 2) as itemCount, ods.create_date, ods.request_delivery_date, ods.logistics_warehouse, 
                             `stst`.`Nestle_Plant_No` as nestle_plant_no,`stst`.`Order_Type_Short` as order_type_short, npmd.`Nestle_BU` as BU
                            FROM orders ods 
                            join order_line_items oli on ods.order_number = oli.order_number
                            left join sold_to_ship_to stst on stst.`Customer_Logistics_Warehouse` = ods.logistics_warehouse and ods.customer_name = stst.`Customer_Name`
                            left join material_master_data npmd on npmd.`Customer_Material_No` = oli.product_code and npmd.`Customer_Name` = ods.customer_name and npmd.`Nestle_Plant_No`= `stst`.`Nestle_Plant_No`
                            where ods.customer_name='{0}' and ods.order_number in ({1})
                            group by ods.order_number
                            order by ods.id asc";
    string 客户平台 = Convert.ToString(GlobalVariable.VariableHelper.GetVariableValue("客户平台"));
    selectSql = String.Format(rawSelectSql, 客户平台, 订单编号字符串);
    Console.WriteLine(selectSql);
}
//在这里编写您的函数或者类