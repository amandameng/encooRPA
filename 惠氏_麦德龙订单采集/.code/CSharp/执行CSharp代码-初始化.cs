//代码执行入口，请勿修改或删除
public void Run()
{
    // 数据库连接 参数如果存在的话，直接赋值，否则抛错
    if(!string.IsNullOrEmpty(数据库连接)){
        sqlConn = 数据库连接;
    }else{
        throw(new Exception("数据库连接不存在，请设置值之后再运行"));
    }
        
    // orderCategoryArr = new string[]{"DMS_Tracker", "skuException", "退货单", "直流CPOException", "RDDException", "无新增订单", "产品数量或金额异常"};
    orderCategoryArr = new string[]{"DMS_Tracker", "Exception", "退货单", "无新增订单"};

    initOrderCatResultDT();
        
    //在这里编写您的代码
    dtProjectSetting = new DataTable();
    dtProjectSetting.Columns.Add("用户名", typeof(string));
    dtProjectSetting.Columns.Add("密码", typeof(string));
    dtProjectSetting.Columns.Add("登录网址", typeof(string));
    dtProjectSetting.Columns.Add("数据库连接", typeof(string));
    dtProjectSetting.Columns.Add("流程异常接收邮件", typeof(string));
    dtProjectSetting.Columns.Add("订单跳转URL", typeof(string));
    dtProjectSetting.Columns.Add("订货开始日期", typeof(string));
    dtProjectSetting.Columns.Add("订货结束日期", typeof(string));
    dtProjectSetting.Columns.Add("项目目录", typeof(string));
    dtProjectSetting.Columns.Add("订单采集批次", typeof(string));
    dtProjectSetting.Columns.Add("订单数据库表名", typeof(string));
    dtProjectSetting.Columns.Add("异常订单数据库表名", typeof(string));
    dtProjectSetting.Columns.Add("clean订单数据库表名", typeof(string));
    
    
    // 相关模板文件
    dtProjectSetting.Columns.Add("订单明细模板文件", typeof(string));
    dtProjectSetting.Columns.Add("DMS_Tracker模板文件", typeof(string));
    dtProjectSetting.Columns.Add("exception_order模板文件", typeof(string));
    
    // 订单Tracker
    // 异常订单Excel文件路径
    dtProjectSetting.Columns.Add("订单明细文件路径", typeof(string));
    
    foreach(string orderCat in orderCategoryArr){
        dtProjectSetting.Columns.Add(orderCat + "文件路径", typeof(string));
        dtProjectSetting.Columns.Add(orderCat + "订单PDF", typeof(string));
    }
    dtProjectSetting.Columns.Add("发件箱配置", typeof(string));
    
    

    dtRow_ProjectSettings = dtProjectSetting.NewRow();
    dtRow_ProjectSettings["数据库连接"] = sqlConn;
    dtRow_ProjectSettings["订单跳转URL"] = "https://mdlvc.wumart.com/#index/cxpomana/metro/vcOrderMana";
    dtRow_ProjectSettings["项目目录"] = @"C:\RPA工作目录\惠氏_麦德龙";
    dtRow_ProjectSettings["订单明细模板文件"] = Path.Combine(dtRow_ProjectSettings["项目目录"].ToString(), @"模板文件\Metro_Tracker_Template.xlsb.xlsx") ;
    dtRow_ProjectSettings["DMS_Tracker模板文件"] = Path.Combine(dtRow_ProjectSettings["项目目录"].ToString(), @"模板文件\DMS_tracker_template.xlsx") ;
    dtRow_ProjectSettings["exception_order模板文件"] = Path.Combine(dtRow_ProjectSettings["项目目录"].ToString(), @"模板文件\exception_order_template.xlsx") ;

    dtRow_ProjectSettings["订单数据库表名"] = "metro_orders";
    dtRow_ProjectSettings["异常订单数据库表名"] = "exception_order";
    dtRow_ProjectSettings["clean订单数据库表名"] = "clean_order_tracker";

    initRelatedDT();
}

public void initRelatedDT(){
    exceptionOrderDT = new DataTable();
    exceptionOrderDT.Columns.Add("order_number",  typeof(string));
    exceptionOrderDT.Columns.Add("exception_reason", typeof(string));

    /*
    orderNoPrintPoDT =  new DataTable();
    orderNoPrintPoDT.Columns.Add("order_number",  typeof(string));
    orderNoPrintPoDT.Columns.Add("print_po", typeof(string));
    */
}

public void initOrderCatResultDT(){
    orderCatResultDT = new DataTable();
    orderCatResultDT.Columns.Add("orderCat", typeof(string));
    orderCatResultDT.Columns.Add("orderCatExcelFileName", typeof(string));
    orderCatResultDT.Columns.Add("orderCatPDFFileName", typeof(string));
    orderCatResultDT.Columns.Add("mailSubject", typeof(string));

    orderCatResultDT.Columns.Add("orderCatPrintURL", typeof(string));
    orderCatResultDT.Columns.Add("orderRelatedDT", typeof(DataTable));
    
    orderCatResultDT.Rows.Add(new Object[]{ "DMS_Tracker",  "Clean_Order", "", "Order to DMS"});
    orderCatResultDT.Rows.Add(new Object[]{ "Exception",  "Exception", "-Exception", "Exception订单"});
    orderCatResultDT.Rows.Add(new Object[]{ "退货单",  "退货单", "-退货单", "退货单"});

}